using System;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using DCSFlightpanels.Properties;
using ClassLibraryCommon;

namespace DCSFlightpanels
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex _singletoneMutex;
        private System.Windows.Forms.NotifyIcon _notifyIcon = null;
        private ServiceCore _serviceCore = null;

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
                Icon = DCSFlightpanels.Properties.Resources.flightpanels02_8Rc_icon,
                Visible = true,
                ContextMenu = notifyIconContextMenu
            };
            _notifyIcon.DoubleClick += new EventHandler(NotifyIcon_Show);

        }

        private void NotifyIcon_Show(object sender, EventArgs args)
        {
            _serviceCore.ShowMainWindow();
//            MainWindow?.Show();
//            if (MainWindow != null)
//            {
//                MainWindow.WindowState = WindowState.Normal;
//            }
        }

        private void NotifyIcon_Quit(object sender, EventArgs args)
        {
            //MainWindow?.Close();
        }

        protected override void OnStartup(StartupEventArgs e)
        {

            //Startup sequince:
            //Check app paths and create log file
            //Forcely set FPPath to portable mode
            FPPaths.SetPortable(true);
            //Set max log output
            Logger.SetLogLevel(Logger.ELogLevel.elDebug);
            Logger.Debug("App dir: " + FPPaths.GetApplicationPath());
            Logger.Debug("User dir: " + FPPaths.GetUserDataPath());



            //Read command line arguments
            bool hasProfileName = false;
            foreach (string arg in e.Args)
            {
                if (arg.ToLower().Contains(Constants.CommandLineArgumentStartMinimized.ToLower()))
                {
                    Settings.Default.RunMinimized = true;
                }
                else if (arg.ToLower().Contains(Constants.CommandLineArgumentOpenProfile.ToLower()))
                {
                    //Example: DCSFlightpanels.exe -OpenProfile="C:\Users\User\Documents\Spitfire_Saitek_DCS_Profile.bindings"
                    if (arg.Contains("NEWPROFILE"))
                    {
                        Settings.Default.LastProfileFileUsed = string.Empty;
                    }
                    else
                    {
                        Settings.Default.LastProfileFileUsed = arg.ToLower().Replace("\"", string.Empty).Replace("'", string.Empty).Replace(Constants.CommandLineArgumentOpenProfile.ToLower(), string.Empty);
                        hasProfileName = true;
                    }
                    //closeCurrentInstance = true;
                }
                else if (arg.ToLower().Contains(Constants.CommandLineArgumentNoStreamDeck.ToLower()))
                {
                    Settings.Default.LoadStreamDeck = false;
                }
            }

            //Check if we alone
            if (CheckIfAnotherInstance())
            {
                //TODO: send another process message to showing up
                if (hasProfileName)
                {
                    //TODO: send profile to running app for apply
                }
                Shutdown(10);
                return;
            }

            //Create tray icon
            InitNotificationIcon();

            //Creating and push the core of app
            _serviceCore = new ServiceCore();
            _serviceCore.Init();

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            //Releasing Mutex without checks, because only we can use it.
            _singletoneMutex.ReleaseMutex();

            if (_serviceCore != null)
            {
                //_serviceCore.Dispose();
            }

            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
            }

            base.OnExit(e);
        }

        protected static bool CheckIfAnotherInstance()
        {
            // get application GUID as defined in AssemblyInfo.cs
            string appGuid = "{23DB8D4F-D76E-4DF4-B04F-4F4EB0A8E992}";

            // unique id for global mutex - Global prefix means it is global to the machine
            string mutexId = "Global\\" + appGuid;

            // Need a place to store a return value in Mutex() constructor call
            MutexAccessRule allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
            MutexSecurity securitySettings = new MutexSecurity();
            securitySettings.AddAccessRule(allowEveryoneRule);
            _singletoneMutex = new Mutex(false, mutexId, out _, securitySettings);
            bool acquired = false;
            try
            {
                //Try to acquire mutex. If can, then we alone.
                acquired = _singletoneMutex.WaitOne(10, false);
            }
            catch (AbandonedMutexException)
            {
                //Mutex was abandoned by previous us.
            }
            //If we can't aquire mutex, then another copy of us present
            return !acquired;
        }
    }
}
