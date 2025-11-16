namespace CoreFreqWindows.Models;

public class CpuIdInfo
{
    public string Vendor { get; set; } = string.Empty;
    public string BrandString { get; set; } = string.Empty;
    public string Family { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Stepping { get; set; } = string.Empty;
    public List<string> Features { get; set; } = new();
}

public class SmbiosInfo
{
    public string Manufacturer { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string BiosVendor { get; set; } = string.Empty;
    public string BiosVersion { get; set; } = string.Empty;
    public DateTime? BiosDate { get; set; }
}

public class SystemInfo
{
    public CpuIdInfo CpuId { get; set; } = new();
    public SmbiosInfo Smbios { get; set; } = new();
    public string Architecture { get; set; } = string.Empty;
    public int PhysicalCores { get; set; }
    public int LogicalCores { get; set; }
    public long TotalMemory { get; set; } // Bytes
    public string OperatingSystem { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
}

