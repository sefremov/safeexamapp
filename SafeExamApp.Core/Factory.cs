using SafeExamApp.Core.Interfaces;
using SafeExamApp.Core.Services;

namespace SafeExamApp.Core {
    public class Factory {
        static Factory instance;

        public static Factory Instance => instance ?? new Factory();

        private Factory() { }

        public ISessionWriter GetSessionManager() {
            return new SessionManager(new CryptoWriter());
        }

        public ISessionReader GetSessionReader() {
            return new SessionManager(new CryptoWriter());
        }

        public IApplicationMonitor GetApplicationMonitor() {
            return new ApplicationMonitor();
        }

        public ISystemInfo GetSystemInfo() {
            return new SystemInfo();
        }
    }
}
