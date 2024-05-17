using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace QCompress
{
    internal class Common
    {
        /// <summary>
        /// Greatest common divisor for two integers
        /// </summary>
        public static int GCD(int a, int b)
        {
            while (a != 0 && b != 0)
            {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }

            return a | b;
        }

        /// <summary>
        /// Start FFmpeg processes
        /// </summary>
        public static StreamReader FF(string exec, string args, ProcessStartInfo? info = null)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = Path.Join(AppSettings.Default.FFmpegPath, $"{exec}.exe"),
                Arguments = args,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            if (info != null)
                startInfo = info;

            try
            {
                Debug.WriteLine($"Running {startInfo.FileName} {startInfo.Arguments}");
                Process p = Process.Start(startInfo)!;
                return p.StandardOutput;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error trying to start process", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }

            return StreamReader.Null;
        }
    }
}
