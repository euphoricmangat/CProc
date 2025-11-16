# Changelog

All notable changes to CoreFreq for Windows will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2024

### Added
- Initial release of CoreFreq for Windows
- WPF GUI with modern dark theme
- Real-time CPU monitoring:
  - Per-core frequency monitoring
  - Per-core temperature monitoring
  - Per-core CPU utilization
  - Package-level statistics (temperature, power, voltage, utilization)
- AMD Ryzen SMT core frequency mapping
- Trend graphs using OxyPlot (CPU utilization & temperature)
- Min/Max tracking with reset functionality
- Data export (JSON/CSV)
- Terminal UI mode using Spectre.Console
- System tray integration with notifications
- Topology view (CPU topology, core mapping, cache hierarchy)
- System Info view (detailed CPU specifications, BIOS information)
- Continuous data logging to CSV files
- Settings window for configuration
- Keyboard shortcuts and context menus

### Technical Details
- Built with .NET 8.0
- Uses LibreHardwareMonitorLib for hardware access
- Requires Administrator privileges for sensor access
- Thread-safe data collection with configurable update intervals

### Known Issues
- Package power may show 0.00 W on some systems (sensor calibration)
- Terminal UI may have display issues in non-interactive environments
- Some sensors may show "N/A" if not available on specific hardware

---

**Note**: Direct CPU overclocking control is not possible on Windows (requires kernel driver). This application focuses on comprehensive monitoring and overclocking support.
