namespace SafeExamApp {
    class Program {

        static void Main(string[] args) {
#if TRIAL
            new UILogic().RunTrial();
#else
            new UILogic().Run();
#endif
        }
       
    }
}
