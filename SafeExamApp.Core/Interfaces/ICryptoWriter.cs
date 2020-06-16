using SafeExamApp.Core.Model;
using System;
using System.IO;

namespace SafeExamApp.Core.Interfaces {
    public interface ICryptoWriter {
        byte[] CreateNew(Session session, byte marker);
        Session InitFromExisting(byte[] data, byte expectedMarker);

        byte[] Encrypt(Action<BinaryWriter> onReadyFunc);
        T Decrypt<T>(byte[] data, Func<BinaryReader, T> onReadyFunc);

        byte[] MakeSignature(byte[] data, byte[] hash);
        int GetHashSize();
    }
}
