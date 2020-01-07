using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

namespace DCS_SR_Music
{
    public partial class App : Application
    {
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        private bool loggingReady = false;

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);

            var location = AppDomain.CurrentDomain.BaseDirectory;

            SetupLogging();


            if (IsAppRunning())
            {
                Environment.Exit(0);
                return;
            }

            InitNotificationIcon();
        }

        private bool IsAppRunning()
        {
            Process currentProcess = Process.GetCurrentProcess();
            string currentProcessName = currentProcess.ProcessName.ToLower().Trim();

            foreach (Process clsProcess in Process.GetProcesses())
            {
                if (clsProcess.Id != currentProcess.Id &&
                    clsProcess.ProcessName.ToLower().Trim() == currentProcessName)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetupLogging()
        {
            string logPath = new Uri(AppDomain.CurrentDomain.BaseDirectory + "logs").LocalPath;

            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory + "logs";
            string logFilePath = Path.Combine(baseDirectory, "log.txt");
            string oldLogFilePath = Path.Combine(baseDirectory, "log.old.txt");

            FileInfo logFileInfo = new FileInfo(logFilePath);

            // Cleanup logfile if > 100MB, keep one old file copy
            if (logFileInfo.Exists && logFileInfo.Length >= 104857600)
            {
                if (File.Exists(oldLogFilePath))
                {
                    try
                    {
                        File.Delete(oldLogFilePath);
                    }

                    catch (Exception) { }
                }

                try
                {
                    File.Move(logFilePath, oldLogFilePath);
                }

                catch (Exception) { }
            }

            var config = new LoggingConfiguration();

            var fileTarget = new FileTarget();
            config.AddTarget("file", fileTarget);

            fileTarget.FileName = "${basedir}/logs/log.txt";
            fileTarget.Layout = @"${longdate} | ${logger} | ${message} ${exception:format=toString,Data:maxInnerExceptionLevel=1}";

#if DEBUG
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, fileTarget));
#else
            config.LoggingRules.Add( new LoggingRule("*", LogLevel.Info, fileTarget));
#endif

            LogManager.Configuration = config;

            loggingReady = true;
        }


        private void InitNotificationIcon()
        {
            System.Windows.Forms.MenuItem notifyIconContextMenuShow = new System.Windows.Forms.MenuItem
            {
                Index = 0,
                Text = "Show"
            };
            notifyIconContextMenuShow.Click += new EventHandler(NotifyIcon_Show);

            System.Windows.Forms.MenuItem notifyIconContextMenuQuit = new System.Windows.Forms.MenuItem
            {
                Index = 1,
                Text = "Quit"
            };
            notifyIconContextMenuQuit.Click += new EventHandler(NotifyIcon_Quit);

            System.Windows.Forms.ContextMenu notifyIconContextMenu = new System.Windows.Forms.ContextMenu();
            notifyIconContextMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] { notifyIconContextMenuShow, notifyIconContextMenuQuit });

            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Visible = true
            };
            _notifyIcon.ContextMenu = notifyIconContextMenu;
            _notifyIcon.DoubleClick += new EventHandler(NotifyIcon_Show);
        }

        private void NotifyIcon_Show(object sender, EventArgs args)
        {
            MainWindow.Show();
            MainWindow.WindowState = WindowState.Normal;
        }

        private void NotifyIcon_Quit(object sender, EventArgs args)
        {
            MainWindow.Close();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon.Visible = false;
            base.OnExit(e);

            // Brutal exit, but necessary to resolve taskcancelledexception in XAML debug mode
            // Environment.Exit(0);
        }

        private void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            if (loggingReady)
            {
                Logger logger = LogManager.GetCurrentClassLogger();
                logger.Error((Exception)e.ExceptionObject, "Received unhandled exception, {0}", e.IsTerminating ? "exiting" : "continuing");
            }
        }
    }
}
