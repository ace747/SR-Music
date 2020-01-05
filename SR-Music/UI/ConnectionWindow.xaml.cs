using DCS_SR_Music.Network;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using MahApps.Metro.Controls;
using System.Windows.Input;
using Prism.Commands;
using System;
using System.Text.RegularExpressions;
using System.ComponentModel;
using NLog;
using System.Windows;
using System.Threading;
using System.Threading.Tasks;

namespace DCS_SR_Music
{
    public partial class ConnectionWindow : MetroWindow
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly DelegateCommand connectCommand;
        private Session session;
        private MainWindow MainWindow;

        public ConnectionWindow()
        {
            InitializeComponent();

            connectCommand = new DelegateCommand(InitConnection);
        }

        private string IP => EnteredIP.Text;
        private string Port => EnteredPort.Text;
        public ICommand ConnectCommand => connectCommand;

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Current.Shutdown();
        }

        private void InitConnection()
        {
            EnteredPort.IsEnabled = false;
            ConnectButton.IsEnabled = false;
            StatusLabel.Content = "";

            try
            {
                if (Port == "")
                {
                    StatusLabel.Content = "please enter a port";
                    ConnectButton.IsEnabled = true;
                    EnteredPort.IsEnabled = true;
                    return;
                }

                if (!IsPortValid())
                {
                    StatusLabel.Content = "invalid port";
                    ConnectButton.IsEnabled = true;
                    EnteredPort.IsEnabled = true;
                    return;
                }

                ConnectButton.Content = "Connecting";
                ConnectingRing.IsActive = true;

                ConnectSession();
            }

            catch (Exception)
            {
                ConnectingRing.IsActive = false;
                ConnectButton.Content = "Connect";
                ConnectButton.IsEnabled = true;
                EnteredPort.IsEnabled = true;
            }
        }

        private void ConnectSession()
        {
            try
            {
                String ip = "";
                String port = "";

                this.Dispatcher.Invoke(() =>
                {
                    ip = IP;
                    port = Port;
                });

                var ipAddress = Dns.GetHostAddresses(ip);
                var resolvedIP = ipAddress.FirstOrDefault(xa => xa.AddressFamily == AddressFamily.InterNetwork);

                session = new Session(new IPEndPoint(resolvedIP, Int32.Parse(port)));
                session.ConnectionEvent += ConnectionEvent;
                session.Connect();
            }

            catch (Exception ex)
            {
                Logger.Error(ex, "Error encountered when calling session connect from UI");
            }
        }

        public void ConnectionEvent(bool connected, string message)
        {
            try
            {
                // session connected
                if (connected)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() => {

                        MainWindow = new MainWindow();
                        Application.Current.MainWindow = MainWindow;
                        MainWindow.InitSession(session);

                        // reset client window UI for when reopened
                        ConnectingRing.IsActive = false;
                        ConnectButton.Content = "Connect";
                        ConnectButton.IsEnabled = true;
                        EnteredPort.IsEnabled = true;

                        this.Visibility = Visibility.Collapsed;
                        MainWindow.Visibility = Visibility.Visible;
                    }));
                }

                // session disconnected or failed to connect
                else
                {
                    Application.Current.Dispatcher.Invoke(new Action(() => {

                        ConnectingRing.IsActive = false;
                        StatusLabel.Content = message;
                        ConnectButton.Content = "Connect";
                        ConnectButton.IsEnabled = true;
                        EnteredPort.IsEnabled = true;

                        if (MainWindow != null)
                        {
                            MainWindow.shutdown = false;
                            MainWindow.Close();
                        }

                        Application.Current.MainWindow = this;
                        this.Visibility = Visibility.Visible;
                    }));
                }
            }

            catch (Exception ex)
            {
                Logger.Error(ex, "Error encountered when handling connection event");

                Application.Current.Shutdown();
            }
        }

        private bool IsPortValid()
        {
            try
            {
                string port = Port;

                if (string.IsNullOrEmpty(port))
                    return false;

                Regex numeric = new Regex(@"^[0-9]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                if (numeric.IsMatch(port))
                {
                    try
                    {
                        if (Convert.ToInt32(port) < 65536)
                            return true;
                    }

                    catch (OverflowException)
                    {
                        return false;
                    }
                }

                return false;
            }

            catch (Exception)
            {
                return false;
            }
        }
    }
}
