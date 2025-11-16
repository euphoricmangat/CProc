namespace CoreFreqWindows.Models;

public class CacheInfo
{
    public int Level { get; set; } // 1, 2, 3
    public long Size { get; set; } // Bytes
    public int Associativity { get; set; }
    public int LineSize { get; set; } // Bytes
    public int Sets { get; set; }
}

public class CoreTopology
{
    public int CoreId { get; set; }
    public int ThreadId { get; set; }
    public int PackageId { get; set; }
    public int NodeId { get; set; } // NUMA node
    public bool IsHyperThreaded { get; set; }
}

public class TopologyData
{
    public int PhysicalCores { get; set; }
    public int LogicalCores { get; set; }
    public int Packages { get; set; }
    public int NumaNodes { get; set; }
    public bool HasHyperThreading { get; set; }
    public List<CoreTopology> CoreTopology { get; set; } = new();
    public List<CacheInfo> CacheHierarchy { get; set; } = new();
}

