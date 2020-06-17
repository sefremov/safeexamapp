using SafeExamApp.Core.Interfaces;
using System.Runtime.InteropServices;

namespace SafeExamApp.Core.Services {
    class SystemInfo : ISystemInfo {

        public string Get() {
            return $"{RuntimeInformation.OSDescription}";
        }
    }
}
