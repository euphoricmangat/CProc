using CoreFreqWindows.Core;
using CoreFreqWindows.Models;
using CoreFreqWindows.Utils;

namespace CoreFreqWindows.Services;

/// <summary>
/// Orchestrates data collection from various hardware sensors and aggregates them into a MonitoringSnapshot.
/// Manages min/max tracking and thread-safe snapshot access.
/// </summary>
public class DataCollectionService
{
    private readonly HardwareMonitor _hardwareMonitor;
    private readonly CpuMonitor _cpuMonitor;
    private readonly SensorReader _sensorReader;
    private readonly FrequencyReader _frequencyReader;
    private readonly PerformanceCounters _performanceCounters;
    private readonly SystemInfoReader _systemInfoReader;
    
    private MonitoringSnapshot _snapshot;
    private readonly object _snapshotLock = new();

    /// <summary>
    /// Initializes a new instance of the DataCollectionService class.
    /// </summary>
    /// <param name="hardwareMonitor">Hardware monitor for sensor access.</param>
    /// <param name="cpuMonitor">CPU monitor for frequency and core data.</param>
    /// <param name="sensorReader">Sensor reader for temperature, power, and voltage.</param>
    /// <param name="frequencyReader">Frequency reader for additional frequency data.</param>
    /// <param name="performanceCounters">Performance counters for CPU utilization.</param>
    /// <param name="systemInfoReader">System info reader for system information.</param>
    public DataCollectionService(
        HardwareMonitor hardwareMonitor,
        CpuMonitor cpuMonitor,
        SensorReader sensorReader,
        FrequencyReader frequencyReader,
        PerformanceCounters performanceCounters,
        SystemInfoReader systemInfoReader)
    {
        _hardwareMonitor = hardwareMonitor;
        _cpuMonitor = cpuMonitor;
        _sensorReader = sensorReader;
        _frequencyReader = frequencyReader;
        _performanceCounters = performanceCounters;
        _systemInfoReader = systemInfoReader;

        // Initialize snapshot
        _snapshot = new MonitoringSnapshot
        {
            SystemInfo = _systemInfoReader.GetSystemInfo(),
            Topology = _systemInfoReader.GetTopologyData(),
            Package = new CpuPackage() // Initialize empty package to avoid null reference
        };
    }

    /// <summary>
    /// Collects data from all hardware sensors and updates the current snapshot.
    /// Preserves min/max values from previous snapshots.
    /// </summary>
    public void CollectData()
    {
        // Update hardware sensors
        _hardwareMonitor.Update();

        // Collect all data
        var systemInfo = _systemInfoReader.GetSystemInfo();
        var topology = _systemInfoReader.GetTopologyData();
        var frequencyData = _frequencyReader.GetFrequencyData();
        var coreFrequencies = _cpuMonitor.GetCoreFrequencies();
        var coreTemperatures = _sensorReader.GetCoreTemperatures();
        var packageTemperature = _sensorReader.GetPackageTemperature();
        var coreUtilization = _performanceCounters.GetCoreUtilization();
        var totalUtilization = _performanceCounters.GetTotalCpuUtilization();
        var packagePower = _sensorReader.GetPackagePower();
        var vcoreVoltage = _sensorReader.GetVcoreVoltage();
        var allSensors = _hardwareMonitor.GetAllSensors();

        // Build package with cores
        var package = new CpuPackage
        {
            PackageId = 0,
            Temperature = packageTemperature,
            Power = packagePower,
            Voltage = vcoreVoltage,
            TotalUtilization = totalUtilization
        };

        // Determine core count
        var coreCount = Math.Max(
            Math.Max(coreFrequencies.Count, coreTemperatures.Count),
            coreUtilization.Count
        );

        if (coreCount == 0)
            coreCount = systemInfo.PhysicalCores;

        // Get previous snapshot for min/max tracking
        MonitoringSnapshot? previousSnapshot = null;
        lock (_snapshotLock)
        {
            previousSnapshot = _snapshot;
        }

        // If no per-core temperatures but package temp exists, use it for all cores (AMD Ryzen behavior)
        if (coreTemperatures.Count == 0 && packageTemperature.HasValue && packageTemperature.Value > 0)
        {
            for (int i = 0; i < coreCount; i++)
            {
                coreTemperatures[i] = packageTemperature;
            }
        }

        // Build cores
        for (int i = 0; i < coreCount; i++)
        {
            var currentFreq = coreFrequencies.GetValueOrDefault(i);
            var currentTemp = coreTemperatures.GetValueOrDefault(i);
            
            // Get previous core data for min/max tracking
            var previousCore = previousSnapshot?.Package.Cores.FirstOrDefault(c => c.CoreId == i);
            
            var core = new CpuCore
            {
                CoreId = i,
                CurrentFrequency = currentFreq > 0 ? currentFreq : null, // Only set if valid
                Temperature = currentTemp > 0 ? currentTemp : null, // Only set if valid
                Utilization = coreUtilization.GetValueOrDefault(i),
                IsActive = true,
                // Preserve min/max from previous snapshot
                MinFrequency = previousCore?.MinFrequency,
                MaxFrequency = previousCore?.MaxFrequency,
                MinTemperature = previousCore?.MinTemperature,
                MaxTemperature = previousCore?.MaxTemperature
            };

            // Update min/max for frequency
            if (core.CurrentFrequency.HasValue)
            {
                core.MinFrequency = MathHelper.Min(core.MinFrequency, core.CurrentFrequency);
                core.MaxFrequency = MathHelper.Max(core.MaxFrequency, core.CurrentFrequency);
            }

            // Update min/max for temperature
            if (core.Temperature.HasValue)
            {
                core.MinTemperature = MathHelper.Min(core.MinTemperature, core.Temperature);
                core.MaxTemperature = MathHelper.Max(core.MaxTemperature, core.Temperature);
            }

            package.Cores.Add(core);
        }

        // Update package min/max (preserve from previous)
        if (previousSnapshot?.Package != null)
        {
            package.MinTemperature = previousSnapshot.Package.MinTemperature;
            package.MaxTemperature = previousSnapshot.Package.MaxTemperature;
        }
        
        if (package.Temperature.HasValue)
        {
            package.MinTemperature = MathHelper.Min(package.MinTemperature, package.Temperature);
            package.MaxTemperature = MathHelper.Max(package.MaxTemperature, package.Temperature);
        }

        // Create new snapshot
        lock (_snapshotLock)
        {
            _snapshot = new MonitoringSnapshot
            {
                Timestamp = DateTime.Now,
                SystemInfo = systemInfo,
                Package = package,
                Frequency = frequencyData,
                Topology = topology,
                AllSensors = allSensors
            };
        }
    }

    /// <summary>
    /// Gets the current monitoring snapshot in a thread-safe manner.
    /// </summary>
    /// <returns>The current MonitoringSnapshot.</returns>
    public MonitoringSnapshot GetSnapshot()
    {
        lock (_snapshotLock)
        {
            return _snapshot;
        }
    }

    /// <summary>
    /// Clears all min/max statistics for cores and package.
    /// </summary>
    public void ClearMinMax()
    {
        lock (_snapshotLock)
        {
            foreach (var core in _snapshot.Package.Cores)
            {
                core.MinFrequency = null;
                core.MaxFrequency = null;
                core.MinTemperature = null;
                core.MaxTemperature = null;
            }
            _snapshot.Package.MinTemperature = null;
            _snapshot.Package.MaxTemperature = null;
        }
    }
}

