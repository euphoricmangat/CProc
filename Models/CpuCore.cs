namespace CoreFreqWindows.Models;

public class CpuCore
{
    public int CoreId { get; set; }
    public double? CurrentFrequency { get; set; } // MHz
    public double? MinFrequency { get; set; } // MHz
    public double? MaxFrequency { get; set; } // MHz
    public double? Temperature { get; set; } // Celsius
    public double? MinTemperature { get; set; } // Celsius
    public double? MaxTemperature { get; set; } // Celsius
    public double? Utilization { get; set; } // Percentage 0-100
    public double? Power { get; set; } // Watts
    public double? Voltage { get; set; } // Volts
    public double? Multiplier { get; set; }
    public bool IsActive { get; set; }
}

