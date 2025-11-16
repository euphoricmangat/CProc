namespace CoreFreqWindows.Models;

public class CpuPackage
{
    public int PackageId { get; set; }
    public double? Temperature { get; set; } // Celsius
    public double? MinTemperature { get; set; } // Celsius
    public double? MaxTemperature { get; set; } // Celsius
    public double? Power { get; set; } // Watts
    public double? MaxPower { get; set; } // Watts (TDP)
    public double? Voltage { get; set; } // Volts
    public double TotalUtilization { get; set; } // Percentage 0-100
    public List<CpuCore> Cores { get; set; } = new();
}

