using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GRL.Logging;
using Serilog;

namespace LoopBack.Services
{
    public class SerilogLoggerService : ILoggerService
    {
        public void LogInformation(string message)
        {
            Log.Information(message);
        }

        public void LogWarning(string message)
        {
            Log.Warning(message);
        }

        public void LogError(string message, Exception? ex = null)
        {
            if (ex != null)
                Log.Error(ex, message);
            else
                Log.Error(message);
        }

        public void WriteLog(string message, LogType logType, Exception? ex = null)
        {
            switch (logType)
            {
                case LogType.Information:
                    LogInformation(message);
                    break;
                case LogType.Warning:
                    LogWarning(message);
                    break;
                case LogType.Error:
                    LogError(message, ex);
                    break;
                default:
                    Log.Information(message);
                    break;
            }
        }

        public void CloseLogger()
        {
            Log.CloseAndFlush();
        }

        public Task<string> ConvertBytesToString(List<byte> bufData, int printLength = -1)
        {
            if (bufData == null || bufData.Count == 0)
                return Task.FromResult(string.Empty);

            int length = printLength < 0 || printLength > bufData.Count ? bufData.Count : printLength;

            var sb = new StringBuilder(length * 3);
            for (int i = 0; i < length; i++)
            {
                sb.Append(bufData[i].ToString("X2"));
                if (i < length - 1) sb.Append(' ');
            }
            return Task.FromResult(sb.ToString());
        }
    }
}
