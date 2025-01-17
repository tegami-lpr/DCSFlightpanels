﻿namespace DCSFlightpanels.PanelUserControls.StreamDeck
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    using ClassLibraryCommon;

    using DCSFlightpanels.Bills;
    using DCSFlightpanels.CustomControls;
    using DCSFlightpanels.Shared;

    using MEF;

    using NonVisuals.Interfaces;
    using NonVisuals.StreamDeck;
    using NonVisuals.StreamDeck.Events;

    public abstract class UserControlStreamDeckUIBase : UserControl, IIsDirty, INvStreamDeckListener, IStreamDeckConfigListener, IOledImageListener
    {
        protected readonly List<StreamDeckImage> ButtonImages = new List<StreamDeckImage>();
        protected readonly List<System.Windows.Controls.Image> DotImages = new List<System.Windows.Controls.Image>();
        protected bool UserControlLoaded;
        protected StreamDeckPanel _streamDeckPanel;

        private string _lastShownLayer = string.Empty;

        protected virtual void SetFormState() { }

        protected virtual int ButtonAmount()
        {
            return 0;
        }


        protected UserControlStreamDeckUIBase(StreamDeckPanel streamDeckPanel)
        {
            _streamDeckPanel = streamDeckPanel;
        }


        protected void ButtonImage_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var image = (StreamDeckImage)sender;

                SetSelectedButtonUIOnly(image.Bill.StreamDeckButtonName);

                if (image.IsSelected)
                {
                    StreamDeckPanelInstance.SelectedButtonName = image.Bill.Button.StreamDeckButtonName;
                    image.Focus();
                }
                else
                {
                    StreamDeckPanelInstance.SelectedButtonName = EnumStreamDeckButtonNames.BUTTON0_NO_BUTTON;
                }

                /*Debug.WriteLine(StreamDeckPanelInstance.GetLayerHandlerInformation());
                Debug.WriteLine(StreamDeckPanelInstance.GetConfigurationInformation());
                Debug.WriteLine(EventHandlers.GetInformation());*/
                SetFormState();
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(ex);
            }
        }

        protected void ButtonImage_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (SelectedButtonName == EnumStreamDeckButtonNames.BUTTON0_NO_BUTTON)
                {
                    return;
                }

                var newlySelectedImage = (StreamDeckImage)sender;

                /*
                 * Here we must check if event if we can change the button that is selected. If there are unsaved configurations we can't
                 */
                if (newlySelectedImage.Bill.Button != _streamDeckPanel.SelectedButton && EventHandlers.AreThereDirtyListeners(this))
                {
                    if (CommonUI.DoDiscardAfterMessage(true, "Discard Changes to " + SelectedButtonName + " ?"))
                    {
                        SetFormState();
                    }
                    else
                    {
                        e.Handled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(ex);
            }
        }

        private void UIShowLayer(string layerName)
        {
            try
            {
                var selectedButton = StreamDeckPanelInstance.SelectedButtonName;

                UpdateButtonInfoFromSource();

                SetSelectedButtonUIOnly(selectedButton);
                SetFormState();
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(ex);
            }
        }

        private void UpdateButtonInfoFromSource()
        {
            HideAllDotImages();

            foreach (var buttonImage in ButtonImages)
            {
                buttonImage.Bill.Clear();

                var streamDeckButton = StreamDeckPanelInstance.SelectedLayer.GetStreamDeckButton(buttonImage.Bill.StreamDeckButtonName);

                buttonImage.Bill.Button = streamDeckButton;

                if (streamDeckButton.HasConfig)
                {
                    SetDotImageStatus(true, StreamDeckCommon.ButtonNumber(streamDeckButton.StreamDeckButtonName));
                }
            }
        }

        private void UnSelect()
        {
            try
            {
                SetSelectedButtonUIOnly(EnumStreamDeckButtonNames.BUTTON0_NO_BUTTON);
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(ex);
            }
        }

        public void Clear()
        {

            HideAllDotImages();

            foreach (var buttonImage in ButtonImages)
            {
                buttonImage.Bill.Clear();
            }
        }

        protected void SetContextMenus()
        {
            /*
                <ContextMenu x:Key="ButtonContextMenu" Opened="ButtonContextMenu_OnOpened" >
                    <MenuItem Name="MenuItemCopy" Header="Copy" Click="ButtonContextMenuCopy_OnClick"/>
                    <MenuItem Name="MenuItemPaste" Header="Paste" Click="ButtonContextMenuPaste_OnClick"/>
                </ContextMenu>
             */
            var contextMenu = new ContextMenu();
            contextMenu.Name = "ButtonContextMenu";
            contextMenu.Opened += ButtonContextMenu_OnOpened;

            var menuItem = new MenuItem();
            menuItem.Name = "MenuItemCopy";
            menuItem.Header = "Copy";
            menuItem.Click += ButtonContextMenuCopy_OnClick;
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Name = "MenuItemPaste";
            menuItem.Header = "Paste";
            menuItem.Click += ButtonContextMenuPaste_OnClick;
            contextMenu.Items.Add(menuItem);

            contextMenu.Items.Add(new Separator());

            menuItem = new MenuItem();
            menuItem.Name = "MenuItemDelete";
            menuItem.Header = "Delete";
            menuItem.Click += ButtonContextMenuDelete_OnClick;
            contextMenu.Items.Add(menuItem);

            foreach (var streamDeckImage in ButtonImages)
            {
                streamDeckImage.ContextMenu = contextMenu;
            }
        }


        private void ButtonContextMenuCopy_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Copy();
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(ex);
            }
        }

        private void ButtonContextMenuPaste_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Paste();
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(ex);
            }
        }

        private void ButtonContextMenuDelete_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var streamDeckButton = _streamDeckPanel.SelectedLayer.GetStreamDeckButton(SelectedButtonName);
                if (MessageBox.Show("Delete button" + streamDeckButton.StreamDeckButtonName.ToString() + "?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _streamDeckPanel.SelectedLayer.RemoveButton(streamDeckButton);
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(ex);
            }
        }

        private void ButtonContextMenu_OnOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                /*
                 * Can't get context menu [ContextMenuOpening] events to work. Workaround.
                 */
                var contextMenu = (ContextMenu)sender;
                MenuItem menuItemCopy = null;
                MenuItem menuItemPaste = null;
                MenuItem menuItemDelete = null;

                foreach (var contextMenuItem in contextMenu.Items)
                {
                    if (contextMenuItem.GetType() == typeof(MenuItem) && ((MenuItem)contextMenuItem).Name == "MenuItemCopy")
                    {
                        menuItemCopy = ((MenuItem)contextMenuItem);
                    }
                }
                foreach (var contextMenuItem in contextMenu.Items)
                {
                    if (contextMenuItem.GetType() == typeof(MenuItem) && ((MenuItem)contextMenuItem).Name == "MenuItemPaste")
                    {
                        menuItemPaste = (MenuItem)contextMenuItem;
                    }
                }
                foreach (var contextMenuItem in contextMenu.Items)
                {
                    if (contextMenuItem.GetType() == typeof(MenuItem) && ((MenuItem)contextMenuItem).Name == "MenuItemPaste")
                    {
                        menuItemDelete = (MenuItem)contextMenuItem;
                    }
                }

                if (menuItemCopy == null || menuItemPaste == null || menuItemDelete == null)
                {
                    return;
                }
                var selectedStreamDeckButton = _streamDeckPanel.SelectedLayer.GetStreamDeckButton(SelectedButtonName);
                menuItemCopy.IsEnabled = selectedStreamDeckButton.HasConfig;
                menuItemDelete.IsEnabled = selectedStreamDeckButton.HasConfig;

                var dataObject = Clipboard.GetDataObject();
                menuItemPaste.IsEnabled = dataObject != null && dataObject.GetDataPresent("NonVisuals.StreamDeck.StreamDeckButton");
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(ex);
            }
        }

        protected void SetDotImageStatus(bool show, int number, bool allOthersNegated = false)
        {

            foreach (var dotImage in DotImages)
            {
                if (allOthersNegated)
                {
                    dotImage.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
                }

                if (dotImage.Name == "DotImage" + number)
                {
                    dotImage.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        public void HideAllDotImages()
        {
            foreach (var dotImage in DotImages)
            {
                dotImage.Visibility = Visibility.Collapsed;
            }
        }


        public StreamDeckPanel StreamDeckPanelInstance
        {
            get => _streamDeckPanel;
            set => _streamDeckPanel = value;
        }

        protected void ShowGraphicConfiguration()
        {
            try
            {
                UIShowLayer(StreamDeckPanelInstance.HomeLayer.Name);
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(ex);
            }
        }

        protected void SetSelectedButtonUIOnly(EnumStreamDeckButtonNames selectedButtonName)
        {
            foreach (var buttonImage in ButtonImages)
            {
                try
                {
                    if (selectedButtonName == buttonImage.Bill.StreamDeckButtonName)
                    {
                        buttonImage.Source = buttonImage.Bill.SelectedImage;
                        buttonImage.IsSelected = true;
                    }
                    else
                    {
                        buttonImage.Source = buttonImage.Bill.DeselectedImage;
                        buttonImage.IsSelected = false;
                    }
                }
                catch (Exception ex)
                {
                    Common.ShowErrorMessageBox(ex);
                }
            }
        }

        public void SetIsDirty()
        {
            IsDirty = true;
        }

        public bool IsDirty { get; set; }

        public void StateSaved()
        {
            IsDirty = false;
        }

        private EnumStreamDeckButtonNames SelectedButtonName
        {
            get
            {
                if (SelectedImageBill == null)
                {
                    return EnumStreamDeckButtonNames.BUTTON0_NO_BUTTON;
                }
                return SelectedImageBill.StreamDeckButtonName;
            }
        }

        private BillStreamDeckFace SelectedImageBill => (from image in ButtonImages where image.IsSelected select image.Bill).FirstOrDefault();

        protected void SetImageBills()
        {
            foreach (var buttonImage in ButtonImages)
            {
                if (buttonImage.Bill != null)
                {
                    continue;
                }
                buttonImage.Bill = new BillStreamDeckFace();
                buttonImage.Bill.StreamDeckButtonName = (EnumStreamDeckButtonNames)Enum.Parse(typeof(EnumStreamDeckButtonNames), "BUTTON" + buttonImage.Name.Replace("ButtonImage", string.Empty));
                buttonImage.Bill.SelectedImage = BitMapCreator.GetButtonImageFromResources(buttonImage.Bill.StreamDeckButtonName, System.Drawing.Color.Green);
                buttonImage.Bill.DeselectedImage = BitMapCreator.GetButtonImageFromResources(buttonImage.Bill.StreamDeckButtonName, Color.Blue);
                buttonImage.Bill.StreamDeckPanelInstance = _streamDeckPanel;
                buttonImage.Source = buttonImage.Bill.DeselectedImage;
            }
        }

        protected void Copy()
        {
            var streamDeckButton = _streamDeckPanel.SelectedLayer.GetStreamDeckButton(SelectedButtonName);
            if (streamDeckButton != null)
            {
                Clipboard.SetDataObject(streamDeckButton);
            }
        }

        protected bool Paste()
        {
            var dataObject = Clipboard.GetDataObject();
            if (dataObject == null || !dataObject.GetDataPresent("NonVisuals.StreamDeck.StreamDeckButton"))
            {
                return false;
            }

            var result = false;
            var newStreamDeckButton = (StreamDeckButton)dataObject.GetData("NonVisuals.StreamDeck.StreamDeckButton");
            var oldStreamDeckButton = _streamDeckPanel.SelectedLayer.GetStreamDeckButton(SelectedButtonName);
            if (oldStreamDeckButton.CheckIfWouldOverwrite(newStreamDeckButton) &&
                MessageBox.Show("Overwrite previous configuration (partial or fully)", "Overwrite?)", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                result = oldStreamDeckButton.Consume(true, newStreamDeckButton);
            }
            else
            {
                result = oldStreamDeckButton.Consume(true, newStreamDeckButton);
            }

            if (result)
            {
                _streamDeckPanel.SelectedLayer.AddButton(oldStreamDeckButton);
                UpdateButtonInfoFromSource();
                SetIsDirty();
            }
            return result;
        }

        public void LayerSwitched(object sender, StreamDeckShowNewLayerArgs e)
        {
            try
            {
                if (_streamDeckPanel.BindingHash == e.BindingHash && _lastShownLayer != e.SelectedLayerName)
                {
                    Dispatcher?.BeginInvoke((Action)(() => UIShowLayer(e.SelectedLayerName)));
                    _lastShownLayer = e.SelectedLayerName;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex);
            }
        }

        public void SelectedButtonChanged(object sender, StreamDeckSelectedButtonChangedArgs e)
        {
            try
            {
                /*
                 * Only do it when it is a different button selected. Should make more comments...
                 */
                if ((_streamDeckPanel.BindingHash == e.BindingHash && SelectedImageBill == null) || (SelectedImageBill != null && SelectedImageBill.Button.GetHash() != e.SelectedButton.GetHash()))
                {
                    SetSelectedButtonUIOnly(e.SelectedButton.StreamDeckButtonName);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex);
            }
        }

        public void IsDirtyQueryReport(object sender, StreamDeckDirtyReportArgs e)
        {
            try
            {
                if (sender.Equals(this))
                {
                    return;
                }

                e.Cancel = IsDirty;
            }
            catch (Exception ex)
            {
                Common.LogError(ex);
            }
        }

        public void SenderIsDirtyNotification(object sender, StreamDeckDirtyNotificationArgs e)
        {
            try
            {
                if (_streamDeckPanel.BindingHash == e.BindingHash)
                {

                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex);
            }
        }

        public void ClearSettings(object sender, StreamDeckClearSettingsArgs e)
        {
            try
            {
                if (_streamDeckPanel.BindingHash == e.BindingHash && e.ClearUIConfiguration)
                {
                    Dispatcher?.BeginInvoke((Action)HideAllDotImages);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex);
            }
        }

        public void SyncConfiguration(object sender, StreamDeckSyncConfigurationArgs e)
        {
            try
            {
                if (_streamDeckPanel.BindingHash == e.BindingHash)
                {
                    Dispatcher?.BeginInvoke((Action)UpdateButtonInfoFromSource);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex);
            }
        }

        public void ConfigurationChanged(object sender, StreamDeckConfigurationChangedArgs e)
        {
            try
            {
                if (_streamDeckPanel.BindingHash == e.BindingHash)
                {
                    Dispatcher?.BeginInvoke((Action)UpdateButtonInfoFromSource);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex);
            }
        }

        public void RemoteLayerSwitch(object sender, RemoteStreamDeckShowNewLayerArgs e)
        {
            try
            {
                if (e.RemoteBindingHash == _streamDeckPanel.BindingHash)
                {
                    Dispatcher?.BeginInvoke((Action)SetFormState);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex);
            }
        }
    }
}
