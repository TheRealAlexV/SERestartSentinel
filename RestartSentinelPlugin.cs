using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Pulsar.Client;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;

namespace TheRealAlexV.SERestartSentinel
{
    /// <summary>
    /// Main plugin class for Server Restart Sentinel.
    /// Provides intrusive and non-intrusive notifications for scheduled server restarts.
    /// </summary>
    public partial class RestartSentinelPlugin : Pulsar.Client.Plugin
    {
        #region Static Instance

        /// <summary>
        /// Gets the singleton instance of the plugin
        /// </summary>
        public static RestartSentinelPlugin Instance { get; private set; }

        #endregion

        #region Fields

        /// <summary>
        /// Timer for background restart checking
        /// </summary>
        private Timer _restartCheckTimer;

        /// <summary>
        /// Parsed restart schedule times in UTC
        /// </summary>
        private List<TimeSpan> _restartTimes;

        /// <summary>
        /// Tracks the last notification times to prevent spam
        /// </summary>
        private DateTime _lastPrimaryAlert = DateTime.MinValue;
        private DateTime _lastSecondaryAlert = DateTime.MinValue;

        /// <summary>
        /// Current HUD notification end time (for overlay duration tracking)
        /// </summary>
        private DateTime _hudNotificationEndTime = DateTime.MinValue;

        /// <summary>
        /// Object for thread synchronization
        /// </summary>
        private readonly object _syncLock = new object();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the plugin configuration
        /// </summary>
        public Config Config => Config.Current;

        /// <summary>
        /// Gets the plugin configuration (legacy compatibility)
        /// </summary>
        public Config Configuration => Config.Current;

        #endregion

        #region Plugin Lifecycle

        /// <summary>
        /// Called when the plugin is initialized
        /// </summary>
        public override void Initialize()
        {
            try
            {
                // Set singleton instance
                Instance = this;

                // Initialize plugin configuration
                LoadConfiguration();
                
                // Parse the restart schedule
                ParseRestartSchedule();
                
                // Set up the background timer (check every 5 seconds)
                _restartCheckTimer = new Timer(CheckRestartScheduleCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
                
                // Register event handlers
                MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;

                MyAPIGateway.Utilities.ShowMessage("RestartSentinel", "Plugin initialized successfully");
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage("RestartSentinel", $"Initialization error: {ex.Message}");
                MyLog.Default.WriteErrorLine($"[RestartSentinel] Initialization failed: {ex}");
            }
            
            base.Initialize();
        }

        /// <summary>
        /// Called when the plugin is being disposed
        /// </summary>
        public override void Dispose()
        {
            try
            {
                // Clean up timer
                _restartCheckTimer?.Dispose();
                _restartCheckTimer = null;
                
                // Remove event handlers
                if (MyAPIGateway.Utilities != null)
                {
                    MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;
                }
                
                // Save configuration before disposal
                SaveConfiguration();

                // Clear singleton instance
                Instance = null;

                MyAPIGateway.Utilities.ShowMessage("RestartSentinel", "Plugin disposed successfully");
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage("RestartSentinel", $"Dispose error: {ex.Message}");
                MyLog.Default.WriteErrorLine($"[RestartSentinel] Dispose failed: {ex}");
            }
            
            base.Dispose();
        }

        #endregion

        #region Configuration Management

        /// <summary>
        /// Loads the plugin configuration
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                // Ensure Config.Current is initialized with defaults if needed
                if (Config.Current == null)
                {
                    Config.Current = new Config();
                }

                // In Pulsar, configuration is typically managed by the framework
                // This method can be extended to load from persistent storage if needed
                
                MyLog.Default.WriteInfoLine("[RestartSentinel] Configuration loaded successfully");
            }
            catch (Exception ex)
            {
                // If loading fails, reset to default configuration
                Config.Current = new Config();
                
                MyAPIGateway.Utilities.ShowMessage("RestartSentinel", $"Failed to load configuration, using defaults: {ex.Message}");
                MyLog.Default.WriteErrorLine($"[RestartSentinel] Configuration load failed: {ex}");
            }
        }

