using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Squirrel;
using System.Threading.Tasks;

namespace QCompress
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly string n = Environment.NewLine;
        OpenFileDialog ofd;
        SaveFileDialog sfd;

        string videoPath = "";
        string outPath = "";

        bool browseOK = false;
        bool vLoaded = false;
        bool acceptData = true;

        int compressOutputLength = 0;
        string compressOutputLines = "";

        int videoFrameCount_orig = 0;
        int videoLength_orig = 0;
        int videoWidth_orig = 0;
        int videoHeight_orig = 0;
        decimal videoFramerate_orig = 1;

        int videoFrameCount = 0;
        int videoLength = 0;
        int videoWidth = 0;
        int videoHeight = 0;
        decimal videoFramerate = 1;

        int audioBitrate = 0;

        long targetBitrate = 0;
        DateTime startTime = DateTime.UnixEpoch;

        public MainWindow()
        {
            InitializeComponent();

            ofd = new OpenFileDialog
            {
                Filter = "All files|*.*"
            };
            sfd = new SaveFileDialog
            {
                DefaultExt = ".mp4",
                AddExtension = true,
                CheckPathExists = true,
                Filter = ofd.Filter,
                OverwritePrompt = true,
            };
        }

        void ChangeBitrate()
        {
            if (browseOK && vLoaded)
            {
                decimal targetFileSize = 0;
                decimal.TryParse(targetTextBox.Text, out targetFileSize);
                if (targetFileSize <= 0)
                    return;

                int abr = Settings.Default.MuteAudio ? 0 : audioBitrate;

                targetBitrate = Convert.ToInt64(videoWidth * videoHeight * videoFramerate * 0.15m);
                long estFileSize = Convert.ToInt64((targetBitrate + abr) * videoLength);

                if (estFileSize > targetFileSize * 8000000)
                {
                    //targetBitrate = Convert.ToInt64(targetFileSize * 8000000 / videoLength);
                    targetBitrate = Convert.ToInt64(targetFileSize * 8000000 / videoLength - abr);
                    estFileSize = Convert.ToInt64((targetBitrate + abr) * videoLength / 8);
                }

                Debug.WriteLine($"Estimated {estFileSize} b with bitrate {Convert.ToInt64(targetBitrate / 1000L)} kbit/s");
                bLabel.Content = $"{Convert.ToInt64(targetBitrate / 1000L)} kbit/s";
                eLabel.Content = $"{Convert.ToInt64(estFileSize / 100000L) / 10d} MB";
            }
        }

        void BrowseVideoFile()
        {
            bool? result = ofd.ShowDialog();
            if ((bool)result!)
            {
                videoPath = ofd.FileName;
                InputVideo();
            }
        }

        void InputVideo()
        {
            this.Title = "QCompress - " + videoPath;
            browseOK = true;
            LoadVideo();
        }

        void LoadVideo()
        {
            if (!browseOK)
            {
                MessageBox.Show("You have to select a video first!", "Video not selected", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            textBox.Clear();

            // Get video info
            JsonNode node = JsonNode.Parse(Common.FF("ffprobe", $"-v error -count_packets -show_streams -of json \"{videoPath}\"").ReadToEnd())!;

            JsonNode data = node["streams"]![0]!;
            //string[] data = FF("ffprobe", $"-v error -select_streams v:0 -count_packets -show_entries stream=width,height,duration,nb_read_packets -of csv=p=0 \"{videoPath}\"").ReadToEnd().Split(',');

            audioBitrate = int.Parse(node["streams"]![1]!["bit_rate"]!.ToString());

            videoWidth_orig = int.Parse(data["width"]!.ToString());
            videoHeight_orig = int.Parse(data["height"]!.ToString());
            videoLength_orig = int.Parse(data["duration"]!.ToString().Split(new char[] { '.', ',' })[0]);

            string[] _fr = data["r_frame_rate"]!.ToString().Split('/', 2);
            videoFramerate_orig = decimal.Parse(_fr[0]) / decimal.Parse(_fr[1]);
            videoFrameCount_orig = int.Parse(data["nb_read_packets"]!.ToString());
            progBar.Maximum = videoFrameCount_orig;

            videoWidth = videoWidth_orig;
            videoHeight = videoHeight_orig;
            videoLength = videoLength_orig;
            videoFramerate = videoFramerate_orig;
            videoFrameCount = videoFrameCount_orig;

            Debug.WriteLine(data.ToJsonString());

            // Output thumbnail to stdout
            image.Source = BitmapFrame.Create(Common.FF("ffmpeg", $"-y -i \"{videoPath}\" -vf \"select=eq(n\\,1)\" -update true -vframes 1 -c:v png -f image2pipe -").BaseStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);

            //outPath = Path.GetDirectoryName(videoPath) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(videoPath) + "_compressed" + Path.GetExtension(videoPath);

            sfd.InitialDirectory = Path.GetDirectoryName(videoPath);
            sfd.FileName = Path.GetFileNameWithoutExtension(videoPath) + "_qc" + Path.GetExtension(videoPath);

            string title = "INPUT FILE";
            int g = Common.GCD(videoWidth, videoHeight);
            string reso = $"Size: {videoWidth} x {videoHeight} ({videoWidth / g}:{videoHeight / g})";
            string length = $"{videoLength} seconds / {videoFrameCount_orig} frames";

            int maxL = Math.Max(length.Length, reso.Length);
            string ntitle = title.PadLeft(maxL / 2 + title.Length / 2, '-').PadRight(maxL, '-');
            string floor = "".PadRight(maxL, '-');

            textBox.AppendText($"{ntitle}{n}{reso}{n}{length}{n}{floor}");

            vLoaded = true;
            ChangeBitrate();
        }

        private void selectbutton_Click(object sender, RoutedEventArgs e)
        {
            BrowseVideoFile();
        }

        private void cbutton_Click(object sender, RoutedEventArgs e)
        {
            if (!vLoaded)
            {
                MessageBox.Show("You have to load a video first!", "Video not loaded", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if ((bool)sfd.ShowDialog()!)
            {
                Debug.WriteLine(sfd.FileName);
                outPath = sfd.FileName;
            }
            else return;

            progBar.Value = progBar.Minimum;
            cbutton.IsEnabled = false;
            groupBox1.IsEnabled = false;
            groupBox2.IsEnabled = false;

            // Set custom output resolution if enabled
            string scale = "";
            if (Settings.Default.CustomResolutionEnabled)
                scale = $"-vf scale={Settings.Default.RWidth}:{Settings.Default.RHeight},setsar=1:1 ";

            string muteAudio = "";
            if (Settings.Default.MuteAudio)
                muteAudio = "-an ";

            string framerate = "";
            if (Settings.Default.CustomFramerate)
                framerate = $"-filter:v fps={Settings.Default.Framerate} ";

            string trimStart = "";
            string trimEnd = "";
            if (Settings.Default.TrimVideoEnabled)
            {
                trimStart = $"-ss {Settings.Default.TrimStart:mm':'ss} ";
                trimEnd = $"-to {Settings.Default.TrimEnd:mm':'ss} ";
            }



            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = Path.Join(AppSettings.Default.FFmpegPath, "ffmpeg.exe"),
                Arguments = $"-y {trimStart}{trimEnd}-i \"{videoPath}\" {scale}{muteAudio}{framerate}-vcodec libx264 -b:v {Convert.ToInt64(targetBitrate / 1000L)}k -loglevel error -progress - -nostats \"{outPath}\"",
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            Debug.WriteLine($"Running {startInfo.FileName} {startInfo.Arguments}");
            Process? p = Process.Start(startInfo);
            if (p != null)
            {
                p.OutputDataReceived += P_OutputDataReceived;
                p.ErrorDataReceived += P_ErrorDataReceived;
                startTime = DateTime.Now;
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
            }
        }
        private void P_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                acceptData = false;
                textBox.AppendText($"[ERROR] {e.Data}");
            }
        }
        private void P_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            //Debug.WriteLine(e.Data);

            if (!acceptData)
                return;

            compressOutputLength += 1;

            if (compressOutputLength < 12)
            {
                compressOutputLines += e.Data + "\n";
            }
            else
            {
                compressOutputLines += e.Data;

                this.Dispatcher.Invoke(() =>
                {
                    textBox.Text = $"--------------------{n}COMPRESSION STARTED{n}--------------------{n}Time elapsed: {DateTime.Now - startTime:hh':'mm':'ss'.'fff}{n}{compressOutputLines}";
                });

                compressOutputLines = "";
                compressOutputLength = 0;
            }

            if (e.Data != null)
            {
                if (e.Data.StartsWith("frame="))
                {
                    int frame = int.Parse(e.Data.Replace("frame=", ""));
                    this.Dispatcher.Invoke(() =>
                    {
                        if (frame <= progBar.Maximum)
                        {
                            progBar.Value = frame;
                        };
                    });
                }
                else if (e.Data.StartsWith("progress=end"))
                {
                    if (((Process)sender).WaitForExit(1000))
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            SystemSounds.Beep.Play();
                            cbutton.IsEnabled = true;
                            groupBox1.IsEnabled = true;
                            groupBox2.IsEnabled = true;

                            progBar.Value = progBar.Minimum;
                            textBox.AppendText($"{n}--------------------{n}COMPRESSION FINISHED{n}--------------------{n}Output file:{n}{Path.GetFileName(outPath)} in {new Uri(Path.GetDirectoryName(outPath)!)}");
                        });
                    }
                }
            }
        }

        private void targetTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            //e.Handled = Regex.IsMatch(e.Text, "^(?:0|[1-9]\\d+|)?(?:.?\\d{0,2})?$");
            e.Handled = Regex.IsMatch(e.Text, "[^0-9]+");
        }

        private void targetTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ChangeBitrate();
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            selectbutton.Background = null;
            if (e.Data != null)
            {
                var input = e.Data.GetData(DataFormats.FileDrop);
                if (e.Data != null && input != null)
                {
                    videoPath = ((string[])input)[0];
                    InputVideo();
                }
            }
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            selectbutton.Background = SystemColors.GradientActiveCaptionBrush;
        }

        private void Window_DragLeave(object sender, DragEventArgs e)
        {
            selectbutton.Background = null;
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            Debug.WriteLine("Window_Initialized");
#if (!DEBUG)
            Common.CheckForUpdates();
#endif
        }
    }
}
