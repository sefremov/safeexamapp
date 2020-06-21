using SafeExamApp.Core;
using SafeExamApp.Core.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace SafeExamApp.Reader {
    class Program {

        const string ScreenshotDir = "screenshots";

        static void PrintSessionInfo(Session session, string fileName) {
            using(var sw = new StreamWriter(fileName, append: false, encoding: Encoding.UTF8)) {
                sw.WriteLine($"{session.Student}-{session.Group}-{session.Subject}");
                sw.WriteLine($"Started: {session.StartDt.ToLocalTime()}");
                sw.WriteLine($"Ended: {session.EndDt.Value.ToLocalTime()}");
                sw.WriteLine($"Pulses: {session.DetailedData.PulseRecords.Count}, expected {(int)(session.EndDt.Value - session.StartDt).TotalSeconds / 5}");

                var extraEvents = new List<(DateTime dt, string type)>();
                extraEvents.AddRange(session.DetailedData.PauseRecords.Select(r => (r, "pause")));
                extraEvents.AddRange(session.DetailedData.ResumeRecords.Select(r => (r, "resume")));

                sw.WriteLine();
                if(extraEvents.Count > 0) {
                    extraEvents.Sort();
                    sw.WriteLine("Extra events: ");
                    foreach(var ev in extraEvents)
                        sw.WriteLine($"{ev.dt.ToLocalTime()}: {ev.type}");
                    sw.WriteLine();
                }

                sw.WriteLine("Application records:");
                foreach(var appRecord in session.DetailedData.ApplicationRecords)
                    sw.WriteLine($"{appRecord.TimeStamp.ToLocalTime()}: {appRecord.ApplicationName}");
            }
        }

        static void SaveScreenshots(Session session, string sessionDirectory) {
            var targetDir = Path.Combine(sessionDirectory, ScreenshotDir);
            Directory.CreateDirectory(targetDir);
            
            foreach (var shot in session.DetailedData.ScreenshotRecords) {
                File.WriteAllBytes(Path.Combine(targetDir, shot.TimeStamp.ToString("yyyyMMdd HHmmss") + ".png"), shot.Data);
            }
        }

        static DateTime InputDateTime(string hint) {
            while(true) {
                Console.Write(hint);
                if(DateTime.TryParseExact(Console.ReadLine(), "dd.MM.yyyy HH:mm", null, DateTimeStyles.None, out var value))
                    return value;
                Console.WriteLine("Incorrect format");
            }
        }

        static void Main(string[] args) {
            var sessionReader = Factory.Instance.GetSessionReader();

            Console.Write("Enter directory: ");
            var directory = Console.ReadLine();

            foreach(var file in Directory.EnumerateFiles(directory, "*.dat", SearchOption.AllDirectories)) {
                var localDir = Path.GetDirectoryName(file);
                var fileNameNoExt = Path.GetFileNameWithoutExtension(file);

                var sessionDir = Path.Combine(localDir, fileNameNoExt);
                Directory.CreateDirectory(sessionDir);
                
                var session = sessionReader.ReadFullSession(file);
                if (session.EndDt == null) {
                    Console.WriteLine($"Session was not ended properly for {Path.GetFileName(localDir)}");
                    session.EndDt = InputDateTime("Input submission time DD.MM.YYYY hh:mm: ").ToUniversalTime();
                }
                PrintSessionInfo(session, Path.Combine(sessionDir, "session.log"));
                SaveScreenshots(session, sessionDir);
            }
            
        }
    }
}
