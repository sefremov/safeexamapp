using SafeExamApp.Core.Model;
using System.Collections.Generic;

namespace SafeExamApp.Core.Interfaces {
    public interface ISessionWriter {
        bool SetSessionDirectory(string directory);

        List<Session> GetOpenSessions();
        void PauseSession(Session session);
        void ResumeSession(Session session);
        void CreateNewSession(Session session);
        void CompleteSession(Session session);
        void WriteApplicationRecord(string appRecord);
        void WriteScreenshot(byte[] screenshot);
        void WritePulse();
    }
}
