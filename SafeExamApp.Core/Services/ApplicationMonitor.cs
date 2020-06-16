using SafeExamApp.Core.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace SafeExamApp.Core.Services
{
    class ApplicationMonitor : IApplicationMonitor
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        private string GetActiveWindowTitleWin()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0) {
                return Buff.ToString();
            }

            return null;
        }

        private string GetActiveWindowTitleMacOS()
        {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = $"-c \"{MacOsExecutablePath} {MacOsScriptPath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();

            var output = process.StandardOutput;

            process.WaitForExit();

            return output.ReadToEnd();
        }

        private const string MacOsExecutablePath = "/usr/bin/osascript";

        private static readonly string MacOsScriptPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            @"Resources/GetActiveWindow.scrt"
        );

        private string currentActive;

        public event Action<string> OnActiveWindowChanged;

        public void CheckActiveApplication()
        {
            string active = GetActiveApplication();            

            if (!string.IsNullOrWhiteSpace(active) && active != currentActive)
            {
                OnActiveWindowChanged?.Invoke(active);
                currentActive = active;
            }
        }

        public string GetActiveApplication() {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return GetActiveWindowTitleMacOS();
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return GetActiveWindowTitleWin();
            else
                return "";
        }
    }
}