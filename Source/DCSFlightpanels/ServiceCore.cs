using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DCSFlightpanels.Properties;
using NonVisuals;

namespace DCSFlightpanels
{
    internal class ServiceCore
    {
        private HIDHandler _hidHandler;

        public bool Init()
        {

            //Initializing HidHandler
            _hidHandler = new HIDHandler();



            return true;
            Application.Current.MainWindow = new MainWindow();
            if (Settings.Default.RunMinimized == false)
            {
                Application.Current.MainWindow.Show();
            }

            return true;
        }

        public void Shutdown()
        {
            Application.Current.Shutdown(0);
        }

        public void ShowMainWindow()
        {
            Application.Current.MainWindow.Show();
        }

    }
}
