using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace ClassLibraryCommon
{
    public class Logger
    {
        /// <summary>
        /// Levels of logging
        /// </summary>
        public enum ELogLevel
        {
            elErrors = 0,
            elWarnings = 1,
            elInfo = 2,
            elDebug = 3
        }

        public static Logger GetInstance()
        {
            if (_instance == null)
            {
                _instance = new Logger();
            }
            return _instance;
        }

        public static void SetLogLevel(ELogLevel level)
        {
            GetInstance()._logLevel = level;
        }

        /// <summary>
        /// Print error message to logfile. Error messages always writed.
        /// </summary>
        /// <param name="message"></param>
        public static void Error(string message)
        {
            message = TStamp() + " ERROR   " + message;
            GetInstance().WriteLine(message);
        }

        public static void Warning(string message)
        {
            if (GetInstance()._logLevel < ELogLevel.elWarnings) return;
            message = TStamp() + " WARNING " + message;
            GetInstance().WriteLine(message);
        }

        public static void Info(string message)
        {
            if (GetInstance()._logLevel < ELogLevel.elInfo) return;
            message = TStamp() + " INFO    " + message;
            GetInstance().WriteLine(message);
        }

        public static void Debug(string message)
        {
            if (GetInstance()._logLevel < ELogLevel.elDebug) return;
            message = TStamp() + " DEBUG   " + message;
            GetInstance().WriteLine(message);
        }

        public void WriteLine(string message)
        {
            if (_fallBack)
            {
                WriteToDebug(message);
                return;
            }

            _logFile.WriteLine(message);
            _logFile.Flush();
        }

        ~Logger()
        {
            _logFile.Close();
        }

        private Logger()
        {
            string logFilePath = FPPaths.PrependPath("DCSFlightpanels.log");
            try
            {
                _logFile = File.AppendText(logFilePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Logger: Can't create log file. Error:"+ex.Message);
                return;
            }

            WriteLine(Environment.NewLine + "=== Log opened UTC " + TStamp());
        }

        private void WriteToDebug(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }

        private static string TStamp()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");
        }

        private static Logger _instance = null;
        private StreamWriter _logFile = null;
        private bool _fallBack = false;
        private ELogLevel _logLevel = ELogLevel.elDebug;
    }
}
