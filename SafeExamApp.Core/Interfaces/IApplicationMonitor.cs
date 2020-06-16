using System;

namespace SafeExamApp.Core.Interfaces {
    public interface IApplicationMonitor {
        event Action<string> OnActiveWindowChanged;

        void CheckActiveApplication();
        string GetActiveApplication();
    }
}
