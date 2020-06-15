using SafeExamApp.Core;
using SafeExamApp.Core.Interfaces;
using SafeExamApp.Core.Model;
using System;
using System.Linq.Expressions;
using System.Timers;

namespace SafeExamApp {
    class Program {

        static void Main(string[] args) {
            new UILogic().Run();            
        }
       
    }
}
