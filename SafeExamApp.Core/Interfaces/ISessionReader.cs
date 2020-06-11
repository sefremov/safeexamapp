using SafeExamApp.Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeExamApp.Core.Interfaces {
    public interface ISessionReader {
        Session ReadFullSession(string fileName);
    }
}
