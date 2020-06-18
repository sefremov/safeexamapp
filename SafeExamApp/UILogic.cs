using SafeExamApp.Core;
using SafeExamApp.Core.Interfaces;
using SafeExamApp.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SafeExamApp {
    class UILogic {
        const int PulseTimerInterval = 5000;
        const int ScreenshotInterval = 300000;
        const int ActiveAppInterval = 1000;

        const int MinIntershotTimeSec = 10;     // Do not take more than 1 shot in 10 seconds
        const int MinAppIntershotTimeSec = 300;    // Do not take more than 1 shot of a single application in 5 minutes

        RepeatedTimer pulseTimer;
        RepeatedTimer activeAppTimer;
        RepeatedTimer regularScreenshotTimer;
        
        readonly ISessionWriter sessionManager;
        readonly IApplicationMonitor appMonitor;
        readonly ScreenshotTaker taker;
        ISystemInfo systemInfo;

        Session session;

        DateTime lastScreenShot = DateTime.MinValue;
        Dictionary<string, DateTime> appScreenShots = new Dictionary<string, DateTime>();

        public UILogic() {
            taker = new ScreenshotTaker();
            sessionManager = Factory.Instance.GetSessionManager();
            appMonitor = Factory.Instance.GetApplicationMonitor();
            systemInfo = Factory.Instance.GetSystemInfo();
        }

        bool StringNotEmpty(string input) => !string.IsNullOrWhiteSpace(input);
        bool IsValidCourseName(string input) => input.ToLower().All(c => (c >= 'a' && c <= 'z') || c == ' ');

        string InputString(string hint, Func<string, bool> check) {
            while(true) {
                Console.Write(hint);
                var input = Console.ReadLine();
                if(check(input))
                    return input;
                Console.WriteLine("Incorrect value. Try again");
            }
        }

        int InputChoice(string hint, int numOfOptions) {
            while(true) {
                Console.Write(hint);
                if(int.TryParse(Console.ReadLine(), out int choice)) {
                    if(choice >= 1 && choice <= numOfOptions)
                        return choice;
                    else
                        Console.WriteLine($"Error. Choose and option between 1 and {numOfOptions}");
                }
                else
                    Console.WriteLine("Error. Incorrect format");
            }
        }

        Session StartNewSession(ISystemInfo systemInfo) {
            Console.WriteLine("New session");
            var student = InputString("Enter your name and surname: ", StringNotEmpty);
            var group = InputString("Enter your group: ", StringNotEmpty);
            var subject = InputString("Enter subject: ", IsValidCourseName);
            return new Session
            {
                Student = student,
                Group = group,
                Subject = subject,
                HardwareInfo = systemInfo.Get()
            };
        }

        Session PrepareSession(ISessionWriter sessionManager, ISystemInfo systemInfo, out bool isNewSession) {
            var sessions = sessionManager.GetOpenSessions();

            Session session = null;
            isNewSession = false;
            if(sessions.Count > 0) {
                Console.WriteLine("Choose session to resume: ");
                for(int i = 0; i < sessions.Count; i++) {
                    Console.WriteLine($"{i + 1}: {sessions[i].Student}, {sessions[i].StartDt.ToLocalTime()}");
                }
                Console.WriteLine($"{sessions.Count + 1}: Start new session");
                var choice = InputChoice("Your choice: ", sessions.Count + 1);
                if(choice <= sessions.Count) {
                    session = sessions[choice - 1];
                    sessionManager.ResumeSession(session);
                }
            }

            if(session == null) {
                session = StartNewSession(systemInfo);
                sessionManager.CreateNewSession(session);
                isNewSession = true;
            }
            return session;
        }

        void InitTimers() {
            pulseTimer = new RepeatedTimer(PulseTimerInterval);
            pulseTimer.Elapsed += () => sessionManager.WritePulse();

            activeAppTimer = new RepeatedTimer(ActiveAppInterval);
            activeAppTimer.Elapsed += () => appMonitor.CheckActiveApplication();

            regularScreenshotTimer = new RepeatedTimer(ScreenshotInterval);
            regularScreenshotTimer.Elapsed += () => TakeScreenshot(null);
        }

        void InputSessionLocation() {
            while(true) {
                Console.Write("Enter a folder to store your session file (leave empty to use Desktop): ");
                var sessionDir = Console.ReadLine();
                if(string.IsNullOrWhiteSpace(sessionDir))
                    sessionDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if(sessionManager.SetSessionDirectory(sessionDir))
                    break;
                else
                    Console.WriteLine("Error locating directory");
            }
        }

        void OnConsoleClose(object sender, EventArgs args) {
            sessionManager.PauseSession(session);
            Console.WriteLine("SafeExamApp is now closing. Please relaunch to continue your session");
            Console.ReadLine();
        }

        void TakeScreenshot(string appName) {
            if((DateTime.Now - lastScreenShot).TotalSeconds < MinIntershotTimeSec)
                return;

            if(!string.IsNullOrEmpty(appName)) {

                // First time we are seeing this application open
                var lastAppScreenshot = appScreenShots.GetValueOrDefault(appName, DateTime.MinValue);

                if((DateTime.Now - lastAppScreenshot).TotalSeconds > MinAppIntershotTimeSec) {
                    sessionManager.WriteScreenshot(taker.TakeScreenshot());
                    lastScreenShot = DateTime.Now;
                    appScreenShots[appName] = DateTime.Now;
                }
            }
            else {
                sessionManager.WriteScreenshot(taker.TakeScreenshot());
                lastScreenShot = DateTime.Now;
            }
        }

        void OnActiveWindowChanged(string windowTitle) {
            sessionManager.WriteApplicationRecord(windowTitle);
            TakeScreenshot(windowTitle);
        }

        bool CheckFunctionality() {
            var screenShot = taker.TakeScreenshot();

            if(screenShot == null) {
                Console.WriteLine("Essential functionality is unavailable (err code = -1). Cannot start the program");
                return false;
            }

            var activeApp = appMonitor.GetActiveApplication();
            if(string.IsNullOrEmpty(activeApp)) {
                Console.WriteLine("Essential functionality is unavailable (err code = -2). Cannot start the program");
                return false;
            }

            return true;
        }
#if TRIAL
        public void RunTrial() {
            if(CheckFunctionality()) {
                Console.WriteLine("Your computer has passed all required checks! You are ready to use SafeExamApp at the exam");
                Console.ReadKey();
            }
        }
#else
        public void Run() {

            if(!CheckFunctionality())
                return;            

            InputSessionLocation();
            
            appMonitor.OnActiveWindowChanged += OnActiveWindowChanged;
            InitTimers();            

            session = PrepareSession(sessionManager, systemInfo, out bool isNew);

            if(isNew) {
                Console.WriteLine("Position the SafeExamApp console on top of your canvas account page and press Enter to start the session");
                Console.ReadLine();
                TakeScreenshot(null);
            }

            var timers = new RepeatedTimer[] { activeAppTimer, pulseTimer, regularScreenshotTimer };
            foreach(var t in timers)
                t.Start();

            AppDomain.CurrentDomain.ProcessExit += OnConsoleClose;

            Console.WriteLine("Your session is now active");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("DON'T CLOSE THIS WINDOW, WHILE YOUR EXAM IS RUNNING");
            Console.ResetColor();
            Console.WriteLine("Press Escape to complete the session at the end of the exam");

            var prevShotTime = DateTime.MinValue;
            while(true) {
                Thread.Sleep(1000);
                try {
                    foreach(var t in timers)
                        t.Poll();

                    if(Console.KeyAvailable) {
                        var key = Console.ReadKey(true);
                        if(key.Key == ConsoleKey.Escape) {
                            Console.WriteLine("Are you sure you want to complete your session (Y/N)?");
                            if(Console.ReadKey(true).Key == ConsoleKey.Y) {
                                sessionManager.CompleteSession(session);
                                Console.WriteLine("Your session is now over. The location of the log file can be seen below");
                                Console.WriteLine(session.FileName);
                                Console.WriteLine("Don't forget to add this file to your submission archive!");
                                break;
                            }
                            else
                                Console.WriteLine("Session is continuing");
                        }
                    }
                }
                catch(Exception e) {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(e.Message);
                    Console.ResetColor();
                }
            }

            AppDomain.CurrentDomain.ProcessExit -= OnConsoleClose;
            Console.ReadLine();
        }
#endif
    }
}
