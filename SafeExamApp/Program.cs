using SafeExamApp.Core;
using SafeExamApp.Core.Interfaces;
using SafeExamApp.Core.Model;
using System;
using System.Timers;

namespace SafeExamApp {
    class Program {
        const int InterScreenshotTime = 60;

        static void MainMenu(ISessionWriter sessionManager) {
            Console.WriteLine("Choose an option: ");            
        }

        static string InputString(string hint) {
            while (true) {
                Console.Write(hint);
                var input = Console.ReadLine();
                if(!string.IsNullOrWhiteSpace(input))
                    return input;
                Console.WriteLine("Cannot be empty. Try again");                
            }            
        }

        static int InputChoice(string hint, int numOfOptions) {
            while(true) {
                Console.Write(hint);
                if (int.TryParse(Console.ReadLine(), out int choice)) {
                    if(choice >= 1 && choice <= numOfOptions)
                        return choice;
                    else
                        Console.WriteLine($"Error. Choose and option between 1 and {numOfOptions}");
                }
                else
                    Console.WriteLine("Error. Incorrect format");
            }
        }

        static Session StartNewSession(ISystemInfo systemInfo) {
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

        static Session PrepareSession(ISessionWriter sessionManager, ISystemInfo systemInfo) {
            var sessions = sessionManager.GetOpenSessions();

            Session session = null;

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
            }
            return session;
        }

        static void Main(string[] args) {            

            var sessionManager = Factory.Instance.GetSessionManager();
            while(true) {
                Console.Write("Enter a folder to store sessions (leave empty to use Desktop): ");
                var sessionDir = Console.ReadLine();
                if(string.IsNullOrWhiteSpace(sessionDir))
                    sessionDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if(sessionManager.SetSessionDirectory(sessionDir))
                    break;
                else
                    Console.WriteLine("Error locating directory");
            }            
            
            var systemInfo = Factory.Instance.GetSystemInfo();
            var taker = new ScreenshotTaker();

            var appMonitor = Factory.Instance.GetApplicationMonitor();
            appMonitor.OnActiveWindowChanged += windowTitle => sessionManager.WriteApplicationRecord(windowTitle);

            var pulseTimer = new RepeatedTimer(10000);
            pulseTimer.Elapsed += () => sessionManager.WritePulse();

            var activeAppTimer = new RepeatedTimer(1000);            
            activeAppTimer.Elapsed += () => appMonitor.CheckActiveApplication();

            var regularScreenshotTimer = new RepeatedTimer(3000);            
            regularScreenshotTimer.Elapsed += () =>
            {
                sessionManager.WriteScreenshot(taker.TakeScreenshot());
            };
            
            var screenShot = taker.TakeScreenshot();
            if (screenShot == null) {
                Console.WriteLine("Essential functionality is limited. Cannot start the program. Press any key");
                return;
            }

            var session = PrepareSession(sessionManager, systemInfo);
            
            var timers = new RepeatedTimer[] { activeAppTimer, pulseTimer, regularScreenshotTimer };
            foreach(var t in timers)
                t.Start();

            AppDomain.CurrentDomain.ProcessExit += (obj, args) => sessionManager.PauseSession(session);

            Console.WriteLine("Your session is now active");
            Console.WriteLine("DON'T CLOSE THIS WINDOW, WHILE YOUR EXAM IS RUNNING");

            var prevShotTime = DateTime.MinValue;
            while(true) {
                foreach(var t in timers)
                    t.Poll();

                if(Console.KeyAvailable) {
                    var key = Console.ReadKey(true);
                    if(key.Key == ConsoleKey.Escape) {
                        Console.WriteLine("Are you sure you want to complete your session (Y/N)?");
                        if (Console.ReadKey(true).Key == ConsoleKey.Y) {
                            Console.Write("Type finish to confirm: ");
                            var input = Console.ReadLine();
                            if(input == "finish") {
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
            }
            
            Console.ReadLine();
        }
       
    }
}
