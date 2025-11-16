namespace CoreFreqWindows.Models;

public enum SensorType
{
    Temperature,
    Voltage,
    Power,
    Clock,
    Load,
    Fan,
    Flow,
    Control,
    Level,
    Factor,
    Data,
    SmallData,
    Throughput,
    TimeSpan,
    Energy,
    Unknown
}

public class SensorData
{
    public string Name { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public SensorType Type { get; set; }
    public double? Value { get; set; }
    public double? Min { get; set; }
    public double? Max { get; set; }
    public string? Unit { get; set; }
    public string? HardwareName { get; set; }
    public int? CoreId { get; set; } // If sensor is per-core
}

