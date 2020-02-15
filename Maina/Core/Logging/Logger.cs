using System;
using System.IO;

namespace Maina.Core.Logging
{
    public static class Logger
    {
        private static LogType _logLevel;
        private static string _filePath;
        private static readonly object _lock = new object();

        public static void Initialize(LogType logType, string filePath, string version)
        {
            _logLevel = logType;
            _filePath = filePath;

            PrintHeader(version);
        }

        public static void LogDebug(string message) => Log(LogType.Debug, message);
        public static void LogVerbose(string message) => Log(LogType.Verbose, message);
        public static void LogInfo(string message) => Log(LogType.Info, message);
        public static void LogWarning(string message) => Log(LogType.Warning, message);
        public static void LogError(string message) => Log(LogType.Error, message);
        public static void LogCritical(string message) => Log(LogType.Critical, message);
        public static void LogForce(string message) => Log(LogType.Force, message);

        private static void Log(LogType logType, string message)
        {
            // Do nothing if this message has a lower severity than we care about
            if (logType < _logLevel)
                return;

            lock (_lock)
            {
                Console.WriteLine();
                // use local time for console/journalctl
                Console.Write(DateTime.Now.ToString("MMM dd HH:mm"));
                Console.ForegroundColor = GetColor(logType);
                Console.Write($" [{logType}] ");
                Console.ResetColor();
                Console.Write(message);

                WriteToFile($"[{DateTime.UtcNow:yyyy/MM/dd - HH:mm}] [{logType}] {message}");
            }
        }

        private static void WriteToFile(string message)
        {
            using (var writer = File.AppendText(_filePath))
                writer.WriteLineAsync(message);
        }

        private static ConsoleColor GetColor(LogType type)
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (type)
            {
                case LogType.Debug:
                    return ConsoleColor.Cyan;
                case LogType.Verbose: 
                    return ConsoleColor.Magenta;
                case LogType.Warning: 
                    return ConsoleColor.Yellow;
                case LogType.Error: 
                    return ConsoleColor.Red;
                case LogType.Critical: 
                    return ConsoleColor.DarkRed;
                case LogType.Force: 
                    return ConsoleColor.DarkYellow;
                // Covers LogType.Info as well
                default: 
                    return ConsoleColor.White;
            }
        }

        private static void PrintHeader(string version)
        {
            lock (_lock)
            {
                string[] header =
                {
                    "███╗   ███╗ █████╗ ██╗███╗   ██╗ █████╗ ",
                    "████╗ ████║██╔══██╗██║████╗  ██║██╔══██╗",
                    "██╔████╔██║███████║██║██╔██╗ ██║███████║",
                    "██║╚██╔╝██║██╔══██║██║██║╚██╗██║██╔══██║",
                    "██║ ╚═╝ ██║██║  ██║██║██║ ╚████║██║  ██║",
                    "╚═╝     ╚═╝╚═╝  ╚═╝╚═╝╚═╝  ╚═══╝╚═╝  ╚═╝",
                    ""
                };

                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var line in header)
                    Console.WriteLine(line);

                Console.ResetColor();
                Console.WriteLine($"Version: {version}");
            }
        }
    }
}