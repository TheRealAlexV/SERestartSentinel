# Server Restart Sentinel

A client-side Space Engineers plugin for the Pulsar loader that provides configurable notifications for scheduled server restarts. Stay informed about upcoming server restarts with customizable alert timings and notification styles to ensure you never lose progress unexpectedly.

## Features

- **Highly configurable restart notifications** - Customize alert timings, styles, and behavior to suit your preferences
- **Time-zone aware schedule management** - Automatically handles time zone conversions for accurate scheduling
- **Two notification styles:**
  - **Intrusive Dialog** - Modal popup that requires acknowledgment
  - **HUD Text Overlay** - Non-intrusive notification displayed on your HUD
- **Configurable alert timers** - Set both Primary and Secondary alert times (default: 10 and 2 minutes)
- **Audible alert sounds** - Optional audio notifications using Space Engineers' built-in sound system
- **In-game configuration menu** - Easy access via the Pulsar settings screen (`F2` → `Pulsar Plugins`)
- **Background monitoring** - Continuously checks restart schedule every 5 seconds
- **Hotkey toggle support** - Quickly enable/disable the plugin with configurable hotkeys

## Installation

This is a client-side plugin for the **Pulsar Plugin Loader**. 

1. Ensure you have Pulsar Plugin Loader installed for Space Engineers
2. Download or clone the Server Restart Sentinel plugin files
3. Place the plugin files into your Pulsar plugins directory:
   ```
   %AppData%\SpaceEngineersModAPI\Pulsar\Plugins\
   ```
4. Launch Space Engineers and the plugin will be automatically loaded by Pulsar

## Configuration

All settings can be configured in-game from the main menu by pressing `F2` → `Pulsar Plugins` → `Server Restart Sentinel`.

### Main Settings

#### **Enable Restart Sentinel**
Master switch for the entire plugin. When disabled, no restart notifications will be displayed.
- **Type:** Toggle (On/Off)
- **Default:** Enabled

#### **Alert Timers**
Configure when restart notifications should be displayed:

- **Primary Alert Time:** First warning displayed to players (in minutes)
  - **Default:** 10 minutes
  - **Range:** 1-120 minutes

- **Secondary Alert Time:** Final warning displayed to players (in minutes)  
  - **Default:** 2 minutes
  - **Range:** 1-120 minutes

#### **Restart Schedule**
Define when server restarts occur using 24-hour time format (`HH:mm`) with comma-separated values.

- **Format:** `HH:mm, HH:mm, HH:mm` (e.g., `17:30, 21:30, 01:30`)
- **Default Schedule (EST):** `17:30, 21:30, 01:30, 05:30, 09:30, 13:30`
  - 5:30 PM, 9:30 PM, 1:30 AM, 5:30 AM, 9:30 AM, 1:30 PM
- **Time Zone:** Plugin automatically handles UTC conversion for accurate timing

#### **Alert Style**
Choose how restart notifications are displayed:

- **Intrusive Dialog:** Modal popup window that pauses gameplay and requires user acknowledgment
- **HUD Text Overlay:** Non-intrusive notification displayed on your HUD that doesn't interrupt gameplay

#### **HUD Overlay Duration**
For HUD Text Overlay style, specify how long notifications remain visible:
- **Default:** 5 seconds
- **Range:** 1-60 seconds

#### **Play Alert Sound**
Toggle for accompanying audio alerts when notifications are displayed:
- **Default:** Enabled
- **Sound:** Uses Space Engineers' built-in "HudGPSNotification3" audio effect

### Advanced Settings

- **Toggle Hotkey:** Keyboard shortcut to quickly enable/disable the plugin (default: `Ctrl+Shift+R`)

## Usage

Once installed and configured, the plugin runs automatically in the background. It will:

1. **Monitor** the configured restart schedule continuously
2. **Calculate** time remaining until the next scheduled restart
3. **Display notifications** at your configured Primary and Secondary alert times
4. **Play audio alerts** (if enabled) to ensure you notice the warnings

The plugin is designed to be unobtrusive during normal gameplay while ensuring you're always aware of upcoming server restarts.

## Technical Details

- **Plugin Type:** Client-side Pulsar plugin
- **Dependencies:** Pulsar.Client framework
- **Update Frequency:** Checks restart schedule every 5 seconds
- **Time Handling:** All calculations performed in UTC for accuracy
- **Persistence:** Configuration settings are automatically saved and restored

## Troubleshooting

If you encounter issues:

1. **Verify Pulsar installation** - Ensure Pulsar Plugin Loader is properly installed
2. **Check configuration** - Verify restart schedule format is correct (`HH:mm`)
3. **Review logs** - Check Space Engineers logs for any error messages from RestartSentinel
4. **Reset settings** - Use the in-game configuration menu to reset to defaults if needed