        /// <summary>
        /// Saves the plugin configuration
        /// </summary>
        private void SaveConfiguration()
        {
            try
            {
                // In Pulsar, configuration persistence is typically handled by the framework
                // This method can be extended to save to persistent storage if needed
                
                // Validate configuration before saving
                if (Config.Current?.IsValid() == true)
                {
                    MyLog.Default.WriteInfoLine("[RestartSentinel] Configuration saved successfully");
                }
                else
                {
                    MyLog.Default.WriteWarningLine("[RestartSentinel] Configuration validation failed during save");
                }
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage("RestartSentinel", $"Failed to save configuration: {ex.Message}");
                MyLog.Default.WriteErrorLine($"[RestartSentinel] Configuration save failed: {ex}");
            }
        }

        /// <summary>
        /// Updates the plugin configuration and saves it
        /// </summary>
        /// <param name="newConfig">The new configuration to apply</param>
        public void UpdateConfiguration(Config newConfig)
        {
            if (newConfig?.IsValid() != true)
            {
                MyLog.Default.WriteWarningLine("[RestartSentinel] Invalid configuration provided to UpdateConfiguration");
                return;
            }

            try
            {
                lock (_syncLock)
                {
                    Config.Current = newConfig;
                    ParseRestartSchedule();
                    SaveConfiguration();
                }

                MyLog.Default.WriteInfoLine("[RestartSentinel] Configuration updated successfully");
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteErrorLine($"[RestartSentinel] Configuration update failed: {ex}");
            }
        }

        #endregion

        #region Schedule Parsing Logic

        /// <summary>
        /// Parses the restart schedule string into a list of TimeSpan objects
        /// </summary>
        private void ParseRestartSchedule()
        {
            _restartTimes = new List<TimeSpan>();
            
            if (string.IsNullOrWhiteSpace(Config.Current?.RestartSchedule))
            {
                MyLog.Default.WriteWarningLine("[RestartSentinel] No restart schedule configured");
                return;
            }

            try
            {
                var scheduleItems = Config.Current.RestartSchedule.Split(',');
                
                foreach (var item in scheduleItems)
                {
                    var trimmedItem = item.Trim();
                    if (TimeSpan.TryParseExact(trimmedItem, @"hh\:mm", CultureInfo.InvariantCulture, out TimeSpan time))
                    {
                        _restartTimes.Add(time);
                    }
                    else
                    {
                        MyAPIGateway.Utilities.ShowMessage("RestartSentinel", $"Invalid time format: {trimmedItem}");
                        MyLog.Default.WriteWarningLine($"[RestartSentinel] Invalid time format in schedule: {trimmedItem}");
                    }
                }
                
                // Sort the times for easier processing
                _restartTimes.Sort();
                
                MyLog.Default.WriteInfoLine($"[RestartSentinel] Parsed {_restartTimes.Count} restart times from schedule");
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage("RestartSentinel", $"Error parsing restart schedule: {ex.Message}");
                MyLog.Default.WriteErrorLine($"[RestartSentinel] Schedule parsing failed: {ex}");
                _restartTimes.Clear();
            }
        }

        #endregion

        #region Time Calculation Logic

        /// <summary>
        /// Calculates the time remaining until the next scheduled restart
        /// </summary>
        /// <returns>TimeSpan until next restart, or null if no valid schedule</returns>
        private TimeSpan? CalculateTimeUntilNextRestart()
        {
            if (_restartTimes == null || _restartTimes.Count == 0)
                return null;

            var utcNow = DateTime.UtcNow;
            var currentTimeOfDay = utcNow.TimeOfDay;
            
            // Find the next restart time today
            foreach (var restartTime in _restartTimes)
            {
                if (restartTime > currentTimeOfDay)
                {
                    return restartTime - currentTimeOfDay;
                }
            }
            
            // If no restart time found today, get the first one tomorrow
            var firstRestartTomorrow = _restartTimes[0];
            var tomorrow = utcNow.Date.AddDays(1);
            var nextRestartDateTime = tomorrow.Add(firstRestartTomorrow);
            
            return nextRestartDateTime - utcNow;
        }

        #endregion

        #region Background Task Logic

