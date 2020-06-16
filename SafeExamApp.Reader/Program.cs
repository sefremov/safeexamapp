using SafeExamApp.Core;
using SafeExamApp.Core.Model;
using System;
using System.IO;

namespace SafeExamApp.Reader {
    class Program {

        const string ScreenshotDir = "screenshots";

        static void PrintSessionInfo(Session session) {
            Console.WriteLine($"{session.Student}-{session.Group}-{session.Subject}");
            Console.WriteLine($"Started: {session.StartDt}");
            Console.WriteLine($"Ended: {session.EndDt}");
            Console.WriteLine($"Pulses: {session.DetailedData.PulseRecords.Count}");
            if (session.DetailedData.PauseRecords.Count > 0) {
                Console.WriteLine("Pause events: ");
                foreach (var ev in session.DetailedData.PauseRecords)
                    Console.WriteLine(ev);
            }

            Console.WriteLine("Application records:");
            foreach (var appRecord in session.DetailedData.ApplicationRecords)
                Console.WriteLine($"{appRecord.TimeStamp}: {appRecord.ApplicationName}");

        }

        static void SaveScreenshots(Session session) {
            Directory.CreateDirectory(ScreenshotDir);
            foreach (var shot in session.DetailedData.ScreenshotRecords) {
                File.WriteAllBytes(Path.Combine(ScreenshotDir, shot.TimeStamp.ToString("yyyyMMdd HHmmss") + ".png"), shot.Data);
            }
        }

        static void Main(string[] args) {
            var sessionReader = Factory.Instance.GetSessionReader();
            
            var session = sessionReader.ReadFullSession(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "programming_1.dat"));
            PrintSessionInfo(session);
            SaveScreenshots(session);
            
        }
    }
}
