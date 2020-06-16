using System;

namespace SafeExamApp.Core.Model {
    public class SessionRecord {
        public DateTime TimeStamp { get; }

        public SessionRecord(DateTime dt) {
            TimeStamp = dt;
        }
    }
}
