using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GRL.Logging
{
    public static class AppHelper
    {
        public static string BaseDirPath = "";
        private const string orgName = "GRL";
        private const string projName = "VDPower-2.0";
        private const string signalFile = "SignalFiles";
        public static string SignalFilePath = "";
        public const string ApplicationName = "V-DPWR-EPR";
        public const string ApplicationVersion = "1.1.1.1";
        public const string ApplicationFWVersion = "1.0.18";
        public const string ReleaseDate = "05-11-2025";
        public const string ConfigFileName = "Configurable_test_data.json";
        public static string CurrentRunFolderPath = "";

        public static void SetupBaseDirectory(string configFilePath = "")
        {
            if (configFilePath == "")
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    string rootPath = Path.GetPathRoot(AppDomain.CurrentDomain.BaseDirectory) ?? "C:\\";
                    BaseDirPath = Path.Combine(rootPath, orgName, projName);
                    if (Directory.Exists(BaseDirPath) == false)
                    {
                        Directory.CreateDirectory(BaseDirPath);
                    }
                    SignalFilePath = Path.Combine(BaseDirPath, signalFile);
                    if (Directory.Exists(SignalFilePath) == false)
                    {
                        Directory.CreateDirectory(SignalFilePath);
                    }
                }
                else
                {
                    BaseDirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), orgName, projName);
                    if (Directory.Exists(BaseDirPath) == false)
                    {
                        Directory.CreateDirectory(BaseDirPath);
                    }
                    SignalFilePath = Path.Combine(BaseDirPath, signalFile);
                    if (Directory.Exists(SignalFilePath) == false)
                    {
                        Directory.CreateDirectory(SignalFilePath);
                    }
                }
            }
            else
            {
                BaseDirPath = configFilePath;
            }
        }
    }
}
