using SafeExamApp.Core.Model;
using System.Collections.Generic;

namespace SafeExamApp.Core.Interfaces {
    public interface ISessionManager {
        List<Session> GetOpenSessions();
        void ResumeSession(Session session);
        void CreateNewSession(Session session);
        void CompleteSession(Session session);
        void WriteApplicationRecord(string appRecord);
    }
}
