using System;

namespace SafeExamApp.Core.Services {
    public class DataBlock {
        public byte Marker { get; set; }
        public DateTime TimeStamp { get; set; }
        public byte[] Data { get; set; }
    }
}
