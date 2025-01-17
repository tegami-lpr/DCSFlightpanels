﻿namespace NonVisuals.EventArgs
{
    using NonVisuals.Saitek.Panels;

    using EventArgs = System.EventArgs;

    public class BipPanelRegisteredEventArgs : EventArgs
    {
        public string UniqueId { get; set; }

        public BacklitPanelBIP BacklitPanelBip { get; set; }
    }
}
