using LibreHardwareMonitor.Hardware;
using CoreFreqWindows.Models;

namespace CoreFreqWindows.Core;

/// <summary>
/// Provides CPU-specific monitoring functionality including CPU name, core counts, and frequency readings.
/// Handles AMD SMT (Simultaneous Multi-Threading) core mapping for proper frequency display.
/// </summary>
public class CpuMonitor
{
    private readonly HardwareMonitor _hardwareMonitor;

    /// <summary>
    /// Initializes a new instance of the CpuMonitor class.
    /// </summary>
    /// <param name="hardwareMonitor">The hardware monitor instance to use for sensor access.</param>
    public CpuMonitor(HardwareMonitor hardwareMonitor)
    {
        _hardwareMonitor = hardwareMonitor;
    }

    /// <summary>
    /// Gets the CPU name/brand string from hardware sensors.
    /// </summary>
    /// <returns>The CPU name, or null if not available.</returns>
    public string? GetCpuName()
    {
        var cpu = _hardwareMonitor.GetCpuHardware();
        return cpu?.Name;
    }

    /// <summary>
    /// Gets the physical core count by detecting clock sensors.
    /// Falls back to Environment.ProcessorCount if sensors are not available.
    /// </summary>
    /// <returns>The number of physical CPU cores.</returns>
    public int GetCoreCount()
    {
        var cpu = _hardwareMonitor.GetCpuHardware();
        if (cpu == null)
            return 0;

        var coreSensors = cpu.Sensors
            .Where(s => s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Clock && 
                       s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return coreSensors.Count > 0 ? coreSensors.Count : Environment.ProcessorCount;
    }

    /// <summary>
    /// Gets the total thread count (logical cores including SMT/hyperthreading).
    /// </summary>
    /// <returns>The number of logical CPU cores/threads.</returns>
    public int GetThreadCount()
    {
        return Environment.ProcessorCount;
    }

    /// <summary>
    /// Gets the current frequency for all logical cores.
    /// For AMD Ryzen with SMT, maps SMT threads to their physical core frequencies.
    /// </summary>
    /// <returns>A dictionary mapping logical core ID to frequency in MHz.</returns>
    public Dictionary<int, double> GetCoreFrequencies()
    {
        var frequencies = new Dictionary<int, double>();
        var cpu = _hardwareMonitor.GetCpuHardware();

        if (cpu == null)
            return frequencies;

        // First, collect physical core frequencies
        var physicalCoreFrequencies = new Dictionary<int, double>();
        foreach (var sensor in cpu.Sensors)
        {
            if (sensor.SensorType == LibreHardwareMonitor.Hardware.SensorType.Clock)
            {
                // Try to extract core ID from sensor name (e.g., "Core #1", "Core #2")
                var coreId = ExtractCoreId(sensor.Name);
                if (coreId.HasValue && sensor.Value.HasValue && !double.IsNaN(sensor.Value.Value) && sensor.Value.Value > 0)
                {
                    physicalCoreFrequencies[coreId.Value] = sensor.Value.Value;
                }
            }
        }

        // Get logical core count from load sensors
        var logicalCoreCount = cpu.Sensors.Count(s => 
            s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Load && 
            s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase));
        
        var physicalCoreCount = physicalCoreFrequencies.Count;

        // Map all logical cores to physical cores (AMD SMT mapping)
        // Logical cores 0 to (physicalCoreCount-1) are physical cores
        // Logical cores physicalCoreCount to (logicalCoreCount-1) are SMT threads
        for (int logicalCore = 0; logicalCore < logicalCoreCount; logicalCore++)
        {
            int physicalCoreId = logicalCore < physicalCoreCount ? logicalCore : logicalCore - physicalCoreCount;
            
            // If we have frequency data for the physical core, use it for this logical core
            if (physicalCoreFrequencies.TryGetValue(physicalCoreId, out double freq))
            {
                frequencies[logicalCore] = freq;
            }
        }

        return frequencies;
    }

    /// <summary>
    /// Extracts the core ID from a sensor name using regex pattern matching.
    /// </summary>
    /// <param name="sensorName">The sensor name (e.g., "Core #0", "CPU Core #1").</param>
    /// <returns>The core ID if found, otherwise null.</returns>
    private static int? ExtractCoreId(string sensorName)
    {
        // Try to find core number in sensor name (e.g., "Core #0", "CPU Core #1", etc.)
        var match = System.Text.RegularExpressions.Regex.Match(sensorName, @"#?(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var coreId))
        {
            return coreId;
        }
        return null;
    }
}