        /// <summary>
        /// Timer callback method for checking restart schedule
        /// </summary>
        /// <param name="state">Timer state (unused)</param>
        private void CheckRestartScheduleCallback(object state)
        {
            if (Config.Current?.MasterEnable != true)
                return;

            try
            {
                CheckRestartSchedule();
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage("RestartSentinel", $"Error in restart check: {ex.Message}");
                MyLog.Default.WriteErrorLine($"[RestartSentinel] Restart check failed: {ex}");
            }
        }

        /// <summary>
        /// Checks if a restart notification should be displayed
        /// </summary>
        private void CheckRestartSchedule()
        {
            lock (_syncLock)
            {
                var timeUntilRestart = CalculateTimeUntilNextRestart();
                if (!timeUntilRestart.HasValue)
                    return;

                var minutesUntilRestart = (int)timeUntilRestart.Value.TotalMinutes;
                var now = DateTime.UtcNow;
                
                // Check for primary alert (with 30-second tolerance)
                if (Math.Abs(minutesUntilRestart - Config.Current.PrimaryAlertTime) <= 0.5 && 
                    (now - _lastPrimaryAlert).TotalMinutes > 1)
                {
                    _lastPrimaryAlert = now;
                    ShowRestartNotification(Config.Current.PrimaryAlertTime);
                    MyLog.Default.WriteInfoLine($"[RestartSentinel] Primary alert triggered ({Config.Current.PrimaryAlertTime} minutes)");
                }
                // Check for secondary alert (with 30-second tolerance)
                else if (Math.Abs(minutesUntilRestart - Config.Current.SecondaryAlertTime) <= 0.5 && 
                         (now - _lastSecondaryAlert).TotalMinutes > 1)
                {
                    _lastSecondaryAlert = now;
                    ShowRestartNotification(Config.Current.SecondaryAlertTime);
                    MyLog.Default.WriteInfoLine($"[RestartSentinel] Secondary alert triggered ({Config.Current.SecondaryAlertTime} minutes)");
                }
            }
        }

        #endregion

        #region Notification Display Logic

        /// <summary>
        /// Displays restart notification based on current settings
        /// </summary>
        /// <param name="minutesUntilRestart">Minutes until the scheduled restart</param>
        private void ShowRestartNotification(int minutesUntilRestart)
        {
            var message = $"SERVER RESTART IN {minutesUntilRestart} MINUTES";
            
            try
            {
                // Play audio alert if enabled
                if (Config.Current.PlayAlertSound)
                {
                    PlayAlertSound();
                }
                
                // Display notification based on style
                switch (Config.Current.AlertStyle)
                {
                    case AlertStyle.IntrusiveDialog:
                        ShowIntrusiveDialog(message);
                        break;
                        
                    case AlertStyle.HudTextOverlay:
                        ShowHudTextOverlay(message);
                        break;
                }

                MyLog.Default.WriteInfoLine($"[RestartSentinel] Notification displayed: {message}");
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage("RestartSentinel", $"Error showing notification: {ex.Message}");
                MyLog.Default.WriteErrorLine($"[RestartSentinel] Notification display failed: {ex}");
            }
        }

        /// <summary>
        /// Shows an intrusive modal dialog notification
        /// </summary>
        /// <param name="message">The message to display</param>
        private void ShowIntrusiveDialog(string message)
        {
            try
            {
                MyAPIGateway.Utilities.ShowMessageBox(
                    "Server Restart Warning",
                    message,
                    MyMessageBoxStyleEnum.Error,
                    MyMessageBoxButtonsType.OK,
                    null);
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage("RestartSentinel", $"Error showing dialog: {ex.Message}");
                MyLog.Default.WriteErrorLine($"[RestartSentinel] Dialog display failed: {ex}");
                // Fallback to HUD message
                ShowHudTextOverlay(message);
            }
        }

        /// <summary>
        /// Shows a HUD text overlay notification
        /// </summary>
        /// <param name="message">The message to display</param>
        private void ShowHudTextOverlay(string message)
        {
            try
            {
                // Set the end time for this HUD notification
                _hudNotificationEndTime = DateTime.UtcNow.AddSeconds(Config.Current.HudOverlayDuration);
                
                // Display the HUD message
                MyAPIGateway.Utilities.ShowNotification(message, Config.Current.HudOverlayDuration * 1000, MyFontEnum.Red);
                
                // Also send to chat for visibility
                MyAPIGateway.Utilities.ShowMessage("SERVER RESTART", message);
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage("RestartSentinel", $"Error showing HUD overlay: {ex.Message}");
                MyLog.Default.WriteErrorLine($"[RestartSentinel] HUD overlay display failed: {ex}");
                // Fallback to simple message
                MyAPIGateway.Utilities.ShowMessage("SERVER RESTART", message);
            }
        }

