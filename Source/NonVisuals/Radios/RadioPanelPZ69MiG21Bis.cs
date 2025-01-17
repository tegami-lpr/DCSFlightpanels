﻿namespace NonVisuals.Radios
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using ClassLibraryCommon;

    using DCS_BIOS;
    using DCS_BIOS.EventArgs;
    using DCS_BIOS.Interfaces;

    using MEF;

    using NonVisuals.Interfaces;
    using NonVisuals.Plugin;
    using NonVisuals.Radios.Knobs;
    using NonVisuals.Saitek;

    public class RadioPanelPZ69MiG21Bis : RadioPanelPZ69Base, IDCSBIOSStringListener, IRadioPanel
    {
        private CurrentMiG21BisRadioMode _currentUpperRadioMode = CurrentMiG21BisRadioMode.Radio;
        private CurrentMiG21BisRadioMode _currentLowerRadioMode = CurrentMiG21BisRadioMode.Radio;

        /*MiG-21bis Radio*/
        // Large dial Freq selector 0-19  RAD_CHAN
        // Small dial Radio volume RAD_VOL +/- 
        // STBY/ACT, radio on/off RAD_PWR TOGGLE
        private volatile uint _radioFreqSelectorPositionCockpit;
        private readonly object _lockRadioFreqSelectorPositionObject = new object();
        private DCSBIOSOutput _radioDcsbiosOutputFreqSelectorPosition;
        private const string RADIO_FREQ_SELECTOR_POSITION_COMMAND_INC = "RAD_CHAN INC\n";
        private const string RADIO_FREQ_SELECTOR_POSITION_COMMAND_DEC = "RAD_CHAN DEC\n";
        private const string RADIO_VOLUME_COMMAND_INC = "RAD_VOL +3200\n";
        private const string RADIO_VOLUME_COMMAND_DEC = "RAD_VOL -3200\n";
        private const string RADIO_ON_OFF_TOGGLE_COMMAND = "RAD_PWR TOGGLE\n";

        /*MiG-21bis RSBN*/
        // Large dial RSBN Nav RSBN_CHAN
        // Small dial RSBN ILS PRMG_CHAN
        // STBY/ACT, RSBN/ARC switch  RSBN_ARC_SEL
        private volatile uint _rsbnNavChannelCockpit = 1;
        private readonly object _lockRsbnNavChannelObject = new object();
        private DCSBIOSOutput _rsbnNavChannelCockpitOutput;
        private volatile uint _rsbnILSChannelCockpit = 1;
        private readonly object _lockRsbnilsChannelObject = new object();
        private DCSBIOSOutput _rsbnILSChannelCockpitOutput;
        private const string RSBN_NAV_CHANNEL_COMMAND_INC = "RSBN_CHAN INC\n";
        private const string RSBN_NAV_CHANNEL_COMMAND_DEC = "RSBN_CHAN DEC\n";
        private const string RSBN_ILS_CHANNEL_COMMAND_INC = "PRMG_CHAN INC\n";
        private const string RSBN_ILS_CHANNEL_COMMAND_DEC = "PRMG_CHAN DEC\n";
        private const string SELECT_RSBN_COMMAND = "RSBN_ARC_SEL INC\n";

        /*MiG-21bis ARC*/
        // Large dial ARC Sector ARC_ZONE
        // Small dial ARC Preset ARC_CHAN
        // STBY/ACT, RSBN/ARC switch  RSBN_ARC_SEL 1
        private volatile uint _arcSectorCockpit = 1;
        private readonly object _lockARCSectorObject = new object();
        private DCSBIOSOutput _arcSectorCockpitOutput;
        private volatile uint _arcPresetChannelCockpit = 1;
        private readonly object _lockARCPresetChannelObject = new object();
        private DCSBIOSOutput _arcPresetChannelCockpitOutput;
        private const string ARC_SECTOR_COMMAND_INC = "ARC_ZONE INC\n";
        private const string ARC_SECTOR_COMMAND_DEC = "ARC_ZONE DEC\n";
        private const string ARC_PRESET_CHANNEL_COMMAND_INC = "ARC_CHAN INC\n";
        private const string ARC_PRESET_CHANNEL_COMMAND_DEC = "ARC_CHAN DEC\n";
        private const string SELECT_ARC_COMMAND = "RSBN_ARC_SEL DEC\n";

        private readonly object _lockShowFrequenciesOnPanelObject = new object();
        private long _doUpdatePanelLCD;

        public RadioPanelPZ69MiG21Bis(HIDSkeleton hidSkeleton) : base(hidSkeleton)
        {
            VendorId = 0x6A3;
            ProductId = 0xD05;
            CreateRadioKnobs();
            Startup();
        }

        public override void DcsBiosDataReceived(object sender, DCSBIOSDataEventArgs e)
        {

            UpdateCounter(e.Address, e.Data);

            /*
             * IMPORTANT INFORMATION REGARDING THE _*WaitingForFeedback variables
             * Once a dial has been deemed to be "off" position and needs to be changed
             * a change command is sent to DCS-BIOS.
             * Only after a *change* has been acknowledged will the _*WaitingForFeedback be
             * reset. Reading the dial's position with no change in value will not reset.
             */

            // Radio
            if (e.Address == _radioDcsbiosOutputFreqSelectorPosition.Address)
            {

                lock (_lockRadioFreqSelectorPositionObject)
                {
                    var tmp = _radioFreqSelectorPositionCockpit;
                    if (tmp != _radioFreqSelectorPositionCockpit)
                    {
                        Interlocked.Add(ref _doUpdatePanelLCD, 1);
                        _radioFreqSelectorPositionCockpit = _radioDcsbiosOutputFreqSelectorPosition.GetUIntValue(e.Data);

                    }
                }
            }

            // RSBN Nav
            if (e.Address == _rsbnNavChannelCockpitOutput.Address)
            {

                lock (_lockRsbnNavChannelObject)
                {
                    var tmp = _rsbnNavChannelCockpit;

                    _rsbnNavChannelCockpit = _rsbnNavChannelCockpitOutput.GetUIntValue(e.Data);
                    if (tmp != _rsbnNavChannelCockpit)
                    {
                        Interlocked.Add(ref _doUpdatePanelLCD, 1);
                    }
                }
            }

            // RSBN ILS
            if (e.Address == _rsbnILSChannelCockpitOutput.Address)
            {

                lock (_lockRsbnilsChannelObject)
                {
                    var tmp = _rsbnILSChannelCockpit;

                    _rsbnILSChannelCockpit = _rsbnILSChannelCockpitOutput.GetUIntValue(e.Data);
                    if (tmp != _rsbnILSChannelCockpit)
                    {
                        Interlocked.Add(ref _doUpdatePanelLCD, 1);
                    }
                }
            }

            // ARC Sector
            if (e.Address == _arcSectorCockpitOutput.Address)
            {

                lock (_lockARCSectorObject)
                {
                    var tmp = _arcSectorCockpit;

                    _arcSectorCockpit = _arcSectorCockpitOutput.GetUIntValue(e.Data);
                    if (tmp != _arcSectorCockpit)
                    {
                        Interlocked.Add(ref _doUpdatePanelLCD, 1);
                    }
                }
            }

            // ARC Preset
            if (e.Address == _arcPresetChannelCockpitOutput.Address)
            {

                lock (_lockARCPresetChannelObject)
                {
                    var tmp = _arcPresetChannelCockpit;

                    _arcPresetChannelCockpit = _arcPresetChannelCockpitOutput.GetUIntValue(e.Data) + 1;
                    if (tmp != _arcPresetChannelCockpit)
                    {
                        Interlocked.Add(ref _doUpdatePanelLCD, 1);
                    }
                }
            }

            // Set once
            DataHasBeenReceivedFromDCSBIOS = true;
            ShowFrequenciesOnPanel();

        }

        public void DCSBIOSStringReceived(object sender, DCSBIOSStringDataEventArgs e) { }

        private void SendFrequencyToDCSBIOS(RadioPanelPZ69KnobsMiG21Bis knob)
        {

            if (IgnoreSwitchButtonOnce() && (knob == RadioPanelPZ69KnobsMiG21Bis.UpperFreqSwitch || knob == RadioPanelPZ69KnobsMiG21Bis.LowerFreqSwitch))
            {
                // Don't do anything on the very first button press as the panel sends ALL
                // switches when it is manipulated the first time
                // This would cause unintended sync.
                return;
            }

            if (!DataHasBeenReceivedFromDCSBIOS)
            {
                // Don't start communication with DCS-BIOS before we have had a first contact from "them"
                return;
            }

            switch (knob)
            {
                case RadioPanelPZ69KnobsMiG21Bis.UpperFreqSwitch:
                    {
                        switch (_currentUpperRadioMode)
                        {
                            case CurrentMiG21BisRadioMode.Radio:
                                {
                                    DCSBIOS.Send(RADIO_ON_OFF_TOGGLE_COMMAND);
                                    break;
                                }

                            case CurrentMiG21BisRadioMode.RSBN:
                                {
                                    DCSBIOS.Send(SELECT_RSBN_COMMAND);
                                    break;
                                }

                            case CurrentMiG21BisRadioMode.ARC:
                                {
                                    DCSBIOS.Send(SELECT_ARC_COMMAND);
                                    break;
                                }
                        }
                        break;
                    }

                case RadioPanelPZ69KnobsMiG21Bis.LowerFreqSwitch:
                    {
                        switch (_currentLowerRadioMode)
                        {
                            case CurrentMiG21BisRadioMode.Radio:
                                {
                                    DCSBIOS.Send(RADIO_ON_OFF_TOGGLE_COMMAND);
                                    break;
                                }

                            case CurrentMiG21BisRadioMode.RSBN:
                                {
                                    DCSBIOS.Send(SELECT_RSBN_COMMAND);
                                    break;
                                }

                            case CurrentMiG21BisRadioMode.ARC:
                                {
                                    DCSBIOS.Send(SELECT_ARC_COMMAND);
                                    break;
                                }
                        }
                        break;
                    }
            }
        }

        private void ShowFrequenciesOnPanel()
        {
            lock (_lockShowFrequenciesOnPanelObject)
            {
                if (!FirstReportHasBeenRead)
                {
                    return;
                }

                if (Interlocked.Read(ref _doUpdatePanelLCD) == 0)
                {
                    return;
                }

                var bytes = new byte[21];
                bytes[0] = 0x0;

                switch (_currentUpperRadioMode)
                {
                    case CurrentMiG21BisRadioMode.Radio:
                        {
                            lock (_lockRadioFreqSelectorPositionObject)
                            {
                                SetPZ69DisplayBytesUnsignedInteger(ref bytes, _radioFreqSelectorPositionCockpit, PZ69LCDPosition.UPPER_ACTIVE_LEFT);
                                SetPZ69DisplayBlank(ref bytes, PZ69LCDPosition.UPPER_STBY_RIGHT);
                            }

                            break;
                        }

                    case CurrentMiG21BisRadioMode.RSBN:
                        {
                            lock (_lockRsbnNavChannelObject)
                            {
                                SetPZ69DisplayBytesUnsignedInteger(ref bytes, _rsbnNavChannelCockpit, PZ69LCDPosition.UPPER_ACTIVE_LEFT);
                            }

                            lock (_lockRsbnilsChannelObject)
                            {
                                SetPZ69DisplayBytesUnsignedInteger(ref bytes, _rsbnILSChannelCockpit, PZ69LCDPosition.UPPER_STBY_RIGHT);
                            }

                            break;
                        }

                    case CurrentMiG21BisRadioMode.ARC:
                        {
                            lock (_lockARCSectorObject)
                            {
                                SetPZ69DisplayBytesCustom1(ref bytes, GetARCSectorBytesForDisplay(), PZ69LCDPosition.UPPER_ACTIVE_LEFT);
                            }

                            lock (_lockARCPresetChannelObject)
                            {
                                SetPZ69DisplayBytesUnsignedInteger(ref bytes, _arcPresetChannelCockpit, PZ69LCDPosition.UPPER_STBY_RIGHT);
                            }

                            break;
                        }
                }
                switch (_currentLowerRadioMode)
                {
                    case CurrentMiG21BisRadioMode.Radio:
                        {
                            lock (_lockRadioFreqSelectorPositionObject)
                            {
                                SetPZ69DisplayBytesUnsignedInteger(ref bytes, _radioFreqSelectorPositionCockpit, PZ69LCDPosition.LOWER_ACTIVE_LEFT);
                                SetPZ69DisplayBlank(ref bytes, PZ69LCDPosition.LOWER_STBY_RIGHT);
                            }

                            break;
                        }

                    case CurrentMiG21BisRadioMode.RSBN:
                        {
                            lock (_lockRsbnNavChannelObject)
                            {
                                SetPZ69DisplayBytesUnsignedInteger(ref bytes, _rsbnNavChannelCockpit, PZ69LCDPosition.LOWER_ACTIVE_LEFT);
                            }

                            lock (_lockRsbnilsChannelObject)
                            {
                                SetPZ69DisplayBytesUnsignedInteger(ref bytes, _rsbnILSChannelCockpit, PZ69LCDPosition.LOWER_STBY_RIGHT);
                            }

                            break;
                        }

                    case CurrentMiG21BisRadioMode.ARC:
                        {
                            lock (_lockARCSectorObject)
                            {
                                SetPZ69DisplayBytesCustom1(ref bytes, GetARCSectorBytesForDisplay(), PZ69LCDPosition.LOWER_ACTIVE_LEFT);
                            }

                            lock (_lockARCPresetChannelObject)
                            {
                                SetPZ69DisplayBytesUnsignedInteger(ref bytes, _arcPresetChannelCockpit, PZ69LCDPosition.LOWER_STBY_RIGHT);
                            }

                            break;
                        }
                }
                SendLCDData(bytes);
            }

            Interlocked.Add(ref _doUpdatePanelLCD, -1);
        }

        private void AdjustFrequency(IEnumerable<object> hashSet)
        {
            if (SkipCurrentFrequencyChange())
            {
                return;
            }

            foreach (var o in hashSet)
            {
                var radioPanelKnobMiG21Bis = (RadioPanelKnobMiG21Bis)o;
                if (radioPanelKnobMiG21Bis.IsOn)
                {
                    switch (radioPanelKnobMiG21Bis.RadioPanelPZ69Knob)
                    {
                        case RadioPanelPZ69KnobsMiG21Bis.UpperLargeFreqWheelInc:
                            {
                                switch (_currentUpperRadioMode)
                                {
                                    case CurrentMiG21BisRadioMode.Radio:
                                        {
                                            DCSBIOS.Send(RADIO_FREQ_SELECTOR_POSITION_COMMAND_INC);
                                            break;
                                        }

                                    case CurrentMiG21BisRadioMode.RSBN:
                                        {
                                            DCSBIOS.Send(RSBN_NAV_CHANNEL_COMMAND_INC);
                                            break;
                                        }

                                    case CurrentMiG21BisRadioMode.ARC:
                                        {
                                            DCSBIOS.Send(ARC_SECTOR_COMMAND_INC);
                                            break;
                                        }
                                }
                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.UpperLargeFreqWheelDec:
                            {
                                switch (_currentUpperRadioMode)
                                {
                                    case CurrentMiG21BisRadioMode.Radio:
                                        {
                                            DCSBIOS.Send(RADIO_FREQ_SELECTOR_POSITION_COMMAND_DEC);
                                            break;
                                        }

                                    case CurrentMiG21BisRadioMode.RSBN:
                                        {
                                            DCSBIOS.Send(RSBN_NAV_CHANNEL_COMMAND_DEC);
                                            break;
                                        }

                                    case CurrentMiG21BisRadioMode.ARC:
                                        {
                                            DCSBIOS.Send(ARC_SECTOR_COMMAND_DEC);
                                            break;
                                        }
                                }
                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.UpperSmallFreqWheelInc:
                            {
                                switch (_currentUpperRadioMode)
                                {
                                    case CurrentMiG21BisRadioMode.Radio:
                                        {
                                            DCSBIOS.Send(RADIO_VOLUME_COMMAND_INC);
                                            break;
                                        }

                                    case CurrentMiG21BisRadioMode.RSBN:
                                        {
                                            DCSBIOS.Send(RSBN_ILS_CHANNEL_COMMAND_INC);
                                            break;
                                        }

                                    case CurrentMiG21BisRadioMode.ARC:
                                        {
                                            DCSBIOS.Send(ARC_PRESET_CHANNEL_COMMAND_INC);
                                            break;
                                        }
                                }
                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.UpperSmallFreqWheelDec:
                            {
                                switch (_currentUpperRadioMode)
                                {
                                    case CurrentMiG21BisRadioMode.Radio:
                                        {
                                            DCSBIOS.Send(RADIO_VOLUME_COMMAND_DEC);
                                            break;
                                        }

                                    case CurrentMiG21BisRadioMode.RSBN:
                                        {
                                            DCSBIOS.Send(RSBN_ILS_CHANNEL_COMMAND_DEC);
                                            break;
                                        }

                                    case CurrentMiG21BisRadioMode.ARC:
                                        {
                                            DCSBIOS.Send(ARC_PRESET_CHANNEL_COMMAND_DEC);
                                            break;
                                        }
                                }
                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.LowerLargeFreqWheelInc:
                            {
                                switch (_currentLowerRadioMode)
                                {
                                    case CurrentMiG21BisRadioMode.Radio:
                                        {
                                            DCSBIOS.Send(RADIO_FREQ_SELECTOR_POSITION_COMMAND_INC);
                                            break;
                                        }

                                    case CurrentMiG21BisRadioMode.RSBN:
                                        {
                                            DCSBIOS.Send(RSBN_NAV_CHANNEL_COMMAND_INC);
                                            break;
                                        }

                                    case CurrentMiG21BisRadioMode.ARC:
                                        {
                                            DCSBIOS.Send(ARC_SECTOR_COMMAND_INC);
                                            break;
                                        }
                                }
                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.LowerLargeFreqWheelDec:
                            {
                                switch (_currentLowerRadioMode)
                                {
                                    case CurrentMiG21BisRadioMode.Radio:
                                        {
                                            DCSBIOS.Send(RADIO_FREQ_SELECTOR_POSITION_COMMAND_DEC);
                                            break;
                                        }

                                    case CurrentMiG21BisRadioMode.RSBN:
                                        {

                                            DCSBIOS.Send(RSBN_NAV_CHANNEL_COMMAND_DEC);
                                            break;
                                        }

                                    case CurrentMiG21BisRadioMode.ARC:
                                        {
                                            DCSBIOS.Send(ARC_SECTOR_COMMAND_DEC);
                                            break;
                                        }
                                }
                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.LowerSmallFreqWheelInc:
                            {
                                switch (_currentLowerRadioMode)
                                {
                                    case CurrentMiG21BisRadioMode.Radio:
                                        {
                                            DCSBIOS.Send(RADIO_VOLUME_COMMAND_INC);
                                            break;
                                        }

                                    case CurrentMiG21BisRadioMode.RSBN:
                                        {
                                            DCSBIOS.Send(RSBN_ILS_CHANNEL_COMMAND_INC);
                                            break;
                                        }

                                    case CurrentMiG21BisRadioMode.ARC:
                                        {
                                            DCSBIOS.Send(ARC_PRESET_CHANNEL_COMMAND_INC);
                                            break;
                                        }
                                }
                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.LowerSmallFreqWheelDec:
                            {
                                switch (_currentLowerRadioMode)
                                {
                                    case CurrentMiG21BisRadioMode.Radio:
                                        {
                                            DCSBIOS.Send(RADIO_VOLUME_COMMAND_DEC);
                                            break;
                                        }

                                    case CurrentMiG21BisRadioMode.RSBN:
                                        {
                                            DCSBIOS.Send(RSBN_ILS_CHANNEL_COMMAND_DEC);
                                            break;
                                        }

                                    case CurrentMiG21BisRadioMode.ARC:
                                        {
                                            DCSBIOS.Send(ARC_PRESET_CHANNEL_COMMAND_DEC);
                                            break;
                                        }
                                }
                                break;
                            }
                    }
                }
            }

            ShowFrequenciesOnPanel();
        }

        public void PZ69KnobChanged(bool isFirstReport, IEnumerable<object> hashSet)
        {
            lock (LockLCDUpdateObject)
            {
                Interlocked.Add(ref _doUpdatePanelLCD, 1);
                foreach (var radioPanelKnobObject in hashSet)
                {
                    var radioPanelKnob = (RadioPanelKnobMiG21Bis)radioPanelKnobObject;

                    switch (radioPanelKnob.RadioPanelPZ69Knob)
                    {
                        case RadioPanelPZ69KnobsMiG21Bis.UpperRadio:
                            {
                                if (radioPanelKnob.IsOn)
                                {
                                    _currentUpperRadioMode = CurrentMiG21BisRadioMode.Radio;
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.UpperRsbn:
                            {
                                if (radioPanelKnob.IsOn)
                                {
                                    _currentUpperRadioMode = CurrentMiG21BisRadioMode.RSBN;
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.UpperArc:
                            {
                                if (radioPanelKnob.IsOn)
                                {
                                    _currentUpperRadioMode = CurrentMiG21BisRadioMode.ARC;
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.UpperCom2:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.UpperNav2:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.UpperDme:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.UpperXpdr:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.LowerRadio:
                            {
                                if (radioPanelKnob.IsOn)
                                {
                                    _currentLowerRadioMode = CurrentMiG21BisRadioMode.Radio;
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.LowerRsbn:
                            {
                                if (radioPanelKnob.IsOn)
                                {
                                    _currentLowerRadioMode = CurrentMiG21BisRadioMode.RSBN;
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.LowerArc:
                            {
                                if (radioPanelKnob.IsOn)
                                {
                                    _currentLowerRadioMode = CurrentMiG21BisRadioMode.ARC;
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.LowerCom2:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.LowerNav2:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.LowerDme:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.LowerXpdr:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.UpperLargeFreqWheelInc:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.UpperLargeFreqWheelDec:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.UpperSmallFreqWheelInc:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.UpperSmallFreqWheelDec:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.LowerLargeFreqWheelInc:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.LowerLargeFreqWheelDec:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.LowerSmallFreqWheelInc:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.LowerSmallFreqWheelDec:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.UpperFreqSwitch:
                            {
                                if (radioPanelKnob.IsOn)
                                {
                                    SendFrequencyToDCSBIOS(RadioPanelPZ69KnobsMiG21Bis.UpperFreqSwitch);
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsMiG21Bis.LowerFreqSwitch:
                            {
                                if (radioPanelKnob.IsOn)
                                {
                                    SendFrequencyToDCSBIOS(RadioPanelPZ69KnobsMiG21Bis.LowerFreqSwitch);
                                }

                                break;
                            }
                    }

                    if (PluginManager.PlugSupportActivated && PluginManager.HasPlugin())
                    {
                        PluginManager.DoEvent(ProfileHandler.SelectedProfile().Description, HIDInstanceId, (int)PluginGamingPanelEnum.PZ69RadioPanel, (int)radioPanelKnob.RadioPanelPZ69Knob, radioPanelKnob.IsOn, null);
                    }
                }

                AdjustFrequency(hashSet);
            }
        }

        public sealed override void Startup()
        {
            try
            {
                StartupBase("MiG21-Bis");

                // Radio
                _radioDcsbiosOutputFreqSelectorPosition = DCSBIOSControlLocator.GetDCSBIOSOutput("RAD_CHAN");

                // RSBN
                _rsbnNavChannelCockpitOutput = DCSBIOSControlLocator.GetDCSBIOSOutput("RSBN_CHAN");
                _rsbnILSChannelCockpitOutput = DCSBIOSControlLocator.GetDCSBIOSOutput("PRMG_CHAN");

                // ARC
                _arcSectorCockpitOutput = DCSBIOSControlLocator.GetDCSBIOSOutput("ARC_ZONE");
                _arcPresetChannelCockpitOutput = DCSBIOSControlLocator.GetDCSBIOSOutput("ARC_CHAN");

                StartListeningForPanelChanges();

                // IsAttached = true;
            }
            catch (Exception ex)
            {
                Common.LogError( ex);
            }
        }

        public override void Dispose()
        {
            try
            {
                ShutdownBase();
            }
            catch (Exception e)
            {
                SetLastException(e);
            }
        }

        public override void ClearSettings(bool setIsDirty = false) {}

        public override DcsOutputAndColorBinding CreateDcsOutputAndColorBinding(SaitekPanelLEDPosition saitekPanelLEDPosition, PanelLEDColor panelLEDColor, DCSBIOSOutput dcsBiosOutput)
        {
            var dcsOutputAndColorBinding = new DcsOutputAndColorBindingPZ55();
            dcsOutputAndColorBinding.DCSBiosOutputLED = dcsBiosOutput;
            dcsOutputAndColorBinding.LEDColor = panelLEDColor;
            dcsOutputAndColorBinding.SaitekLEDPosition = saitekPanelLEDPosition;
            return dcsOutputAndColorBinding;
        }

        protected override void GamingPanelKnobChanged(bool isFirstReport, IEnumerable<object> hashSet)
        {
            PZ69KnobChanged(isFirstReport, hashSet);
        }

        private void CreateRadioKnobs()
        {
            SaitekPanelKnobs = RadioPanelKnobMiG21Bis.GetRadioPanelKnobs();
        }

        private byte[] GetARCSectorBytesForDisplay()
        {
            var result = new byte[5];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = 0xff;
            }

            lock (_lockARCSectorObject)
            {
                switch (_arcSectorCockpit)
                {
                    case 0:
                        {
                            // 1  1 
                            result[0] = 1;
                            result[4] = 1;
                            break;
                        }

                    case 1:
                        {
                            // 1  2
                            result[0] = 1;
                            result[4] = 2;
                            break;
                        }

                    case 2:
                        {
                            // 2  1
                            result[0] = 2;
                            result[4] = 1;
                            break;
                        }

                    case 3:
                        {
                            // 2  2
                            result[0] = 2;
                            result[4] = 2;
                            break;
                        }

                    case 4:
                        {
                            // 3  1
                            result[0] = 3;
                            result[4] = 1;
                            break;
                        }

                    case 5:
                        {
                            // 3  2
                            result[0] = 3;
                            result[4] = 2;
                            break;
                        }

                    case 6:
                        {
                            // 4  1
                            result[0] = 4;
                            result[4] = 1;
                            break;
                        }

                    case 7:
                        {
                            // 4  2
                            result[0] = 4;
                            result[4] = 2;
                            break;
                        }
                }
            }

            return result;
        }

        public override void RemoveSwitchFromList(object controlList, PanelSwitchOnOff panelSwitchOnOff)
        {
        }

        public override void AddOrUpdateKeyStrokeBinding(PanelSwitchOnOff panelSwitchOnOff, string keyPress, KeyPressLength keyPressLength)
        {
        }

        public override void AddOrUpdateSequencedKeyBinding(PanelSwitchOnOff panelSwitchOnOff, string description, SortedList<int, IKeyPressInfo> keySequence)
        {
        }

        public override void AddOrUpdateDCSBIOSBinding(PanelSwitchOnOff panelSwitchOnOff, List<DCSBIOSInput> dcsbiosInputs, string description)
        {
        }

        public override void AddOrUpdateBIPLinkBinding(PanelSwitchOnOff panelSwitchOnOff, BIPLink bipLink)
        {
        }

        public override void AddOrUpdateOSCommandBinding(PanelSwitchOnOff panelSwitchOnOff, OSCommand operatingSystemCommand)
        {
        }
    }

}
