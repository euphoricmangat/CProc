namespace CoreFreqWindows.Models;

public class MonitoringSnapshot
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public SystemInfo SystemInfo { get; set; } = new();
    public CpuPackage Package { get; set; } = new();
    public FrequencyData Frequency { get; set; } = new();
    public TopologyData Topology { get; set; } = new();
    public List<SensorData> AllSensors { get; set; } = new();
}

