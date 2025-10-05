using Microsoft.Win32;
using StreamingVideo.Common;
using System.Net;
using System.Windows;


namespace StreamingVideo.Server {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private readonly SimpleVideoServer _server;
        private readonly MyStreamingSocket SocketServer = new MyStreamingSocket();
        public MainWindow() {
            InitializeComponent();
            _server = new SimpleVideoServer();
            StatusText.Text = "Start Server";
            SocketServer.StartSocket(SocketTypes.Server, new IPAddress([127, 0, 0, 1]));
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e) {
            SocketServer.Dispose();
        }

        private void StartServer_Click(object sender, RoutedEventArgs e) {
            try {
                _server.SetPathVideo(Properties.Instance.FilePath);
                _server.Start(8080);
                StatusText.Text = "Server avviato";
            }
            catch { }
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Video files (*.mp4;*.avi)|*.mp4;*.avi|All files (*.*)|*.*";
            openFileDialog.Title = "Select Video";

            if (openFileDialog.ShowDialog() == true) {
                string selectedFile = openFileDialog.FileName;
                Properties.Instance.FilePath = selectedFile;
            }
        }
        private void Button_UpdateDynu(object sender, RoutedEventArgs e) {
            string resp = DynuDns.Update(
                Properties.Instance.DynuHostname,
                Properties.Instance.DynuUsername,
                Properties.Instance.DynuPassword);
            tb_dynu.Text = resp;
        }

        private void Button_Save(object sender, RoutedEventArgs e) {
            Properties.Instance.Save();
        }
    }
}