using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
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
using NLog;

namespace WpfClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<string> _messages = new ObservableCollection<string>();
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private const string TargetName = "memoryex";



        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindowLoaded;
            //SizeChanged +=  MainWindowSizeChanged;

            var target =
                LogManager.Configuration.AllTargets
                .Where(x => x.Name == TargetName)
                .Single() as MemoryTargetEx;

            if (target != null)
                target.Messages.Subscribe(msg => _messages.Add(msg));
        }

        void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            IncomingMessages = _messages;
            Logger.Info("Window is loaded");
            Logger.Info("These messages are logged in ..\\..\\Logs\\NLogSamples.log as well.");
            //Logger.Info("I will log when I am resized");
        }

        public ObservableCollection<string> IncomingMessages
        {
            get { return _messages; }
            private set { _messages = value; }
        }

        private void connect_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            IPAddress ipAddress;
            int port;

            //TODO Check if ip address is valid
            ipAddress = IPAddress.Parse(serverIP.Text);
            //TODO port range is 0-65000
            port = int.Parse(serverPort.Text);

            StartClient(ipAddress, port);
        }

        private static async void StartClient(IPAddress serverIpAddress, int port)
        {
            var client = new TcpClient();
            //can i try/catch to catch await exception?
            try
            {
                await client.ConnectAsync(serverIpAddress, port);
            }
            catch (Exception e)
            {
                Logger.Info(e);                
            }
            Logger.Info("Connected to server");
            using (var networkStream = client.GetStream())
            using (var writer = new StreamWriter(networkStream))
            using (var reader = new StreamReader(networkStream))
            {
                writer.AutoFlush = true;
                for (int i = 0; i < 10; i++)
                {
                    Logger.Info("Writing to server");
                    await writer.WriteLineAsync(DateTime.Now.ToLongDateString());
                    Logger.Info("Reading from server");
                    var dataFromServer = await reader.ReadLineAsync();
                    if (!string.IsNullOrEmpty(dataFromServer))
                    {
                        Logger.Info(dataFromServer);
                    }

                }
            }
            if (client != null)
            {
                client.Close();
                Logger.Info("Connection closed");
            }

        }
    }
}
