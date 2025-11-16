using CoreFreqWindows.Models;

namespace CoreFreqWindows.Core;

public class FrequencyReader
{
    private readonly HardwareMonitor _hardwareMonitor;
    private readonly CpuMonitor _cpuMonitor;

    public FrequencyReader(HardwareMonitor hardwareMonitor, CpuMonitor cpuMonitor)
    {
        _hardwareMonitor = hardwareMonitor;
        _cpuMonitor = cpuMonitor;
    }

    public FrequencyData GetFrequencyData()
    {
        var data = new FrequencyData();
        var coreFrequencies = _cpuMonitor.GetCoreFrequencies();

        foreach (var (coreId, frequency) in coreFrequencies)
        {
            data.CoreFrequencies[coreId] = frequency;
            
            // Calculate multiplier (assuming base clock of 100 MHz, adjust if needed)
            var baseClock = 100.0; // Default base clock
            data.CoreMultipliers[coreId] = frequency / baseClock;
        }

        if (coreFrequencies.Count > 0)
        {
            data.MinFrequency = coreFrequencies.Values.Min();
            data.MaxFrequency = coreFrequencies.Values.Max();
            data.MaxTurboFrequency = data.MaxFrequency;
        }

        // Try to get base clock from WMI or use default
        data.BaseClock = 100.0; // Will be refined with SystemInfoReader
        data.BusSpeed = data.BaseClock;

        return data;
    }
}

