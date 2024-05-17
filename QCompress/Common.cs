using Squirrel;
using System;
using System.Diagnostics;
using System.IO;
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

        public static async void CheckForUpdates()
        {
            try
            {
                using (var mgr = await UpdateManager.GitHubUpdateManager("https://github.com/xNPx3/QCompress"))
                {
                    var release = await mgr.UpdateApp();
                }
            }
            catch (Exception ex)
            {
                string message = "UpdateManager:" + Environment.NewLine + ex.Message + Environment.NewLine;
                if (ex.InnerException != null)
                    message += ex.InnerException.Message;
                MessageBox.Show(message);
            }
        }
    }
}
