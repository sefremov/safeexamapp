using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace SafeExamApp.Core.Model {
    public class SessionRecord {
        public DateTime TimeStamp { get; }

        public SessionRecord(DateTime dt) {
            TimeStamp = dt;
        }
    }
}
