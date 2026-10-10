using System;
using System.IO;

namespace AQT
{
    // Simple log to %TEMP%\AQTrainer.log (safe from any thread).
    internal static class Log
    {
        private static readonly object Gate = new object();
        private static string _path;

        public static string Path
        {
            get
            {
                if (_path == null)
                {
                    try { _path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AQTrainer.log"); }
                    catch { _path = "AQTrainer.log"; }
                }
                return _path;
            }
        }

        public static void Info(string msg)
        {
            Write("INFO ", msg);
        }

        public static void Error(string where, Exception e)
        {
            Write("ERROR", where + ": " + e);
        }

        private static void Write(string level, string msg)
        {
            try
            {
                lock (Gate)
                {
                    File.AppendAllText(Path, DateTime.Now.ToString("HH:mm:ss") + " " + level + " " + msg + Environment.NewLine);
                }
            }
            catch { }
            try { UnityEngine.Debug.Log("[AQTrainer] " + msg); } catch { }
        }
    }
}
