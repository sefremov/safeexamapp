using System;

namespace SafeExamApp.Core.Model {
    public class Session {
        public DateTime StartDt { get; set; }
        public DateTime? EndDt { get; set; }
        public string Student { get; set; }
        public string Group { get; set; }
        public string Subject { get; set; }
        public string HardwareInfo { get; set; }
        public string FileName { get; set; }

        public bool HashCorrect { get; set; }
        public SessionDetailedData DetailedData { get; set; }
    }
}
