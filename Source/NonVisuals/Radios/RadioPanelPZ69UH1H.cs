namespace NonVisuals.Radios
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
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

    public class RadioPanelPZ69UH1H : RadioPanelPZ69Base, IDCSBIOSStringListener, IRadioPanel
    {
        private CurrentUH1HRadioMode _currentUpperRadioMode = CurrentUH1HRadioMode.UHF;

        private CurrentUH1HRadioMode _currentLowerRadioMode = CurrentUH1HRadioMode.UHF;

        /*UH-1H INTERCOMM*/
        // PVT INT 1 2 3 4  (6 positions 0-5)
        private volatile uint _intercommSkipper;

        private readonly object _lockIntercommDialObject = new object();

        // private volatile uint _intercommDialPosStandby = 0;  <--- Only active, user operates the knob directly, no need for a standby value
        // private volatile uint _intercommSavedCockpitDialPosition = 0;
        private DCSBIOSOutput _intercommDcsbiosOutputCockpitPos;

        private volatile uint _intercommCockpitDial1Pos;

        private const string INTERCOMM_DIAL_COMMAND_INC = "INT_MODE INC\n";

        private const string INTERCOMM_DIAL_COMMAND_DEC = "INT_MODE DEC\n";

        private long _intercommThreadNowSynching;

        private long _intercommDialWaitingForFeedback;

        private const string INTERCOMM_VOLUME_KNOB_COMMAND_INC = "INT_VOL +2500\n";

        private const string INTERCOMM_VOLUME_KNOB_COMMAND_DEC = "INT_VOL -2500\n";

        /*UH-1H AN/ARC-134 VHF Comm Radio Set Left side of lower control panel */
        // Large dial 116-149 [step of 1]
        // Small dial 0.00-0.95 [step 0 2 5 7]
        private readonly object _lockVhfCommDialsObject1 = new object();

        private readonly object _lockVhfCommDialsObject2 = new object();

        private volatile uint _vhfCommBigFrequencyStandby = 116;

        private volatile uint _vhfCommSmallFrequencyStandby;

        private volatile uint _vhfCommSavedCockpitSmallFrequency;

        private volatile uint _vhfCommSavedCockpitBigFrequency = 116;

        private double _vhfCommCockpitFrequency = 116.00;

        private long _vhfCommThreadNowSynching;

        private volatile uint _vhfCommCockpitDial1Frequency = 116;

        private volatile uint _vhfCommCockpitDial2Frequency = 95;

        private long _vhfCommDial1FreqWaitingForFeedback;

        private long _vhfCommDial2FreqWaitingForFeedback;

        private DCSBIOSOutput _vhfCommDcsbiosOutputCockpitFrequency;

        private const string VHF_COMM_FREQ1_DIAL_COMMAND = "VHFCOMM_MHZ ";

        private const string VHF_COMM_FREQ2_DIAL_COMMAND = "VHFCOMM_KHZ ";

        private Thread _vhfCommSyncThread;

        /*AN/ARC-51BX UHF radio set*/
        // Large dial 200-399 [step of 1]
        // Small dial 0.00-0.95 [step 0.05]
        private bool _uhfIncreasePresetChannel;

        private uint _uhfCockpitPresetChannel = 1;

        private const string UHF_PRESET_DIAL_COMMAND_INC = "UHF_PRESET INC\n";

        private const string UHF_PRESET_DIAL_COMMAND_DEC = "UHF_PRESET DEC\n";

        private DCSBIOSOutput _uhfDcsbiosOutputCockpitPresetChannel;

        private readonly object _lockUhfPresetChannelObject = new object();

        // private Thread _uhfPresetChannelSyncThread;
        // private long _uhfPresetChannelThreadNowSynching = 0;
        // private long _uhfPresetChannelWaitingForFeedback = 0;
        private readonly object _lockUhfDialsObject1 = new object();

        private readonly object _lockUhfDialsObject2 = new object();

        private readonly object _lockUhfDialsObject3 = new object();

        private volatile uint _uhfBigFrequencyStandby = 200;

        private volatile uint _uhfSmallFrequencyStandby;

        private volatile uint _uhfSavedCockpitBigFrequency = 200;

        private volatile uint _uhfSavedCockpitSmallFrequency;

        private DCSBIOSOutput _uhfDcsbiosOutputCockpitFrequency;

        private double _uhfCockpitFrequency = 225.00;

        private volatile uint _uhfCockpitDial1Frequency = 22;

        private volatile uint _uhfCockpitDial2Frequency = 5;

        private volatile uint _uhfCockpitDial3Frequency;

        private const string UHF_FREQ1_DIAL_COMMAND = "UHF_10MHZ "; // 20-39

        private const string UHF_FREQ2_DIAL_COMMAND = "UHF_1MHZ "; // 0 1 2 3 4 5 6 7 8 9

        private const string UHF_FREQ3_DIAL_COMMAND = "UHF_50KHZ "; // 00 - 95

        private Thread _uhfSyncThread;

        private long _uhfThreadNowSynching;

        private long _uhfDial1WaitingForFeedback;

        private long _uhfDial2WaitingForFeedback;

        private long _uhfDial3WaitingForFeedback;

        /*UH-1H AN/ARN-82 VHF Navigation Set*/
        // Large dial 107-126 [step of 1]
        // Small dial 0.00-0.95 [step of 0.05]
        private readonly object _lockVhfNavDialsObject1 = new object();

        private readonly object _lockVhfNavDialsObject2 = new object();

        private volatile uint _vhfNavBigFrequencyStandby = 107;

        private volatile uint _vhfNavSmallFrequencyStandby;

        private volatile uint _vhfNavSavedCockpitBigFrequency = 107;

        private volatile uint _vhfNavSavedCockpitSmallFrequency;

        private DCSBIOSOutput _vhfNavDcsbiosOutputCockpitFrequency;

        private double _vhfNavCockpitFrequency = 107.00;

        private volatile uint _vhfNavCockpitDial1Frequency = 107;

        private volatile uint _vhfNavCockpitDial2Frequency;

        private const string VHF_NAV_FREQ1_DIAL_COMMAND = "VHFNAV_MHZ ";

        private const string VHF_NAV_FREQ2_DIAL_COMMAND = "VHFNAV_KHZ ";

        private Thread _vhfNavSyncThread;

        private long _vhfNavThreadNowSynching;

        private long _vhfNavDial1WaitingForFeedback;

        private long _vhfNavDial2WaitingForFeedback;

        /*UH-1H ARC-131 VHF FM*/
        private uint _vhfFmBigFrequencyStandby = 30;

        private uint _vhfFmSmallFrequencyStandby;

        private uint _vhfFmSavedCockpitBigFrequency = 30;

        private uint _vhfFmSavedCockpitSmallFrequency;

        private readonly object _lockVhfFmDialsObject1 = new object();

        private readonly object _lockVhfFmDialsObject2 = new object();

        private readonly object _lockVhfFmDialsObject3 = new object();

        private readonly object _lockVhfFmDialsObject4 = new object();

        private DCSBIOSOutput _vhfFmDcsbiosOutputFreqDial1;

        private DCSBIOSOutput _vhfFmDcsbiosOutputFreqDial2;

        private DCSBIOSOutput _vhfFmDcsbiosOutputFreqDial3;

        private DCSBIOSOutput _vhfFmDcsbiosOutputFreqDial4;

        private volatile uint _vhfFmCockpitFreq1DialPos = 1;

        private volatile uint _vhfFmCockpitFreq2DialPos = 1;

        private volatile uint _vhfFmCockpitFreq3DialPos = 1;

        private volatile uint _vhfFmCockpitFreq4DialPos = 1;

        private const string VHF_FM_FREQ_1DIAL_COMMAND = "VHFFM_FREQ1 "; // 3 4 5 6

        private const string VHF_FM_FREQ_2DIAL_COMMAND = "VHFFM_FREQ2 "; // 0 1 2 3 4 5 6 7 8 9

        private const string VHF_FM_FREQ_3DIAL_COMMAND = "VHFFM_FREQ3 "; // 0 1 2 3 4 5 6 7 8 9

        private const string VHF_FM_FREQ_4DIAL_COMMAND = "VHFFM_FREQ4 "; // 0 5

        private Thread _vhfFmSyncThread;

        private long _vhfFmThreadNowSynching;

        private long _vhfFmDial1WaitingForFeedback;

        private long _vhfFmDial2WaitingForFeedback;

        private long _vhfFmDial3WaitingForFeedback;

        private long _vhfFmDial4WaitingForFeedback;

        /*UH-1H ADF*/
        /*
            Small Knob - GAIN
            Large Knob - TUNE (Direct tune)
            ACT/STBY - BAND SELECTOR. 
            ACTIVE SCREEN - Frequency
            STANDBY SCREEN - SIGNAL STRENGTH
            
            190-1800 kHz
         */
        private bool _increaseAdfBand;

        private readonly object _lockAdfFrequencyBandObject = new object();

        private readonly object _lockAdfCockpitFrequencyObject = new object();

        private readonly object _lockAdfSignalStrengthObject = new object();

        private DCSBIOSOutput _adfDcsbiosOutputCockpitFrequencyBand;

        private DCSBIOSOutput _adfDcsbiosOutputCockpitFrequency;

        private DCSBIOSOutput _adfDcsbiosOutputSignalStrength;

        private volatile uint _adfCockpitFrequencyRaw = 0;

        private double _adfCockpitFrequency;

        private volatile uint _adfSignalStrengthRaw;

        private double _adfSignalStrength;

        private volatile uint _adfCockpitFrequencyBand;

        private volatile uint _adfStandbyFrequencyBand;

        private const string ADF_TUNE_KNOB_COMMAND_INC = "ADF_TUNE -1000\n";

        private const string ADF_TUNE_KNOB_COMMAND_DEC = "ADF_TUNE +1000\n";

        private const string ADF_GAIN_KNOB_COMMAND_INC = "ADF_GAIN -2000\n";

        private const string ADF_GAIN_KNOB_COMMAND_DEC = "ADF_GAIN +2000\n";

        private const string ADF_FREQUENCY_BAND_COMMAND = "ADF_BAND ";

        private Thread _adfSyncThread;

        private long _adfThreadNowSynching;

        private long _adfFrequencyBandWaitingForFeedback;

        private readonly object _lockShowFrequenciesOnPanelObject = new object();

        private long _doUpdatePanelLCD;

        public RadioPanelPZ69UH1H(HIDSkeleton hidSkeleton)
            : base(hidSkeleton)
        {
            VendorId = 0x6A3;
            ProductId = 0xD05;
            CreateRadioKnobs();
            Startup();
        }

        public void DCSBIOSStringReceived(object sender, DCSBIOSStringDataEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(e.StringData))
                {
                    return;
                }

                if (e.Address.Equals(_vhfCommDcsbiosOutputCockpitFrequency.Address))
                {
                    /*UH-1H AN/ARC-134 VHF Comm Radio Set*/

                    // Large dial 116 - 149
                    // Small dial 000 - 975 [step of 25]
                    // NOT USED NOT USED NOT USED NOT USED NOT USED NOT USED NOT USED NOT USED NOT USED NOT USED NOT USED NOT USED NOT USED NOT USED NOT USED NOT USED NOT USED NOT USED
                    // 000 025 050 075 100 125 150 175 200 225 250 275 300 325 350 375 400 425 450 475 500 525 550 575 600 625 650 675 700 725 750 775 800 825 850 875 900 925 950 975
                    // 1   2   3   4   5   6   7   8   9   10  11  12  13  14  15  16  17  18  19  *20*  21  22  23  24  25  26  27  28  29  30  31  32  33  34  35  36  37  38  39  40
                    // Only these are used because of PZ69 limitations
                    // 00 05 10 15 20 25 30 35 40 45 50 55 60 65 70 75 80 85 90 95
                    // 1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17 18 19 20   

                    // "116.975" (7 characters)
                    var tmpFreq = double.Parse(e.StringData, NumberFormatInfoFullDisplay);
                    if (!tmpFreq.Equals(_vhfCommCockpitFrequency))
                    {
                        Interlocked.Add(ref _doUpdatePanelLCD, 1);
                    }

                    if (tmpFreq.Equals(_vhfCommCockpitFrequency))
                    {
                        // No need to process same data over and over
                        return;
                    }

                    _vhfCommCockpitFrequency = tmpFreq;
                    lock (_lockVhfCommDialsObject1)
                    {
                        var tmp = _vhfCommCockpitDial1Frequency;
                        _vhfCommCockpitDial1Frequency = uint.Parse(e.StringData.Substring(0, 3));
                        if (tmp != _vhfCommCockpitDial1Frequency)
                        {
                            Interlocked.Add(ref _doUpdatePanelLCD, 1);

                            Interlocked.Exchange(ref _vhfCommDial1FreqWaitingForFeedback, 0);
                        }
                    }

                    lock (_lockVhfCommDialsObject2)
                    {
                        var tmp = _vhfCommCockpitDial2Frequency;

                        // var beforeRounding = e.StringData.Substring(4, 2);
                        _vhfCommCockpitDial2Frequency = uint.Parse(e.StringData.Substring(4, 2));

                        // 975
                        // Do not round this. Rounding means that the synch process thinks the frequency is OK which it isn't
                        if (tmp != _vhfCommCockpitDial2Frequency)
                        {
                            Interlocked.Add(ref _doUpdatePanelLCD, 1);

                            Interlocked.Exchange(ref _vhfCommDial2FreqWaitingForFeedback, 0);
                        }
                    }
                }

                if (e.Address.Equals(_uhfDcsbiosOutputCockpitFrequency.Address))
                {
                    /*UH-1H AN/ARC-134 VHF Comm Radio Set*/

                    // Large dial 200 - 399
                    // Small dial 00 - 95 [step of 5]
                    // "225.95" (6 characters)
                    var tmpFreq = double.Parse(e.StringData, NumberFormatInfoFullDisplay);
                    if (!tmpFreq.Equals(_uhfCockpitFrequency))
                    {
                        Interlocked.Add(ref _doUpdatePanelLCD, 1);
                    }

                    if (tmpFreq.Equals(_uhfCockpitFrequency))
                    {
                        // No need to process same data over and over
                        return;
                    }

                    _uhfCockpitFrequency = tmpFreq;
                    lock (_lockUhfDialsObject1)
                    {
                        var tmp = _uhfCockpitDial1Frequency;
                        _uhfCockpitDial1Frequency = uint.Parse(e.StringData.Substring(0, 2));
                        if (tmp != _uhfCockpitDial1Frequency)
                        {
                            Interlocked.Add(ref _doUpdatePanelLCD, 1);

                            Interlocked.Exchange(ref _uhfDial1WaitingForFeedback, 0);
                        }
                    }

                    lock (_lockUhfDialsObject2)
                    {
                        var tmp = _uhfCockpitDial2Frequency;
                        _uhfCockpitDial2Frequency = uint.Parse(e.StringData.Substring(2, 1));
                        if (tmp != _uhfCockpitDial2Frequency)
                        {
                            Interlocked.Add(ref _doUpdatePanelLCD, 1);

                            Interlocked.Exchange(ref _uhfDial2WaitingForFeedback, 0);
                        }
                    }

                    lock (_lockUhfDialsObject3)
                    {
                        var tmp = _uhfCockpitDial3Frequency;
                        _uhfCockpitDial3Frequency = uint.Parse(e.StringData.Substring(4, 2));
                        if (tmp != _uhfCockpitDial3Frequency)
                        {
                            Interlocked.Add(ref _doUpdatePanelLCD, 1);

                            Interlocked.Exchange(ref _uhfDial3WaitingForFeedback, 0);
                        }
                    }
                }

                if (e.Address.Equals(_vhfNavDcsbiosOutputCockpitFrequency.Address))
                {
                    /*UH-1H AN/ARN-82 VHF Navigation Set*/
                    // Large dial 107-126 [step of 1]
                    // Small dial 0.00-0.95 [step of 0.05]
                    var tmpFreq = double.Parse(e.StringData, NumberFormatInfoFullDisplay);
                    if (!tmpFreq.Equals(_vhfNavCockpitFrequency))
                    {
                        Interlocked.Add(ref _doUpdatePanelLCD, 1);
                    }

                    if (tmpFreq.Equals(_vhfNavCockpitFrequency))
                    {
                        // No need to process same data over and over
                        return;
                    }

                    _vhfNavCockpitFrequency = tmpFreq;

                    // 107.95
                    lock (_lockVhfNavDialsObject1)
                    {
                        var tmp = _vhfNavCockpitDial1Frequency;
                        _vhfNavCockpitDial1Frequency = uint.Parse(e.StringData.Substring(0, 3));
                        if (tmp != _vhfNavCockpitDial1Frequency)
                        {
                            Interlocked.Add(ref _doUpdatePanelLCD, 1);

                            Interlocked.Exchange(ref _vhfNavDial1WaitingForFeedback, 0);
                        }
                    }

                    lock (_lockVhfNavDialsObject2)
                    {
                        var tmp = _vhfNavCockpitDial2Frequency;
                        _vhfNavCockpitDial2Frequency = uint.Parse(e.StringData.Substring(4, 2));
                        if (tmp != _vhfNavCockpitDial2Frequency)
                        {
                            Interlocked.Add(ref _doUpdatePanelLCD, 1);

                            Interlocked.Exchange(ref _vhfNavDial2WaitingForFeedback, 0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(ex, "DCSBIOSStringReceived()");
            }

            ShowFrequenciesOnPanel();
        }

        public override void DcsBiosDataReceived(object sender, DCSBIOSDataEventArgs e)
        {
            // INTERCOMM
            if (e.Address == _intercommDcsbiosOutputCockpitPos.Address)
            {
                lock (_lockIntercommDialObject)
                {
                    var tmp = _intercommCockpitDial1Pos;
                    _intercommCockpitDial1Pos = _intercommDcsbiosOutputCockpitPos.GetUIntValue(e.Data);
                    if (tmp != _intercommCockpitDial1Pos)
                    {
                        Interlocked.Add(ref _doUpdatePanelLCD, 1);

                        Interlocked.Exchange(ref _intercommDialWaitingForFeedback, 0);
                    }
                }
            }

            // VHF FM
            if (e.Address == _vhfFmDcsbiosOutputFreqDial1.Address)
            {
                lock (_lockVhfFmDialsObject1)
                {
                    var tmp = _vhfFmCockpitFreq1DialPos;
                    _vhfFmCockpitFreq1DialPos = _vhfFmDcsbiosOutputFreqDial1.GetUIntValue(e.Data);
                    if (tmp != _vhfFmCockpitFreq1DialPos)
                    {
                        Interlocked.Add(ref _doUpdatePanelLCD, 1);

                        Interlocked.Exchange(ref _vhfFmDial1WaitingForFeedback, 0);
                    }
                }
            }

            if (e.Address == _vhfFmDcsbiosOutputFreqDial2.Address)
            {
                lock (_lockVhfFmDialsObject2)
                {
                    var tmp = _vhfFmCockpitFreq2DialPos;
                    _vhfFmCockpitFreq2DialPos = _vhfFmDcsbiosOutputFreqDial2.GetUIntValue(e.Data);
                    if (tmp != _vhfFmCockpitFreq2DialPos)
                    {
                        Interlocked.Add(ref _doUpdatePanelLCD, 1);

                        Interlocked.Exchange(ref _vhfFmDial2WaitingForFeedback, 0);
                    }
                }
            }

            if (e.Address == _vhfFmDcsbiosOutputFreqDial3.Address)
            {
                lock (_lockVhfFmDialsObject3)
                {
                    var tmp = _vhfFmCockpitFreq3DialPos;
                    _vhfFmCockpitFreq3DialPos = _vhfFmDcsbiosOutputFreqDial3.GetUIntValue(e.Data);
                    if (tmp != _vhfFmCockpitFreq3DialPos)
                    {
                        Interlocked.Add(ref _doUpdatePanelLCD, 1);

                        Interlocked.Exchange(ref _vhfFmDial3WaitingForFeedback, 0);
                    }
                }
            }

            if (e.Address == _vhfFmDcsbiosOutputFreqDial4.Address)
            {
                lock (_lockVhfFmDialsObject4)
                {
                    var tmp = _vhfFmCockpitFreq4DialPos;
                    _vhfFmCockpitFreq4DialPos = _vhfFmDcsbiosOutputFreqDial4.GetUIntValue(e.Data);
                    if (tmp != _vhfFmCockpitFreq4DialPos)
                    {
                        Interlocked.Add(ref _doUpdatePanelLCD, 1);

                        Interlocked.Exchange(ref _vhfFmDial4WaitingForFeedback, 0);
                    }
                }
            }

            // ADF
            if (e.Address == _adfDcsbiosOutputCockpitFrequencyBand.Address)
            {
                lock (_lockAdfFrequencyBandObject)
                {
                    var tmp = _adfCockpitFrequencyBand;
                    _adfCockpitFrequencyBand = _adfDcsbiosOutputCockpitFrequencyBand.GetUIntValue(e.Data);
                    if (tmp != _adfCockpitFrequencyBand)
                    {
                        Interlocked.Add(ref _doUpdatePanelLCD, 1);

                        Interlocked.Exchange(ref _adfFrequencyBandWaitingForFeedback, 0);
                    }
                }
            }

            if (e.Address == _adfDcsbiosOutputCockpitFrequency.Address)
            {
                UpdateCockpitAdfFrequency(_adfDcsbiosOutputCockpitFrequency.GetUIntValue(e.Data));
            }

            if (e.Address == _adfDcsbiosOutputSignalStrength.Address)
            {
                lock (_lockAdfSignalStrengthObject)
                {
                    var tmp = _adfSignalStrengthRaw;
                    _adfSignalStrengthRaw = _adfDcsbiosOutputSignalStrength.GetUIntValue(e.Data);
                    if (tmp != _adfSignalStrengthRaw)
                    {
                        Interlocked.Add(ref _doUpdatePanelLCD, 1);
                        const float maxValue = 65535;
                        _adfSignalStrength = ((_adfSignalStrengthRaw / maxValue) * 100);
                    }
                }
            }

            // UHF Preset Channel
            if (e.Address.Equals(_uhfDcsbiosOutputCockpitPresetChannel.Address))
            {
                lock (_lockUhfPresetChannelObject)
                {
                    var tmp = _uhfCockpitPresetChannel;
                    _uhfCockpitPresetChannel = _uhfDcsbiosOutputCockpitPresetChannel.GetUIntValue(e.Data) + 1;
                    if (!tmp.Equals(_uhfCockpitPresetChannel))
                    {
                        Interlocked.Add(ref _doUpdatePanelLCD, 1);
                    }
                }
            }

            // Set once
            DataHasBeenReceivedFromDCSBIOS = true;
            ShowFrequenciesOnPanel();
        }

        private void UpdateCockpitAdfFrequency(uint adfCockpitFrequencyRaw)
        {
            lock (_lockAdfCockpitFrequencyObject)
            {
                if (adfCockpitFrequencyRaw != _adfCockpitFrequencyRaw)
                {
                    // Update only if data has changed. Max 65535 
                    const float maxValue = 65535;
                    switch (_adfCockpitFrequencyBand)
                    {
                        case 0:
                            {
                                // 190-400 kHz (~210kHz)
                                // A = desired freq
                                // B = freq based on adfCockpitFrequencyRaw
                                // (A>200) A = B - ((B * -0.04408) + 18.31) //Creds Paul Marsh
                                var b = ((adfCockpitFrequencyRaw / maxValue) * 210) + 190;
                                _adfCockpitFrequency = b - ((b * -0.04408) + 18.31);
                                break;
                            }

                        case 1:
                            {
                                // 400-850 kHz (~450kHz)
                                var b = ((adfCockpitFrequencyRaw / maxValue) * 450) + 400;
                                if (b < 451)
                                {
                                    // A = B + ((B * -0.44837) + 177.08)
                                    _adfCockpitFrequency = b + ((b * -0.44837) + 181.58);
                                }
                                else
                                {
                                    // A = B + ((B * 0.11291) - 100.61)
                                    _adfCockpitFrequency = b + ((b * 0.11291) - 96.11);
                                }

                                break;
                            }

                        case 2:
                            {
                                // 850-1800 kHz (~950kHz)
                                // A = B - ((B * -0.04532) + 91.54)
                                var b = ((adfCockpitFrequencyRaw / maxValue) * 950) + 850;
                                _adfCockpitFrequency = b - ((b * -0.04532) + 91.54);
                                break;
                            }
                    }
                }
            }
        }

        /*
        private void UpdateCockpitAdfFrequency()
        {
            lock (_lockAdfCockpitFrequencyObject)
            {
                const float maxValue = 65535;
                switch (_adfCockpitFrequencyBand)
                {
                    case 0: //190-400 kHz (~210kHz)
                        {
                            _adfCockpitFrequency = ((_adfCockpitFrequencyRaw / maxValue) * 210) + 190;
                            break;
                        }
                    case 1: //400-850 kHz (~450kHz)
                        {
                            _adfCockpitFrequency = ((_adfCockpitFrequencyRaw / maxValue) * 450) + 400;
                            break;
                        }
                    case 2: //850-1800 kHz (~950kHz)
                        {
                            _adfCockpitFrequency = ((_adfCockpitFrequencyRaw / maxValue) * 950) + 850;
                            break;
                        }
                }
            }
        }
        */
        private void SendFrequencyToDCSBIOS(RadioPanelPZ69KnobsUH1H knob)
        {
            if (IgnoreSwitchButtonOnce() && (knob == RadioPanelPZ69KnobsUH1H.UPPER_FREQ_SWITCH || knob == RadioPanelPZ69KnobsUH1H.LOWER_FREQ_SWITCH))
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
                case RadioPanelPZ69KnobsUH1H.UPPER_FREQ_SWITCH:
                    {
                        switch (_currentUpperRadioMode)
                        {
                            case CurrentUH1HRadioMode.INTERCOMM:
                                {
                                    SendUhfPresetChannelChangeToDCSBIOS();
                                    break;
                                }

                            case CurrentUH1HRadioMode.VHFCOMM:
                                {
                                    SendVhfCommToDCSBIOS();
                                    break;
                                }

                            case CurrentUH1HRadioMode.UHF:
                                {
                                    SendUhfToDCSBIOS();
                                    break;
                                }

                            case CurrentUH1HRadioMode.VHFFM:
                                {
                                    SendVhfFmToDCSBIOS();
                                    break;
                                }

                            case CurrentUH1HRadioMode.VHFNAV:
                                {
                                    SendVhfNavToDCSBIOS();
                                    break;
                                }

                            case CurrentUH1HRadioMode.ADF:
                                {
                                    SendAdfBandChangeToDCSBIOS();
                                    break;
                                }
                        }

                        break;
                    }

                case RadioPanelPZ69KnobsUH1H.LOWER_FREQ_SWITCH:
                    {
                        switch (_currentLowerRadioMode)
                        {
                            case CurrentUH1HRadioMode.INTERCOMM:
                                {
                                    SendUhfPresetChannelChangeToDCSBIOS();
                                    break;
                                }

                            case CurrentUH1HRadioMode.VHFCOMM:
                                {
                                    SendVhfCommToDCSBIOS();
                                    break;
                                }

                            case CurrentUH1HRadioMode.UHF:
                                {
                                    SendUhfToDCSBIOS();
                                    break;
                                }

                            case CurrentUH1HRadioMode.VHFFM:
                                {
                                    SendVhfFmToDCSBIOS();
                                    break;
                                }

                            case CurrentUH1HRadioMode.VHFNAV:
                                {
                                    SendVhfNavToDCSBIOS();
                                    break;
                                }

                            case CurrentUH1HRadioMode.ADF:
                                {
                                    SendAdfBandChangeToDCSBIOS();
                                    break;
                                }
                        }

                        break;
                    }
            }
        }

        /*
        private void SendIntercommToDCSBIOS()
        {
            if (IntercommSyncing())
            {
                return;
            }
            SaveCockpitIntercomm();
            if (_intercommSyncThread != null)
            {
                _intercommSyncThread.Aborttt();
            }
            
            _intercommSyncThread = new Thread(IntercommSynchThreadMethod);
            _intercommSyncThread.Start();
        }

        private void IntercommSynchThreadMethod()
        {
            try
            {
                try
                {
                    String str;
                    Interlocked.Exchange(ref _intercommThreadNowSynching, 1);
                    var inc = "INC\n";
                    var dec = "DEC\n";
                    long dial1Timeout = DateTime.Now.Ticks;
                    long dial1OkTime = 0;
                    var dial1SendCount = 0;

                    do
                    {
                        if (IsTimedOut(ref dial1Timeout, ResetSyncTimeout, "INTERCOMM dial1Timeout"))
                        {
                            //Lets do an ugly reset
                            Interlocked.Exchange(ref _intercommDialWaitingForFeedback, 0);
                            Common.DebugP("Resetting SYNC for INTERCOMM");
                        }

                        //0 - 5
                        if (Interlocked.Read(ref _intercommDialWaitingForFeedback) == 0)
                        {
                            lock (_lockIntercommDialObject)
                            {

                                
                                if (_intercommDialPosStandby != _intercommCockpitDial1Pos)
                                {
                                    dial1OkTime = DateTime.Now.Ticks;
                                    str = IntercommDialCommand + (_intercommDialPosStandby < _intercommCockpitDial1Pos ? dec : inc);
                                    Common.DebugP("Sending " + str);
                                    DCSBIOS.Send(str);
                                    dial1SendCount++;
                                    Interlocked.Exchange(ref _intercommDialWaitingForFeedback, 1);
                                }
                                Reset(ref dial1Timeout);
                            }
                        }
                        else
                        {
                            dial1OkTime = DateTime.Now.Ticks;
                        }

                        if (dial1SendCount > 5)
                        {
                            //"Race" condition detected?
                            dial1SendCount = 0;
                            Thread.Sleep(5000);
                        }

                        Thread.Sleep(SynchSleepTime); //Should be enough to get an update cycle from DCS-BIOS
                    } while (IsTooShort(dial1OkTime));

                    SwapCockpitIntercomm();
                    ShowFrequenciesOnPanel();
                }
                finally
                {
                    Interlocked.Exchange(ref _intercommThreadNowSynching, 0);
                }
            }
            catch (ThreadAbortException)
            { }
            catch (Exception ex)
            {
                Common.LogError( ex);
            }
        }
        */
        private void SendVhfCommToDCSBIOS()
        {
            if (VhfCommSyncing())
            {
                return;
            }

            SaveCockpitFrequencyVhfComm();

            // Frequency selector 1 rotates both ways
            // "116" "117" "118" .. "149"

            // Frequency selector 2 rotates both ways
            // 0.00 - 0.95

            // Send INC / DEC until frequency is correct. NOT THE DIALS!
            _vhfCommSyncThread?.Abort();
            _vhfCommSyncThread = new Thread(VhfCommSynchThreadMethod);
            _vhfCommSyncThread.Start();
        }

        private void VhfCommSynchThreadMethod()
        {
            try
            {
                try
                {
                    Interlocked.Exchange(ref _vhfCommThreadNowSynching, 1);
                    long dial1Timeout = DateTime.Now.Ticks;
                    long dial2Timeout = DateTime.Now.Ticks;
                    long dial1OkTime = 0;
                    long dial2OkTime = 0;
                    var dial1SendCount = 0;
                    var dial2SendCount = 0;

                    // Reason for this is to separate the standby frequency from the sync loop
                    // If not the sync would pick up any changes made by the user during the
                    // sync process
                    var localVhfCommBigFrequencyStandby = _vhfCommBigFrequencyStandby;
                    var localVhfCommSmallFrequencyStandby = _vhfCommSmallFrequencyStandby;

                    do
                    {
                        if (IsTimedOut(ref dial1Timeout, ResetSyncTimeout, "VHF COMM dial1Timeout"))
                        {
                            // Lets do an ugly reset
                            Interlocked.Exchange(ref _vhfCommDial1FreqWaitingForFeedback, 0);
                        }

                        if (IsTimedOut(ref dial2Timeout, ResetSyncTimeout, "VHF COMM dial2Timeout"))
                        {
                            // Lets do an ugly reset
                            Interlocked.Exchange(ref _vhfCommDial2FreqWaitingForFeedback, 0);
                        }

                        string str;
                        if (Interlocked.Read(ref _vhfCommDial1FreqWaitingForFeedback) == 0)
                        {
                            lock (_lockVhfCommDialsObject1)
                            {
                                if (localVhfCommBigFrequencyStandby != _vhfCommCockpitDial1Frequency)
                                {
                                    dial1OkTime = DateTime.Now.Ticks;
                                    str = VHF_COMM_FREQ1_DIAL_COMMAND + GetCommandDirectionForVhfCommDial1(localVhfCommBigFrequencyStandby, _vhfCommCockpitDial1Frequency);
                                    DCSBIOS.Send(str);
                                    dial1SendCount++;
                                    Interlocked.Exchange(ref _vhfCommDial1FreqWaitingForFeedback, 1);
                                }

                                Reset(ref dial1Timeout);
                            }
                        }
                        else
                        {
                            dial1OkTime = DateTime.Now.Ticks;
                        }

                        if (Interlocked.Read(ref _vhfCommDial2FreqWaitingForFeedback) == 0)
                        {
                            lock (_lockVhfCommDialsObject2)
                            {
                                if (localVhfCommSmallFrequencyStandby != _vhfCommCockpitDial2Frequency)
                                {
                                    dial2OkTime = DateTime.Now.Ticks;
                                    str = VHF_COMM_FREQ2_DIAL_COMMAND + GetCommandDirectionForVhfCommDial2(localVhfCommSmallFrequencyStandby, _vhfCommCockpitDial2Frequency);
                                    DCSBIOS.Send(str);
                                    dial2SendCount++;
                                    Interlocked.Exchange(ref _vhfCommDial2FreqWaitingForFeedback, 1);
                                }

                                Reset(ref dial2Timeout);
                            }
                        }
                        else
                        {
                            dial2OkTime = DateTime.Now.Ticks;
                        }

                        if (dial1SendCount > 20 || dial2SendCount > 25)
                        {
                            // "Race" condition detected?
                            dial1SendCount = 0;
                            dial2SendCount = 0;
                            Thread.Sleep(5000);
                        }

                        Thread.Sleep(SynchSleepTime); // Should be enough to get an update cycle from DCS-BIOS
                    }
                    while (IsTooShort(dial1OkTime) || IsTooShort(dial2OkTime));

                    SwapCockpitStandbyFrequencyVhfComm();
                    ShowFrequenciesOnPanel();
                }
                catch (ThreadAbortException)
                {
                }
                catch (Exception ex)
                {
                    Common.LogError(ex);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _vhfCommThreadNowSynching, 0);
            }

            Interlocked.Add(ref _doUpdatePanelLCD, 1);
        }

        private void SendUhfToDCSBIOS()
        {
            if (UhfSyncing())
            {
                return;
            }

            SaveCockpitFrequencyUhf();

            // Frequency selector 1 rotates both ways
            // "20" "21" "22" .. "39"

            // Frequency selector 2 rotates both ways
            // 0 - 9

            // Frequency selector 2 rotates both ways
            // 0 - 95 (-/+ 5)

            // Send INC / DEC until frequency is correct. NOT THE DIALS!
            _uhfSyncThread?.Abort();

            _uhfSyncThread = new Thread(UhfSynchThreadMethod);
            _uhfSyncThread.Start();
        }

        private void UhfSynchThreadMethod()
        {
            try
            {
                try
                {
                    Interlocked.Exchange(ref _uhfThreadNowSynching, 1);
                    long dial1Timeout = DateTime.Now.Ticks;
                    long dial2Timeout = DateTime.Now.Ticks;
                    long dial3Timeout = DateTime.Now.Ticks;
                    long dial1OkTime = 0;
                    long dial2OkTime = 0;
                    long dial3OkTime = 0;
                    var dial1SendCount = 0;
                    var dial2SendCount = 0;
                    var dial3SendCount = 0;

                    // Reason for having it outside the loop is to separate the standby frequency from the sync loop
                    // If not the sync would pick up any changes made by the user during the
                    // sync process
                    // 225.95
                    var filler = string.Empty;
                    if (_uhfSmallFrequencyStandby < 10)
                    {
                        filler = "0";
                    }

                    var standbyFrequency = double.Parse(_uhfBigFrequencyStandby + "." + filler + _uhfSmallFrequencyStandby, NumberFormatInfoFullDisplay);
                    var dial1StandbyFrequency = uint.Parse(standbyFrequency.ToString("0.00", NumberFormatInfoFullDisplay).Substring(0, 2));
                    var dial2StandbyFrequency = uint.Parse(standbyFrequency.ToString("0.00", NumberFormatInfoFullDisplay).Substring(2, 1));
                    var dial3StandbyFrequency = uint.Parse(standbyFrequency.ToString("0.00", NumberFormatInfoFullDisplay).Substring(4, 2));

                    do
                    {
                        if (IsTimedOut(ref dial1Timeout, ResetSyncTimeout, "UHF dial1Timeout"))
                        {
                            // Lets do an ugly reset
                            Interlocked.Exchange(ref _uhfDial1WaitingForFeedback, 0);
                        }

                        if (IsTimedOut(ref dial2Timeout, ResetSyncTimeout, "UHF dial2Timeout"))
                        {
                            // Lets do an ugly reset
                            Interlocked.Exchange(ref _uhfDial2WaitingForFeedback, 0);
                        }

                        if (IsTimedOut(ref dial3Timeout, ResetSyncTimeout, "UHF dial3Timeout"))
                        {
                            // Lets do an ugly reset
                            Interlocked.Exchange(ref _uhfDial3WaitingForFeedback, 0);
                        }

                        string str;
                        if (Interlocked.Read(ref _uhfDial1WaitingForFeedback) == 0)
                        {
                            lock (_lockUhfDialsObject1)
                            {
                                if (dial1StandbyFrequency != _uhfCockpitDial1Frequency)
                                {
                                    dial1OkTime = DateTime.Now.Ticks;
                                    str = UHF_FREQ1_DIAL_COMMAND + GetCommandDirectionForUhfDial1(dial1StandbyFrequency, _uhfCockpitDial1Frequency);
                                    DCSBIOS.Send(str);
                                    dial1SendCount++;
                                    Interlocked.Exchange(ref _uhfDial1WaitingForFeedback, 1);
                                }

                                Reset(ref dial1Timeout);
                            }
                        }
                        else
                        {
                            dial1OkTime = DateTime.Now.Ticks;
                        }

                        if (Interlocked.Read(ref _uhfDial2WaitingForFeedback) == 0)
                        {
                            lock (_lockUhfDialsObject2)
                            {
                                if (dial2StandbyFrequency != _uhfCockpitDial2Frequency)
                                {
                                    dial2OkTime = DateTime.Now.Ticks;
                                    str = UHF_FREQ2_DIAL_COMMAND + GetCommandDirectionForUhfDial2(dial2StandbyFrequency, _uhfCockpitDial2Frequency);
                                    DCSBIOS.Send(str);
                                    dial2SendCount++;
                                    Interlocked.Exchange(ref _uhfDial2WaitingForFeedback, 1);
                                }

                                Reset(ref dial2Timeout);
                            }
                        }
                        else
                        {
                            dial2OkTime = DateTime.Now.Ticks;
                        }

                        if (Interlocked.Read(ref _uhfDial3WaitingForFeedback) == 0)
                        {
                            lock (_lockUhfDialsObject3)
                            {
                                if (dial3StandbyFrequency != _uhfCockpitDial3Frequency)
                                {
                                    dial3OkTime = DateTime.Now.Ticks;
                                    str = UHF_FREQ3_DIAL_COMMAND + GetCommandDirectionForUhfDial3(dial3StandbyFrequency, _uhfCockpitDial3Frequency);
                                    DCSBIOS.Send(str);
                                    dial3SendCount++;
                                    Interlocked.Exchange(ref _uhfDial3WaitingForFeedback, 1);
                                }

                                Reset(ref dial3Timeout);
                            }
                        }
                        else
                        {
                            dial3OkTime = DateTime.Now.Ticks;
                        }

                        if (dial1SendCount > 19 || dial2SendCount > 9 || dial3SendCount > 20)
                        {
                            // "Race" condition detected?
                            dial1SendCount = 0;
                            dial2SendCount = 0;
                            dial3SendCount = 0;
                            Thread.Sleep(5000);
                        }

                        Thread.Sleep(SynchSleepTime); // Should be enough to get an update cycle from DCS-BIOS
                    }
                    while (IsTooShort(dial1OkTime) || IsTooShort(dial2OkTime) || IsTooShort(dial3OkTime));

                    SwapCockpitStandbyFrequencyUhf();
                    ShowFrequenciesOnPanel();
                }
                catch (ThreadAbortException)
                {
                }
                catch (Exception ex)
                {
                    Common.LogError(ex);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _uhfThreadNowSynching, 0);
            }

            Interlocked.Add(ref _doUpdatePanelLCD, 1);
        }

        private void SendVhfNavToDCSBIOS()
        {
            if (VhfNavSyncing())
            {
                return;
            }

            SaveCockpitFrequencyVhfNav();
            _vhfNavSyncThread?.Abort();
            _vhfNavSyncThread = new Thread(VhfNavSynchThreadMethod);
            _vhfNavSyncThread.Start();
        }

        private void VhfNavSynchThreadMethod()
        {
            try
            {
                try
                {
                    Interlocked.Exchange(ref _vhfNavThreadNowSynching, 1);
                    long dial1Timeout = DateTime.Now.Ticks;
                    long dial2Timeout = DateTime.Now.Ticks;
                    long dial1OkTime = 0;
                    long dial2OkTime = 0;
                    var dial1SendCount = 0;
                    var dial2SendCount = 0;

                    // Reason for having it outside the loop is to separate the standby frequency from the sync loop
                    // If not the sync would pick up any changes made by the user during the
                    // sync process
                    // 107.95
                    var filler = string.Empty;
                    if (_vhfNavSmallFrequencyStandby < 10)
                    {
                        filler = "0";
                    }

                    var standbyFrequency = double.Parse(_vhfNavBigFrequencyStandby + "." + filler + _vhfNavSmallFrequencyStandby, NumberFormatInfoFullDisplay);
                    var dial1StandbyFrequency = uint.Parse(standbyFrequency.ToString("0.00", NumberFormatInfoFullDisplay).Substring(0, 3));
                    var dial2StandbyFrequency = uint.Parse(standbyFrequency.ToString("0.00", NumberFormatInfoFullDisplay).Substring(4, 2));

                    do
                    {
                        if (IsTimedOut(ref dial1Timeout, ResetSyncTimeout, "VHF NAV dial1Timeout"))
                        {
                            // Lets do an ugly reset
                            Interlocked.Exchange(ref _vhfNavDial1WaitingForFeedback, 0);
                        }

                        if (IsTimedOut(ref dial2Timeout, ResetSyncTimeout, "VHF NAV dial2Timeout"))
                        {
                            // Lets do an ugly reset
                            Interlocked.Exchange(ref _vhfNavDial2WaitingForFeedback, 0);
                        }

                        string str;
                        if (Interlocked.Read(ref _vhfNavDial1WaitingForFeedback) == 0)
                        {
                            lock (_lockVhfNavDialsObject1)
                            {
                                if (dial1StandbyFrequency != _vhfNavCockpitDial1Frequency)
                                {
                                    dial1OkTime = DateTime.Now.Ticks;
                                    str = VHF_NAV_FREQ1_DIAL_COMMAND + GetCommandDirectionForVhfNavDial1(dial1StandbyFrequency, _vhfNavCockpitDial1Frequency);
                                    DCSBIOS.Send(str);
                                    dial1SendCount++;
                                    Interlocked.Exchange(ref _vhfNavDial1WaitingForFeedback, 1);
                                }

                                Reset(ref dial1Timeout);
                            }
                        }
                        else
                        {
                            dial1OkTime = DateTime.Now.Ticks;
                        }

                        if (Interlocked.Read(ref _vhfNavDial2WaitingForFeedback) == 0)
                        {
                            lock (_lockVhfNavDialsObject2)
                            {
                                if (dial2StandbyFrequency != _vhfNavCockpitDial2Frequency)
                                {
                                    dial2OkTime = DateTime.Now.Ticks;

                                    // Compatible : GetCommandDirectionForUhfDial3
                                    str = VHF_NAV_FREQ2_DIAL_COMMAND + GetCommandDirectionForUhfDial3(dial2StandbyFrequency, _vhfNavCockpitDial2Frequency);
                                    DCSBIOS.Send(str);
                                    dial2SendCount++;
                                    Interlocked.Exchange(ref _vhfNavDial2WaitingForFeedback, 1);
                                }

                                Reset(ref dial2Timeout);
                            }
                        }
                        else
                        {
                            dial2OkTime = DateTime.Now.Ticks;
                        }

                        if (dial1SendCount > 19 || dial2SendCount > 9)
                        {
                            // "Race" condition detected?
                            dial1SendCount = 0;
                            dial2SendCount = 0;
                            Thread.Sleep(5000);
                        }

                        Thread.Sleep(SynchSleepTime); // Should be enough to get an update cycle from DCS-BIOS
                    }
                    while (IsTooShort(dial1OkTime) || IsTooShort(dial2OkTime));

                    SwapCockpitStandbyFrequencyVhfNav();
                    ShowFrequenciesOnPanel();
                }
                catch (ThreadAbortException)
                {
                }
                catch (Exception ex)
                {
                    Common.LogError(ex);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _vhfNavThreadNowSynching, 0);
            }

            Interlocked.Add(ref _doUpdatePanelLCD, 1);
        }

        private void SendVhfFmToDCSBIOS()
        {
            if (VhfFmSyncing())
            {
                return;
            }

            SaveCockpitFrequencyVhfFm();
            _vhfFmSyncThread?.Abort();
            _vhfFmSyncThread = new Thread(VhfFmSynchThreadMethod);

            _vhfFmSyncThread.Start();
        }

        private void VhfFmSynchThreadMethod()
        {
            try
            {
                try
                {
                    Interlocked.Exchange(ref _vhfFmThreadNowSynching, 1);
                    long dial1Timeout = DateTime.Now.Ticks;
                    long dial2Timeout = DateTime.Now.Ticks;
                    long dial3Timeout = DateTime.Now.Ticks;
                    long dial4Timeout = DateTime.Now.Ticks;
                    long dial1OkTime = 0;
                    long dial2OkTime = 0;
                    long dial3OkTime = 0;
                    long dial4OkTime = 0;
                    var dial1SendCount = 0;
                    var dial2SendCount = 0;
                    var dial3SendCount = 0;
                    var dial4SendCount = 0;

                    // Frequency selector 1     
                    // 3  4   5   6   7
                    // Pos    0  1   2   3   4

                    // Frequency selector 2      
                    // 0 1 2 3 4 5

                    // Frequency selector 3
                    // 0 1 2 3 4 5 6 7 8 9

                    // Frequency selector 4
                    // 0 5
                    // Pos   0 1

                    // Large dial 30 - 75 [step of 1]
                    // Small dial 0.00-0.95 [step of 0.05]
                    var filler = string.Empty;
                    if (_vhfFmSmallFrequencyStandby < 10)
                    {
                        filler = "0";
                    }

                    var frequencyAsString = _vhfFmBigFrequencyStandby + "." + filler + _vhfFmSmallFrequencyStandby;

                    // 75.95
                    var desiredFreqDial1Pos = int.Parse(frequencyAsString.Substring(0, 1)) - 3;
                    var desiredFreqDial2Pos = int.Parse(frequencyAsString.Substring(1, 1));
                    var desiredFreqDial3Pos = int.Parse(frequencyAsString.Substring(3, 1));
                    var desiredFreqDial4Pos = int.Parse(frequencyAsString.Substring(4, 1));
                    if (desiredFreqDial4Pos == 5)
                    {
                        // 0 -> pos 0
                        // 5 -> pos 1
                        desiredFreqDial4Pos = 1;
                    }

                    do
                    {
                        if (IsTimedOut(ref dial1Timeout, ResetSyncTimeout, "VHF FM dial1Timeout"))
                        {
                            // Lets do an ugly reset
                            Interlocked.Exchange(ref _vhfFmDial1WaitingForFeedback, 0);
                        }

                        if (IsTimedOut(ref dial2Timeout, ResetSyncTimeout, "VHF FM dial2Timeout"))
                        {
                            // Lets do an ugly reset
                            Interlocked.Exchange(ref _vhfFmDial2WaitingForFeedback, 0);
                        }

                        if (IsTimedOut(ref dial3Timeout, ResetSyncTimeout, "VHF FM dial3Timeout"))
                        {
                            // Lets do an ugly reset
                            Interlocked.Exchange(ref _vhfFmDial3WaitingForFeedback, 0);
                        }

                        if (IsTimedOut(ref dial4Timeout, ResetSyncTimeout, "VHF FM dial4Timeout"))
                        {
                            // Lets do an ugly reset
                            Interlocked.Exchange(ref _vhfFmDial4WaitingForFeedback, 0);
                        }

                        if (Interlocked.Read(ref _vhfFmDial1WaitingForFeedback) == 0)
                        {
                            lock (_lockVhfFmDialsObject1)
                            {
                                if (_vhfFmCockpitFreq1DialPos != desiredFreqDial1Pos)
                                {
                                    dial1OkTime = DateTime.Now.Ticks;
                                }

                                if (_vhfFmCockpitFreq1DialPos < desiredFreqDial1Pos)
                                {
                                    const string str = VHF_FM_FREQ_1DIAL_COMMAND + "INC\n";
                                    DCSBIOS.Send(str);
                                    dial1SendCount++;
                                    Interlocked.Exchange(ref _vhfFmDial1WaitingForFeedback, 1);
                                }
                                else if (_vhfFmCockpitFreq1DialPos > desiredFreqDial1Pos)
                                {
                                    const string str = VHF_FM_FREQ_1DIAL_COMMAND + "DEC\n";
                                    DCSBIOS.Send(str);
                                    dial1SendCount++;
                                    Interlocked.Exchange(ref _vhfFmDial1WaitingForFeedback, 1);
                                }

                                Reset(ref dial1Timeout);
                            }
                        }
                        else
                        {
                            dial1OkTime = DateTime.Now.Ticks;
                        }

                        if (Interlocked.Read(ref _vhfFmDial2WaitingForFeedback) == 0)
                        {
                            lock (_lockVhfFmDialsObject2)
                            {
                                if (_vhfFmCockpitFreq2DialPos != desiredFreqDial2Pos)
                                {
                                    dial2OkTime = DateTime.Now.Ticks;
                                }

                                if (_vhfFmCockpitFreq2DialPos < desiredFreqDial2Pos)
                                {
                                    const string str = VHF_FM_FREQ_2DIAL_COMMAND + "INC\n";
                                    DCSBIOS.Send(str);
                                    dial2SendCount++;
                                    Interlocked.Exchange(ref _vhfFmDial2WaitingForFeedback, 1);
                                }
                                else if (_vhfFmCockpitFreq2DialPos > desiredFreqDial2Pos)
                                {
                                    const string str = VHF_FM_FREQ_2DIAL_COMMAND + "DEC\n";
                                    DCSBIOS.Send(str);
                                    dial2SendCount++;
                                    Interlocked.Exchange(ref _vhfFmDial2WaitingForFeedback, 1);
                                }

                                Reset(ref dial2Timeout);
                            }
                        }
                        else
                        {
                            dial2OkTime = DateTime.Now.Ticks;
                        }

                        if (Interlocked.Read(ref _vhfFmDial3WaitingForFeedback) == 0)
                        {
                            lock (_lockVhfFmDialsObject3)
                            {
                                if (_vhfFmCockpitFreq3DialPos != desiredFreqDial3Pos)
                                {
                                    dial3OkTime = DateTime.Now.Ticks;
                                }

                                if (_vhfFmCockpitFreq3DialPos < desiredFreqDial3Pos)
                                {
                                    const string str = VHF_FM_FREQ_3DIAL_COMMAND + "INC\n";
                                    DCSBIOS.Send(str);
                                    dial3SendCount++;
                                    Interlocked.Exchange(ref _vhfFmDial3WaitingForFeedback, 1);
                                }
                                else if (_vhfFmCockpitFreq3DialPos > desiredFreqDial3Pos)
                                {
                                    const string str = VHF_FM_FREQ_3DIAL_COMMAND + "DEC\n";
                                    DCSBIOS.Send(str);
                                    dial3SendCount++;
                                    Interlocked.Exchange(ref _vhfFmDial3WaitingForFeedback, 1);
                                }

                                Reset(ref dial3Timeout);
                            }
                        }
                        else
                        {
                            dial3OkTime = DateTime.Now.Ticks;
                        }

                        if (Interlocked.Read(ref _vhfFmDial4WaitingForFeedback) == 0)
                        {
                            lock (_lockVhfFmDialsObject4)
                            {
                                if (_vhfFmCockpitFreq4DialPos != desiredFreqDial4Pos)
                                {
                                    dial4OkTime = DateTime.Now.Ticks;
                                }

                                if (_vhfFmCockpitFreq4DialPos < desiredFreqDial4Pos)
                                {
                                    const string str = VHF_FM_FREQ_4DIAL_COMMAND + "INC\n";
                                    DCSBIOS.Send(str);
                                    dial4SendCount++;
                                    Interlocked.Exchange(ref _vhfFmDial4WaitingForFeedback, 1);
                                }
                                else if (_vhfFmCockpitFreq4DialPos > desiredFreqDial4Pos)
                                {
                                    const string str = VHF_FM_FREQ_4DIAL_COMMAND + "DEC\n";
                                    DCSBIOS.Send(str);
                                    dial4SendCount++;
                                    Interlocked.Exchange(ref _vhfFmDial4WaitingForFeedback, 1);
                                }

                                Reset(ref dial4Timeout);
                            }
                        }
                        else
                        {
                            dial4OkTime = DateTime.Now.Ticks;
                        }

                        if (dial1SendCount > 4 || dial2SendCount > 10 || dial3SendCount > 10 || dial4SendCount > 2)
                        {
                            // "Race" condition detected?
                            dial1SendCount = 0;
                            dial2SendCount = 0;
                            dial3SendCount = 0;
                            dial4SendCount = 0;
                            Thread.Sleep(5000);
                        }

                        Thread.Sleep(SynchSleepTime); // Should be enough to get an update cycle from DCS-BIOS
                    }
                    while (IsTooShort(dial1OkTime) || IsTooShort(dial2OkTime) || IsTooShort(dial3OkTime) || IsTooShort(dial4OkTime));

                    SwapCockpitStandbyFrequencyVhfFm();
                    ShowFrequenciesOnPanel();
                }
                catch (ThreadAbortException)
                {
                }
                catch (Exception ex)
                {
                    Common.ShowErrorMessageBox(ex);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _vhfFmThreadNowSynching, 0);
            }

            Interlocked.Add(ref _doUpdatePanelLCD, 1);
        }

        private void SendAdfBandChangeToDCSBIOS()
        {
            _adfSyncThread?.Abort();
            _adfSyncThread = new Thread(AdfBandChangeSynchThreadMethod);

            _adfSyncThread.Start();
        }

        private void AdfBandChangeSynchThreadMethod()
        {
            try
            {
                try
                {
                    Interlocked.Exchange(ref _adfThreadNowSynching, 1);
                    long freqBandDialTimeout = DateTime.Now.Ticks;
                    long freqBandDialOkTime = 0;
                    var freqBandDialSendCount = 0;
                    var once = true;

                    // Frequency Band selector
                    // Pos    0  1   2
                    switch (_adfStandbyFrequencyBand)
                    {
                        case 0:
                            {
                                _increaseAdfBand = true;
                                _adfStandbyFrequencyBand = 1;
                                break;
                            }

                        case 1:
                            {
                                _adfStandbyFrequencyBand = _increaseAdfBand ? 2 : (uint)0;
                                break;
                            }

                        case 2:
                            {
                                _increaseAdfBand = false;
                                _adfStandbyFrequencyBand = 1;
                                break;
                            }
                    }

                    var desiredFreqBandDialPos = _adfStandbyFrequencyBand;

                    do
                    {
                        if (IsTimedOut(ref freqBandDialTimeout, ResetSyncTimeout, "ADF Frequency Band Selector dial1Timeout"))
                        {
                            // Lets do an ugly reset
                            Interlocked.Exchange(ref _adfFrequencyBandWaitingForFeedback, 0);
                        }

                        if (Interlocked.Read(ref _adfFrequencyBandWaitingForFeedback) == 0)
                        {
                            lock (_lockAdfFrequencyBandObject)
                            {
                                if (_adfCockpitFrequencyBand != desiredFreqBandDialPos)
                                {
                                    freqBandDialOkTime = DateTime.Now.Ticks;
                                    once = false;
                                }

                                if (_adfCockpitFrequencyBand < desiredFreqBandDialPos)
                                {
                                    const string str = ADF_FREQUENCY_BAND_COMMAND + "INC\n";
                                    DCSBIOS.Send(str);
                                    freqBandDialSendCount++;
                                    Interlocked.Exchange(ref _adfFrequencyBandWaitingForFeedback, 1);
                                }
                                else if (_adfCockpitFrequencyBand > desiredFreqBandDialPos)
                                {
                                    const string str = ADF_FREQUENCY_BAND_COMMAND + "DEC\n";
                                    DCSBIOS.Send(str);
                                    freqBandDialSendCount++;
                                    Interlocked.Exchange(ref _adfFrequencyBandWaitingForFeedback, 1);
                                }

                                Reset(ref freqBandDialTimeout);
                            }
                        }
                        else
                        {
                            freqBandDialOkTime = DateTime.Now.Ticks;
                        }

                        if (freqBandDialSendCount > 3)
                        {
                            // "Race" condition detected?
                            freqBandDialSendCount = 0;
                            Thread.Sleep(5000);
                        }

                        Thread.Sleep(SynchSleepTime); // Should be enough to get an update cycle from DCS-BIOS

                        if (once)
                        {
                            UpdateCockpitAdfFrequency(_adfCockpitFrequencyRaw);
                            once = false;
                        }
                    }
                    while (IsTooShort(freqBandDialOkTime));
                }
                catch (ThreadAbortException)
                {
                }
                catch (Exception ex)
                {
                    Common.LogError(ex);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _adfThreadNowSynching, 0);
            }

            Interlocked.Add(ref _doUpdatePanelLCD, 1);
        }

        private void SendUhfPresetChannelChangeToDCSBIOS()
        {
            try
            {
                // Preset Channel selector
                // Pos    1 - 20
                lock (_lockUhfPresetChannelObject)
                {
                    switch (_uhfCockpitPresetChannel)
                    {
                        case 1:
                            {
                                _uhfIncreasePresetChannel = true;
                                break;
                            }

                        case 20:
                            {
                                _uhfIncreasePresetChannel = false;
                                break;
                            }
                    }
                }

                DCSBIOS.Send(_uhfIncreasePresetChannel ? UHF_PRESET_DIAL_COMMAND_INC : UHF_PRESET_DIAL_COMMAND_DEC);
                Interlocked.Add(ref _doUpdatePanelLCD, 1);
            }
            catch (Exception ex)
            {
                Common.LogError(ex);
            }
        }

        private void CheckFrequenciesForValidity()
        {
            // Crude fix if any freqs are outside the valid boundaries

            // VHF COMM
            // 116.00 - 149.975
            if (_vhfCommBigFrequencyStandby < 116)
            {
                _vhfCommBigFrequencyStandby = 116;
            }

            if (_vhfCommBigFrequencyStandby > 151)
            {
                _vhfCommBigFrequencyStandby = 151;
            }
        }

        private string GetVhfCommSmallFreqString()
        {
            if (_vhfCommSmallFrequencyStandby < 10)
            {
                return "0" + _vhfCommSmallFrequencyStandby;
            }

            return _vhfCommSmallFrequencyStandby.ToString();
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

                CheckFrequenciesForValidity();

                /*
                1 byte (header byte 0x0) [0]
                5 bytes upper left LCD   [1 - 5]
                5 bytes upper right LCD  [6 - 10]
                5 bytes lower left LCD   [11- 15]
                5 bytes lower right LCD  [16- 20]

                0x01 - 0x09 displays the figure 1-9
                0xD1 - 0xD9 displays the figure 1.-9. (figure followed by dot)
                0xFF -> blank, nothing is shown in that spot.
             */
                var bytes = new byte[21];
                bytes[0] = 0x0;

                switch (_currentUpperRadioMode)
                {
                    case CurrentUH1HRadioMode.INTERCOMM:
                        {
                            lock (_lockIntercommDialObject)
                            {
                                SetPZ69DisplayBytesUnsignedInteger(ref bytes, Convert.ToUInt32(_uhfCockpitPresetChannel), PZ69LCDPosition.UPPER_STBY_RIGHT);
                                SetPZ69DisplayBytesUnsignedInteger(ref bytes, _intercommCockpitDial1Pos, PZ69LCDPosition.UPPER_ACTIVE_LEFT);
                            }

                            break;
                        }

                    case CurrentUH1HRadioMode.VHFCOMM:
                        {
                            lock (_lockVhfCommDialsObject1)
                            {
                                lock (_lockVhfCommDialsObject2)
                                {
                                    SetPZ69DisplayBytesDefault(ref bytes, _vhfCommCockpitFrequency, PZ69LCDPosition.UPPER_ACTIVE_LEFT);
                                    SetPZ69DisplayBytesDefault(
                                        ref bytes,
                                        double.Parse(_vhfCommBigFrequencyStandby + "." + GetVhfCommSmallFreqString(), NumberFormatInfoFullDisplay),
                                        PZ69LCDPosition.UPPER_STBY_RIGHT);
                                }
                            }

                            break;
                        }

                    case CurrentUH1HRadioMode.UHF:
                        {
                            lock (_lockUhfDialsObject1)
                            {
                                lock (_lockUhfDialsObject2)
                                {
                                    lock (_lockUhfDialsObject3)
                                    {
                                        // 251.75
                                        var filler = string.Empty;
                                        if (_uhfCockpitDial3Frequency < 10)
                                        {
                                            filler = "0";
                                        }

                                        var fillerUhf = string.Empty;
                                        if (_uhfSmallFrequencyStandby < 10)
                                        {
                                            fillerUhf = "0";
                                        }

                                        var lcdFrequencyCockpit = double.Parse(
                                            this._uhfCockpitDial1Frequency + _uhfCockpitDial2Frequency.ToString() + "." + filler + this._uhfCockpitDial3Frequency,
                                            NumberFormatInfoFullDisplay);
                                        var lcdFrequencyStandby = double.Parse(this._uhfBigFrequencyStandby + "." + fillerUhf + this._uhfSmallFrequencyStandby, NumberFormatInfoFullDisplay);
                                        SetPZ69DisplayBytesDefault(ref bytes, lcdFrequencyCockpit, PZ69LCDPosition.UPPER_ACTIVE_LEFT);
                                        SetPZ69DisplayBytesDefault(ref bytes, lcdFrequencyStandby, PZ69LCDPosition.UPPER_STBY_RIGHT);
                                    }
                                }
                            }

                            break;
                        }

                    case CurrentUH1HRadioMode.VHFNAV:
                        {
                            lock (_lockVhfNavDialsObject1)
                            {
                                lock (_lockVhfNavDialsObject2)
                                {
                                    // 107.75
                                    var filler = string.Empty;
                                    if (_vhfNavCockpitDial2Frequency < 10)
                                    {
                                        filler = "0";
                                    }

                                    var fillerVhfNav = string.Empty;
                                    if (_vhfNavSmallFrequencyStandby < 10)
                                    {
                                        fillerVhfNav = "0";
                                    }

                                    var lcdFrequencyCockpit = double.Parse(this._vhfNavCockpitDial1Frequency + "." + filler + this._vhfNavCockpitDial2Frequency, NumberFormatInfoFullDisplay);
                                    var lcdFrequencyStandby = double.Parse(this._vhfNavBigFrequencyStandby + "." + fillerVhfNav + this._vhfNavSmallFrequencyStandby, NumberFormatInfoFullDisplay);
                                    SetPZ69DisplayBytesDefault(ref bytes, lcdFrequencyCockpit, PZ69LCDPosition.UPPER_ACTIVE_LEFT);
                                    SetPZ69DisplayBytesDefault(ref bytes, lcdFrequencyStandby, PZ69LCDPosition.UPPER_STBY_RIGHT);
                                }
                            }

                            break;
                        }

                    case CurrentUH1HRadioMode.VHFFM:
                        {
                            // Mhz   30-75
                            // Khz   0 - 95
                            lock (_lockVhfFmDialsObject1)
                            {
                                lock (_lockVhfFmDialsObject2)
                                {
                                    lock (_lockVhfFmDialsObject3)
                                    {
                                        lock (_lockVhfFmDialsObject4)
                                        {
                                            var activeFrequencyAsString = (this._vhfFmCockpitFreq1DialPos + 3) + _vhfFmCockpitFreq2DialPos.ToString() + "." + this._vhfFmCockpitFreq3DialPos
                                                                          + (_vhfFmCockpitFreq4DialPos == 0 ? "0" : "5");

                                            var lcdFrequencyCockpit = double.Parse(activeFrequencyAsString, NumberFormatInfoFullDisplay);

                                            var fillerVhfFm = string.Empty;
                                            if (_vhfFmSmallFrequencyStandby < 10)
                                            {
                                                fillerVhfFm = "0";
                                            }

                                            var lcdFrequencyStandby = double.Parse(_vhfFmBigFrequencyStandby + "." + fillerVhfFm + _vhfFmSmallFrequencyStandby, NumberFormatInfoFullDisplay);
                                            SetPZ69DisplayBytes(ref bytes, lcdFrequencyCockpit, 2, PZ69LCDPosition.UPPER_ACTIVE_LEFT);
                                            SetPZ69DisplayBytes(ref bytes, lcdFrequencyStandby, 2, PZ69LCDPosition.UPPER_STBY_RIGHT);
                                        }
                                    }
                                }
                            }

                            break;
                        }

                    case CurrentUH1HRadioMode.ADF:
                        {
                            lock (_lockAdfCockpitFrequencyObject)
                            {
                                SetPZ69DisplayBytesDefault(ref bytes, _adfCockpitFrequency, PZ69LCDPosition.UPPER_ACTIVE_LEFT);
                            }

                            lock (_lockAdfSignalStrengthObject)
                            {
                                SetPZ69DisplayBytesInteger(ref bytes, Convert.ToInt32(Math.Truncate(_adfSignalStrength)), PZ69LCDPosition.UPPER_STBY_RIGHT);
                            }

                            break;
                        }
                }

                switch (_currentLowerRadioMode)
                {
                    case CurrentUH1HRadioMode.INTERCOMM:
                        {
                            lock (_lockIntercommDialObject)
                            {
                                SetPZ69DisplayBytesUnsignedInteger(ref bytes, Convert.ToUInt32(_uhfCockpitPresetChannel), PZ69LCDPosition.LOWER_STBY_RIGHT);
                                SetPZ69DisplayBytesUnsignedInteger(ref bytes, _intercommCockpitDial1Pos, PZ69LCDPosition.LOWER_ACTIVE_LEFT);
                            }

                            break;
                        }

                    case CurrentUH1HRadioMode.VHFCOMM:
                        {
                            lock (_lockVhfCommDialsObject1)
                            {
                                lock (_lockVhfCommDialsObject2)
                                {
                                    SetPZ69DisplayBytesDefault(ref bytes, _vhfCommCockpitFrequency, PZ69LCDPosition.LOWER_ACTIVE_LEFT);
                                    SetPZ69DisplayBytesDefault(
                                        ref bytes,
                                        double.Parse(_vhfCommBigFrequencyStandby + "." + GetVhfCommSmallFreqString(), NumberFormatInfoFullDisplay),
                                        PZ69LCDPosition.LOWER_STBY_RIGHT);
                                }
                            }

                            break;
                        }

                    case CurrentUH1HRadioMode.UHF:
                        {
                            lock (_lockUhfDialsObject1)
                            {
                                lock (_lockUhfDialsObject2)
                                {
                                    lock (_lockUhfDialsObject3)
                                    {
                                        // 251.75
                                        var filler = string.Empty;
                                        if (_uhfCockpitDial3Frequency < 10)
                                        {
                                            filler = "0";
                                        }

                                        var fillerUhf = string.Empty;
                                        if (_uhfSmallFrequencyStandby < 10)
                                        {
                                            fillerUhf = "0";
                                        }

                                        var lcdFrequencyCockpit = double.Parse(
                                            this._uhfCockpitDial1Frequency + _uhfCockpitDial2Frequency.ToString() + "." + filler + this._uhfCockpitDial3Frequency,
                                            NumberFormatInfoFullDisplay);
                                        var lcdFrequencyStandby = double.Parse(this._uhfBigFrequencyStandby + "." + fillerUhf + this._uhfSmallFrequencyStandby, NumberFormatInfoFullDisplay);
                                        SetPZ69DisplayBytesDefault(ref bytes, lcdFrequencyCockpit, PZ69LCDPosition.LOWER_ACTIVE_LEFT);
                                        SetPZ69DisplayBytesDefault(ref bytes, lcdFrequencyStandby, PZ69LCDPosition.LOWER_STBY_RIGHT);
                                    }
                                }
                            }

                            break;
                        }

                    case CurrentUH1HRadioMode.VHFNAV:
                        {
                            lock (_lockVhfNavDialsObject1)
                            {
                                lock (_lockVhfNavDialsObject2)
                                {
                                    // 107.75
                                    var filler = string.Empty;
                                    if (_vhfNavCockpitDial2Frequency < 10)
                                    {
                                        filler = "0";
                                    }

                                    var fillerVhfNav = string.Empty;
                                    if (_vhfNavSmallFrequencyStandby < 10)
                                    {
                                        fillerVhfNav = "0";
                                    }

                                    var lcdFrequencyCockpit = double.Parse(this._vhfNavCockpitDial1Frequency + "." + filler + this._vhfNavCockpitDial2Frequency, NumberFormatInfoFullDisplay);
                                    var lcdFrequencyStandby = double.Parse(this._vhfNavBigFrequencyStandby + "." + fillerVhfNav + this._vhfNavSmallFrequencyStandby, NumberFormatInfoFullDisplay);
                                    SetPZ69DisplayBytesDefault(ref bytes, lcdFrequencyCockpit, PZ69LCDPosition.LOWER_ACTIVE_LEFT);
                                    SetPZ69DisplayBytesDefault(ref bytes, lcdFrequencyStandby, PZ69LCDPosition.LOWER_STBY_RIGHT);
                                }
                            }

                            break;
                        }

                    case CurrentUH1HRadioMode.VHFFM:
                        {
                            // Mhz   30-75
                            // Khz   0 - 95
                            lock (_lockVhfFmDialsObject1)
                            {
                                lock (_lockVhfFmDialsObject2)
                                {
                                    lock (_lockVhfFmDialsObject3)
                                    {
                                        lock (_lockVhfFmDialsObject4)
                                        {
                                            var activeFrequencyAsString = (this._vhfFmCockpitFreq1DialPos + 3) + _vhfFmCockpitFreq2DialPos.ToString() + "." + this._vhfFmCockpitFreq3DialPos
                                                                          + (_vhfFmCockpitFreq4DialPos == 0 ? "0" : "5");

                                            var lcdFrequencyCockpit = double.Parse(activeFrequencyAsString, NumberFormatInfoFullDisplay);

                                            var fillerVhfFm = string.Empty;
                                            if (_vhfFmSmallFrequencyStandby < 10)
                                            {
                                                fillerVhfFm = "0";
                                            }

                                            var lcdFrequencyStandby = double.Parse(_vhfFmBigFrequencyStandby + "." + fillerVhfFm + _vhfFmSmallFrequencyStandby, NumberFormatInfoFullDisplay);
                                            SetPZ69DisplayBytes(ref bytes, lcdFrequencyCockpit, 2, PZ69LCDPosition.LOWER_ACTIVE_LEFT);
                                            SetPZ69DisplayBytes(ref bytes, lcdFrequencyStandby, 2, PZ69LCDPosition.LOWER_STBY_RIGHT);
                                        }
                                    }
                                }
                            }

                            break;
                        }

                    case CurrentUH1HRadioMode.ADF:
                        {
                            lock (_lockAdfCockpitFrequencyObject)
                            {
                                SetPZ69DisplayBytesDefault(ref bytes, _adfCockpitFrequency, PZ69LCDPosition.LOWER_ACTIVE_LEFT);
                            }

                            lock (_lockAdfSignalStrengthObject)
                            {
                                SetPZ69DisplayBytesInteger(ref bytes, Convert.ToInt32(Math.Truncate(_adfSignalStrength)), PZ69LCDPosition.LOWER_STBY_RIGHT);
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
                var radioPanelKnobUh1H = (RadioPanelKnobUH1H)o;
                if (radioPanelKnobUh1H.IsOn)
                {
                    switch (radioPanelKnobUh1H.RadioPanelPZ69Knob)
                    {
                        case RadioPanelPZ69KnobsUH1H.UPPER_LARGE_FREQ_WHEEL_INC:
                            {
                                switch (_currentUpperRadioMode)
                                {
                                    case CurrentUH1HRadioMode.INTERCOMM:
                                        {
                                            if (!SkipIntercomm())
                                            {
                                                DCSBIOS.Send(INTERCOMM_VOLUME_KNOB_COMMAND_DEC);
                                            }

                                            break;
                                        }

                                    case CurrentUH1HRadioMode.VHFCOMM:
                                        {
                                            // 116-149
                                            if (_vhfCommBigFrequencyStandby.Equals(149))
                                            {
                                                // @ max value
                                                break;
                                            }

                                            _vhfCommBigFrequencyStandby++;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.UHF:
                                        {
                                            // 225-399
                                            if (_uhfBigFrequencyStandby.Equals(399))
                                            {
                                                // @ max value
                                                break;
                                            }

                                            _uhfBigFrequencyStandby++;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.VHFFM:
                                        {
                                            // 30-75
                                            if (_vhfFmBigFrequencyStandby.Equals(75))
                                            {
                                                // @ max value
                                                break;
                                            }

                                            _vhfFmBigFrequencyStandby++;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.VHFNAV:
                                        {
                                            if (_vhfNavBigFrequencyStandby.Equals(126))
                                            {
                                                // @ max value
                                                break;
                                            }

                                            _vhfNavBigFrequencyStandby++;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.ADF:
                                        {
                                            DCSBIOS.Send(ADF_TUNE_KNOB_COMMAND_INC);
                                            break;
                                        }
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.UPPER_LARGE_FREQ_WHEEL_DEC:
                            {
                                switch (_currentUpperRadioMode)
                                {
                                    case CurrentUH1HRadioMode.INTERCOMM:
                                        {
                                            if (!SkipIntercomm())
                                            {
                                                DCSBIOS.Send(INTERCOMM_VOLUME_KNOB_COMMAND_INC);
                                            }

                                            break;
                                        }

                                    case CurrentUH1HRadioMode.VHFCOMM:
                                        {
                                            // 116-149
                                            if (_vhfCommBigFrequencyStandby.Equals(116))
                                            {
                                                // @ min value
                                                break;
                                            }

                                            _vhfCommBigFrequencyStandby--;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.UHF:
                                        {
                                            if (_uhfBigFrequencyStandby.Equals(200))
                                            {
                                                // @ min value
                                                break;
                                            }

                                            _uhfBigFrequencyStandby--;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.VHFFM:
                                        {
                                            // 30-75
                                            if (_vhfFmBigFrequencyStandby.Equals(30))
                                            {
                                                // @ min value
                                                break;
                                            }

                                            _vhfFmBigFrequencyStandby--;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.VHFNAV:
                                        {
                                            if (_vhfNavBigFrequencyStandby.Equals(107))
                                            {
                                                // @ min value
                                                break;
                                            }

                                            _vhfNavBigFrequencyStandby--;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.ADF:
                                        {
                                            DCSBIOS.Send(ADF_TUNE_KNOB_COMMAND_DEC);
                                            break;
                                        }
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.UPPER_SMALL_FREQ_WHEEL_INC:
                            {
                                switch (_currentUpperRadioMode)
                                {
                                    case CurrentUH1HRadioMode.INTERCOMM:
                                        {
                                            if (!SkipIntercomm())
                                            {
                                                DCSBIOS.Send(INTERCOMM_DIAL_COMMAND_INC);
                                            }

                                            break;
                                        }

                                    case CurrentUH1HRadioMode.VHFCOMM:
                                        {
                                            _vhfCommSmallFrequencyStandby = QuarterFrequencyStandbyAdjust(_vhfCommSmallFrequencyStandby, true);
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.UHF:
                                        {
                                            // Small dial 0.000 0.025 0.050 0.075 [only 0.00 and 0.05 are used]
                                            if (_uhfSmallFrequencyStandby >= 95)
                                            {
                                                // At max value
                                                _uhfSmallFrequencyStandby = 0;
                                                break;
                                            }

                                            _uhfSmallFrequencyStandby = _uhfSmallFrequencyStandby + 5;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.VHFFM:
                                        {
                                            if (_vhfFmSmallFrequencyStandby >= 95)
                                            {
                                                // At max value
                                                _vhfFmSmallFrequencyStandby = 0;
                                                break;
                                            }

                                            _vhfFmSmallFrequencyStandby += 5;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.VHFNAV:
                                        {
                                            if (_vhfNavSmallFrequencyStandby >= 95)
                                            {
                                                // At max value
                                                _vhfNavSmallFrequencyStandby = 0;
                                                break;
                                            }

                                            _vhfNavSmallFrequencyStandby += 5;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.ADF:
                                        {
                                            DCSBIOS.Send(ADF_GAIN_KNOB_COMMAND_INC);
                                            break;
                                        }
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.UPPER_SMALL_FREQ_WHEEL_DEC:
                            {
                                switch (_currentUpperRadioMode)
                                {
                                    case CurrentUH1HRadioMode.INTERCOMM:
                                        {
                                            if (!SkipIntercomm())
                                            {
                                                DCSBIOS.Send(INTERCOMM_DIAL_COMMAND_DEC);
                                            }

                                            break;
                                        }

                                    case CurrentUH1HRadioMode.VHFCOMM:
                                        {
                                            _vhfCommSmallFrequencyStandby = QuarterFrequencyStandbyAdjust(_vhfCommSmallFrequencyStandby, false);
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.UHF:
                                        {
                                            if (_uhfSmallFrequencyStandby <= 0)
                                            {
                                                // At min value
                                                _uhfSmallFrequencyStandby = 95;
                                                break;
                                            }

                                            _uhfSmallFrequencyStandby = _uhfSmallFrequencyStandby - 5;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.VHFFM:
                                        {
                                            if (_vhfFmSmallFrequencyStandby <= 0)
                                            {
                                                // At min value
                                                _vhfFmSmallFrequencyStandby = 95;
                                                break;
                                            }

                                            _vhfFmSmallFrequencyStandby -= 5;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.VHFNAV:
                                        {
                                            if (_vhfNavSmallFrequencyStandby <= 0)
                                            {
                                                // At min value
                                                _vhfNavSmallFrequencyStandby = 95;
                                                break;
                                            }

                                            _vhfNavSmallFrequencyStandby -= 5;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.ADF:
                                        {
                                            DCSBIOS.Send(ADF_GAIN_KNOB_COMMAND_DEC);
                                            break;
                                        }
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.LOWER_LARGE_FREQ_WHEEL_INC:
                            {
                                switch (_currentLowerRadioMode)
                                {
                                    case CurrentUH1HRadioMode.INTERCOMM:
                                        {
                                            if (!SkipIntercomm())
                                            {
                                                DCSBIOS.Send(INTERCOMM_VOLUME_KNOB_COMMAND_DEC);
                                            }

                                            break;
                                        }

                                    case CurrentUH1HRadioMode.VHFCOMM:
                                        {
                                            // 116-149
                                            if (_vhfCommBigFrequencyStandby.Equals(149))
                                            {
                                                // @ max value
                                                break;
                                            }

                                            _vhfCommBigFrequencyStandby++;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.UHF:
                                        {
                                            // 225-399
                                            if (_uhfBigFrequencyStandby.Equals(399))
                                            {
                                                // @ max value
                                                break;
                                            }

                                            _uhfBigFrequencyStandby++;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.VHFFM:
                                        {
                                            // 30-75
                                            if (_vhfFmBigFrequencyStandby.Equals(75))
                                            {
                                                // @ max value
                                                break;
                                            }

                                            _vhfFmBigFrequencyStandby++;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.VHFNAV:
                                        {
                                            if (_vhfNavBigFrequencyStandby.Equals(126))
                                            {
                                                // @ max value
                                                break;
                                            }

                                            _vhfNavBigFrequencyStandby++;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.ADF:
                                        {
                                            DCSBIOS.Send(ADF_TUNE_KNOB_COMMAND_INC);
                                            break;
                                        }
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.LOWER_LARGE_FREQ_WHEEL_DEC:
                            {
                                switch (_currentLowerRadioMode)
                                {
                                    case CurrentUH1HRadioMode.INTERCOMM:
                                        {
                                            if (!SkipIntercomm())
                                            {
                                                DCSBIOS.Send(INTERCOMM_VOLUME_KNOB_COMMAND_INC);
                                            }

                                            break;
                                        }

                                    case CurrentUH1HRadioMode.VHFCOMM:
                                        {
                                            // 116-149
                                            if (_vhfCommBigFrequencyStandby.Equals(116))
                                            {
                                                // @ min value
                                                break;
                                            }

                                            _vhfCommBigFrequencyStandby--;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.UHF:
                                        {
                                            if (_uhfBigFrequencyStandby.Equals(200))
                                            {
                                                // @ min value
                                                break;
                                            }

                                            _uhfBigFrequencyStandby--;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.VHFFM:
                                        {
                                            // 30-75
                                            if (_vhfFmBigFrequencyStandby.Equals(30))
                                            {
                                                // @ min value
                                                break;
                                            }

                                            _vhfFmBigFrequencyStandby--;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.VHFNAV:
                                        {
                                            if (_vhfNavBigFrequencyStandby.Equals(107))
                                            {
                                                // @ min value
                                                break;
                                            }

                                            _vhfNavBigFrequencyStandby--;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.ADF:
                                        {
                                            DCSBIOS.Send(ADF_TUNE_KNOB_COMMAND_DEC);
                                            break;
                                        }
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.LOWER_SMALL_FREQ_WHEEL_INC:
                            {
                                switch (_currentLowerRadioMode)
                                {
                                    case CurrentUH1HRadioMode.INTERCOMM:
                                        {
                                            if (!SkipIntercomm())
                                            {
                                                DCSBIOS.Send(INTERCOMM_DIAL_COMMAND_INC);
                                            }

                                            break;
                                        }

                                    case CurrentUH1HRadioMode.VHFCOMM:
                                        {
                                            _vhfCommSmallFrequencyStandby = QuarterFrequencyStandbyAdjust(_vhfCommSmallFrequencyStandby, true);
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.UHF:
                                        {
                                            // Small dial 0.000 0.025 0.050 0.075 [only 0.00 and 0.05 are used]
                                            if (_uhfSmallFrequencyStandby >= 95)
                                            {
                                                // At max value
                                                _uhfSmallFrequencyStandby = 0;
                                                break;
                                            }

                                            _uhfSmallFrequencyStandby = _uhfSmallFrequencyStandby + 5;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.VHFFM:
                                        {
                                            if (_vhfFmSmallFrequencyStandby >= 95)
                                            {
                                                // At max value
                                                _vhfFmSmallFrequencyStandby = 0;
                                                break;
                                            }

                                            _vhfFmSmallFrequencyStandby += 5;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.VHFNAV:
                                        {
                                            if (_vhfNavSmallFrequencyStandby >= 95)
                                            {
                                                // At max value
                                                _vhfNavSmallFrequencyStandby = 0;
                                                break;
                                            }

                                            _vhfNavSmallFrequencyStandby += 5;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.ADF:
                                        {
                                            DCSBIOS.Send(ADF_GAIN_KNOB_COMMAND_INC);
                                            break;
                                        }
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.LOWER_SMALL_FREQ_WHEEL_DEC:
                            {
                                switch (_currentLowerRadioMode)
                                {
                                    case CurrentUH1HRadioMode.INTERCOMM:
                                        {
                                            if (!SkipIntercomm())
                                            {
                                                DCSBIOS.Send(INTERCOMM_DIAL_COMMAND_DEC);
                                            }

                                            break;
                                        }

                                    case CurrentUH1HRadioMode.VHFCOMM:
                                        {
                                            _vhfCommSmallFrequencyStandby = QuarterFrequencyStandbyAdjust(_vhfCommSmallFrequencyStandby, true);
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.UHF:
                                        {
                                            if (_uhfSmallFrequencyStandby <= 0)
                                            {
                                                // At min value
                                                _uhfSmallFrequencyStandby = 95;
                                                break;
                                            }

                                            _uhfSmallFrequencyStandby = _uhfSmallFrequencyStandby - 5;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.VHFFM:
                                        {
                                            if (_vhfFmSmallFrequencyStandby <= 0)
                                            {
                                                // At min value
                                                _vhfFmSmallFrequencyStandby = 95;
                                                break;
                                            }

                                            _vhfFmSmallFrequencyStandby -= 5;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.VHFNAV:
                                        {
                                            if (_vhfNavSmallFrequencyStandby <= 0)
                                            {
                                                // At min value
                                                _vhfNavSmallFrequencyStandby = 95;
                                                break;
                                            }

                                            _vhfNavSmallFrequencyStandby -= 5;
                                            break;
                                        }

                                    case CurrentUH1HRadioMode.ADF:
                                        {
                                            DCSBIOS.Send(ADF_GAIN_KNOB_COMMAND_DEC);
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

        private uint QuarterFrequencyStandbyAdjust(uint frequency, bool increase)
        {
            uint result = 0;
            var tmp = frequency.ToString(CultureInfo.InvariantCulture);
            if (increase)
            {
                /*
                 * "1"
                 * "12"
                 * "15"
                 * "17"
                 * "97"
                 */
                if (tmp.Length == 1)
                {
                    switch (frequency)
                    {
                        case 0:
                            {
                                result = frequency + 2;
                                break;
                            }

                        case 2:
                            {
                                result = frequency + 3;
                                break;
                            }

                        case 5:
                            {
                                result = frequency + 2;
                                break;
                            }

                        case 7:
                            {
                                result = frequency + 3;
                                break;
                            }

                        default:
                            {
                                // In case it is an invalid position invalid
                                result = 0;
                                break;
                            }
                    }
                }
                else if (tmp.Length == 2)
                {
                    if (tmp.EndsWith("0"))
                    {
                        result = frequency + 2;
                    }
                    else if (tmp.EndsWith("2"))
                    {
                        result = frequency + 3;
                    }
                    else if (tmp.EndsWith("5"))
                    {
                        result = frequency + 2;
                    }
                    else if (tmp.EndsWith("7"))
                    {
                        result = frequency + 3;
                    }
                    else
                    {
                        // In case it is in an invalid position
                        result = 0;
                    }
                }
            }
            else
            {
                if (tmp.Length == 1)
                {
                    switch (frequency)
                    {
                        case 0:
                            {
                                result = 97;
                                break;
                            }

                        case 2:
                            {
                                result = frequency - 2;
                                break;
                            }

                        case 5:
                            {
                                result = frequency - 3;
                                break;
                            }

                        case 7:
                            {
                                result = frequency - 2;
                                break;
                            }

                        default:
                            {
                                // In case it is in an invalid position
                                result = 0;
                                break;
                            }
                    }
                }
                else if (tmp.Length == 2)
                {
                    if (tmp.EndsWith("0"))
                    {
                        if ((int)frequency - 3 < 0)
                        {
                            result = 97;
                        }
                        else
                        {
                            result = frequency - 3;
                        }
                    }
                    else if (tmp.EndsWith("2"))
                    {
                        if ((int)frequency - 3 < 0)
                        {
                            result = 97;
                        }
                        else
                        {
                            result = frequency - 2;
                        }
                    }
                    else if (tmp.EndsWith("5"))
                    {
                        if ((int)frequency - 3 < 0)
                        {
                            result = 97;
                        }
                        else
                        {
                            result = frequency - 3;
                        }
                    }
                    else if (tmp.EndsWith("7"))
                    {
                        if ((int)frequency - 3 < 0)
                        {
                            result = 97;
                        }
                        else
                        {
                            result = frequency - 2;
                        }
                    }
                    else
                    {
                        // In case it is in an invalid position
                        result = 0;
                    }
                }
            }

            if (frequency > 97)
            {
                result = 0;
            }

            return result;
        }

        private bool SkipIntercomm()
        {
            if (_currentUpperRadioMode == CurrentUH1HRadioMode.INTERCOMM || _currentLowerRadioMode == CurrentUH1HRadioMode.INTERCOMM)
            {
                if (_intercommSkipper > 1)
                {
                    _intercommSkipper = 0;
                    return false;
                }

                this._intercommSkipper++;
                return true;
            }

            return false;
        }

        public void PZ69KnobChanged(bool isFirstReport, IEnumerable<object> hashSet)
        {
            Interlocked.Add(ref _doUpdatePanelLCD, 1);
            lock (LockLCDUpdateObject)
            {
                foreach (var radioPanelKnobObject in hashSet)
                {
                    var radioPanelKnob = (RadioPanelKnobUH1H)radioPanelKnobObject;

                    switch (radioPanelKnob.RadioPanelPZ69Knob)
                    {
                        case RadioPanelPZ69KnobsUH1H.UPPER_INTERCOMM:
                            {
                                if (radioPanelKnob.IsOn)
                                {
                                    _currentUpperRadioMode = CurrentUH1HRadioMode.INTERCOMM;
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.UPPER_VHFCOMM:
                            {
                                if (radioPanelKnob.IsOn)
                                {
                                    _currentUpperRadioMode = CurrentUH1HRadioMode.VHFCOMM;
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.UPPER_UHF:
                            {
                                if (radioPanelKnob.IsOn)
                                {
                                    _currentUpperRadioMode = CurrentUH1HRadioMode.UHF;
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.UPPER_VHFFM:
                            {
                                if (radioPanelKnob.IsOn)
                                {
                                    _currentUpperRadioMode = CurrentUH1HRadioMode.VHFFM;
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.UPPER_VHFNAV:
                            {
                                if (radioPanelKnob.IsOn)
                                {
                                    _currentUpperRadioMode = CurrentUH1HRadioMode.VHFNAV;
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.UPPER_ADF:
                            {
                                if (radioPanelKnob.IsOn)
                                {
                                    _currentUpperRadioMode = CurrentUH1HRadioMode.ADF;
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.UPPER_DME:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.LOWER_INTERCOMM:
                            {
                                if (radioPanelKnob.IsOn)
                                {
                                    _currentLowerRadioMode = CurrentUH1HRadioMode.INTERCOMM;
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.LOWER_VHFCOMM:
                            {
                                if (radioPanelKnob.IsOn)
                                {
                                    _currentLowerRadioMode = CurrentUH1HRadioMode.VHFCOMM;
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.LOWER_UHF:
                            {
                                if (radioPanelKnob.IsOn)
                                {
                                    _currentLowerRadioMode = CurrentUH1HRadioMode.UHF;
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.LOWER_VHFFM:
                            {
                                if (radioPanelKnob.IsOn)
                                {
                                    _currentLowerRadioMode = CurrentUH1HRadioMode.VHFFM;
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.LOWER_VHFNAV:
                            {
                                if (radioPanelKnob.IsOn)
                                {
                                    _currentLowerRadioMode = CurrentUH1HRadioMode.VHFNAV;
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.LOWER_ADF:
                            {
                                if (radioPanelKnob.IsOn)
                                {
                                    _currentLowerRadioMode = CurrentUH1HRadioMode.ADF;
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.LOWER_DME:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.UPPER_LARGE_FREQ_WHEEL_INC:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.UPPER_LARGE_FREQ_WHEEL_DEC:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.UPPER_SMALL_FREQ_WHEEL_INC:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.UPPER_SMALL_FREQ_WHEEL_DEC:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.LOWER_LARGE_FREQ_WHEEL_INC:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.LOWER_LARGE_FREQ_WHEEL_DEC:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.LOWER_SMALL_FREQ_WHEEL_INC:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.LOWER_SMALL_FREQ_WHEEL_DEC:
                            {
                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.UPPER_FREQ_SWITCH:
                            {
                                if (_currentUpperRadioMode == CurrentUH1HRadioMode.ADF)
                                {
                                    if (radioPanelKnob.IsOn)
                                    {
                                        SendAdfBandChangeToDCSBIOS();
                                    }
                                }
                                else
                                {
                                    if (radioPanelKnob.IsOn)
                                    {
                                        SendFrequencyToDCSBIOS(RadioPanelPZ69KnobsUH1H.UPPER_FREQ_SWITCH);
                                    }
                                }

                                break;
                            }

                        case RadioPanelPZ69KnobsUH1H.LOWER_FREQ_SWITCH:
                            {
                                if (_currentLowerRadioMode == CurrentUH1HRadioMode.ADF)
                                {
                                    if (radioPanelKnob.IsOn)
                                    {
                                        SendAdfBandChangeToDCSBIOS();
                                    }
                                }
                                else
                                {
                                    if (radioPanelKnob.IsOn)
                                    {
                                        SendFrequencyToDCSBIOS(RadioPanelPZ69KnobsUH1H.LOWER_FREQ_SWITCH);
                                    }
                                }

                                break;
                            }
                    }

                    if (PluginManager.PlugSupportActivated && PluginManager.HasPlugin())
                    {
                        PluginManager.DoEvent(
                            ProfileHandler.SelectedProfile().Description,
                            HIDInstanceId,
                            (int)PluginGamingPanelEnum.PZ69RadioPanel,
                            (int)radioPanelKnob.RadioPanelPZ69Knob,
                            radioPanelKnob.IsOn,
                            null);
                    }
                }

                AdjustFrequency(hashSet);
            }
        }

        public sealed override void Startup()
        {
            try
            {
                StartupBase("UH-1H");

                NumberFormatInfoFullDisplay = new NumberFormatInfo();
                NumberFormatInfoFullDisplay.NumberDecimalSeparator = ".";
                NumberFormatInfoFullDisplay.NumberDecimalDigits = 4;
                NumberFormatInfoFullDisplay.NumberGroupSeparator = string.Empty;

                // VHF COMM
                _vhfCommDcsbiosOutputCockpitFrequency = DCSBIOSControlLocator.GetDCSBIOSOutput("VHFCOMM_FREQ");
                DCSBIOSStringManager.AddListener(_vhfCommDcsbiosOutputCockpitFrequency, this);

                // UHF
                _uhfDcsbiosOutputCockpitPresetChannel = DCSBIOSControlLocator.GetDCSBIOSOutput("UHF_PRESET");
                _uhfDcsbiosOutputCockpitFrequency = DCSBIOSControlLocator.GetDCSBIOSOutput("UHF_FREQ");
                DCSBIOSStringManager.AddListener(_uhfDcsbiosOutputCockpitFrequency, this);

                // VHF NAV
                _vhfNavDcsbiosOutputCockpitFrequency = DCSBIOSControlLocator.GetDCSBIOSOutput("VHFNAV_FREQ");
                DCSBIOSStringManager.AddListener(_vhfNavDcsbiosOutputCockpitFrequency, this);

                // INTERCOMM
                _intercommDcsbiosOutputCockpitPos = DCSBIOSControlLocator.GetDCSBIOSOutput("INT_MODE");

                // VHF FM
                _vhfFmDcsbiosOutputFreqDial1 = DCSBIOSControlLocator.GetDCSBIOSOutput("VHFFM_FREQ1");
                _vhfFmDcsbiosOutputFreqDial2 = DCSBIOSControlLocator.GetDCSBIOSOutput("VHFFM_FREQ2");
                _vhfFmDcsbiosOutputFreqDial3 = DCSBIOSControlLocator.GetDCSBIOSOutput("VHFFM_FREQ3");
                _vhfFmDcsbiosOutputFreqDial4 = DCSBIOSControlLocator.GetDCSBIOSOutput("VHFFM_FREQ4");

                // ADF (0-2)
                _adfDcsbiosOutputCockpitFrequencyBand = DCSBIOSControlLocator.GetDCSBIOSOutput("ADF_BAND");
                _adfDcsbiosOutputCockpitFrequency = DCSBIOSControlLocator.GetDCSBIOSOutput("ADF_FREQ");
                _adfDcsbiosOutputSignalStrength = DCSBIOSControlLocator.GetDCSBIOSOutput("ADF_SIGNAL");

                StartListeningForPanelChanges();
            }
            catch (Exception ex)
            {
                Common.LogError(ex);
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

        public override void ClearSettings(bool setIsDirty = false)
        {
        }

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
            SaitekPanelKnobs = RadioPanelKnobUH1H.GetRadioPanelKnobs();
        }

        private string GetCommandDirectionForVhfCommDial1(uint desiredFreq, uint actualFreq)
        {
            /*UH-1H AN/ARC-134 VHF Comm Radio Set*/
            // Large dial 116 - 149 [step of 1]
            const string inc = "INC\n";
            const string dec = "DEC\n";
            Debug.Print("Desired = " + desiredFreq + ", actual = " + actualFreq);
            if (desiredFreq > actualFreq && desiredFreq - actualFreq >= 16)
            {
                Debug.Print("A Returning DEC " + desiredFreq + ", actual = " + actualFreq);
                return dec;
            }

            if (desiredFreq > actualFreq && desiredFreq - actualFreq < 16)
            {
                Debug.Print("B Returning INC " + desiredFreq + ", actual = " + actualFreq);
                return inc;
            }

            if (desiredFreq < actualFreq && actualFreq - desiredFreq >= 16)
            {
                Debug.Print("C Returning INC " + desiredFreq + ", actual = " + actualFreq);
                return inc;
            }

            if (desiredFreq < actualFreq && actualFreq - desiredFreq < 16)
            {
                Debug.Print("D Returning DEC " + desiredFreq + ", actual = " + actualFreq);
                return dec;
            }

            throw new Exception("Should reach this code. GetCommandDirectionForVhfCommDial1(int desiredFreq, uint actualFreq)) -> " + desiredFreq + "   " + actualFreq);
        }

        private string GetCommandDirectionForVhfCommDial2(uint desiredFreq, uint actualFreq)
        {
            /*UH-1H AN/ARC-134 VHF Comm Radio Set*/

            // Small dial 000 - 975 [step of 25]
            // 000 025 050 075 100 125 150 175 200 225 250 275 300 325 350 375 400 425 450 475 500 525 550 575 600 625 650 675 700 725 750 775 800 825 850 875 900 925 950 975
            // 1   2   3   4   5   6   7   8   9   10  11  12  13  14  15  16  17  18  19  *20*  21  22  23  24  25  26  27  28  29  30  31  32  33  34  35  36  37  38  39  40
            // Only these are used because of PZ69 limitations
            // 00 05 10 15 20 25 30 35 40 45 50 55 60 65 70 75 80 85 90 95
            // 1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17 18 19 20   
            const int breakValue = 50;
            const string inc = "INC\n";
            const string dec = "DEC\n";
            if (desiredFreq > actualFreq && desiredFreq - actualFreq >= breakValue)
            {
                return dec;
            }

            if (desiredFreq > actualFreq && desiredFreq - actualFreq < breakValue)
            {
                return inc;
            }

            if (desiredFreq < actualFreq && actualFreq - desiredFreq >= breakValue)
            {
                return inc;
            }

            if (desiredFreq < actualFreq && actualFreq - desiredFreq < breakValue)
            {
                return dec;
            }

            throw new Exception("Should reach this code. GetCommandDirectionForVhfCommDial2(int desiredFreq, uint actualFreq)) -> " + desiredFreq + "   " + actualFreq);
        }

        private string GetCommandDirectionForUhfDial1(uint desiredFreq, uint actualFreq)
        {
            // Large dial 20 - 39 [step of 1]
            // d19 +/-10
            const string inc = "INC\n";
            const string dec = "DEC\n";
            if (desiredFreq > actualFreq && desiredFreq - actualFreq >= 10)
            {
                return dec;
            }

            if (desiredFreq > actualFreq && desiredFreq - actualFreq < 10)
            {
                return inc;
            }

            if (desiredFreq < actualFreq && actualFreq - desiredFreq >= 10)
            {
                return inc;
            }

            if (desiredFreq < actualFreq && actualFreq - desiredFreq < 10)
            {
                return dec;
            }

            throw new Exception("Should reach this code. GetCommandDirectionForUhfDial1(int desiredFreq, uint actualFreq)) -> " + desiredFreq + "   " + actualFreq);
        }

        private string GetCommandDirectionForUhfDial2(uint desiredFreq, uint actualFreq)
        {
            // 2nd dial 0 - 9 [step of 1]
            // +/-9
            const string inc = "INC\n";
            const string dec = "DEC\n";
            if (desiredFreq > actualFreq && desiredFreq - actualFreq >= 5)
            {
                return dec;
            }

            if (desiredFreq > actualFreq && desiredFreq - actualFreq < 5)
            {
                return inc;
            }

            if (desiredFreq < actualFreq && actualFreq - desiredFreq >= 5)
            {
                return inc;
            }

            if (desiredFreq < actualFreq && actualFreq - desiredFreq < 5)
            {
                return dec;
            }

            throw new Exception("Should reach this code. GetCommandDirectionForUhfDial2(int desiredFreq, uint actualFreq)) -> " + desiredFreq + "   " + actualFreq);
        }

        private string GetCommandDirectionForUhfDial3(uint desiredFreq, uint actualFreq)
        {
            // 00 05 10 15 20 25 30 35 40 45 50 55 60 65 70 75 80 85 90 95
            // 1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17 18 19 20   
            var breakValue = 50;
            const string inc = "INC\n";
            const string dec = "DEC\n";
            if (desiredFreq > actualFreq && desiredFreq - actualFreq >= breakValue)
            {
                return dec;
            }

            if (desiredFreq > actualFreq && desiredFreq - actualFreq < breakValue)
            {
                return inc;
            }

            if (desiredFreq < actualFreq && actualFreq - desiredFreq >= breakValue)
            {
                return inc;
            }

            if (desiredFreq < actualFreq && actualFreq - desiredFreq < breakValue)
            {
                return dec;
            }

            throw new Exception("Should reach this code. GetCommandDirectionForUhfDial3(int desiredFreq, uint actualFreq)) -> " + desiredFreq + "   " + actualFreq);
        }

        private string GetCommandDirectionForVhfNavDial1(uint desiredFreq, uint actualFreq)
        {
            /*UH-1H AN/ARC-134 VHF Comm Radio Set*/
            // Large dial 107-126  [step of 1]
            const string inc = "INC\n";
            const string dec = "DEC\n";
            if (desiredFreq > actualFreq && desiredFreq - actualFreq >= 10)
            {
                return dec;
            }

            if (desiredFreq > actualFreq && desiredFreq - actualFreq < 10)
            {
                return inc;
            }

            if (desiredFreq < actualFreq && actualFreq - desiredFreq >= 10)
            {
                return inc;
            }

            if (desiredFreq < actualFreq && actualFreq - desiredFreq < 10)
            {
                return dec;
            }

            throw new Exception("Should reach this code. GetCommandDirectionForVhfNavDial1(int desiredFreq, uint actualFreq)) -> " + desiredFreq + "   " + actualFreq);
        }

        private void SaveCockpitFrequencyVhfComm()
        {
            lock (_lockVhfCommDialsObject1)
            {
                lock (_lockVhfCommDialsObject2)
                {
                    _vhfCommSavedCockpitBigFrequency = _vhfCommCockpitDial1Frequency;
                    _vhfCommSavedCockpitSmallFrequency = _vhfCommCockpitDial2Frequency;
                }
            }
        }

        private void SwapCockpitStandbyFrequencyVhfComm()
        {
            _vhfCommBigFrequencyStandby = _vhfCommSavedCockpitBigFrequency;
            _vhfCommSmallFrequencyStandby = _vhfCommSavedCockpitSmallFrequency;
        }

        private void SaveCockpitFrequencyUhf()
        {
            lock (_lockUhfDialsObject1)
            {
                lock (_lockUhfDialsObject2)
                {
                    lock (_lockUhfDialsObject3)
                    {
                        _uhfSavedCockpitBigFrequency = uint.Parse(this._uhfCockpitDial1Frequency + _uhfCockpitDial2Frequency.ToString());
                        _uhfSavedCockpitSmallFrequency = _uhfCockpitDial3Frequency;
                    }
                }
            }
        }

        private void SwapCockpitStandbyFrequencyUhf()
        {
            _uhfBigFrequencyStandby = _uhfSavedCockpitBigFrequency;
            _uhfSmallFrequencyStandby = _uhfSavedCockpitSmallFrequency;
        }

        private void SaveCockpitFrequencyVhfFm()
        {
            lock (_lockVhfFmDialsObject1)
            {
                lock (_lockVhfFmDialsObject2)
                {
                    lock (_lockVhfFmDialsObject3)
                    {
                        lock (_lockVhfFmDialsObject4)
                        {
                            uint dial4 = 0;
                            switch (_vhfFmCockpitFreq4DialPos)
                            {
                                case 0:
                                    {
                                        dial4 = 0;
                                        break;
                                    }

                                case 1:
                                    {
                                        dial4 = 5;
                                        break;
                                    }
                            }

                            _vhfFmSavedCockpitBigFrequency = uint.Parse((this._vhfFmCockpitFreq1DialPos + 3) + _vhfFmCockpitFreq2DialPos.ToString());
                            _vhfFmSavedCockpitSmallFrequency = uint.Parse(this._vhfFmCockpitFreq3DialPos + dial4.ToString());
                        }
                    }
                }
            }
        }

        private void SwapCockpitStandbyFrequencyVhfFm()
        {
            _vhfFmBigFrequencyStandby = _vhfFmSavedCockpitBigFrequency;
            _vhfFmSmallFrequencyStandby = _vhfFmSavedCockpitSmallFrequency;
        }

        private void SaveCockpitFrequencyVhfNav()
        {
            lock (_lockVhfNavDialsObject1)
            {
                lock (_lockVhfNavDialsObject2)
                {
                    _vhfNavSavedCockpitBigFrequency = _vhfNavCockpitDial1Frequency;
                    _vhfNavSavedCockpitSmallFrequency = _vhfNavCockpitDial2Frequency;
                }
            }
        }

        private void SwapCockpitStandbyFrequencyVhfNav()
        {
            _vhfNavBigFrequencyStandby = _vhfNavSavedCockpitBigFrequency;
            _vhfNavSmallFrequencyStandby = _vhfNavSavedCockpitSmallFrequency;
        }

        private bool IntercommSyncing()
        {
            return Interlocked.Read(ref _intercommThreadNowSynching) > 0;
        }

        private bool VhfCommSyncing()
        {
            return Interlocked.Read(ref _vhfCommThreadNowSynching) > 0;
        }

        private bool VhfFmSyncing()
        {
            return Interlocked.Read(ref _vhfFmThreadNowSynching) > 0;
        }

        private bool UhfSyncing()
        {
            return Interlocked.Read(ref _uhfThreadNowSynching) > 0;
        }

        private bool VhfNavSyncing()
        {
            return Interlocked.Read(ref _vhfNavThreadNowSynching) > 0;
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

