using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GRL.Logging
{
    public interface ILoggerService
    {
       // void CreateLog(string logName, string logDirectory);
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message, Exception? ex = null);
        void WriteLog(string message, LogType logType, Exception? ex = null);
        void CloseLogger();
        Task<string> ConvertBytesToString(List<byte> bufData, int printLength = -1);
    }
}
