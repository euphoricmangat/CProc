namespace CoreFreqWindows.Models;

public class FrequencyData
{
    public double BaseClock { get; set; } // MHz
    public double BusSpeed { get; set; } // MHz
    public Dictionary<int, double> CoreFrequencies { get; set; } = new(); // CoreId -> Frequency (MHz)
    public Dictionary<int, double> CoreMultipliers { get; set; } = new(); // CoreId -> Multiplier
    public double? MaxTurboFrequency { get; set; } // MHz
    public double? MinFrequency { get; set; } // MHz
    public double? MaxFrequency { get; set; } // MHz
}

