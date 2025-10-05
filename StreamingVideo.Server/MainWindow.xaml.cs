using Microsoft.Win32;
using StreamingVideo.Common;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace StreamingVideo {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private SimpleVideoServer _server;

        public MainWindow() {
            InitializeComponent();
            _server = new SimpleVideoServer();
            StatusText.Text = "Start Server";
            Task.Run(() => {
                AsynchronousSocketListener.StartSocket(SocketTypes.Server, new IPAddress([127, 0, 0, 1]));
            });
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