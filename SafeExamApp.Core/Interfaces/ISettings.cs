using System;
using System.Collections.Generic;
using System.Text;

namespace SafeExamApp.Core.Interfaces {
    interface ISettings {
        T GetSetting<T>(string name);
    }
}
