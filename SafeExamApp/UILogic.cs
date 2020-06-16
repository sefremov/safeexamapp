﻿using SafeExamApp.Core;
using SafeExamApp.Core.Interfaces;
using SafeExamApp.Core.Model;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;

namespace SafeExamApp {
    class UILogic {
        const int PulseTimerInterval = 5000;
        const int ScreenshotInterval = 30000;
        const int ActiveAppInterval = 1000;

        RepeatedTimer pulseTimer;
        RepeatedTimer activeAppTimer;
        RepeatedTimer regularScreenshotTimer;
        
        readonly ISessionWriter sessionManager;
        readonly IApplicationMonitor appMonitor;
        readonly ScreenshotTaker taker;
        ISystemInfo systemInfo;

        Session session;

        public UILogic() {
            taker = new ScreenshotTaker();
            sessionManager = Factory.Instance.GetSessionManager();
            appMonitor = Factory.Instance.GetApplicationMonitor();
            systemInfo = Factory.Instance.GetSystemInfo();
        }

        string InputString(string hint) {
            while(true) {
                Console.Write(hint);
                var input = Console.ReadLine();
                if(!string.IsNullOrWhiteSpace(input))
                    return input;
                Console.WriteLine("Cannot be empty. Try again");
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
            var student = InputString("Enter your name and surname: ");
            var group = InputString("Enter your group: ");
            var subject = InputString("Enter subject: ");
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
            regularScreenshotTimer.Elapsed += () => sessionManager.WriteScreenshot(null);
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

        void OnActiveWindowChanged(string windowTitle) {
            sessionManager.WriteApplicationRecord(windowTitle);
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

        public void RunTrial() {
            if(CheckFunctionality()) {
                Console.WriteLine("Your computer has passed all required checks! You are ready to use SafeExamApp at the exam");
                Console.ReadKey();
            }
        }

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
                sessionManager.WriteScreenshot(taker.TakeScreenshot());
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
    }
}
