using System;

namespace SafeExamApp.Core.Services {
    class DataBlock {
        public byte Marker { get; set; }
        public DateTime TimeStamp { get; set; }
        public byte[] Data { get; set; }
    }
}
