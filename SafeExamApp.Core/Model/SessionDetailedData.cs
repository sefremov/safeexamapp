using System;
using System.Collections.Generic;
using System.Text;

namespace SafeExamApp.Core.Model {
    public class SessionDetailedData {
        public List<ApplicationRecord> ApplicationRecords { get; } = new List<ApplicationRecord>();
        public List<DateTime> PauseRecords { get; } = new List<DateTime>();
        public List<DateTime> ResumeRecords { get; } = new List<DateTime>();
        public List<ScreenshotRecord> ScreenshotRecords { get; } = new List<ScreenshotRecord>();
    }
}
