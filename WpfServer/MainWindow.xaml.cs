using System;
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
						if (!ct.IsCancellationRequested)
						{
							var myCompleteMessage = new StringBuilder();
							// Check to see if this NetworkStream is readable. 
							if (networkStream.CanRead)
							{
								byte[] readBuffer = new byte[1500];
								int numberOfBytesRead = 0;

								// Incoming message may be larger than the buffer size. 
								do
								{
									numberOfBytesRead = await networkStream.ReadAsync(readBuffer, 0, readBuffer.Length);

									// Using string as dynamic byte buffer
									myCompleteMessage.AppendFormat("{0}", Encoding.ASCII.GetString(readBuffer, 0, numberOfBytesRead));
								}
								while (networkStream.DataAvailable);

								Logger.Info("Received : " + myCompleteMessage);
							}
							else
							{
								Logger.Info("Cannot read from this NetworkStream.");
							}

							if (networkStream.CanWrite)
							{
								var writeBuffer = Encoding.ASCII.GetBytes(myCompleteMessage.ToString());
								await networkStream.WriteAsync(writeBuffer, 0, writeBuffer.Length);
								Logger.Info("Sent : " + Encoding.ASCII.GetString(writeBuffer));
							}
							else
							{
								Logger.Info("Cannot write from this NetworkStream.");
							}
						}
					}
				}
				//Communicate(client, ct);
            }
        }
		private async Task Communicate(TcpClient client, CancellationToken ct)
		{
			using (var networkStream = client.GetStream())
			{
				if (!ct.IsCancellationRequested)
				{
					var myCompleteMessage = new StringBuilder();
					// Check to see if this NetworkStream is readable. 
					if (networkStream.CanRead)
					{
						byte[] readBuffer = new byte[1500];
						int numberOfBytesRead = 0;

						// Incoming message may be larger than the buffer size. 
						do
						{
							numberOfBytesRead = await networkStream.ReadAsync(readBuffer, 0, readBuffer.Length);

							// Using string as dynamic byte buffer
							myCompleteMessage.AppendFormat("{0}", Encoding.ASCII.GetString(readBuffer, 0, numberOfBytesRead));
						}
						while (networkStream.DataAvailable);

						Logger.Info("Received : " + myCompleteMessage);
					}
					else
					{
						Logger.Info("Cannot read from this NetworkStream.");
					}

					if (networkStream.CanWrite)
					{
						var writeBuffer = Encoding.ASCII.GetBytes(myCompleteMessage.ToString());
						await networkStream.WriteAsync(writeBuffer, 0, writeBuffer.Length);
						Logger.Info("Sent : " + Encoding.ASCII.GetString(writeBuffer));						
					}
					else
					{
						Logger.Info("Cannot write from this NetworkStream.");
					}
				}
			}
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
