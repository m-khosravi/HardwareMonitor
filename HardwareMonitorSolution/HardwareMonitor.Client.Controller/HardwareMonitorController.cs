﻿using HardwareMonitor.Client.Controller.Monitors;
using HardwareMonitor.Client.Domain.Contracts;
using HardwareMonitor.Client.Domain.Entities;
using HardwareMonitor.Client.Temperature.Utils;
using System;
using System.Windows.Forms;

namespace HardwareMonitor.Client.Controller
{
    public class HardwareMonitorController : IController
    {
        private const string _APPLICATION_NAME = "HardwareMonitor";
        private const int _NOTIFICATION_TIMEOUT = 10000;
        private const string _MONITORS_ICON_NAME = "Monitors";
        private const string _SETTINGS_ICON_NAME = "Settings";

        #region Temperature
        private const string _TEMPERATURE_ICON_NAME = "Temperature";
        private readonly static string _TEMPERATURE_UI_NAME = $"{_APPLICATION_NAME} - {_TEMPERATURE_ICON_NAME}";

        private TemperatureMonitor _temperatureMonitor;

        private ITemperatureUI _temperatureUI;
        public ITemperatureUI TemperatureUI {
            set
            {
                InitTemperatureMonitorIfNull();

                _temperatureUI?.Close();
                _temperatureUI = value;
                GetMonitorsToolStripItemCollection().RemoveByKey(_TEMPERATURE_ICON_NAME);
                
                if (_temperatureUI != null)
                {
                    _temperatureUI.Name = _TEMPERATURE_UI_NAME;
                    _temperatureUI.OnTemperatureAlertLevelChanged += (s, e) =>
                    {
                        if (e.Value != null && e.Save && _temperatureMonitor != null)
                            _temperatureMonitor.Settings.TemperatureAlertLevel = (int)e.Value;
                    };

                    _temperatureUI.OnUpdateTimeChanged += (s, e) =>
                    {
                        if (e.Value != null && e.Save && _temperatureMonitor != null)
                            _temperatureMonitor.Settings.UpdateTime = (int)e.Value;
                    };

                    _temperatureUI.OnObserversCountChanged += (s, e) =>
                    {
                        if (e.Value != null && e.Save && _temperatureMonitor != null)
                            _temperatureMonitor.Settings.ObserversCount = (int)e.Value;
                    };

                    _temperatureUI.OnNotificationMethodChanged += (s, e) =>
                    {
                        if (e.Value != null && e.Save && _temperatureMonitor != null)
                            _temperatureMonitor.Settings.Notification = (NotificationMethod)e.Value;
                    };

                    _temperatureUI.OnNotification += (s, message) =>
                    {
                        if (!_isShowingNotification)
                        {
                            _isShowingNotification = true;
                            _notifyIcon.ShowBalloonTip(_NOTIFICATION_TIMEOUT, _APPLICATION_NAME, message, ToolTipIcon.Info);
                        }
                    };
                    //_temperatureUI.OnLog += (s, message) => { };
                    //_temperatureUI.OnViewExit += (s, e) => { };
                    _temperatureUI.OnRequestUpdate += (s, e) => UpdateTemperatureUI();

                    GetMonitorsToolStripItemCollection().Add(_TEMPERATURE_ICON_NAME, _temperatureUI.Icon,
                        (s, e) => _temperatureUI?.Show(true)).Name = _TEMPERATURE_ICON_NAME;
                    
                    UpdateTemperatureUI();
                }
                else
                {
                    _temperatureMonitor.Stop();
                    _temperatureMonitor = null;
                }
            }
        }
        #endregion

        private ToolStripItemCollection GetMonitorsToolStripItemCollection()
        {
            var items = _notifyIcon.ContextMenuStrip.Items;
            var monitorsItem = items[items.IndexOfKey(_MONITORS_ICON_NAME)] as ToolStripMenuItem;
            return monitorsItem.DropDown.Items;
        }

        private NotifyIcon _notifyIcon;
        private bool _isShowingNotification;

        public HardwareMonitorController()
        {
            Application.ApplicationExit += (s, e) =>
            {
                _temperatureUI?.Close();
                _temperatureMonitor?.Stop();
                _notifyIcon?.Dispose();
            };

            #region Init tray icon
            _notifyIcon = new NotifyIcon()
            {
                Text = _APPLICATION_NAME,
                Visible = true,
                Icon = Properties.Resources.ohmlogo
            };
            
            _notifyIcon.BalloonTipClosed += (s, e) => _isShowingNotification = false;

            var trayMenuStrip = new ContextMenuStrip();

            trayMenuStrip.Items.Add(_MONITORS_ICON_NAME).Name = _MONITORS_ICON_NAME;
            trayMenuStrip.Items.Add(_SETTINGS_ICON_NAME, Properties.Resources.Settings);
            trayMenuStrip.Items.Add(new ToolStripSeparator());
            trayMenuStrip.Items.Add("Exit", null, (s, e) => Application.Exit());

            _notifyIcon.ContextMenuStrip = trayMenuStrip;
            _notifyIcon.ShowBalloonTip(_NOTIFICATION_TIMEOUT, _APPLICATION_NAME, "The program has started successfully", ToolTipIcon.None);
            #endregion
        }

        private void UpdateTemperatureUI()
        {
            if (_temperatureMonitor.IsServiceReady)
                _temperatureUI?.SetAvgCPUsTemperature((int)_temperatureMonitor.GetAvgCPUsTemperature().GetValueOrDefault());
        }

        private void InitTemperatureMonitorIfNull()
        {
            _temperatureMonitor = new TemperatureMonitor();
            _temperatureMonitor.OnServiceReady += () =>
            {
                if (_temperatureUI != null) UpdateTemperatureUI();
            };
            _temperatureMonitor.OnEventTriggered += () =>
            {
                if (_temperatureMonitor != null) //if the UI is removed, the monitor is stopped and set to null
                    UpdateTemperatureUI();
            };
            _temperatureMonitor.Start();
        }
    }
}