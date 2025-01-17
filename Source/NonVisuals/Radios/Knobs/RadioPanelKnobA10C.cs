﻿namespace NonVisuals.Radios.Knobs
{
    using System;
    using System.Collections.Generic;

    using MEF;

    using NonVisuals.Interfaces;

    public class RadioPanelKnobA10C : ISaitekPanelKnob
    {
        public RadioPanelKnobA10C(int group, int mask, bool isOn, RadioPanelPZ69KnobsA10C radioPanelPZ69Knob)
        {
            Group = group;
            Mask = mask;
            IsOn = isOn;
            RadioPanelPZ69Knob = radioPanelPZ69Knob;
        }

        public int Group { get; set; }

        public int Mask { get; set; }

        public bool IsOn { get; set; }

        public RadioPanelPZ69KnobsA10C RadioPanelPZ69Knob { get; set; }

        public string ExportString()
        {
            return "RadioPanelKnob{" + Enum.GetName(typeof(RadioPanelPZ69KnobsA10C), RadioPanelPZ69Knob) + "}";
        }

        public static HashSet<ISaitekPanelKnob> GetRadioPanelKnobs()
        {
            // true means clockwise turn
            var result = new HashSet<ISaitekPanelKnob>();

            // Group 0
            result.Add(new RadioPanelKnobA10C(2, Convert.ToInt32("1", 2), true, RadioPanelPZ69KnobsA10C.UPPER_SMALL_FREQ_WHEEL_INC));
            result.Add(new RadioPanelKnobA10C(2, Convert.ToInt32("10", 2), false, RadioPanelPZ69KnobsA10C.UPPER_SMALL_FREQ_WHEEL_DEC));
            result.Add(new RadioPanelKnobA10C(2, Convert.ToInt32("100", 2), true, RadioPanelPZ69KnobsA10C.UPPER_LARGE_FREQ_WHEEL_INC));
            result.Add(new RadioPanelKnobA10C(2, Convert.ToInt32("1000", 2), false, RadioPanelPZ69KnobsA10C.UPPER_LARGE_FREQ_WHEEL_DEC));
            result.Add(new RadioPanelKnobA10C(2, Convert.ToInt32("10000", 2), true, RadioPanelPZ69KnobsA10C.LOWER_SMALL_FREQ_WHEEL_INC));
            result.Add(new RadioPanelKnobA10C(2, Convert.ToInt32("100000", 2), false, RadioPanelPZ69KnobsA10C.LOWER_SMALL_FREQ_WHEEL_DEC));
            result.Add(new RadioPanelKnobA10C(2, Convert.ToInt32("1000000", 2), true, RadioPanelPZ69KnobsA10C.LOWER_LARGE_FREQ_WHEEL_INC));
            result.Add(new RadioPanelKnobA10C(2, Convert.ToInt32("10000000", 2), false, RadioPanelPZ69KnobsA10C.LOWER_LARGE_FREQ_WHEEL_DEC));

            // Group 1
            result.Add(new RadioPanelKnobA10C(1, Convert.ToInt32("1", 2), true, RadioPanelPZ69KnobsA10C.LOWER_UHF));
            result.Add(new RadioPanelKnobA10C(1, Convert.ToInt32("10", 2), true, RadioPanelPZ69KnobsA10C.LOWER_VHFFM));
            result.Add(new RadioPanelKnobA10C(1, Convert.ToInt32("100", 2), true, RadioPanelPZ69KnobsA10C.LOWER_ILS));
            result.Add(new RadioPanelKnobA10C(1, Convert.ToInt32("1000", 2), true, RadioPanelPZ69KnobsA10C.LOWER_TACAN));
            result.Add(new RadioPanelKnobA10C(1, Convert.ToInt32("10000", 2), true, RadioPanelPZ69KnobsA10C.LOWER_DME));
            result.Add(new RadioPanelKnobA10C(1, Convert.ToInt32("100000", 2), true, RadioPanelPZ69KnobsA10C.LOWER_XPDR));
            result.Add(new RadioPanelKnobA10C(1, Convert.ToInt32("1000000", 2), true, RadioPanelPZ69KnobsA10C.UPPER_FREQ_SWITCH));
            result.Add(new RadioPanelKnobA10C(1, Convert.ToInt32("10000000", 2), true, RadioPanelPZ69KnobsA10C.LOWER_FREQ_SWITCH));

            // Group 2
            result.Add(new RadioPanelKnobA10C(0, Convert.ToInt32("1", 2), true, RadioPanelPZ69KnobsA10C.UPPER_VHFAM)); // UPPER COM 1
            result.Add(new RadioPanelKnobA10C(0, Convert.ToInt32("10", 2), true, RadioPanelPZ69KnobsA10C.UPPER_UHF)); // UPPER COM 2
            result.Add(new RadioPanelKnobA10C(0, Convert.ToInt32("100", 2), true, RadioPanelPZ69KnobsA10C.UPPER_VHFFM)); // UPPER NAV 1
            result.Add(new RadioPanelKnobA10C(0, Convert.ToInt32("1000", 2), true, RadioPanelPZ69KnobsA10C.UPPER_ILS)); // UPPER NAV 2
            result.Add(new RadioPanelKnobA10C(0, Convert.ToInt32("10000", 2), true, RadioPanelPZ69KnobsA10C.UPPER_TACAN)); // UPPER ADF
            result.Add(new RadioPanelKnobA10C(0, Convert.ToInt32("100000", 2), true, RadioPanelPZ69KnobsA10C.UPPER_DME)); // UPPER DME
            result.Add(new RadioPanelKnobA10C(0, Convert.ToInt32("1000000", 2), true, RadioPanelPZ69KnobsA10C.UPPER_XPDR)); // UPPER XPDR
            result.Add(new RadioPanelKnobA10C(0, Convert.ToInt32("10000000", 2), true, RadioPanelPZ69KnobsA10C.LOWER_VHFAM)); // LOWER COM 1 
            return result;
        }
    }
}
