using System;
using System.Collections.Generic;
using System.Text;

namespace SafeExamApp.Core.Interfaces {
    public interface IApplicationMonitor {
        event Action<string> OnActiveWindowChanged;

        void CheckActiveApplication();
    }
}
