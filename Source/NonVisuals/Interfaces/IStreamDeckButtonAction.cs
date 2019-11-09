﻿using System.Threading;
using NonVisuals.StreamDeck;

namespace NonVisuals.Interfaces
{

    public enum EnumStreamDeckActionType
    {
        Unknown = 0,
        KeyPress = 1,
        DCSBIOS = 2,
        OSCommand = 4,
        LayerNavigation = 16,
        Custom = 32
    }

    public interface IStreamDeckButtonAction
    {
        EnumStreamDeckActionType ActionType { get; }
        string Description { get; }
        void Execute(StreamDeckRequisites streamDeckRequisites);
        bool IsRunning();
        bool IsRepeatable();
    }
}
