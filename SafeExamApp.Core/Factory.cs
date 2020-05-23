using SafeExamApp.Core.Interfaces;
using SafeExamApp.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeExamApp.Core {
    public class Factory {
        static Factory instance;

        public static Factory Instance => instance ?? new Factory();

        private Factory() { }

        public ISessionManager GetSessionManager() {
            return new SessionManager(new CryptoWriter());
        }

        public IApplicationMonitor GetApplicationMonitor() {
            return new ApplicationMonitor();
        }
    }
}
