namespace SafeExamApp.Core.Interfaces {
    interface ISettings {
        T GetSetting<T>(string name);
    }
}
