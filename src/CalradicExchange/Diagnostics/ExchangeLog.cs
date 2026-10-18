using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace CalradicExchange.Diagnostics
{
    /// <summary>
    /// Our own log file at Modules\CalradicExchange\Logs\exchange.log.
    /// On every start the previous files are rotated: exchange.log -> exchange.1.log -> exchange.2.log (last 3 kept).
    /// Never throws.
    /// </summary>
    public static class ExchangeLog
    {
        private const string FileName = "exchange.log";
        private const int KeepFiles = 3;
        private static readonly object Sync = new object();
        private static string _path;

        public static string FilePath => _path;

        public static void Initialize(string folder)
        {
            lock (Sync)
            {
                if (_path != null) return;
                Directory.CreateDirectory(folder);
                Rotate(folder);
                _path = Path.Combine(folder, FileName);
                File.WriteAllText(_path, string.Empty, Encoding.UTF8);
            }
        }

        public static void Info(string message) => Write("INFO", message);

        public static void Warn(string message) => Write("WARN", message);

        public static void Error(string message, Exception ex = null)
        {
            Write("ERROR", ex == null ? message : message + Environment.NewLine + ex);
        }

        private static void Write(string level, string message)
        {
            try
            {
                lock (Sync)
                {
                    if (_path == null) return;
                    string line = string.Format(CultureInfo.InvariantCulture, "{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] {2}{3}",
                        DateTime.Now, level, message, Environment.NewLine);
                    File.AppendAllText(_path, line, Encoding.UTF8);
                }
            }
            catch (Exception)
            {
                // Swallow: a logging failure must never break the campaign.
            }
        }

        private static void Rotate(string folder)
        {
            try
            {
                string Numbered(int n) => Path.Combine(folder, n == 0 ? FileName : $"exchange.{n}.log");

                string oldest = Numbered(KeepFiles - 1);
                if (File.Exists(oldest)) File.Delete(oldest);
                for (int i = KeepFiles - 2; i >= 0; i--)
                {
                    string from = Numbered(i);
                    if (File.Exists(from)) File.Move(from, Numbered(i + 1));
                }
            }
            catch (Exception)
            {
                // Rotation is best effort.
            }
        }
    }
}
