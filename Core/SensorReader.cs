using LibreHardwareMonitor.Hardware;
using CoreFreqWindows.Models;

namespace CoreFreqWindows.Core;

public class SensorReader
{
    private readonly HardwareMonitor _hardwareMonitor;

    public SensorReader(HardwareMonitor hardwareMonitor)
    {
        _hardwareMonitor = hardwareMonitor;
    }

    public Dictionary<int, double?> GetCoreTemperatures()
    {
        var temperatures = new Dictionary<int, double?>();
        var cpu = _hardwareMonitor.GetCpuHardware();

        if (cpu == null)
            return temperatures;

        // AMD Ryzen CPUs typically don't have per-core temperature sensors
        // They only have package-level temperature (Tctl/Tdie)
        // For now, return empty - we'll use package temp for all cores if needed
        foreach (var sensor in cpu.Sensors)
        {
            if (sensor.SensorType == LibreHardwareMonitor.Hardware.SensorType.Temperature && 
                sensor.Value.HasValue &&
                sensor.Value.Value > 0) // Filter out invalid readings
            {
                var coreId = ExtractCoreId(sensor.Name);
                if (coreId.HasValue)
                {
                    temperatures[coreId.Value] = sensor.Value.Value;
                }
            }
        }

        return temperatures;
    }

    public double? GetPackageTemperature()
    {
        var cpu = _hardwareMonitor.GetCpuHardware();
        if (cpu == null)
            return null;

        // Try multiple patterns for package temperature
        var packageSensor = cpu.Sensors
            .FirstOrDefault(s => s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Temperature &&
                                s.Value.HasValue &&
                                s.Value.Value > 0 &&
                                (s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase) ||
                                 s.Name.Contains("Tctl", StringComparison.OrdinalIgnoreCase) ||
                                 s.Name.Contains("Tdie", StringComparison.OrdinalIgnoreCase) ||
                                 (s.Name.Contains("CPU", StringComparison.OrdinalIgnoreCase) &&
                                  !s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase))));

        return packageSensor?.Value;
    }

    public double? GetVcoreVoltage()
    {
        var cpu = _hardwareMonitor.GetCpuHardware();
        if (cpu == null)
            return null;

        // Try multiple patterns for CPU core voltage
        var vcoreSensor = cpu.Sensors
            .FirstOrDefault(s => s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Voltage &&
                                s.Value.HasValue &&
                                (s.Name.Contains("Vcore", StringComparison.OrdinalIgnoreCase) ||
                                 s.Name.Contains("Core (SVI2", StringComparison.OrdinalIgnoreCase) ||
                                 s.Name.Contains("CPU Core", StringComparison.OrdinalIgnoreCase)));

        return vcoreSensor?.Value;
    }

    public double? GetPackagePower()
    {
        var cpu = _hardwareMonitor.GetCpuHardware();
        if (cpu == null)
            return null;

        var powerSensor = cpu.Sensors
            .FirstOrDefault(s => s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Power &&
                                (s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase) ||
                                 s.Name.Contains("CPU", StringComparison.OrdinalIgnoreCase)));

        return powerSensor?.Value;
    }

    public Dictionary<int, double?> GetCorePower()
    {
        var power = new Dictionary<int, double?>();
        var cpu = _hardwareMonitor.GetCpuHardware();

        if (cpu == null)
            return power;

        foreach (var sensor in cpu.Sensors)
        {
            if (sensor.SensorType == LibreHardwareMonitor.Hardware.SensorType.Power && 
                sensor.Value.HasValue)
            {
                var coreId = ExtractCoreId(sensor.Name);
                if (coreId.HasValue)
                {
                    power[coreId.Value] = sensor.Value.Value;
                }
            }
        }

        return power;
    }

    private static int? ExtractCoreId(string sensorName)
    {
        var match = System.Text.RegularExpressions.Regex.Match(sensorName, @"#?(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var coreId))
        {
            return coreId;
        }
        return null;
    }
}

