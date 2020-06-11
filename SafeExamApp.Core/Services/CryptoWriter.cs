using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using SafeExamApp.Core.Interfaces;
using SafeExamApp.Core.Model;
using System.Net.Http.Headers;
using SafeExamApp.Core.Exceptions;

namespace SafeExamApp.Core.Services {
    class CryptoWriter : ICryptoWriter {
        private const int KeySize = 16;
        private const int HashSize = 256 / 8;

        private byte[] defaultKey = { 4, 5, 250, 235, 47, 102, 10, 46, 78, 24, 12, 57, 192, 167, 251, 19 };
        private byte[] iv;
        private byte[] key;

        private byte[] RandomKey(int size) {
            var result = new byte[size];
            var r = new Random();
            r.NextBytes(result);
            return result;
        }

        private byte[] MakeKey(int size, string student, string group) {
            return Encoding.Unicode.GetBytes((student + group).PadRight(size)).Take(size).ToArray();
        }

        public byte[] CreateNew(Session session, byte marker) {
            return Encrypt(defaultKey, new byte[KeySize], bw =>
            {
                iv = RandomKey(KeySize);
                key = MakeKey(KeySize, session.Student, session.Group);

                bw.Write(marker);
                bw.Write(DateTime.UtcNow.Ticks);

                bw.Write(iv);
                bw.Write(session.Student);
                bw.Write(session.Group);
                bw.Write(session.Subject);
                bw.Write(session.HardwareInfo);
            });
        }

        public Session InitFromExisting(byte[] data, byte expectedMarker) {
            using(var algorithm = Rijndael.Create()) {
                algorithm.Key = defaultKey;
                algorithm.IV = new byte[KeySize];

                var decryptor = algorithm.CreateDecryptor();

                using(var ms = new MemoryStream(data)) {
                    using(var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read)) {
                        using(var br = new BinaryReader(cs)) {
                            
                            var marker = br.ReadByte(); // marker

                            if(marker != expectedMarker)
                                throw new InvalidFormatException();
                            
                            var timestamp = new DateTime(br.ReadInt64());
                            iv = br.ReadBytes(KeySize);

                            var student = br.ReadString();
                            var group = br.ReadString();
                            var subject = br.ReadString();
                            var hardware = br.ReadString();
                            
                            key = MakeKey(KeySize, student, group);

                            return new Session
                            {
                                Student = student,
                                Group = group,
                                StartDt = timestamp,
                                Subject = subject,
                                HardwareInfo = hardware
                            };
                        }
                    }
                }
            }
        }
       

        public T Decrypt<T>(byte[] data, Func<BinaryReader, T> onReadyFunc) {
            return Decrypt(data, key, iv, onReadyFunc);
        }

        private T Decrypt<T>(byte[] data, byte[] key, byte[] iv, Func<BinaryReader, T> onReadyFunc) {
            using(var algorithm = Rijndael.Create()) {
                algorithm.Key = key;
                algorithm.IV = iv;

                var decryptor = algorithm.CreateDecryptor();

                using(var ms = new MemoryStream(data)) {
                    using(var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read)) {
                        using(var br = new BinaryReader(cs)) {
                            return onReadyFunc(br);
                        }
                    }
                }
            }
        }

        public byte[] Encrypt(Action<BinaryWriter> onReadyFunc) {
            return Encrypt(key, iv, onReadyFunc);
        }

        private byte[] Encrypt(byte[] key, byte[] iv, Action<BinaryWriter> onReadyFunc) {
            using(var algorithm = Rijndael.Create()) {
                algorithm.Key = key;
                algorithm.IV = iv;

                var encryptor = algorithm.CreateEncryptor();
                using(var ms = new MemoryStream()) {
                    using(var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write)) {
                        using(var bw = new BinaryWriter(cs)) {
                            onReadyFunc(bw);
                        }
                    }
                    return ms.ToArray();
                }
            }
        } 

        public byte[] MakeSignature(byte[] data) {            
            using(var hmac = new HMACSHA256(key)) {
                return hmac.ComputeHash(data);
            }            
        }

        public int GetHashSize() {
            return HashSize;
        }
    }
}
