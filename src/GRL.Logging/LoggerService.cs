using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace GRL.Logging
{
    public class LoggerService : ILoggerService, IDisposable
    {
        private ILogger _logger = null!;
        private IConfiguration _configuration;
        //private readonly ConcurrentQueue<LogModel> _logQueue = new();
        //private CancellationTokenSource _cancellationTokenSource = new();
        //private Task _logProcessingTask;

        public LoggerService(IConfiguration configuration, string logName, string logDirectory)
        {
            _configuration = configuration;
            CreateLog(logName, logDirectory);
        }

        private void CreateLog(string logName, string logDirectory)
        {
            if (_logger == null)
            {
                string folderPath = Path.Combine(logDirectory, "AppData");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string logFileName = Path.Combine(folderPath, $"{logName}_{DateTime.Now:yyyyMMdd_HHmmss}.log");

                _logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(_configuration)
                    .WriteTo.File(logFileName, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .CreateLogger();

                _logger.Information("Logger initialized successfully.");
            }

            //if (_logProcessingTask == null)
            //{
                //_cancellationTokenSource = new CancellationTokenSource();
                //_logProcessingTask = Task.Run(() => ProcessLogQueueAsync(_cancellationTokenSource.Token));
                //_logger.Information("Log processing task started.");
            //}
        }

        public void LogError(string message, Exception? ex = null)
        {
            if (ex != null)
            {
                _logger.Error(ex, message);  // ✅ Fixed format
            }
            else
            {
                _logger.Error(message);
            }
        }

        public void LogInformation(string message)
        {
            _logger.Information(message);
        }

        public void LogWarning(string message)
        {
            _logger.Warning(message);
        }

        public void WriteLog(string message, LogType logType, Exception? ex = null)
        {
            //LogModel logModel = new LogModel() { LogType = logType, Message = message, MessageException = ex };
            //_logQueue.Enqueue(logModel);
            switch (logType)
            {
                case LogType.Error:
                    LogError(message, ex);
                    break;
                case LogType.Information:
                    LogInformation(message);
                    break;
                case LogType.Warning:
                    LogWarning(message);
                    break;
            }
        }

        private async Task ProcessLogQueueAsync(CancellationToken cancellationToken)
        {
            try
            {
                //while (!cancellationToken.IsCancellationRequested)
                //{
                //    while (_logQueue.TryDequeue(out var logModel))
                //    {
                //        if (logModel != null)
                //        {
                //            switch (logModel.LogType)
                //            {
                //                case LogType.Error:
                //                    LogError(logModel.Message, logModel.MessageException);
                //                    break;
                //                case LogType.Information:
                //                    LogInformation(logModel.Message);
                //                    break;
                //                case LogType.Warning:
                //                    LogWarning(logModel.Message);
                //                    break;
                //            }
                //        }
                //    }

                //    await Task.Delay(500, cancellationToken); // Avoid busy-waiting
                //}
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in ProcessLogQueueAsync");
            }
        }
        public async Task<string> ConvertBytesToString(List<byte> bufData, int printLength = -1)
        {
            string strFinalData = string.Empty;
            try
            {
                if (printLength <= 0)
                    printLength = bufData.Count;
                if (printLength > bufData.Count)
                    printLength = bufData.Count;
                List<byte> tempBuf = new List<byte>();
                if (printLength < bufData.Count)
                {
                    tempBuf = new List<byte>(bufData.GetRange(0, printLength));
                }
                else
                {
                    tempBuf.AddRange(bufData);
                }
                if (tempBuf.Count > 10000)
                {
                    int singleBufCnt = tempBuf.Count / 4;

                    List<byte> bufOne = tempBuf.GetRange(0, singleBufCnt);
                    tempBuf.RemoveRange(0, singleBufCnt);
                    var task1 = Task.Run(() =>
                    {
                        return ByteToStringInHexFormat(bufOne);
                    });

                    List<byte> bufTwo = tempBuf.GetRange(0, singleBufCnt);
                    tempBuf.RemoveRange(0, singleBufCnt);
                    var task2 = Task.Run(() =>
                    {
                        return ByteToStringInHexFormat(bufTwo);
                    });

                    List<byte> bufThree = tempBuf.GetRange(0, singleBufCnt);
                    tempBuf.RemoveRange(0, singleBufCnt);
                    var task3 = Task.Run(() =>
                    {
                        return ByteToStringInHexFormat(bufThree);
                    });

                    List<byte> bufFour = tempBuf.GetRange(0, tempBuf.Count);
                    tempBuf.Clear();
                    var task4 = Task.Run(() =>
                    {
                        return ByteToStringInHexFormat(bufFour);
                    });

                    var task1Awaiter = task1.GetAwaiter();
                    string strDataOne = task1Awaiter.GetResult();
                    var task2Awaiter = task2.GetAwaiter();
                    string strDataTwo = task2Awaiter.GetResult();
                    var task3Awaiter = task3.GetAwaiter();
                    string strDataThree = task3Awaiter.GetResult();
                    var task4Awaiter = task4.GetAwaiter();
                    string strDataFour = task4Awaiter.GetResult();
                    strFinalData = strDataOne + strDataTwo + strDataThree + strDataFour;
                }
                else
                {
                    strFinalData = await ByteToStringInHexFormat(tempBuf);
                }
            }
            catch (Exception ex)
            {
                WriteLog(MethodInfo.GetCurrentMethod().Name, LogType.Error, ex);
            }
            return strFinalData;
        }
        private async Task<string> ByteToStringInHexFormat(List<byte> data)
        {
            string strData = string.Empty;
            try
            {
                for (int p = 0; p < data.Count; p++)
                {
                    strData += "0x" + data[p].ToString("X").ToUpper().PadLeft(2, '0') + ", ";
                }
            }
            catch (Exception ex)
            {
                WriteLog(MethodInfo.GetCurrentMethod().Name, LogType.Error, ex);
            }
            return strData;
        }

        public void CloseLogger()
        {
            Dispose();
        }

        public void Dispose()
        {
            //_cancellationTokenSource.Cancel();
            //_logProcessingTask.Wait();
            //_cancellationTokenSource.Dispose();

            if (_logger is IDisposable disposableLogger)
                disposableLogger.Dispose();
        }
    }
}
