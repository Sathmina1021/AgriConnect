using System;

namespace Farmer.Service.Integration
{
    /// <summary>Minimal console logger so the demo host shows what the service is doing.</summary>
    public static class ServiceLog
    {
        public static void Info(string message) { Write("INFO ", message); }
        public static void Warn(string message) { Write("WARN ", message); }
        public static void Error(string message, Exception ex = null)
        {
            Write("ERROR", message + (ex == null ? "" : " | " + ex.GetType().Name + ": " + ex.Message));
        }

        private static void Write(string level, string message)
        {
            Console.WriteLine("[{0:HH:mm:ss}] {1} {2}", DateTime.Now, level, message);
        }
    }
}
