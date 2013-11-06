using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

namespace WpfServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<string> _messages = new ObservableCollection<string>();
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private const string TargetName = "memoryex";

	    private CancellationTokenSource _cts;
		private static TcpListener _listener;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindowLoaded;

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
            Logger.Info("Press start button to start server");
        }

        public ObservableCollection<string> IncomingMessages
        {
            get { return _messages; }
            private set { _messages = value; }
        }

        private void startServer_Click(object sender, RoutedEventArgs e)
        {
	        IPAddress ip;
	        int port;

            if (anyIP.IsChecked == true)
            {
	            ip = IPAddress.Any;
            }
            else
            {
				//TODO try/catch Parse or TryParse
				ip = IPAddress.Parse(serverIP.Text);
            }

	        port = Int32.Parse(serverPort.Text);
			Logger.Info("Ip Address : " + ip.ToString() + " Port : " + port);

			StartServer(ip, port);
        }

	    private void StartServer(IPAddress ip, int port)
	    {
		    _cts = new CancellationTokenSource();
		    _listener = new TcpListener(ip, port);
			try
			{
				_listener.Start();
				Logger.Info("Listening");
				HandleConnectionAsync(_listener, _cts.Token);

			}
			catch (Exception e)
			{
				Logger.Info(e.Message);
			}
		}

        private async Task HandleConnectionAsync(TcpListener tcpListener, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
				//TODO catch Socket exception
	            using (var client = await tcpListener.AcceptTcpClientAsync())
	            {
					//TODO provide connected client details
					Logger.Info("Client connected");

					using (var networkStream = client.GetStream())
					{
						if (ct.IsCancellationRequested) continue;
						var accumBuffer = new byte[] { };

						// Check to see if this NetworkStream is readable. 
						if (networkStream.CanRead)
						{
							var readBuffer = new byte[1500];

							// Incoming message may be larger than the buffer size. 
							do
							{
								var numberOfBytesRead = await networkStream.ReadAsync(readBuffer, 0, readBuffer.Length, ct);
								accumBuffer = Combine(accumBuffer, readBuffer, numberOfBytesRead);
							}
							while (networkStream.DataAvailable);

							Logger.Info("Received : " + Encoding.ASCII.GetString(accumBuffer));
						}
						else
						{
							Logger.Info("Cannot read from this NetworkStream.");
						}

						if (networkStream.CanWrite)
						{
							await networkStream.WriteAsync(accumBuffer, 0, accumBuffer.Length, ct);
							Logger.Info("Sent : " + Encoding.ASCII.GetString(accumBuffer));
						}
						else
						{
							Logger.Info("Cannot write from this NetworkStream.");
						}
					}
				}
            }
        }

		public static byte[] Combine(byte[] first, byte[] second)
		{
			var ret = new byte[first.Length + second.Length];
			Buffer.BlockCopy(first, 0, ret, 0, first.Length);
			Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
			return ret;
		}

		public static byte[] Combine(byte[] first, byte[] second, int secondLength)
		{
			var ret = new byte[first.Length + secondLength];
			Buffer.BlockCopy(first, 0, ret, 0, first.Length);
			Buffer.BlockCopy(second, 0, ret, first.Length, secondLength);
			return ret;
		}

		private void stopServer_Click(object sender, RoutedEventArgs e)
        {
            _cts.Cancel();
	        _cts.Dispose();
            _listener.Stop();
            Logger.Info("Stopped listening");
        }

        private void anyIP_Checked(object sender, RoutedEventArgs e)
        {
            if (serverIPConfig != null) serverIPConfig.IsEnabled = false;
        }

        private void anyIP_Unchecked(object sender, RoutedEventArgs e)
        {
            if (serverIPConfig != null) serverIPConfig.IsEnabled = true;
        }
    }
}
