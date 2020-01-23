using DCS_SR_Music.Network;
using MahApps.Metro.Controls;
using NLog;
using Prism.Commands;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DCS_SR_Music
{
    public partial class MainWindow : MetroWindow
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private DelegateCommand disconnectCommand;
        private Session session;

        public bool shutdown { get; set; } = true;

        public MainWindow()
        {
            InitializeComponent();

            disconnectCommand = new DelegateCommand(DisconnectSession);
        }

        public void InitSession(Session session)
        {
            this.session = session;

            // Initialize directories to app start-up location
            Directory0.Text = session.Stations[0].Directory;
            Directory1.Text = session.Stations[1].Directory;
            Directory2.Text = session.Stations[2].Directory;
            Directory3.Text = session.Stations[3].Directory;

            // Initialize station names
            session.Stations[0].StationName = StationName0.Text;
            session.Stations[1].StationName = StationName1.Text;
            session.Stations[2].StationName = StationName2.Text;
            session.Stations[3].StationName = StationName3.Text;

            // Initialize station frequencies
            session.Stations[0].Frequency = Slider0.Value;
            session.Stations[1].Frequency = Slider1.Value;
            session.Stations[2].Frequency = Slider2.Value;
            session.Stations[3].Frequency = Slider3.Value;

            // UI stop music event
            session.Stations[0].StopMusicEvent += StopMusicEvent;
            session.Stations[1].StopMusicEvent += StopMusicEvent;
            session.Stations[2].StopMusicEvent += StopMusicEvent;
            session.Stations[3].StopMusicEvent += StopMusicEvent;

            // UI track name event
            session.Stations[0].StationMusicController.TrackNameUpdate += TrackNameUpdate;
            session.Stations[1].StationMusicController.TrackNameUpdate += TrackNameUpdate;
            session.Stations[2].StationMusicController.TrackNameUpdate += TrackNameUpdate;
            session.Stations[3].StationMusicController.TrackNameUpdate += TrackNameUpdate;

            // UI track time event
            session.Stations[0].StationMusicController.TrackTimerUpdate += TrackTimerUpdate;
            session.Stations[1].StationMusicController.TrackTimerUpdate += TrackTimerUpdate;
            session.Stations[2].StationMusicController.TrackTimerUpdate += TrackTimerUpdate;
            session.Stations[3].StationMusicController.TrackTimerUpdate += TrackTimerUpdate;

            // UI track number event
            session.Stations[0].StationMusicController.TrackNumberUpdate += TrackNumberUpdate;
            session.Stations[1].StationMusicController.TrackNumberUpdate += TrackNumberUpdate;
            session.Stations[2].StationMusicController.TrackNumberUpdate += TrackNumberUpdate;
            session.Stations[3].StationMusicController.TrackNumberUpdate += TrackNumberUpdate;
        }

        public ICommand DisconnectCommand => disconnectCommand;

        protected override void OnClosing(CancelEventArgs e)
        {
            if (shutdown)
            {
                // This should be the only place where a hard session quit/disconnect is called
                foreach (Station station in session.Stations)
                {
                    if (station.PlayingMusic)
                    {
                        station.StopMusic();
                    }

                    if (station.StationBroadcaster.IsRunning)
                    {
                        station.StationBroadcaster.Stop();
                    }
                }

                session.Quit = true;
                Task.Run(session.Disconnect).Wait();
                Logger.Info("Disconnnected from server");
                Application.Current.Shutdown();
            }

            base.OnClosing(e);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Normal)
            {
                Logger.Debug("MAIN WINDOW RESTORED");
            }
            else if (this.WindowState == System.Windows.WindowState.Minimized)
            {
                Logger.Debug("MAIN WINDOW MINIMIZED");
            }

            base.OnStateChanged(e);
        }

        private void DisconnectSession()
        {
            shutdown = false;
            session.UpdateConnectionStatus(false, "");
        }

        // //////////////////////////////////////////////////////////
        // ********* XAML Control Handlers *********
        // //////////////////////////////////////////////////////////

        private void Slider0_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (session == null || session.Stations[0] == null)
                {
                    return;
                }

                string freq = Slider0.Value.ToString("0.00");
                System.Double verifiedFreq = session.VerifyUniqueFrequency(0, double.Parse(freq));

                Slider0.Value = verifiedFreq;
                session.Stations[0].Frequency = verifiedFreq;
            }

            catch (Exception ex)
            {
                Logger.Debug(ex, "Exception encountered when setting frequency");
            }
        }

        private void Slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (session == null || session.Stations[1] == null)
                {
                    return;
                }

                string freq = Slider1.Value.ToString("0.00");
                System.Double verifiedFreq = session.VerifyUniqueFrequency(1, double.Parse(freq));

                Slider1.Value = verifiedFreq;
                session.Stations[1].Frequency = verifiedFreq;
            }

            catch (Exception ex)
            {
                Logger.Debug(ex, "Exception encountered when setting frequency");
            }
        }

        private void Slider2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (session == null || session.Stations[2] == null)
                {
                    return;
                }

                string freq = Slider2.Value.ToString("0.00");
                System.Double verifiedFreq = session.VerifyUniqueFrequency(2, double.Parse(freq));

                Slider2.Value = verifiedFreq;
                session.Stations[2].Frequency = verifiedFreq;
            }

            catch (Exception ex)
            {
                Logger.Debug(ex, "Exception encountered when setting frequency");
            }
        }

        private void Slider3_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (session == null || session.Stations[3] == null)
                {
                    return;
                }

                string freq = Slider3.Value.ToString("0.00");
                System.Double verifiedFreq = session.VerifyUniqueFrequency(3, double.Parse(freq));

                Slider3.Value = verifiedFreq;
                session.Stations[3].Frequency = verifiedFreq;
            }

            catch (Exception ex)
            {
                Logger.Debug(ex, "Exception encountered when setting frequency");
            }
        }

        private void Modulation0_IsCheckedChanged(object sender, System.EventArgs e)
        {
            bool modulationChecked = Modulation0.IsChecked.HasValue ? Modulation0.IsChecked.Value : false;

            if (!modulationChecked)
            {
                TabModulation0.Content = "AM";
                session.Stations[0].Modulation = 0;
            }

            else
            {
                TabModulation0.Content = "FM";
                session.Stations[0].Modulation = 1;
            }
        }

        private void Modulation1_IsCheckedChanged(object sender, System.EventArgs e)
        {
            bool modulationChecked = Modulation1.IsChecked.HasValue ? Modulation1.IsChecked.Value : false;

            if (!modulationChecked)
            {
                TabModulation1.Content = "AM";
                session.Stations[1].Modulation = 0;
            }

            else
            {
                TabModulation1.Content = "FM";
                session.Stations[1].Modulation = 1;
            }
        }

        private void Modulation2_IsCheckedChanged(object sender, System.EventArgs e)
        {
            bool modulationChecked = Modulation2.IsChecked.HasValue ? Modulation2.IsChecked.Value : false;

            if (!modulationChecked)
            {
                TabModulation2.Content = "AM";
                session.Stations[2].Modulation = 0;
            }

            else
            {
                TabModulation2.Content = "FM";
                session.Stations[2].Modulation = 1;
            }
        }

        private void Modulation3_IsCheckedChanged(object sender, System.EventArgs e)
        {
            bool modulationChecked = Modulation3.IsChecked.HasValue ? Modulation3.IsChecked.Value : false;

            if (!modulationChecked)
            {
                TabModulation3.Content = "AM";
                session.Stations[3].Modulation = 0;
            }

            else
            {
                TabModulation3.Content = "FM";
                session.Stations[3].Modulation = 1;
            }
        }

        private void DirectoryBrowse0_Click(object sender, RoutedEventArgs e)
        {
            DirectoryBrowse0.IsEnabled = false;

            string currentDir = Directory0.Text;

            try
            {
                var dialog = new CommonOpenFileDialog();
                dialog.InitialDirectory = session.Stations[0].Directory;
                dialog.IsFolderPicker = true;
                CommonFileDialogResult result = dialog.ShowDialog(this);

                if (result == CommonFileDialogResult.Ok)
                {
                    Directory0.Text = dialog.FileName;
                    session.Stations[0].Directory = new Uri(dialog.FileName).LocalPath;
                }
            }

            catch (Exception)
            {
                Directory0.Text = currentDir;
                session.Stations[0].Directory = new Uri(currentDir).LocalPath;
            }

            DirectoryBrowse0.IsEnabled = true;
        }

        private void DirectoryBrowse1_Click(object sender, RoutedEventArgs e)
        {
            DirectoryBrowse1.IsEnabled = false;

            string currentDir = Directory1.Text;

            try
            {
                var dialog = new CommonOpenFileDialog();
                dialog.InitialDirectory = session.Stations[1].Directory;
                dialog.IsFolderPicker = true;
                CommonFileDialogResult result = dialog.ShowDialog(this);

                if (result == CommonFileDialogResult.Ok)
                {
                    Directory1.Text = dialog.FileName;
                    session.Stations[1].Directory = new Uri(dialog.FileName).LocalPath;
                }
            }

            catch (Exception)
            {
                Directory1.Text = currentDir;
                session.Stations[1].Directory = new Uri(currentDir).LocalPath;
            }

            DirectoryBrowse1.IsEnabled = true;
        }

        private void DirectoryBrowse2_Click(object sender, RoutedEventArgs e)
        {
            DirectoryBrowse2.IsEnabled = false;

            string currentDir = Directory2.Text;

            try
            {
                var dialog = new CommonOpenFileDialog();
                dialog.InitialDirectory = session.Stations[2].Directory;
                dialog.IsFolderPicker = true;
                CommonFileDialogResult result = dialog.ShowDialog(this);

                if (result == CommonFileDialogResult.Ok)
                {
                    Directory2.Text = dialog.FileName;
                    session.Stations[2].Directory = new Uri(dialog.FileName).LocalPath;
                }
            }

            catch (Exception)
            {
                Directory2.Text = currentDir;
                session.Stations[2].Directory = new Uri(currentDir).LocalPath;
            }

            DirectoryBrowse2.IsEnabled = true;
        }

        private void DirectoryBrowse3_Click(object sender, RoutedEventArgs e)
        {
            DirectoryBrowse3.IsEnabled = false;

            string currentDir = Directory3.Text;

            try
            {
                var dialog = new CommonOpenFileDialog();
                dialog.InitialDirectory = session.Stations[3].Directory;
                dialog.IsFolderPicker = true;
                CommonFileDialogResult result = dialog.ShowDialog(this);

                if (result == CommonFileDialogResult.Ok)
                {
                    Directory3.Text = dialog.FileName;
                    session.Stations[3].Directory = new Uri(dialog.FileName).LocalPath;
                }
            }

            catch (Exception)
            {
                Directory3.Text = currentDir;
                session.Stations[3].Directory = new Uri(currentDir).LocalPath;
            }

            DirectoryBrowse3.IsEnabled = true;
        }

        private void StationName0_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                if (session == null || session.Stations[0] == null)
                {
                    return;
                }

                session.Stations[0].StationName = StationName0.Text;
            }

            catch (Exception ex)
            {
                Logger.Debug(ex, "Exception encountered when setting stationName");
            }
        }

        private void StationName1_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                if (session == null || session.Stations[1] == null)
                {
                    return;
                }

                session.Stations[1].StationName = StationName1.Text;
            }

            catch (Exception ex)
            {
                Logger.Debug(ex, "Exception encountered when setting stationName");
            }
        }

        private void StationName2_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                if (session == null || session.Stations[2] == null)
                {
                    return;
                }

                session.Stations[2].StationName = StationName2.Text;
            }

            catch (Exception ex)
            {
                Logger.Debug(ex, "Exception encountered when setting stationName");
            }
        }

        private void StationName3_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                if (session == null || session.Stations[3] == null)
                {
                    return;
                }

                session.Stations[3].StationName = StationName3.Text;
            }

            catch (Exception ex)
            {
                Logger.Debug(ex, "Exception encountered when setting stationName");
            }
        }

        private void PlayButton0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (session.Stations[0].PlayingMusic)
                {
                    PlayButton0.IsEnabled = false;
                    session.Stations[0].StopMusic();
                }

                else
                {
                    Settings0.IsEnabled = false;
                    PlayButton0.IsEnabled = false;
                    TrackLabel0.Visibility = Visibility.Visible;
                    TrackNumber0.Visibility = Visibility.Visible;
                    Timer0.Visibility = Visibility.Visible;

                    Task.Run(() => session.Stations[0].StartMusic()).Wait();
                    PlayIcon0.Kind = MahApps.Metro.IconPacks.PackIconFontAwesomeKind.StopSolid;
                    PlayStatusIcon0.Visibility = Visibility.Visible;
                    PlayButton0.IsEnabled = true;
                }
            }

            catch (Exception ex)
            {
                Logger.Debug(ex, "Exception encountered when starting/stopping music");
            }
        }

        private void PlayButton1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (session.Stations[1].PlayingMusic)
                {
                    PlayButton1.IsEnabled = false;
                    session.Stations[1].StopMusic();
                }

                else
                {
                    Settings1.IsEnabled = false;
                    PlayButton1.IsEnabled = false;
                    TrackLabel1.Visibility = Visibility.Visible;
                    TrackNumber1.Visibility = Visibility.Visible;
                    Timer1.Visibility = Visibility.Visible;

                    Task.Run(() => session.Stations[1].StartMusic()).Wait();
                    PlayIcon1.Kind = MahApps.Metro.IconPacks.PackIconFontAwesomeKind.StopSolid;
                    PlayStatusIcon1.Visibility = Visibility.Visible;
                    PlayButton1.IsEnabled = true;
                }
            }

            catch (Exception ex)
            {
                Logger.Debug(ex, "Exception encountered when starting/stopping music");
            }
        }

        private void PlayButton2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (session.Stations[2].PlayingMusic)
                {
                    PlayButton2.IsEnabled = false;
                    session.Stations[2].StopMusic();
                }

                else
                {
                    Settings2.IsEnabled = false;
                    PlayButton2.IsEnabled = false;
                    TrackLabel2.Visibility = Visibility.Visible;
                    TrackNumber2.Visibility = Visibility.Visible;
                    Timer2.Visibility = Visibility.Visible;

                    Task.Run(() => session.Stations[2].StartMusic()).Wait();
                    PlayIcon2.Kind = MahApps.Metro.IconPacks.PackIconFontAwesomeKind.StopSolid;
                    PlayStatusIcon2.Visibility = Visibility.Visible;
                    PlayButton2.IsEnabled = true;
                }
            }

            catch (Exception ex)
            {
                Logger.Debug(ex, "Exception encountered when starting/stopping music");
            }
        }

        private void PlayButton3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (session.Stations[3].PlayingMusic)
                {
                    PlayButton3.IsEnabled = false;
                    session.Stations[3].StopMusic();
                }

                else
                {
                    Settings3.IsEnabled = false;
                    PlayButton3.IsEnabled = false;
                    TrackLabel3.Visibility = Visibility.Visible;
                    TrackNumber3.Visibility = Visibility.Visible;
                    Timer3.Visibility = Visibility.Visible;

                    Task.Run(() => session.Stations[3].StartMusic()).Wait();
                    PlayIcon3.Kind = MahApps.Metro.IconPacks.PackIconFontAwesomeKind.StopSolid;
                    PlayStatusIcon3.Visibility = Visibility.Visible;
                    PlayButton3.IsEnabled = true;
                }
            }

            catch (Exception ex)
            {
                Logger.Debug(ex, "Exception encountered when starting/stopping music");
            }
        }


        private void SkipForward0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                session.Stations[0].StationMusicController.SkipForward();
            }

            catch (Exception ex)
            {
                Logger.Debug(ex, "Exception encountered when skipping track forward");
            }
        }

        private void SkipBackward0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                session.Stations[0].StationMusicController.SkipBackward();
            }

            catch (Exception ex)
            {
                Logger.Debug(ex, "Exception encountered when skipping track backward");
            }
        }

        private void SkipForward1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                session.Stations[1].StationMusicController.SkipForward();
            }

            catch (Exception ex)
            {
                Logger.Debug(ex, "Exception encountered when skipping track forward");
            }
        }

        private void SkipBackward1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                session.Stations[1].StationMusicController.SkipBackward();
            }

            catch (Exception ex)
            {
                Logger.Debug(ex, "Exception encountered when skipping track backward");
            }
        }

        private void SkipForward2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                session.Stations[2].StationMusicController.SkipForward();
            }

            catch (Exception ex)
            {
                Logger.Debug(ex, "Exception encountered when skipping track forward");
            }
        }

        private void SkipBackward2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                session.Stations[2].StationMusicController.SkipBackward();
            }

            catch (Exception ex)
            {
                Logger.Debug(ex, "Exception encountered when skipping track backward");
            }
        }

        private void SkipForward3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                session.Stations[3].StationMusicController.SkipForward();
            }

            catch (Exception ex)
            {
                Logger.Debug(ex, "Exception encountered when skipping track forward");
            }
        }

        private void SkipBackward3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                session.Stations[3].StationMusicController.SkipBackward();
            }

            catch (Exception ex)
            {
                Logger.Debug(ex, "Exception encountered when skipping track backward");
            }
        }


        private void Repeat0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool repeatValue = Repeat0.IsChecked.HasValue ? Repeat0.IsChecked.Value : false;
                session.Stations[0].StationMusicController.Repeat(repeatValue);
            }

            catch (Exception ex)
            {
                Logger.Debug(ex, "Exception encountered when toggling repeat");
            }
        }

        private void Repeat1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool repeatValue = Repeat1.IsChecked.HasValue ? Repeat1.IsChecked.Value : false;
                session.Stations[1].StationMusicController.Repeat(repeatValue);
            }

            catch (Exception ex)
            {
                Logger.Debug(ex, "Exception encountered when toggling repeat");
            }
        }

        private void Repeat2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool repeatValue = Repeat2.IsChecked.HasValue ? Repeat2.IsChecked.Value : false;
                session.Stations[2].StationMusicController.Repeat(repeatValue);
            }

            catch (Exception ex)
            {
                Logger.Debug(ex, "Exception encountered when toggling repeat");
            }
        }

        private void Repeat3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool repeatValue = Repeat3.IsChecked.HasValue ? Repeat3.IsChecked.Value : false;
                session.Stations[3].StationMusicController.Repeat(repeatValue);
            }

            catch (Exception ex)
            {
                Logger.Debug(ex, "Exception encountered when toggling repeat");
            }
        }

        private void resetPlayerLabels(int station)
        {
            try
            {
                switch (station)
                {
                    case 0:
                        TrackTitle0.Content = "";
                        TrackNumber0.Content = "";
                        Timer0.Content = "00:00";
                        break;

                    case 1:
                        TrackTitle1.Content = "";
                        TrackNumber1.Content = "";
                        Timer1.Content = "00:00";
                        break;

                    case 2:
                        TrackTitle2.Content = "";
                        TrackNumber2.Content = "";
                        Timer2.Content = "00:00";
                        break;

                    // station 3
                    default:
                        TrackTitle3.Content = "";
                        TrackNumber3.Content = "";
                        Timer3.Content = "00:00";
                        break;
                }
            }

            catch (Exception ex)
            {
                Logger.Error(ex, "Error encountered when handling music event");
            }
        }

        public void StopMusicEvent(int station)
        {
            try
            {
                switch (station)
                {
                    case 0:
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                            PlayButton0.IsEnabled = false;
                            TrackLabel0.Visibility = Visibility.Collapsed;
                            TrackNumber0.Visibility = Visibility.Collapsed;
                            Timer0.Visibility = Visibility.Collapsed;

                            resetPlayerLabels(0);

                            PlayIcon0.Kind = MahApps.Metro.IconPacks.PackIconFontAwesomeKind.PlaySolid;
                            PlayStatusIcon0.Visibility = Visibility.Hidden;
                            Settings0.IsEnabled = true;
                            PlayButton0.IsEnabled = true;
                        }));
                        break;

                    case 1:
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                            PlayButton1.IsEnabled = false;
                            TrackLabel1.Visibility = Visibility.Collapsed;
                            TrackNumber1.Visibility = Visibility.Collapsed;
                            Timer1.Visibility = Visibility.Collapsed;

                            resetPlayerLabels(1);

                            PlayIcon1.Kind = MahApps.Metro.IconPacks.PackIconFontAwesomeKind.PlaySolid;
                            PlayStatusIcon1.Visibility = Visibility.Hidden;
                            Settings1.IsEnabled = true;
                            PlayButton1.IsEnabled = true;
                        }));
                        break;

                    case 2:
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                            PlayButton2.IsEnabled = false;
                            TrackLabel2.Visibility = Visibility.Collapsed;
                            TrackNumber2.Visibility = Visibility.Collapsed;
                            Timer2.Visibility = Visibility.Collapsed;

                            resetPlayerLabels(2);

                            PlayIcon2.Kind = MahApps.Metro.IconPacks.PackIconFontAwesomeKind.PlaySolid;
                            PlayStatusIcon2.Visibility = Visibility.Hidden;
                            Settings2.IsEnabled = true;
                            PlayButton2.IsEnabled = true;
                        }));
                        break;

                    // station 3
                    default:
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                            PlayButton3.IsEnabled = false;
                            TrackLabel3.Visibility = Visibility.Collapsed;
                            TrackNumber3.Visibility = Visibility.Collapsed;
                            Timer3.Visibility = Visibility.Collapsed;

                            resetPlayerLabels(3);

                            PlayIcon3.Kind = MahApps.Metro.IconPacks.PackIconFontAwesomeKind.PlaySolid;
                            PlayStatusIcon3.Visibility = Visibility.Hidden;
                            Settings3.IsEnabled = true;
                            PlayButton3.IsEnabled = true;
                        }));
                        break;
                }
            }

            catch (Exception ex)
            {
                Logger.Error(ex, "Error encountered when handling music event");
            }
        }

        public void TrackNameUpdate(int station, string name)
        {
            try
            {
                switch (station)
                {
                    case 0:
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                            TrackTitle0.Content = name;
                        }));
                        break;

                    case 1:
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                            TrackTitle1.Content = name;
                        }));
                        break;

                    case 2:
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                            TrackTitle2.Content = name;
                        }));
                        break;

                    // station 3
                    default:
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                            TrackTitle3.Content = name;
                        }));
                        break;
                }
            }

            catch (Exception ex)
            {
                Logger.Error(ex, "Error encountered when updating track name");
            }
        }

        public void TrackTimerUpdate(int station, string time)
        {
            try
            {
                switch (station)
                {
                    case 0:
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                            Timer0.Content = time;
                        }));
                        break;

                    case 1:
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                            Timer1.Content = time;
                        }));
                        break;

                    case 2:
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                            Timer2.Content = time;
                        }));
                        break;

                    // station 3
                    default:
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                            Timer3.Content = time;
                        }));
                        break;
                }
            }

            catch (Exception ex)
            {
                Logger.Error(ex, "Error encountered when updating track name");
            }
        }

        public void TrackNumberUpdate(int station, string trackNumberLabel)
        {
            try
            {
                switch (station)
                {
                    case 0:
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                            TrackNumber0.Content = trackNumberLabel;
                        }));
                        break;

                    case 1:
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                            TrackNumber1.Content = trackNumberLabel;
                        }));
                        break;

                    case 2:
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                            TrackNumber2.Content = trackNumberLabel;
                        }));
                        break;

                    // station 3
                    default:
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                            TrackNumber3.Content = trackNumberLabel;
                        }));
                        break;
                }
            }

            catch (Exception ex)
            {
                Logger.Error(ex, "Error encountered when updating track number");
            }
        }
    }
}
