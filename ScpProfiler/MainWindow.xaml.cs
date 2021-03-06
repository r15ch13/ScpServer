﻿using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ScpControl;
using ScpControl.Profiler;
using ScpControl.ScpCore;
using Ds3Button = ScpControl.Profiler.Ds3Button;
using Ds4Button = ScpControl.Profiler.Ds4Button;

namespace ScpProfiler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ScpProxy _proxy = new ScpProxy();
        private DsPadId _currentPad;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            _proxy.NativeFeedReceived += ProxyOnNativeFeedReceived;
            _proxy.Start();

        }

        private void ProxyOnNativeFeedReceived(object sender, ScpHidReport report)
        {
            if(report.PadId != _currentPad) return;

            CurrentDualShockProfile.Model = report.Model;
            CurrentDualShockProfile.MacAddress = string.Join(":",
                (from z in report.PadMacAddress.GetAddressBytes() select z.ToString("X2")).ToArray());
            CurrentDualShockProfile.PadId = report.PadId;

            CurrentDualShockProfile.Remap(report);

            switch (report.Model)
            {
                case DsModel.DS3:
                    CurrentDualShockProfile.Ps.CurrentValue = report[Ds3Button.Ps].Value;
                    CurrentDualShockProfile.Circle.CurrentValue = report[Ds3Button.Circle].Value;
                    CurrentDualShockProfile.Cross.CurrentValue = report[Ds3Button.Cross].Value;
                    CurrentDualShockProfile.Square.CurrentValue = report[Ds3Button.Square].Value;
                    CurrentDualShockProfile.Triangle.CurrentValue = report[Ds3Button.Triangle].Value;
                    CurrentDualShockProfile.Select.CurrentValue = report[Ds3Button.Select].Value;
                    CurrentDualShockProfile.Start.CurrentValue = report[Ds3Button.Start].Value;
                    CurrentDualShockProfile.LeftShoulder.CurrentValue = report[Ds3Button.L1].Value;
                    CurrentDualShockProfile.RightShoulder.CurrentValue = report[Ds3Button.R1].Value;
                    CurrentDualShockProfile.LeftTrigger.CurrentValue = report[Ds3Button.L2].Value;
                    CurrentDualShockProfile.RightTrigger.CurrentValue = report[Ds3Button.R2].Value;
                    CurrentDualShockProfile.LeftThumb.CurrentValue = report[Ds3Button.L3].Value;
                    CurrentDualShockProfile.RightThumb.CurrentValue = report[Ds3Button.R3].Value;
                    CurrentDualShockProfile.Up.CurrentValue = report[Ds3Button.Up].Value;
                    CurrentDualShockProfile.Right.CurrentValue = report[Ds3Button.Right].Value;
                    CurrentDualShockProfile.Down.CurrentValue = report[Ds3Button.Down].Value;
                    CurrentDualShockProfile.Left.CurrentValue = report[Ds3Button.Left].Value;
                    break;
                case DsModel.DS4:
                    CurrentDualShockProfile.Ps.CurrentValue = report[Ds4Button.Ps].Value;
                    CurrentDualShockProfile.Circle.CurrentValue = report[Ds4Button.Circle].Value;
                    CurrentDualShockProfile.Cross.CurrentValue = report[Ds4Button.Cross].Value;
                    CurrentDualShockProfile.Square.CurrentValue = report[Ds4Button.Square].Value;
                    CurrentDualShockProfile.Triangle.CurrentValue = report[Ds4Button.Triangle].Value;
                    CurrentDualShockProfile.Select.CurrentValue = report[Ds4Button.Share].Value;
                    CurrentDualShockProfile.Start.CurrentValue = report[Ds4Button.Options].Value;
                    CurrentDualShockProfile.LeftShoulder.CurrentValue = report[Ds4Button.L1].Value;
                    CurrentDualShockProfile.RightShoulder.CurrentValue = report[Ds4Button.R1].Value;
                    CurrentDualShockProfile.LeftTrigger.CurrentValue = report[Ds4Button.L2].Value;
                    CurrentDualShockProfile.RightTrigger.CurrentValue = report[Ds4Button.R2].Value;
                    CurrentDualShockProfile.LeftThumb.CurrentValue = report[Ds4Button.L3].Value;
                    CurrentDualShockProfile.RightThumb.CurrentValue = report[Ds4Button.R3].Value;
                    CurrentDualShockProfile.Up.CurrentValue = report[Ds4Button.Up].Value;
                    CurrentDualShockProfile.Right.CurrentValue = report[Ds4Button.Right].Value;
                    CurrentDualShockProfile.Down.CurrentValue = report[Ds4Button.Down].Value;
                    CurrentDualShockProfile.Left.CurrentValue = report[Ds4Button.Left].Value;
                    break;
            }
        }

        private void CurrentPad_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentPad = (DsPadId)((ComboBox)sender).SelectedItem;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentDualShockProfile.Save(Path.Combine(GlobalConfiguration.AppDirectory, "Profiles",
                CurrentDualShockProfile.FileName));
        }

        private void NewButton_OnClick(object sender, RoutedEventArgs e)
        {
            
        }

        private void EditButton_OnClick(object sender, RoutedEventArgs e)
        {
            EditProfileChildWindow.Show();
        }
    }
}
