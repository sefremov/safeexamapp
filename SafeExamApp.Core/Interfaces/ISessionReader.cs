using SafeExamApp.Core.Model;

namespace SafeExamApp.Core.Interfaces {
    public interface ISessionReader {
        Session ReadFullSession(string fileName);
    }
}
