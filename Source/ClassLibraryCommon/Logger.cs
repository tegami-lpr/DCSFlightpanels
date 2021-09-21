using System;
using System.Diagnostics;
using System.Reflection;
using System.IO;


namespace ClassLibraryCommon
{
    /// <summary>
    /// Class for write messages to log file
    /// </summary>
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

        /// <summary>
        /// Create if needed and return instance of logger
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Print warning message to logfile.
        /// </summary>
        public static void Warning(string message)
        {
            if (GetInstance()._logLevel < ELogLevel.elWarnings) return;
            message = TStamp() + " WARNING " + message;
            GetInstance().WriteLine(message);
        }

        /// <summary>
        /// Print info message to logfile.
        /// </summary>
        public static void Info(string message)
        {
            if (GetInstance()._logLevel < ELogLevel.elInfo) return;
            message = TStamp() + " INFO    " + message;
            GetInstance().WriteLine(message);
        }

        /// <summary>
        /// Print debug message to logfile.
        /// </summary>
        public static void Debug(string message)
        {
            if (GetInstance()._logLevel < ELogLevel.elDebug) return;
            message = TStamp() + " DEBUG   " + message;
            GetInstance().WriteLine(message);
        }

        /// <summary>
        /// Write line to log file w/o decorations
        /// </summary>
        public void WriteLine(string message)
        {
            lock (_logLock)
            {
                if (_fallBack)
                {
                    WriteToDebug(message);
                    return;
                }

                _logFile.WriteLine(message);
                _logFile.Flush();
            }
        }

        /// <summary>
        /// Private constructor
        /// </summary>
        private Logger()
        {
            _logLock = new object();
            //TODO: add non blocking way to write logs
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

            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fileVersionInfo.FileVersion;

            WriteLine(Environment.NewLine + "=== Log opened UTC " + TStamp() + ". Version  " + version);

        }

        /// <summary>
        /// Write message to debbuger
        /// </summary>
        /// <param name="message"></param>
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
        private object _logLock = null;
    }
}
