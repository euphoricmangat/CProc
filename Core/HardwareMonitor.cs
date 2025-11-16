using LibreHardwareMonitor.Hardware;
using CoreFreqWindows.Models;

namespace CoreFreqWindows.Core;

public class HardwareMonitor : IDisposable
{
    private Computer? _computer;
    private bool _isInitialized = false;
    private readonly object _lockObject = new();

    public bool IsInitialized => _isInitialized;

    public void Initialize()
    {
        if (_isInitialized)
            return;

        try
        {
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = false,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true,
                IsStorageEnabled = false,
                IsNetworkEnabled = false
            };

            _computer.Open();
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to initialize LibreHardwareMonitor: {ex.Message}", ex);
        }
    }

    public void Update()
    {
        if (!_isInitialized || _computer == null)
            return;

        lock (_lockObject)
        {
            foreach (var hardware in _computer.Hardware)
            {
                hardware.Update();
            }
        }
    }

    public IEnumerable<IHardware> GetHardware()
    {
        if (!_isInitialized || _computer == null)
            return Enumerable.Empty<IHardware>();

        lock (_lockObject)
        {
            return _computer.Hardware.ToList();
        }
    }

    public IHardware? GetCpuHardware()
    {
        if (!_isInitialized || _computer == null)
            return null;

        lock (_lockObject)
        {
            return _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        }
    }

    public IHardware? GetMotherboardHardware()
    {
        if (!_isInitialized || _computer == null)
            return null;

        lock (_lockObject)
        {
            return _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Motherboard);
        }
    }

    public List<SensorData> GetAllSensors()
    {
        var sensors = new List<SensorData>();

        if (!_isInitialized || _computer == null)
            return sensors;

        lock (_lockObject)
        {
            foreach (var hardware in _computer.Hardware)
            {
                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.Value.HasValue)
                    {
                        sensors.Add(new SensorData
                        {
                            Name = sensor.Name,
                            Identifier = sensor.Identifier.ToString(),
                            Type = MapSensorType(sensor.SensorType),
                            Value = sensor.Value,
                            Min = sensor.Min,
                            Max = sensor.Max,
                            Unit = null, // ISensor doesn't expose Unit directly
                            HardwareName = hardware.Name
                        });
                    }
                }
            }
        }

        return sensors;
    }

    private static Models.SensorType MapSensorType(LibreHardwareMonitor.Hardware.SensorType sensorType)
    {
        return sensorType switch
        {
            LibreHardwareMonitor.Hardware.SensorType.Temperature => Models.SensorType.Temperature,
            LibreHardwareMonitor.Hardware.SensorType.Voltage => Models.SensorType.Voltage,
            LibreHardwareMonitor.Hardware.SensorType.Power => Models.SensorType.Power,
            LibreHardwareMonitor.Hardware.SensorType.Clock => Models.SensorType.Clock,
            LibreHardwareMonitor.Hardware.SensorType.Load => Models.SensorType.Load,
            LibreHardwareMonitor.Hardware.SensorType.Fan => Models.SensorType.Fan,
            LibreHardwareMonitor.Hardware.SensorType.Flow => Models.SensorType.Flow,
            LibreHardwareMonitor.Hardware.SensorType.Control => Models.SensorType.Control,
            LibreHardwareMonitor.Hardware.SensorType.Level => Models.SensorType.Level,
            LibreHardwareMonitor.Hardware.SensorType.Factor => Models.SensorType.Factor,
            LibreHardwareMonitor.Hardware.SensorType.Data => Models.SensorType.Data,
            LibreHardwareMonitor.Hardware.SensorType.SmallData => Models.SensorType.SmallData,
            LibreHardwareMonitor.Hardware.SensorType.Throughput => Models.SensorType.Throughput,
            LibreHardwareMonitor.Hardware.SensorType.TimeSpan => Models.SensorType.TimeSpan,
            LibreHardwareMonitor.Hardware.SensorType.Energy => Models.SensorType.Energy,
            _ => Models.SensorType.Unknown
        };
    }

    public void Dispose()
    {
        if (_computer != null)
        {
            _computer.Close();
            _computer = null;
        }
        _isInitialized = false;
    }
}

