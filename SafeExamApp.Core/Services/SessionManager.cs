using SafeExamApp.Core.Exceptions;
using SafeExamApp.Core.Interfaces;
using SafeExamApp.Core.Model;
using System;
using System.Collections.Generic;
using System.IO;

namespace SafeExamApp.Core.Services {
    class SessionManager : ISessionManager {

        private const byte BeginSessionMarker = 0xD7;
        private const byte EndSessionMarker = 0xD8;
        private const byte AppRecordMarker = 0xA9;
        private const byte ScreenshotRecordMarker = 0xE4;

        private const string SessionDirectory = "sessions";
        private ICryptoWriter cryptoWriter;
        
        public SessionManager(ICryptoWriter cryptoWriter) {
            this.cryptoWriter = cryptoWriter;
            try {
                Directory.CreateDirectory(SessionDirectory);
            }
            catch { }            
        }

        private DataBlock ReadBlock(Stream stream) {
            var br = new BinaryReader(stream);
            var marker = br.ReadByte();
            var len = br.ReadInt32();

            return new DataBlock
            {
                Marker = marker,
                Data = br.ReadBytes(len)
            };
        }

        private void WriteBlock(Stream stream, byte marker, byte[] data) {
            using (var bw = new BinaryWriter(stream)) {
                bw.Write(marker);
                bw.Write(data.Length);
                bw.Write(data);
            }
        }

        public void CreateNewSession(Session session) {
            int index = 0;
            do {
                index++;
                session.FileName = Path.Combine(SessionDirectory, $"{index}.dat");
            } while(File.Exists(session.FileName));            
            
            using (var fs = new FileStream(session.FileName, FileMode.Create)) {
                WriteBlock(fs, BeginSessionMarker, cryptoWriter.InitNew(session));
            }
        }

        private Session GetSessionFromFile(string path) {
            using (var fs = new FileStream(path, FileMode.Open)) {
                var block = ReadBlock(fs);
                if(block.Marker != BeginSessionMarker)
                    throw new InvalidFormatException();
                var crypto = new CryptoWriter();
                var session = crypto.InitFromExisting(block.Data);
                session.FileName = path;
                while (fs.Position < fs.Length) {
                    block = ReadBlock(fs);
                    if(block.Marker == EndSessionMarker)
                        session.EndDt = crypto.ReadTimeStamp(block.Data);
                }
                return session;
            }
        }

        public List<Session> GetOpenSessions() {
            var files = Directory.GetFiles(SessionDirectory, "*.dat");
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

        public void ResumeSession(Session session) {
            using(var fs = new FileStream(session.FileName, FileMode.Open)) {
                var block = ReadBlock(fs);
                if(block.Marker != BeginSessionMarker)
                    throw new InvalidFormatException();
                cryptoWriter.InitFromExisting(block.Data);
            }
        }

        public void CompleteSession(Session session) {
            session.EndDt = DateTime.UtcNow;
            using (var fs = new FileStream(session.FileName, FileMode.Append)) {
                WriteBlock(fs, EndSessionMarker, cryptoWriter.MakeCloseSession());
            }
        }

        public void WriteApplicationRecord(Session session, string appName) {
            using (var fs = new FileStream(session.FileName, FileMode.Append)) {
                WriteBlock(fs, AppRecordMarker, cryptoWriter.MakeApplicationRecord(appName));
            }
        }
    }
}
