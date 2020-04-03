using SafeExamApp.Core.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SafeExamApp.Core.Interfaces {
    public interface ICryptoWriter {
        Session InitFromExisting(byte[] data);
        byte[] InitNew(Session session);
        byte[] MakeApplicationRecord(string applicationName);
        byte[] MakeScreenRecord(string fileName);
        byte[] MakeCloseSession();
        DateTime ReadTimeStamp(byte[] data);
    }
}
