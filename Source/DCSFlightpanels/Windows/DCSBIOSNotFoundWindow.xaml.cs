﻿namespace DCSFlightpanels.Windows
{
    using System;
    using System.Windows;

    using ClassLibraryCommon;

    /// <summary>
    /// Interaction logic for DCSBIOSNotFoundWindow.xaml
    /// </summary>
    public partial class DCSBIOSNotFoundWindow : Window
    {
        private string _dcsbiosLocation;

        public DCSBIOSNotFoundWindow(string dcsbiosLocation)
        {
            InitializeComponent();
            _dcsbiosLocation = dcsbiosLocation;
        }

        private void ButtonClose_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Close();
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        private void DCSBIOSNotFoundWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                RunSetting.Text = _dcsbiosLocation;
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }
    }
}