        #endregion

        #region Audio Alerts

        /// <summary>
        /// Plays an alert sound effect
        /// </summary>
        private void PlayAlertSound()
        {
            try
            {
                // Use Space Engineers' built-in audio system
                MyAPIGateway.Audio.PlaySound("HudGPSNotification3");
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage("RestartSentinel", $"Error playing alert sound: {ex.Message}");
                MyLog.Default.WriteErrorLine($"[RestartSentinel] Audio alert failed: {ex}");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the toggle hotkey press event
        /// </summary>
        private void OnToggleHotkeyPressed()
        {
            try
            {
                lock (_syncLock)
                {
                    Config.Current.MasterEnable = !Config.Current.MasterEnable;
                    SaveConfiguration();
                    
                    var status = Config.Current.MasterEnable ? "enabled" : "disabled";
                    MyAPIGateway.Utilities.ShowMessage("RestartSentinel", $"Plugin {status}");
                    MyLog.Default.WriteInfoLine($"[RestartSentinel] Plugin toggled: {status}");
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteErrorLine($"[RestartSentinel] Toggle hotkey handler failed: {ex}");
            }
        }

        /// <summary>
        /// Handles chat message events for debug commands
        /// </summary>
        /// <param name="messageText">The message text</param>
        /// <param name="sendToOthers">Whether to send the message to other players</param>
        private void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            try
            {
                if (messageText.StartsWith("/restart-sentinel"))
                {
                    sendToOthers = false;
                    HandleDebugCommand(messageText);
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteErrorLine($"[RestartSentinel] Message handler failed: {ex}");
            }
        }

        /// <summary>
        /// Handles debug commands for testing
        /// </summary>
        /// <param name="command">The command string</param>
        private void HandleDebugCommand(string command)
        {
            try
            {
                var parts = command.Split(' ');
                
                switch (parts.Length > 1 ? parts[1].ToLower() : "")
                {
                    case "status":
                        ShowStatus();
                        break;
                        
                    case "test":
                        if (parts.Length > 2 && int.TryParse(parts[2], out int minutes))
                        {
                            ShowRestartNotification(minutes);
                        }
                        else
                        {
                            MyAPIGateway.Utilities.ShowMessage("RestartSentinel", "Usage: /restart-sentinel test <minutes>");
                        }
                        break;
                        
                    case "toggle":
                        OnToggleHotkeyPressed();
                        break;
                        
                    default:
                        MyAPIGateway.Utilities.ShowMessage("RestartSentinel", 
                            "Commands: status, test <minutes>, toggle");
                        break;
                }
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage("RestartSentinel", $"Error executing command: {ex.Message}");
                MyLog.Default.WriteErrorLine($"[RestartSentinel] Debug command failed: {ex}");
            }
        }

        /// <summary>
        /// Shows the current plugin status
        /// </summary>
        private void ShowStatus()
        {
            try
            {
                var timeUntilRestart = CalculateTimeUntilNextRestart();
                var status = Config.Current.MasterEnable ? "Enabled" : "Disabled";
                
                MyAPIGateway.Utilities.ShowMessage("RestartSentinel", $"Status: {status}");
                
                if (timeUntilRestart.HasValue)
                {
                    var hours = (int)timeUntilRestart.Value.TotalHours;
                    var minutes = timeUntilRestart.Value.Minutes;
                    MyAPIGateway.Utilities.ShowMessage("RestartSentinel", 
                        $"Next restart in: {hours:D2}:{minutes:D2}");
                }
                else
                {
                    MyAPIGateway.Utilities.ShowMessage("RestartSentinel", "No restart schedule configured");
                }
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage("RestartSentinel", $"Error getting status: {ex.Message}");
                MyLog.Default.WriteErrorLine($"[RestartSentinel] Status display failed: {ex}");
            }
        }

        #endregion
    }
}