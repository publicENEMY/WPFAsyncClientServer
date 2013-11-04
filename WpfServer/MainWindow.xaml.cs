using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        CancellationTokenSource cts = new CancellationTokenSource();
        TcpListener listener;

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
            //Logger.Info("I will log when I am resized");
        }

        public ObservableCollection<string> IncomingMessages
        {
            get { return _messages; }
            private set { _messages = value; }
        }

        private void startServer_Click(object sender, RoutedEventArgs e)
        {
            if (anyIP.IsChecked == true)
            {
                listener = new TcpListener(IPAddress.Any, Int32.Parse(serverPort.Text));
                Logger.Info("Ip Address : " + IPAddress.Any + " Port : " + serverPort.Text);
            }
            else
            {
                listener = new TcpListener(IPAddress.Parse(serverIP.Text), Int32.Parse(serverPort.Text));
                Logger.Info("Ip Address : " + serverIP.Text + " Port : " + serverPort.Text);
            }
            try
            {
                listener.Start();
                Logger.Info("Listening");
                HandleConnectionAsync(listener, cts.Token);
            }
            catch (Exception exception)
            {
                Logger.Info(exception);
            }
            //finally
            //{
                //cts.Cancel();
                //listener.Stop();
                //Logger.Info("Stop listening");
            //}

            //cts.Cancel();
        }

        async Task HandleConnectionAsync(TcpListener listener, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                Logger.Info("Accepting client");
                //TcpClient client = await listener.AcceptTcpClientAsync();
                TcpClient client = await listener.AcceptTcpClientAsync();
                Logger.Info("Client accepted");
                EchoAsync(client, ct);
            }

        }

        async Task EchoAsync(TcpClient client, CancellationToken ct)
        {
            var buf = new byte[4096];
            var stream = client.GetStream();
            while (!ct.IsCancellationRequested)
            {
                var amountRead = await stream.ReadAsync(buf, 0, buf.Length, ct);
                Logger.Info("Receive " + stream.ToString());
                if (amountRead == 0) break; //end of stream.
                await stream.WriteAsync(buf, 0, amountRead, ct);
                Logger.Info("Echo to client");
            }
        }

        private void stopServer_Click(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
            listener.Stop();
            Logger.Info("Stop listening");
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
