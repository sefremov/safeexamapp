using System;

namespace SafeExamApp.Core.Model {
    public class ScreenshotRecord : SessionRecord {
        public byte[] Data { get; }

        public ScreenshotRecord(DateTime dt, byte[] data) : base(dt) {
            Data = data;
        }
    }
}
