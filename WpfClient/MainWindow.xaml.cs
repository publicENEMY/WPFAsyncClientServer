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

		private static async Task StartClient(IPAddress serverIpAddress, int port)
		{
			using (var client = new TcpClient())
			{
				try
				{
					await client.ConnectAsync(serverIpAddress, port);
					Logger.Info("Connected to server");
					using (var networkStream = client.GetStream())
					{
						string message = DateTime.Now.ToLongDateString();

						if (networkStream.CanWrite)
						{
							var writeBuffer = Encoding.ASCII.GetBytes(message);
							await networkStream.WriteAsync(writeBuffer, 0, writeBuffer.Length);
							Logger.Info("Sent : " + Encoding.ASCII.GetString(writeBuffer));
						}
						else
						{
							Logger.Info("Cannot write from this NetworkStream.");
						}

						var accumBuffer = new byte[] { };
						// Check to see if this NetworkStream is readable. 
						if (networkStream.CanRead)
						{
							var readBuffer = new byte[1500];

							// Incoming message may be larger than the buffer size. 
							do
							{
								var numberOfBytesRead = await networkStream.ReadAsync(readBuffer, 0, readBuffer.Length);
								accumBuffer = Combine(accumBuffer, readBuffer, numberOfBytesRead);
							}
							while (networkStream.DataAvailable);

							Logger.Info("Received : " + Encoding.ASCII.GetString(accumBuffer));
						}
						else
						{
							Logger.Info("Cannot read from this NetworkStream.");
						}
					}
				}
				catch (Exception e)
				{
					Logger.Info(e);
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

	}
}
