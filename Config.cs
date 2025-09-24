using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace TheRealAlexV.SERestartSentinel
{
    /// <summary>
    /// Defines the different alert styles available for restart notifications
    /// </summary>
    public enum AlertStyle
    {
        /// <summary>
        /// HUD text overlay notification
        /// </summary>
        [Description("HUD Text Overlay")]
        HudTextOverlay = 0,

        /// <summary>
        /// Intrusive dialog notification
        /// </summary>
        [Description("Intrusive Dialog")]
        IntrusiveDialog = 1
    }

    /// <summary>
    /// Configuration class for the Server Restart Sentinel plugin.
    /// Contains all user-configurable settings for restart notifications.
    /// </summary>
    public class Config : INotifyPropertyChanged
    {
        #region Fields

        // Backing fields for properties
        private bool _masterEnable = true;
        private string _toggleHotkey = "Ctrl+Shift+R";
        private int _primaryAlertTime = 10;
        private int _secondaryAlertTime = 2;
        private string _restartSchedule = "17:30, 21:30, 01:30, 05:30, 09:30, 13:30";
        private AlertStyle _alertStyle = AlertStyle.HudTextOverlay;
        private int _hudOverlayDuration = 5;
        private bool _playAlertSound = true;

        #endregion

        #region Events

        /// <summary>
        /// Occurs when a property value changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Static Properties

        /// <summary>
        /// Gets the default configuration instance
        /// </summary>
        public static readonly Config Default = new Config();

        /// <summary>
        /// Gets or sets the current configuration instance
        /// </summary>
        public static Config Current { get; set; } = new Config();

        #endregion

        #region General Settings

        /// <summary>
        /// Master enable/disable switch for the entire plugin
        /// </summary>
        [DisplayName("Enable Restart Sentinel")]
        [Description("Master enable/disable switch for the entire plugin")]
        [Category("General Settings")]
        public bool MasterEnable
        {
            get => _masterEnable;
            set => SetField(ref _masterEnable, value);
        }

        /// <summary>
        /// Hotkey combination to toggle the plugin on/off
        /// Format: "Ctrl+Shift+R" or similar key combination string
        /// </summary>
        [DisplayName("Toggle Hotkey")]
        [Description("Hotkey combination to toggle the plugin on/off (e.g., Ctrl+Shift+R)")]
        [Category("General Settings")]
        public string ToggleHotkey
        {
            get => _toggleHotkey;
            set => SetField(ref _toggleHotkey, value);
        }

        #endregion

        #region Alert Timing Settings

        /// <summary>
        /// Primary alert time in minutes before server restart
        /// This is the first warning given to players
        /// </summary>
        [DisplayName("Primary Alert Time")]
        [Description("Primary alert time in minutes before server restart (first warning)")]
        [Category("Alert Timing Settings")]
        [Range(1, 120)]
        public int PrimaryAlertTime
        {
            get => _primaryAlertTime;
            set => SetField(ref _primaryAlertTime, value);
        }

        /// <summary>
        /// Secondary alert time in minutes before server restart
        /// This is the final warning given to players
        /// </summary>
        [DisplayName("Secondary Alert Time")]
        [Description("Secondary alert time in minutes before server restart (final warning)")]
        [Category("Alert Timing Settings")]
        [Range(1, 120)]
        public int SecondaryAlertTime
        {
            get => _secondaryAlertTime;
            set => SetField(ref _secondaryAlertTime, value);
        }

        #endregion

        #region Schedule Settings

        /// <summary>
        /// Server restart schedule in time format
        /// Examples: "17:30, 21:30, 01:30, 05:30, 09:30, 13:30"
        /// </summary>
        [DisplayName("Restart Schedule")]
        [Description("Server restart schedule as comma-separated times (e.g., 17:30, 21:30, 01:30)")]
        [Category("Schedule Settings")]
        [Multiline]
        public string RestartSchedule
        {
            get => _restartSchedule;
            set => SetField(ref _restartSchedule, value);
        }

        #endregion

        #region Display Settings

        /// <summary>
        /// The style/intrusiveness level of restart alerts
        /// </summary>
        [DisplayName("Alert Style")]
        [Description("The style of restart notifications to display")]
        [Category("Display Settings")]
        public AlertStyle AlertStyle
        {
            get => _alertStyle;
            set => SetField(ref _alertStyle, value);
        }

        /// <summary>
        /// Duration in seconds that HUD overlays should remain visible
        /// </summary>
        [DisplayName("HUD Overlay Duration")]
        [Description("Duration in seconds that HUD overlays should remain visible")]
        [Category("Display Settings")]
        [Range(1, 60)]
        public int HudOverlayDuration
        {
            get => _hudOverlayDuration;
            set => SetField(ref _hudOverlayDuration, value);
        }

        #endregion

        #region Audio Settings

        /// <summary>
        /// Whether to play alert sounds with notifications
        /// </summary>
        [DisplayName("Play Alert Sound")]
        [Description("Whether to play alert sounds with restart notifications")]
        [Category("Audio Settings")]
        public bool PlayAlertSound
        {
            get => _playAlertSound;
            set => SetField(ref _playAlertSound, value);
        }

        #endregion

        #region Property Change Notification

        /// <summary>
        /// Helper method to set a field value and raise PropertyChanged if the value changes
        /// </summary>
        /// <typeparam name="T">The type of the field</typeparam>
        /// <param name="field">Reference to the backing field</param>
        /// <param name="value">The new value to set</param>
        /// <param name="propertyName">The name of the property (automatically provided)</param>
        /// <returns>True if the value was changed, false otherwise</returns>
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Raises the PropertyChanged event
        /// </summary>
        /// <param name="propertyName">The name of the property that changed</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Validates the current configuration settings
        /// </summary>
        /// <returns>True if all settings are valid, false otherwise</returns>
        public bool IsValid()
        {
            // Validate alert times
            if (PrimaryAlertTime <= 0 || SecondaryAlertTime <= 0)
                return false;

            if (SecondaryAlertTime >= PrimaryAlertTime)
                return false;

            // Validate HUD overlay duration
            if (HudOverlayDuration <= 0)
                return false;

            // Validate hotkey format (basic check)
            if (string.IsNullOrWhiteSpace(ToggleHotkey))
                return false;

            // Validate restart schedule (basic check)
            if (string.IsNullOrWhiteSpace(RestartSchedule))
                return false;

            return true;
        }

        /// <summary>
        /// Resets all configuration values to their defaults
        /// </summary>
        public void ResetToDefaults()
        {
            MasterEnable = true;
            ToggleHotkey = "Ctrl+Shift+R";
            PrimaryAlertTime = 10;
            SecondaryAlertTime = 2;
            RestartSchedule = "17:30, 21:30, 01:30, 05:30, 09:30, 13:30";
            AlertStyle = AlertStyle.HudTextOverlay;
            HudOverlayDuration = 5;
            PlayAlertSound = true;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Creates a deep copy of the current configuration
        /// </summary>
        /// <returns>A new Config instance with the same values</returns>
        public Config Clone()
        {
            return new Config
            {
                MasterEnable = this.MasterEnable,
                ToggleHotkey = this.ToggleHotkey,
                PrimaryAlertTime = this.PrimaryAlertTime,
                SecondaryAlertTime = this.SecondaryAlertTime,
                RestartSchedule = this.RestartSchedule,
                AlertStyle = this.AlertStyle,
                HudOverlayDuration = this.HudOverlayDuration,
                PlayAlertSound = this.PlayAlertSound
            };
        }

        #endregion
    }
}