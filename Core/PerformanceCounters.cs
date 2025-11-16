using System.Diagnostics;

namespace CoreFreqWindows.Core;

public class PerformanceCounters : IDisposable
{
    private readonly PerformanceCounter[] _cpuCounters;
    private readonly PerformanceCounter _totalCpuCounter;
    private bool _initialized = false;

    public PerformanceCounters()
    {
        var coreCount = Environment.ProcessorCount;
        _cpuCounters = new PerformanceCounter[coreCount];
        _totalCpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
        
        Initialize();
    }

    private void Initialize()
    {
        try
        {
            var coreCount = Environment.ProcessorCount;
            for (int i = 0; i < coreCount; i++)
            {
                try
                {
                    _cpuCounters[i] = new PerformanceCounter("Processor", "% Processor Time", $"CPU {i}", true);
                    _cpuCounters[i].NextValue(); // First call always returns 0
                }
                catch
                {
                    // If specific core counter fails, try alternative naming
                    try
                    {
                        _cpuCounters[i] = new PerformanceCounter("Processor", "% Processor Time", i.ToString(), true);
                        _cpuCounters[i].NextValue();
                    }
                    catch
                    {
                        // Counter not available for this core
                    }
                }
            }
            _totalCpuCounter.NextValue(); // Initialize
            _initialized = true;
        }
        catch
        {
            // Performance counters may not be available
            _initialized = false;
        }
    }

    public Dictionary<int, double> GetCoreUtilization()
    {
        var utilization = new Dictionary<int, double>();

        if (!_initialized)
            return utilization;

        for (int i = 0; i < _cpuCounters.Length; i++)
        {
            if (_cpuCounters[i] != null)
            {
                try
                {
                    var value = _cpuCounters[i].NextValue();
                    utilization[i] = Math.Max(0, Math.Min(100, value));
                }
                catch
                {
                    utilization[i] = 0;
                }
            }
        }

        return utilization;
    }

    public double GetTotalCpuUtilization()
    {
        if (!_initialized)
            return 0;

        try
        {
            return Math.Max(0, Math.Min(100, _totalCpuCounter.NextValue()));
        }
        catch
        {
            return 0;
        }
    }

    public void Dispose()
    {
        foreach (var counter in _cpuCounters)
        {
            counter?.Dispose();
        }
        _totalCpuCounter?.Dispose();
    }
}

