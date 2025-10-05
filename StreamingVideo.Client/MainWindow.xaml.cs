using StreamingVideo.Common;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Numerics;
using System.Text;
using System.Text.Json;
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
using Wpf.Ui.Controls;

namespace StreamingVideo_Client {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private bool isFullscreen = false;
        private WindowState prevState;
        private WindowStyle prevStyle;
        private ResizeMode prevResize;
        DispatcherTimer timer = new DispatcherTimer();
        public MainWindow() {
            InitializeComponent();

            AsynchronousSocketListener.StartSocket(SocketTypes.Client, new IPAddress([127, 0, 0, 1]));
            AsynchronousSocketListener.CommandRecived += CommandRecived;

            VideoPlayer.Source = new Uri($"https://lucadalessandromira.freeddns.org:8080/");
            VideoPlayer.BufferingStarted += VideoPlayer_BufferingStarted;

            timelineSlider.Value = 0;
            timer.Interval = TimeSpan.FromMilliseconds(1000); // aggiorna ogni mezzo secondo
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e) {
            if (VideoPlayer.NaturalDuration.HasTimeSpan) {
                timelineSlider.Value = VideoPlayer.Position.TotalMilliseconds;
            }
        }
        private void VideoPlayer_BufferingStarted(object sender, RoutedEventArgs e) {
            Dispatcher.BeginInvoke(new Action(() => {
                if (VideoPlayer.NaturalDuration.HasTimeSpan) {
                    var time = VideoPlayer.NaturalDuration.TimeSpan;
                    timelineSlider.Maximum = time.TotalMilliseconds;
                }
            }));
        }
        private void CommandRecived(object? sender, Command e) {
            Dispatcher.BeginInvoke(new Action(() => {
                switch (e.Cmd) {
                    case CommandType.Play:
                        VideoPlayer.Play();
                        break;
                    case CommandType.Pause:
                        VideoPlayer.Pause();
                        break;
                    case CommandType.Stop:
                        VideoPlayer.Stop();
                        break;
                    case CommandType.Seek:
                        VideoPlayer.Position = TimeSpan.FromMilliseconds(e.TimeSkipMillis);
                        timelineSlider.Value = e.TimeSkipMillis;
                        break;
                }
            }));
        }
        private async void BtnFullScreen_Click(object sender, RoutedEventArgs e) {
            if (!isFullscreen) {
                prevState = this.WindowState;
                prevStyle = this.WindowStyle;
                prevResize = this.ResizeMode;

                this.WindowStyle = WindowStyle.None;
                this.WindowState = WindowState.Maximized;
                this.ResizeMode = ResizeMode.NoResize;
                isFullscreen = true;
                bt_fullScreen.Icon = new SymbolIcon(SymbolRegular.ArrowMinimize24);
            }
            else {
                this.WindowStyle = prevStyle;
                this.WindowState = prevState;
                this.ResizeMode = prevResize;
                isFullscreen = false;
                bt_fullScreen.Icon = new SymbolIcon(SymbolRegular.ArrowMaximize24);
            }
        }
        private async void BtnPlay_Click(object sender, RoutedEventArgs e) { 
            AsynchronousSocketListener.SendCmd(CommandType.Play); 
            VideoPlayer.Play();
        }
        private async void BtnPause_Click(object sender, RoutedEventArgs e) {
            AsynchronousSocketListener.SendCmd(CommandType.Pause);
            VideoPlayer.Pause();
        }
        private async void BtnStop_Click(object sender, RoutedEventArgs e) {
            AsynchronousSocketListener.SendCmd(CommandType.Stop);
            VideoPlayer?.Stop();
            AsynchronousSocketListener.SendCmd(CommandType.Seek, 0);
        }
        private void commands_MouseEnter(object sender, MouseEventArgs e) {
            //commands.Visibility = Visibility.Visible;
            //volumeSlider.Visibility = Visibility.Visible;
            //timelineSlider.Visibility = Visibility.Visible;
            controls.Visibility = Visibility.Visible;
        }
        private void commands_MouseLeave(object sender, MouseEventArgs e) {
            //volumeSlider.Visibility = Visibility.Hidden;
            //timelineSlider.Visibility = Visibility.Hidden;
            //commands.Visibility = Visibility.Hidden;
            controls.Visibility = Visibility.Hidden;
        }
        private void ChangeMediaVolume(object sender, RoutedPropertyChangedEventArgs<double> args) {
            VideoPlayer.Volume = (double)volumeSlider.Value;
        }
        private void timelineSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            Dispatcher.BeginInvoke(new Action(() => {
                var seekTime = timelineSlider.Value;
                AsynchronousSocketListener.SendCmd(CommandType.Seek, seekTime);
            }));
        }
    }
}