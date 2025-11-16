# CoreFreq for Windows

A Windows-native CPU monitoring application inspired by [CoreFreq](https://github.com/cyring/CoreFreq) (Linux). Provides real-time monitoring of CPU frequency, temperature, utilization, and power consumption with a modern WPF GUI and terminal UI.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)
![License](https://img.shields.io/badge/license-MIT-green)

## Features

### Core Monitoring
- **Per-core frequency monitoring** (with AMD SMT support)
- **Per-core temperature monitoring**
- **Per-core CPU utilization** with visual progress bars
- **Package-level metrics**: temperature, power, voltage, total utilization
- **Min/Max tracking** with reset functionality

### User Interface
- **Modern WPF GUI** with dark theme
- **Real-time trend graphs** (CPU utilization & temperature)
- **Color-coded indicators** for frequency and temperature
- **Terminal UI** using Spectre.Console for command-line monitoring
- **System tray integration** with notifications
- **Topology view** showing CPU topology, core mapping, and cache hierarchy
- **System Info view** with detailed CPU specifications and BIOS information

### Data Management
- **JSON/CSV export** with full data snapshots
- **Continuous data logging** to CSV files
- **Configurable update intervals**

### AMD Ryzen Support
- Proper SMT (Simultaneous Multi-Threading) core frequency mapping
- Handles shared package temperature (Tctl/Tdie)
- Correctly maps logical cores to physical cores

## Requirements

- **OS**: Windows 10/11 (x64)
- **.NET**: .NET 8.0 Runtime
- **Permissions**: Administrator privileges (required for hardware sensor access)

## Installation

### Build from Source

```bash
# Clone repository
git clone <repository-url>
cd CProc

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run (as Administrator)
dotnet run
```

### Command Line Options

```bash
# GUI mode (default)
dotnet run

# Terminal UI mode
dotnet run -- --terminal

# Debug sensors
dotnet run -- --debug-sensors

# Show version
dotnet run -- --version
```

## Usage

### GUI Mode

The application launches with a modern WPF interface showing real-time CPU metrics, trend graphs, and detailed views.

**Keyboard Shortcuts:**
- `R` - Reset Min/Max statistics
- `E` - Export data to JSON
- `Ctrl+C` - Export data to CSV
- `F1` - Show help
- `Esc` - Close application

**Tabs:**
- **Monitoring** - Main dashboard with cores, graphs, and package stats
- **Topology** - CPU topology visualization and core mapping
- **System Info** - Detailed CPU specifications and BIOS information

### Terminal UI Mode

Launch with `--terminal` flag for a text-based interface suitable for SSH or remote monitoring.

## Configuration

Edit `appsettings.json` to customize:

```json
{
  "UpdateInterval": 1000,
  "Logging": {
    "DataLogging": {
      "Enabled": false,
      "Path": "data/sensors-{Date}.csv",
      "Interval": 1000
    }
  }
}
```

## Technical Details

### Architecture
- **Hardware Access**: [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) (no custom drivers required)
- **UI Framework**: WPF for GUI, [Spectre.Console](https://github.com/spectreconsole/spectre.console) for terminal
- **Charting**: [OxyPlot](https://github.com/oxyplot/oxyplot) for real-time graphs
- **Data Collection**: Thread-safe services with configurable update intervals

### Sensor Support
- **Frequency**: Clock sensors from LibreHardwareMonitor
- **Temperature**: Tctl/Tdie for AMD, package/core sensors for Intel
- **Power**: Package power sensors
- **Voltage**: Vcore voltage sensors
- **Utilization**: Windows Performance Counters

### AMD Ryzen SMT Mapping
For AMD Ryzen processors with SMT (e.g., 8 physical cores, 16 logical threads):
- Physical cores (0-7) have direct frequency sensors
- SMT threads (8-15) are mapped to their corresponding physical cores
- All logical cores display the frequency of their physical core

## Limitations

**Direct CPU Tuning**: Not possible on Windows (requires kernel driver). CPU frequency/voltage control is handled by:
- BIOS/UEFI firmware
- Manufacturer utilities (Intel XTU, AMD Ryzen Master)
- Third-party kernel drivers (not available for general use)

This application focuses on **comprehensive monitoring and overclocking support** rather than direct control.

## Project Structure

```
CoreFreqWindows/
├── Core/              # Core monitoring classes
├── GUI/               # WPF GUI components
├── UI/                # Terminal UI
├── Services/          # Data collection services
├── Models/            # Data models
├── Utils/             # Utility classes
└── Config/            # Configuration
```

## Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

MIT License - see LICENSE file for details

## Credits & References

- **Inspired by**: [CoreFreq](https://github.com/cyring/CoreFreq) by [Cyril Courtiat](https://github.com/cyring) - Linux CPU monitoring and tuning software
- **Hardware Monitoring**: [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) - Open-source hardware monitoring library
- **Terminal UI**: [Spectre.Console](https://github.com/spectreconsole/spectre.console) - .NET library for beautiful console applications
- **Charting**: [OxyPlot](https://github.com/oxyplot/oxyplot) - Cross-platform plotting library for .NET
- **Command Line Parsing**: [CommandLineParser](https://github.com/commandlineparser/commandline) - The best C# command line parser
- **Logging**: [Serilog](https://github.com/serilog/serilog) - Structured logging for .NET
- **Configuration**: [Microsoft.Extensions.Configuration](https://github.com/dotnet/runtime) - Configuration infrastructure for .NET

## Acknowledgments

Special thanks to the open-source community and the developers of the libraries that made this project possible.

---

**Note**: This application requires Administrator privileges to access hardware sensors. Always run from an elevated command prompt or with "Run as Administrator".

---

<sub>Created with ❤️ using [Cursor](https://cursor.sh)</sub>
