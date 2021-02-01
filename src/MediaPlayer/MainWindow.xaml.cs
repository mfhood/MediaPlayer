using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MediaPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //进度时间设置
        private TimeSpan duration;
        private DispatcherTimer timer = new DispatcherTimer();
        private const string extensions = ".avi,.wmv,.mp4";
        private LoopStatus loopStatus;
        private SearchOption searchOption;
        private bool isPlaying;
        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
            SetPlayer(false);
        }

        private void Root_Closing(object sender, CancelEventArgs e)
        {
            SaveSettings();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                this.DragMove();
            }
            catch (Exception)
            {
            }
        }

        private void mediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            timelineSlider.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalMilliseconds;
            duration = mediaElement.NaturalDuration.HasTimeSpan ? mediaElement.NaturalDuration.TimeSpan : TimeSpan.FromMilliseconds(0);
            totalTime.Text = string.Format("{0}{1:00}:{2:00}:{3:00}", "/", duration.Hours, duration.Minutes, duration.Seconds);

            if (!(this.IsFixedWindowSizeCheckBox.IsChecked ?? false) && mediaElement.HasVideo)
            {
                this.Height = mediaElement.NaturalVideoHeight > 1080 ? 1080 : mediaElement.NaturalVideoHeight;
                this.Width = mediaElement.NaturalVideoWidth > 1920 ? 1920 : mediaElement.NaturalVideoWidth;
                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;
                double windowWidth = this.Width;
                double windowHeight = this.Height;
                this.Left = (screenWidth / 2) - (windowWidth / 2);
                this.Top = (screenHeight / 2) - (windowHeight / 2);
            }
            if (mediaElement.HasVideo)
            {
                playBtn_Click(null, null);
            }
        }

        private void mediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            stopBtn_Click(null, null);
            timelineSlider.Value = 0;
            switch (loopStatus)
            {
                case LoopStatus.NoLoop:
                    break;
                case LoopStatus.ListLoop:
                    SkipForwardBtn_Click(null, null);
                    break;
                case LoopStatus.RandomLoop:
                    var fileInfo = new FileInfo(mediaElement.Source.OriginalString);
                    var files = fileInfo.Directory.GetFiles("*", searchOption).Where(c => extensions.Contains(c.Extension)).ToList();
                    var index = files.FindIndex(c => c.FullName == fileInfo.FullName);
                    var random = new Random();
                    mediaElement.Source = new Uri(files[random.Next(0, files.Count)].FullName);
                    playBtn_Click(null, null);
                    break;
                case LoopStatus.Loop:
                    playBtn_Click(null, null);
                    break;
                default:
                    break;
            }
        }


        // 设置播放，暂停，停止，前进，后退按钮是否可用
        private void SetPlayer(bool bVal)
        {
            playBtn.IsEnabled = !bVal;
            pauseBtn.IsEnabled = bVal;
            stopBtn.IsEnabled = bVal;
        }

        //选择视频文件对话框
        private void openBtn_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = @"视频文件(*.avi格式)|*.avi|视频文件(*.wav格式)|*.wav|视频文件(*.wmv格式)|*.wmv|(*.mp4格式)|*.mp4|All Files|*.*"
            };
            var result = openFileDialog.ShowDialog();
            if (result == true)
            {
                mediaElement.Source = new Uri(openFileDialog.FileName, UriKind.Relative);
                Settings.Default.DefaultPath = new FileInfo(openFileDialog.FileName).DirectoryName;
                playBtn_Click(null, null);
            }
        }

        #region 播放控制
        //播放视频
        private void playBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isPlaying)
            {
                return;
            }
            isPlaying = true;
            SetPlayer(true);
            mediaElement.Play();
        }

        //暂停播放视频
        private void pauseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!isPlaying)
            {
                return;
            }
            isPlaying = false;
            SetPlayer(false);
            mediaElement.Pause();
        }

        //停止播放
        private void stopBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!isPlaying)
            {
                return;
            }
            isPlaying = false;
            SetPlayer(false);
            mediaElement.Stop();
        }

        //后退播放
        private void backBtn_Click(object sender, RoutedEventArgs e)
        {
            mediaElement.Position = mediaElement.Position - TimeSpan.FromSeconds(10);
        }

        //快进播放
        private void forwardBtn_Click(object sender, RoutedEventArgs e)
        {
            mediaElement.Position = mediaElement.Position + TimeSpan.FromSeconds(10);
        }

        //播放上一个
        private void SkipBackwardBtn_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.Source == null && !Directory.Exists(Settings.Default.DefaultPath)) return;
            string fileName;
            if (mediaElement.Source == null)
            {
                fileName = new DirectoryInfo(Settings.Default.DefaultPath).GetFiles("*", searchOption).LastOrDefault(c => extensions.Contains(c.Extension))?.FullName;
                if (fileName == null) return;
            }
            else
            {
                var fileInfo = new FileInfo(mediaElement.Source.OriginalString);
                var files = fileInfo.Directory.GetFiles("*", searchOption).Where(c => extensions.Contains(c.Extension)).ToList();
                if (loopStatus == LoopStatus.RandomLoop)
                {
                    var random = new Random();
                    fileName = files[random.Next(0, files.Count)].FullName;
                }
                else
                {
                    var index = files.FindIndex(c => c.FullName == fileInfo.FullName);
                    if (index == -1) return;
                    index = --index == -1 ? files.Count - 1 : index;
                    fileName = files[index].FullName;
                }
            }
            mediaElement.Source = new Uri(fileName);
            playBtn_Click(null, null);
        }

        //播放下一个
        private void SkipForwardBtn_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.Source == null && !Directory.Exists(Settings.Default.DefaultPath)) return;
            string fileName;
            if (mediaElement.Source == null)
            {
                fileName = new DirectoryInfo(Settings.Default.DefaultPath).GetFiles("*", searchOption).FirstOrDefault(c => extensions.Contains(c.Extension))?.FullName;
                if (fileName == null) return;
            }
            else
            {
                var fileInfo = new FileInfo(mediaElement.Source.OriginalString);
                var files = fileInfo.Directory.GetFiles("*", searchOption).Where(c => extensions.Contains(c.Extension)).ToList();
                if (loopStatus == LoopStatus.RandomLoop)
                {
                    var random = new Random();
                    fileName = files[random.Next(0, files.Count)].FullName;
                }
                else
                {
                    var index = files.FindIndex(c => c.FullName == fileInfo.FullName);
                    if (index == -1) return;
                    index = ++index == files.Count ? 0 : index;
                    fileName = files[index].FullName;
                }
            }
            mediaElement.Source = new Uri(fileName);
            playBtn_Click(null, null);
        }

        //是否静音
        private void IsMutedBtn_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.IsMuted == true)
            {
                IsMutedIcon.Kind = PackIconKind.VolumeHigh;
                mediaElement.IsMuted = false;
            }
            else
            {
                IsMutedIcon.Kind = PackIconKind.VolumeOff;
                mediaElement.IsMuted = true;
            }
        }
        #endregion

        #region 用户设置
        private void LoadSettings()
        {
            AllowsTransparency = Settings.Default.IsAllowsTransparency;
            IsAllowsTransparencyCheckBox.IsChecked = AllowsTransparency;
            Topmost = Settings.Default.IsTopmost;
            mediaElement.Volume = Settings.Default.Volume;
            loopStatus = (LoopStatus)Settings.Default.LoopStatus;
            LoopPlayBtn.ToolTip = loopStatus.GetDescription();
            searchOption = Settings.Default.AllSearchOption ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            IsAllSearchOptionCheckBox.IsChecked = Settings.Default.AllSearchOption;
            IsFixedWindowSizeCheckBox.IsChecked = Settings.Default.IsFixedWindowSizeCheckBox;
            mediaElement.IsMuted = Settings.Default.IsMuted;
            IsMutedIcon.Kind = Settings.Default.IsMuted ? PackIconKind.VolumeOff : PackIconKind.VolumeHigh;
        }

        private void SaveSettings()
        {
            Settings.Default.IsTopmost = Topmost;
            Settings.Default.IsAllowsTransparency = IsAllowsTransparencyCheckBox.IsChecked ?? false;
            Settings.Default.Volume = mediaElement.Volume;
            Settings.Default.LoopStatus = (int)loopStatus;
            Settings.Default.AllSearchOption = searchOption == SearchOption.AllDirectories;
            Settings.Default.IsMuted = mediaElement.IsMuted;
            Settings.Default.Save();
        }
        #endregion

        #region 定时更新视频进度条
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 每 500 毫秒调用一次指定的方法
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            //使播放进度条跟随播放时间移动
            timelineSlider.ToolTip = mediaElement.Position.ToString().Substring(0, 8);
            timelineSlider.Value = mediaElement.Position.TotalMilliseconds;
            txtTime.Text = string.Format("{0}{1:00}:{2:00}:{3:00}", "", mediaElement.Position.Hours,
                               mediaElement.Position.Minutes, mediaElement.Position.Seconds);

        }
        #endregion

        #region 窗口状态控制
        private void MaxBtn_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                MaxWindowIcon.Kind = PackIconKind.WindowMaximize;
            }
            else
            {
                WindowState = WindowState.Maximized;
                MaxWindowIcon.Kind = PackIconKind.WindowRestore;
            }
        }

        private void MinBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Root_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                MaxWindowIcon.Kind = PackIconKind.WindowMaximize;
            }
            else
            {
                WindowState = WindowState.Maximized;
                MaxWindowIcon.Kind = PackIconKind.WindowRestore;
            }
        }
        #endregion

        private void LoopPlayBtn_Click(object sender, RoutedEventArgs e)
        {
            if (loopStatus == LoopStatus.Loop)
            {
                loopStatus = LoopStatus.NoLoop;
            }
            else
            {
                loopStatus++;
            }

            LoopPlayBtn.ToolTip = loopStatus.GetDescription();
        }


        private void IsAllSearchOptionCheckBox_Click(object sender, RoutedEventArgs e)
        {
            searchOption = IsAllSearchOptionCheckBox.IsChecked ?? false ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        }

        //键盘控制
        private void Root_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    mediaElement.Volume += 0.1;
                    break;
                case Key.Down:
                    mediaElement.Volume -= 0.1;
                    break;
                case Key.Left:
                    backBtn_Click(null, null);
                    break;
                case Key.Right:
                    forwardBtn_Click(null, null);
                    break;
                case Key.Space:
                    if (isPlaying)
                    {
                        pauseBtn_Click(null, null);
                    }
                    else
                    {
                        playBtn_Click(null, null);
                    }
                    break;
                case Key.MediaPlayPause:
                    if (isPlaying)
                    {
                        pauseBtn_Click(null, null);
                    }
                    else
                    {
                        playBtn_Click(null, null);
                    }
                    break;
                case Key.Escape:
                    CloseBtn_Click(null, null);
                    break;
                case Key.Enter:
                    MaxBtn_Click(null, null);
                    break;
                case Key.MediaPreviousTrack:
                    SkipBackwardBtn_Click(null, null);
                    break;
                case Key.MediaNextTrack:
                    SkipForwardBtn_Click(null, null);
                    break;
                case Key.MediaStop:
                    stopBtn_Click(null, null);
                    break;
                default:
                    break;
            }
        }

        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            volumeSlider.ToolTip = e.NewValue.ToString("00%");
        }
        private Point mousePoint;
        private void mediaElement_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mousePoint = e.GetPosition(this);
        }


        private void timelineSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            int SliderValue = (int)timelineSlider.Value;
            TimeSpan ts = new TimeSpan(0, 0, 0, 0, SliderValue);
            mediaElement.Position = ts;
            timelineSlider.ToolTip = mediaElement.Position.ToString().Substring(0, 8);
        }

        private void networkStreamBpx_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UrlTextBox.Text))
            {
                return;
            }
            try
            {
                mediaElement.Source = new Uri(UrlTextBox.Text);
            }
            catch (Exception)
            {
            }
            
        }

        private void Root_Drop(object sender, DragEventArgs e)
        {
            
            var formats = e.Data.GetFormats();
            if (formats == null || formats.Length <= 0)
            {
                return;
            }
            try
            {
                if (formats.Contains(DataFormats.FileDrop))
                {
                    var fileInfo = new FileInfo((e.Data.GetData(DataFormats.FileDrop) as string[])[0]);
                    mediaElement.Source = new Uri(fileInfo.FullName);
                }
            }
            catch (Exception)
            {
            }
            e.Handled = true;
        }

        private void mediaElement_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (mousePoint == e.GetPosition(this))
            {
                if (isPlaying)
                {
                    pauseBtn_Click(null, null);
                }
                else
                {
                    playBtn_Click(null, null);
                }
            }
            Console.WriteLine($"{mousePoint}{e.GetPosition(this)}");
        }

        private void Root_MouseMove(object sender, MouseEventArgs e)
        {
            var point = e.GetPosition(this);
            if (this.ActualHeight - point.Y < 100)
            {
                if (this.StatusInfo.Visibility == Visibility.Visible) return;
                this.StatusInfo.Visibility = Visibility.Visible;
            }
            else
            {
                if (this.StatusInfo.Visibility != Visibility.Hidden)
                    this.StatusInfo.Visibility = Visibility.Hidden;
                if (this.ActualWidth - point.X < 80 && point.Y < 40)
                {
                    if (this.WindowSizeContrlBtn.Visibility != Visibility.Visible)
                        this.WindowSizeContrlBtn.Visibility = Visibility.Visible;
                }
                else
                {
                    if (this.WindowSizeContrlBtn.Visibility != Visibility.Hidden)
                        this.WindowSizeContrlBtn.Visibility = Visibility.Hidden;
                }
            }
            
        }
    }

    enum LoopStatus : int
    {
        [Description("不循环")]
        NoLoop = 0,
        [Description("列表循环")]
        ListLoop = 1,
        [Description("随机循环")]
        RandomLoop = 2,
        [Description("洗脑循环")]
        Loop = 3
    }
}
