using System;

namespace LanControl.Core.Utils
{
    public static class Logger
    {
        private static readonly object Lock = new object();
        public static event Action<string> OnLog;

        public static void Info(string message, params object[] args)
        {
            Log("INFO", ConsoleColor.Cyan, message, args);
        }

        public static void Success(string message, params object[] args)
        {
            Log("SUCCESS", ConsoleColor.Green, message, args);
        }

        public static void Warn(string message, params object[] args)
        {
            Log("WARN", ConsoleColor.Yellow, message, args);
        }

        public static void Error(string message, params object[] args)
        {
            Log("ERROR", ConsoleColor.Red, message, args);
        }

        private static void Log(string level, ConsoleColor color, string message, params object[] args)
        {
            string formattedMsg = (args != null && args.Length > 0) ? string.Format(message, args) : message;
            string logLine = string.Format("[{0:HH:mm:ss}] [{1}] {2}", DateTime.Now, level, formattedMsg);

            try
            {
                lock (Lock)
                {
                    ConsoleColor oldColor = Console.ForegroundColor;
                    Console.ForegroundColor = color;
                    Console.WriteLine(logLine);
                    Console.ForegroundColor = oldColor;
                }
            }
            catch { }

            if (OnLog != null)
            {
                try { OnLog(logLine); } catch { }
            }
        }
    }
}
