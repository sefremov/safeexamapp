using SafeExamApp.Core.Exceptions;
using SafeExamApp.Core.Interfaces;
using SafeExamApp.Core.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace SafeExamApp.Core.Services {
    class SessionManager : ISessionWriter, ISessionReader {

        private const byte BeginSessionMarker = 0xD7;
        private const byte EndSessionMarker = 0xD8;
        private const byte AppRecordMarker = 0xA9;
        private const byte ScreenshotRecordMarker = 0xE4;
        private const byte PauseSessionMarker = 0x56;
        private const byte ResumeSessionMarker = 0x57;
        private const byte PulseMarker = 0x80;
        
        private byte[] hash;

        private string sessionDirectory = "";
        private readonly ICryptoWriter cryptoWriter;
        private Session activeSession;

        public SessionManager(ICryptoWriter cryptoWriter) {
            this.cryptoWriter = cryptoWriter;            
            hash = new byte[cryptoWriter.GetHashSize()];
        }

        public bool SetSessionDirectory(string directory) {
            if(Directory.Exists(directory)) {
                sessionDirectory = directory;
                return true;
            }
            try {
                Directory.CreateDirectory(sessionDirectory);
                sessionDirectory = directory;
                return true;
            }
            catch {
                return false;
            }            
        }

        private DataBlock ReadBlock(Stream stream) {
            var reader = new BinaryReader(stream);
            var len = reader.ReadInt32();

            var data = reader.ReadBytes(len);

            return cryptoWriter.Decrypt(data, br => new DataBlock
            {
                Data = data,
                Marker = br.ReadByte(),
                TimeStamp = new DateTime(br.ReadInt64())
            });
        }

        private byte[] ReadBlockNoDecode(Stream stream) {
            var reader = new BinaryReader(stream);
            var len = reader.ReadInt32();
            return reader.ReadBytes(len);
        }

        private void WriteBlock(Stream stream, byte[] data) {
            hash = cryptoWriter.MakeSignature(data);
            using (var bw = new BinaryWriter(stream)) {
                bw.Seek(0, SeekOrigin.End);

                bw.Write(data.Length);
                bw.Write(data);

                bw.Seek(0, SeekOrigin.Begin);
                bw.Write(hash, 0, hash.Length);
            }            
        }

        private void WriteSimpleMarker(Session session, byte marker) {
            using(var fs = new FileStream(activeSession.FileName, FileMode.Open, FileAccess.ReadWrite)) {

                var data = cryptoWriter.Encrypt(bw =>
                {
                    bw.Write(marker);
                    bw.Write(DateTime.UtcNow.Ticks);
                });                

                WriteBlock(fs, data);                
            }
        }

        public void CreateNewSession(Session session) {
            int index = 0;
            do {
                index++;
                
                session.FileName = Path.Combine(sessionDirectory, $"{session.Subject}_{index}.dat");
            } while(File.Exists(session.FileName));

            activeSession = session;

            using (var fs = new FileStream(session.FileName, FileMode.Create, FileAccess.ReadWrite)) {
                // Reserve space for the hash
                hash = new byte[cryptoWriter.GetHashSize()];
                fs.Write(hash, 0, hash.Length);

                WriteBlock(fs, cryptoWriter.CreateNew(session, BeginSessionMarker));
            }
        }

        private Session GetSessionFromFile(string path) {
            using (var fs = new FileStream(path, FileMode.Open)) {
                if(fs.Length < cryptoWriter.GetHashSize())
                    throw new InvalidFormatException();
                fs.Read(hash, 0, cryptoWriter.GetHashSize());

                var data = ReadBlockNoDecode(fs);

                var session = cryptoWriter.InitFromExisting(data, BeginSessionMarker);
                session.FileName = path;
                while (fs.Position < fs.Length) {
                    var block = ReadBlock(fs);
                    if(block.Marker == EndSessionMarker)
                        session.EndDt = block.TimeStamp;
                }
                return session;
            }
        }

        public List<Session> GetOpenSessions() {
            var files = Directory.GetFiles(sessionDirectory, "*.dat");
            var result = new List<Session>();
            foreach (var file in files) {
                try {
                    var session = GetSessionFromFile(file);
                    if (session.EndDt == null)
                        result.Add(GetSessionFromFile(file));
                }
                catch(Exception ex) {

                }
            }
            return result;
        }

        public void PauseSession(Session session) {
            WriteSimpleMarker(session, PauseSessionMarker);
        }

        public void ResumeSession(Session session) {
            activeSession = GetSessionFromFile(session.FileName);
            WriteSimpleMarker(activeSession, ResumeSessionMarker);
        }

        public void CompleteSession(Session session) {
            session.EndDt = DateTime.UtcNow;
            WriteSimpleMarker(session, EndSessionMarker);
        }

        public void WritePulse() {
            WriteSimpleMarker(activeSession, PulseMarker);
        }

        public void WriteApplicationRecord(string appName) {
            using (var fs = new FileStream(activeSession.FileName, FileMode.Open, FileAccess.ReadWrite)) {
                WriteBlock(fs, cryptoWriter.Encrypt(bw => {
                    bw.Write(AppRecordMarker);
                    bw.Write(DateTime.UtcNow.Ticks);
                    bw.Write(appName);
                }));
            }
        }

        public void WriteScreenshot(byte[] screenshot) {
            using(var fs = new FileStream(activeSession.FileName, FileMode.Open, FileAccess.ReadWrite)) {
                WriteBlock(fs, cryptoWriter.Encrypt(bw => {
                    bw.Write(ScreenshotRecordMarker);
                    bw.Write(DateTime.UtcNow.Ticks);
                    bw.Write(screenshot.Length);
                    bw.Write(screenshot);
                }));
            }
        }

        private void SkipSectionHeader(BinaryReader br) {
            br.ReadByte();      // Marker
            br.ReadInt64();     // Timestamp
        }


        public Session ReadFullSession(string fileName) {
            using(var fs = new FileStream(fileName, FileMode.Open)) {
                if(fs.Length < cryptoWriter.GetHashSize())
                    throw new InvalidFormatException();
                fs.Seek(cryptoWriter.GetHashSize(), SeekOrigin.Begin);

                var data = ReadBlockNoDecode(fs);

                var session = cryptoWriter.InitFromExisting(data, BeginSessionMarker);
                session.FileName = fileName;
                var detailedData = new SessionDetailedData();                
                while(fs.Position < fs.Length) {
                    var block = ReadBlock(fs);
                    switch(block.Marker) {
                        case PauseSessionMarker:
                            detailedData.PauseRecords.Add(block.TimeStamp);
                            break;
                        case ResumeSessionMarker:
                            detailedData.ResumeRecords.Add(block.TimeStamp);
                            break;
                        case AppRecordMarker:
                            detailedData.ApplicationRecords.Add(cryptoWriter.Decrypt(block.Data, br =>
                            {
                                SkipSectionHeader(br);
                                return new ApplicationRecord(block.TimeStamp, br.ReadString());
                            }));
                            break;
                        case ScreenshotRecordMarker:
                            detailedData.ScreenshotRecords.Add(cryptoWriter.Decrypt(block.Data, br =>
                            {
                                SkipSectionHeader(br);
                                int len = br.ReadInt32();
                                return new ScreenshotRecord(block.TimeStamp, br.ReadBytes(len));
                            }));
                            break;                            
                        case EndSessionMarker:
                            session.EndDt = block.TimeStamp;
                            break;
                    }
                }
                session.DetailedData = detailedData;
                return session;
            }
        }

     
    }
}
