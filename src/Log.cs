using System;
using System.Globalization;
using System.IO;

namespace MemEase
{
    internal static class Log
    {
        private static readonly object Gate = new object();

        public static void Error(string context, Exception exception)
        {
            if (exception == null) return;
            try
            {
                lock (Gate)
                {
                    string directory = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "MemEase");
                    Directory.CreateDirectory(directory);
                    string file = Path.Combine(directory, "error.log");
                    if (File.Exists(file) && new FileInfo(file).Length > 128 * 1024)
                    {
                        string old = file + ".old";
                        try { File.Delete(old); }
                        catch { }
                        try { File.Move(file, old); }
                        catch { }
                    }
                    string entry = string.Format(
                        CultureInfo.InvariantCulture,
                        "{0:yyyy-MM-dd HH:mm:ss}  {1}\r\n{2}\r\n\r\n",
                        DateTime.Now,
                        context,
                        exception);
                    File.AppendAllText(file, entry);
                }
            }
            catch
            {
                // Logging must never throw.
            }
        }
    }
}
