﻿using System;
using System.Collections.Generic;
using ClassLibraryCommon;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NonVisuals;

namespace TestNonVisuals
{
    [TestClass]
    public class TestBacklitPanelBIP
    {
        [TestMethod]
        public void TestConstructorMethod1()
        {
            var hidSkeleton = new HIDSkeleton(SaitekPanelsEnum.BackLitPanel, "");
            var backlitPanelBIP = new BacklitPanelBIP(10, hidSkeleton, false);

            Assert.IsNotNull(backlitPanelBIP);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestConstructorMethod1InvalidSaitekPanelsEnum()
        {
            var hidSkeleton = new HIDSkeleton(SaitekPanelsEnum.PZ55SwitchPanel, "");
            var backlitPanelBIP = new BacklitPanelBIP(10, hidSkeleton, false);

            Assert.IsNotNull(backlitPanelBIP);
        }

        [TestMethod]
        public void TestGetSetterSettingsVersion()
        {
            var versionNumber = "0X";
            var hidSkeleton = new HIDSkeleton(SaitekPanelsEnum.BackLitPanel, "");
            var backlitPanelBIP = new BacklitPanelBIP(10, hidSkeleton, false);

            Assert.AreEqual(versionNumber, backlitPanelBIP.SettingsVersion());
        }

        [TestMethod]
        public void TestLEDBrightnessIncrease()
        {
            var initialBrightness = 50;
            var hidSkeleton = new HIDSkeleton(SaitekPanelsEnum.BackLitPanel, "");
            var backlitPanelBIP = new BacklitPanelBIP(10, hidSkeleton, false);

            backlitPanelBIP.LEDBrightness = initialBrightness;
            backlitPanelBIP.LEDBrightnessIncrease();

            Assert.AreEqual(initialBrightness + 10, backlitPanelBIP.LEDBrightness);
        }

        [TestMethod]
        public void TestLEDBrightnessDecrease()
        {
            var initialBrightness = 50;
            var hidSkeleton = new HIDSkeleton(SaitekPanelsEnum.BackLitPanel, "");
            var backlitPanelBIP = new BacklitPanelBIP(10, hidSkeleton, false);

            backlitPanelBIP.LEDBrightness = initialBrightness;
            backlitPanelBIP.LEDBrightnessDecrease();

            Assert.AreEqual(initialBrightness - 10, backlitPanelBIP.LEDBrightness);
        }

        [TestMethod]
        public void TestGetRow()
        {
            var position = BIPLedPositionEnum.Position_2_5;
            var row = 2;
            var hidSkeleton = new HIDSkeleton(SaitekPanelsEnum.BackLitPanel, "");
            var backlitPanelBIP = new BacklitPanelBIP(10, hidSkeleton, false);
            
            Assert.AreEqual(row, backlitPanelBIP.GetRow(position));
        }

        [TestMethod]
        public void TestGetIndex()
        {
            var position = BIPLedPositionEnum.Position_2_5;
            var index = 5;
            var hidSkeleton = new HIDSkeleton(SaitekPanelsEnum.BackLitPanel, "");
            var backlitPanelBIP = new BacklitPanelBIP(10, hidSkeleton, false);

            Assert.AreEqual(index, backlitPanelBIP.GetIndex(position));
        }

        [TestMethod]
        public void TestGetPositionString()
        {
            var position = BIPLedPositionEnum.Position_2_5;
            var positionAsString = "2_5";
            var hidSkeleton = new HIDSkeleton(SaitekPanelsEnum.BackLitPanel, "");
            var backlitPanelBIP = new BacklitPanelBIP(10, hidSkeleton, false);

            Assert.AreEqual(positionAsString, backlitPanelBIP.GetPosString(position));
        }

        [TestMethod]
        public void TestHasPositionDCSBIOSConfigurationTrue()
        {
            var position = BIPLedPositionEnum.Position_2_5;
            var dcsOutputAndColorBinding = new DcsOutputAndColorBindingBIP();
            var hidSkeleton = new HIDSkeleton(SaitekPanelsEnum.BackLitPanel, "");
            var backlitPanelBIP = new BacklitPanelBIP(10, hidSkeleton, false);
            var saitekPanelLEDPosition = new SaitekPanelLEDPosition(position);
            var dcsOutputAndColorBindings = new List<DcsOutputAndColorBinding>();

            dcsOutputAndColorBinding.DCSBiosOutputLED = null;
            dcsOutputAndColorBinding.LEDColor = PanelLEDColor.DARK;
            dcsOutputAndColorBinding.SaitekLEDPosition = saitekPanelLEDPosition;
            dcsOutputAndColorBindings.Add(dcsOutputAndColorBinding);
            backlitPanelBIP.SetLedDcsBiosOutput(position, dcsOutputAndColorBindings);

            Assert.IsTrue(backlitPanelBIP.HasConfiguration(position));
        }

        [TestMethod]
        public void TestHasPositionDCSBIOSConfigurationFalse()
        {
            var positionAdded = BIPLedPositionEnum.Position_2_5;
            var nonExistentPosition = BIPLedPositionEnum.Position_1_5;
            var dcsOutputAndColorBinding = new DcsOutputAndColorBindingBIP();
            var hidSkeleton = new HIDSkeleton(SaitekPanelsEnum.BackLitPanel, "");
            var backlitPanelBIP = new BacklitPanelBIP(10, hidSkeleton, false);
            var saitekPanelLEDPosition = new SaitekPanelLEDPosition(positionAdded);
            var dcsOutputAndColorBindings = new List<DcsOutputAndColorBinding>();

            dcsOutputAndColorBinding.DCSBiosOutputLED = null;
            dcsOutputAndColorBinding.LEDColor = PanelLEDColor.DARK;
            dcsOutputAndColorBinding.SaitekLEDPosition = saitekPanelLEDPosition;
            dcsOutputAndColorBindings.Add(dcsOutputAndColorBinding);
            backlitPanelBIP.SetLedDcsBiosOutput(positionAdded, dcsOutputAndColorBindings);

            Assert.IsFalse(backlitPanelBIP.HasConfiguration(nonExistentPosition));
        }
    }
}