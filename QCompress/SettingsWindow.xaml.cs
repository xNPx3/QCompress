using System;
using System.Linq;
using System.Windows;

namespace QCompress
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        int vLength;
        decimal vFramerate;
        string vLengthTime;
        public SettingsWindow(int length, decimal framerate)
        {
            InitializeComponent();

            vLength = length;
            vFramerate = framerate;
            vLengthTime = TimeSpan.FromSeconds(length).ToString("mm':'ss");

            VideoResolution[] values = new VideoResolution[]
            {
                // 4:3
                new VideoResolution(800, 600),
                new VideoResolution(1280, 960),
                new VideoResolution(1440, 1080),
                new VideoResolution(1920, 1440),

                // 16:9
                new VideoResolution(1280, 720),
                new VideoResolution(1920, 1080),
                new VideoResolution(2560, 1440),
            };
            var data = values.Select(d => new VideoResolutionC
            {
                Width = d.Width,
                Height = d.Height,
                Text = $"{d.Width} x {d.Height} ({d.Width / Common.GCD(d.Width, d.Height)}:{d.Height / Common.GCD(d.Width, d.Height)})",
            }).ToList();

            comboBox.ItemsSource = data;
            comboBox.DisplayMemberPath = "Text";
            comboBox.SelectedItem = data.Find(v => v.Width == Preferences.LocalSettings.Default.RWidth && v.Height == Preferences.LocalSettings.Default.RHeight)
                                         ?? data.Find(v => v.Width == 1280 && v.Height == 720);

            textBox.Text = Preferences.AppSettings.Default.FFmpegPath;

            
            frameCheckBox.IsChecked = Preferences.LocalSettings.Default.CustomFramerateEnabled;
            //framerateNumericUpDown1.Enabled = Preferences.LocalSettings.Default.CustomFramerateEnabled;
            //framerateNumericUpDown1.Value = vFramerate;
            if (vFramerate != 1)
                //framerateNumericUpDown1.Maximum = vFramerate;
            if (Preferences.LocalSettings.Default.Framerate != -1)
            {
                //framerateNumericUpDown1.Value = Preferences.LocalSettings.Default.Framerate;
            }

            trimCheckBox.IsChecked = Preferences.LocalSettings.Default.TrimVideoEnabled;
            //cutEndMaskedTextBox1.Text = vLengthTime;
            if (Preferences.LocalSettings.Default.TrimEnd != TimeSpan.FromSeconds(1))
            {
                //cutStartMaskedTextBox1.Text = Preferences.LocalSettings.Default.TrimStart.ToString("mm':'ss");
                //cutEndMaskedTextBox1.Text = Preferences.LocalSettings.Default.TrimEnd.ToString("mm':'ss");
            }

            muteCheckBox.IsChecked = Preferences.LocalSettings.Default.MuteAudio;

            resoCheckBox.IsChecked = Preferences.LocalSettings.Default.CustomResolutionEnabled;
            //widthNumericUpDown1.Value = Preferences.LocalSettings.Default.RWidth;
            //heightNumericUpDown1.Value = Preferences.LocalSettings.Default.RHeight;
            if (data.Find(v => v.Width == Preferences.LocalSettings.Default.RWidth && v.Height == Preferences.LocalSettings.Default.RHeight) == null)
            {
                customResoCheckBox.IsChecked = resoCheckBox.IsChecked;
            }
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
    public class VideoResolution
    {
        public VideoResolution(int w, int h)
        {
            Width = w;
            Height = h;
        }
        public int Width { get; set; }
        public int Height { get; set; }
    }
    public class VideoResolutionC
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string? Text { get; set; }
    }
}
