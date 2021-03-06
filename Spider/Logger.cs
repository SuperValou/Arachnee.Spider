﻿using System;
using System.IO;

namespace Spider
{
    public class Logger
    {
        private static Logger _logger;
        private readonly string _logFile;

        public Logger(string logFile)
        {
            this._logFile = logFile;
            File.AppendAllText(logFile, string.Empty);
        }

        public static Logger Instance
        {
            get
            {
                if (_logger == null)
                {
                    throw new Exception("Logger not initialized.");
                }

                return _logger;
            }
        }


        public static void Initialize(string logFile)
        {
            _logger = new Logger(logFile);
        }

        public void LogDebug(string message)
        {
            //Console.WriteLine(message);
            //File.AppendAllText(_logFile, $"\n{DateTime.Now:G} DEBUG: " + message);
        }

        public void LogMessage(string message)
        {
            Console.WriteLine(message);
            File.AppendAllText(_logFile, $"\n{DateTime.Now:G} INFO: " + message);
        }

        public void LogError(string error)
        {
            Console.WriteLine(error);
            File.AppendAllText(_logFile, $"\n{DateTime.Now:G} ERROR: " + error);
        }

        public void LogException(Exception exception)
        {
            Console.WriteLine(exception.Message);
            File.AppendAllText(_logFile, $"\n{DateTime.Now:G} EXCEPTION: " + exception.Message + "\n" + exception.StackTrace);
        }
    }
}