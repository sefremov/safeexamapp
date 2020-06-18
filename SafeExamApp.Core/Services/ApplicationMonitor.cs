using SafeExamApp.Core.Exceptions;
using SafeExamApp.Core.Interfaces;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace SafeExamApp.Core.Services
{
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

        private string ExecuteOsCommand(string executable, string parameters) {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = $"-c \"{executable} {parameters}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();

            var output = process.StandardOutput;
            var error = process.StandardError;

            process.WaitForExit();

            if(!error.EndOfStream) {
                throw new ShellCommandExecuteException();
            }

            return output.ReadToEnd();
        }

        private string GetActiveWindowTitleMacOS() {
            using(var func = SHA512.Create()) {
                using(var contents = File.Open(MacOsScriptPath, FileMode.Open)) {
                    var hash = BitConverter
                        .ToString(func.ComputeHash(contents))
                        .Replace("-", "")
                        .ToLowerInvariant();

                    if(hash != MacOsScriptHash) {
                        throw new HashException();
                    }
                }
            }

            try {
                return ExecuteOsCommand(MacOsExecutablePath, "'" + MacOsScriptPath + "'");
            }
            catch(ShellCommandExecuteException) {
                return null;
            }
        }

        private string GetActiveWindowsTitleUnix() {
            try {
                return ExecuteOsCommand(UnixExecutablePath, UnixExecutableParams);
            }
            catch(ShellCommandExecuteException) {
                return null;
            }
        }

        private const string MacOsScriptHash =
            "81f11773b616958849506a396cbc454833e223661e4b2ea206c320d571395dea6aefbdc68d277468847236140d2218e9ba2a4846ec769b6e74a92f0a0fadd051";

        private const string MacOsExecutablePath = "/usr/bin/osascript";

        private static readonly string MacOsScriptPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            @"Resources/GetActiveWindow.scrt");

        private const string UnixExecutablePath = "xdotool";
        private const string UnixExecutableParams = "getactivewindow getwindowname";

        private string currentActive;

        public event Action<string> OnActiveWindowChanged;

        public void CheckActiveApplication()
        {
            string active;
            try {
                active = GetActiveApplication();
            }
            catch (HashException) {
                active = "!!!osascript corrupted!!!";
            }

            if (!string.IsNullOrWhiteSpace(active) && active != currentActive)
            {
                currentActive = active;
                OnActiveWindowChanged?.Invoke(active);                
            }
        }

        public string GetActiveApplication() {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return GetActiveWindowTitleMacOS();
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return GetActiveWindowTitleWin();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return GetActiveWindowsTitleUnix();
            else
                return "";
        }
    }
}