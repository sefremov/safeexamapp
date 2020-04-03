using SafeExamApp.Core;
using SafeExamApp.Core.Interfaces;
using SafeExamApp.Core.Model;
using System;

namespace SafeExamApp {
    class Program {
        const int InterScreenshotTime = 60;

        static void MainMenu(ISessionManager sessionManager) {
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

        static Session StartNewSession() {
            Console.WriteLine("New session");
            var student = InputString("Enter your name and surname: ");
            var group = InputString("Enter yout group: ");
            return new Session
            {
                Student = student,
                Group = group
            };            
        }

        static void Main(string[] args) {

            var sessionManager = Factory.Instance.GetSessionManager();
            var taker = new ScreenshotTaker();

            var screenShot = taker.TakeScreenshot();
            if (screenShot == null) {
                Console.WriteLine("Essential functionality is limited. Cannot start the program");
                return;
            }


            var sessions = sessionManager.GetOpenSessions();
            
            Session session = null;

            if (sessions.Count > 0) {
                Console.WriteLine("Choose session to resume: ");
                for(int i = 0; i < sessions.Count; i++) {
                    Console.WriteLine($"{i+1}: {sessions[i].Student}, {sessions[i].StartDt.ToLocalTime()}");
                }
                Console.WriteLine($"{sessions.Count+1}: Start new session");
                var choice = InputChoice("Your choice: ", sessions.Count + 1);
                if(choice <= sessions.Count) {
                    session = sessions[choice - 1];
                    sessionManager.ResumeSession(session);
                }
            }

            if (session == null) {
                session = StartNewSession();
                sessionManager.CreateNewSession(session);
            }

            
            var prevShotTime = DateTime.MinValue;
            while(true) {
                //if((DateTime.Now - prevShotTime).TotalSeconds > InterScreenshotTime) {
                //    try {
                //        using(var fs = new FileStream($"image{index++}.png", FileMode.Create)) {
                //            taker.TakeScreenshot(fs);
                //        }
                //    }
                //    catch(Exception e) {
                //        Console.WriteLine(e.Message);
                //        Console.WriteLine(e.StackTrace);
                //    }
                //    prevShotTime = DateTime.Now;
                //}
                if(Console.KeyAvailable) {
                    var key = Console.ReadKey(true);
                    if(key.Key == ConsoleKey.Escape)
                        break;
                }
            }
        }
    }
}