## Development

### Prerequisites

- **Visual Studio 2019/2022** or **JetBrains Rider**
- **.NET Framework 4.8** (Space Engineers compatibility requirement)
- **Space Engineers** (for testing and API references)
- **Pulsar Plugin Loader** (development dependency)

### Development Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/TheRealAlexV/SERestartSentinel.git
   cd SERestartSentinel
   ```

2. **Set up Space Engineers references:**
   - Ensure Space Engineers is installed
   - Update project references to point to your SE installation directory
   - Typical paths:
     - `C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineers\Bin64\`
     - `%AppData%\SpaceEngineersModAPI\`

3. **Install Pulsar SDK:**
   - Download Pulsar Plugin Loader
   - Reference `Pulsar.Client.dll` in your project

### Building the Plugin

1. **Debug Build:**
   ```bash
   dotnet build --configuration Debug
   ```

2. **Release Build:**
   ```bash
   dotnet build --configuration Release
   ```

3. **Output Location:**
   - Compiled plugin files will be in `bin/Debug/` or `bin/Release/`
   - Copy the built assemblies to your Pulsar plugins directory for testing

### Code Structure

```
├── Config.cs                    # Configuration management and data models
├── RestartSentinelPlugin.cs     # Main plugin logic and Pulsar integration
├── ServerRestartSentinel.xml    # Plugin metadata and dependencies
└── README.md                    # Documentation
```

#### Key Components

- **`Config.cs`**: Defines all user-configurable settings with validation and property change notifications
- **`RestartSentinelPlugin.cs`**: Main plugin class implementing Pulsar's Plugin interface
  - Schedule parsing and time calculations
  - Background monitoring timer
  - Notification display logic (HUD/Dialog)
  - Audio alert management
  - Configuration persistence

#### Architecture Notes

- **Thread Safety**: Uses `lock (_syncLock)` for concurrent access protection
- **Error Handling**: Comprehensive try-catch blocks with logging to SE logs
- **Performance**: 5-second timer interval balances responsiveness with resource usage
- **Modularity**: Clear separation between configuration, scheduling, and notification systems

### Development Workflow

1. **Make changes** to the source code
2. **Build** the project (`dotnet build`)
3. **Copy** built files to Pulsar plugins directory
4. **Test** in Space Engineers
5. **Check logs** for any errors or warnings
6. **Iterate** as needed

### Testing

#### Manual Testing Checklist

- [ ] Plugin loads without errors in Pulsar
- [ ] Configuration UI accessible via `F2` → `Pulsar Plugins`
- [ ] All configuration options save and persist
- [ ] Schedule parsing works with various time formats
- [ ] Primary and Secondary alerts trigger at correct times
- [ ] Both HUD and Dialog notification styles function
- [ ] Audio alerts play (if enabled)
- [ ] Hotkey toggle works correctly
- [ ] Plugin unloads cleanly

#### Test Configurations

1. **Quick Test Schedule**: `"23:59, 00:01"` (for immediate testing)
2. **Edge Cases**: Invalid time formats, empty schedules, boundary values
3. **Performance**: Extended runtime with frequent schedule checks

### Contributing

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Follow** existing code style and conventions
4. **Add** comprehensive error handling and logging
5. **Test** thoroughly with various configurations
6. **Update** documentation if adding new features
7. **Commit** changes (`git commit -m 'Add amazing feature'`)
8. **Push** to branch (`git push origin feature/amazing-feature`)
9. **Open** a Pull Request

#### Code Standards

- Use **C# naming conventions** (PascalCase for public members, camelCase for private)
- Add **XML documentation** for all public methods and properties
- Include **comprehensive error handling** with appropriate logging
- Follow **SOLID principles** and maintain existing architectural patterns
- Ensure **thread safety** for shared resources

#### Pull Request Guidelines

- **Describe** what your changes do and why
- **Include** test results and any relevant screenshots
- **Reference** any related issues
- **Ensure** code builds without warnings
- **Update** README.md if adding user-facing features

## Version Information

- **Current Version:** 1.0.0
- **Author:** Alex 'DeMiNe0' Vanino
- **Plugin ID:** TheRealAlexV/SERestartSentinel
- **License:** MIT License - see [LICENSE](LICENSE) file for details

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

The MIT License allows you to:
- ✅ **Use** the software for any purpose
- ✅ **Modify** and adapt the code
- ✅ **Distribute** copies of the software
- ✅ **Sell** copies or include in commercial products
- ✅ **Sublicense** under different terms

**Requirements:**
- Include the original copyright notice and license text in any copies
- The software is provided "as is" without warranty