using SafeExamApp.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SafeExamApp.Core.Services {
    class ApplicationMonitor : IApplicationMonitor {

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        private string GetActiveWindowTitleWin() {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if(GetWindowText(handle, Buff, nChars) > 0) {
                return Buff.ToString();
            }
            return null;
        }

        private string GetActiveWindowTitleOSX() {
            return null;
        }

        private string currentActive;

        public event Action<string> OnActiveWindowChanged;

        public void CheckActiveApplication() {
            string active;
            if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                active = GetActiveWindowTitleOSX();
            else
                active = GetActiveWindowTitleWin();

            if (!string.IsNullOrWhiteSpace(active) && active != currentActive) {
                OnActiveWindowChanged?.Invoke(active);
                currentActive = active;
            }
        }
    }
}
