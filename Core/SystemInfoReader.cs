using System.Management;
using CoreFreqWindows.Models;

namespace CoreFreqWindows.Core;

public class SystemInfoReader
{
    public SystemInfo GetSystemInfo()
    {
        var info = new SystemInfo
        {
            Architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86",
            PhysicalCores = Environment.ProcessorCount,
            LogicalCores = Environment.ProcessorCount,
            OperatingSystem = Environment.OSVersion.Platform.ToString(),
            OsVersion = Environment.OSVersion.VersionString
        };

        try
        {
            // Get CPUID info from WMI
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                info.CpuId.BrandString = obj["Name"]?.ToString() ?? string.Empty;
                info.CpuId.Vendor = obj["Manufacturer"]?.ToString() ?? string.Empty;
                info.PhysicalCores = Convert.ToInt32(obj["NumberOfCores"] ?? Environment.ProcessorCount);
                info.LogicalCores = Convert.ToInt32(obj["NumberOfLogicalProcessors"] ?? Environment.ProcessorCount);
                
                // Get additional CPU details
                info.CpuId.Family = obj["Family"]?.ToString() ?? string.Empty;
                info.CpuId.Model = obj["Description"]?.ToString() ?? string.Empty;
                
                // Get cache sizes (in KB, convert to bytes)
                var l2Cache = obj["L2CacheSize"]?.ToString();
                var l3Cache = obj["L3CacheSize"]?.ToString();
                if (!string.IsNullOrEmpty(l2Cache) && int.TryParse(l2Cache, out var l2Size))
                {
                    // L2CacheSize is in KB
                }
                if (!string.IsNullOrEmpty(l3Cache) && int.TryParse(l3Cache, out var l3Size))
                {
                    // L3CacheSize is in KB
                }
                
                break; // Usually one CPU
            }

            // Get SMBIOS info
            using var mbSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
            foreach (ManagementObject obj in mbSearcher.Get())
            {
                info.Smbios.Manufacturer = obj["Manufacturer"]?.ToString() ?? string.Empty;
                info.Smbios.Product = obj["Product"]?.ToString() ?? string.Empty;
                info.Smbios.Version = obj["Version"]?.ToString() ?? string.Empty;
                info.Smbios.SerialNumber = obj["SerialNumber"]?.ToString() ?? string.Empty;
                break;
            }

            // Get BIOS info
            using var biosSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS");
            foreach (ManagementObject obj in biosSearcher.Get())
            {
                info.Smbios.BiosVendor = obj["Manufacturer"]?.ToString() ?? string.Empty;
                info.Smbios.BiosVersion = obj["Version"]?.ToString() ?? string.Empty;
                var releaseDate = obj["ReleaseDate"]?.ToString();
                if (!string.IsNullOrEmpty(releaseDate) && 
                    ManagementDateTimeConverter.ToDateTime(releaseDate) != DateTime.MinValue)
                {
                    info.Smbios.BiosDate = ManagementDateTimeConverter.ToDateTime(releaseDate);
                }
                break;
            }

            // Get total memory
            using var memSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
            foreach (ManagementObject obj in memSearcher.Get())
            {
                var totalMemory = Convert.ToInt64(obj["TotalPhysicalMemory"] ?? 0);
                info.TotalMemory = totalMemory;
                break;
            }
        }
        catch
        {
            // WMI queries may fail, use defaults
        }

        return info;
    }

    public TopologyData GetTopologyData()
    {
        var topology = new TopologyData
        {
            PhysicalCores = Environment.ProcessorCount,
            LogicalCores = Environment.ProcessorCount,
            Packages = 1,
            NumaNodes = 1,
            HasHyperThreading = false
        };

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                topology.PhysicalCores = Convert.ToInt32(obj["NumberOfCores"] ?? Environment.ProcessorCount);
                topology.LogicalCores = Convert.ToInt32(obj["NumberOfLogicalProcessors"] ?? Environment.ProcessorCount);
                topology.HasHyperThreading = topology.LogicalCores > topology.PhysicalCores;
                break;
            }

            // Build core topology
            for (int i = 0; i < topology.LogicalCores; i++)
            {
                topology.CoreTopology.Add(new CoreTopology
                {
                    CoreId = i % topology.PhysicalCores,
                    ThreadId = i,
                    PackageId = 0,
                    NodeId = 0,
                    IsHyperThreaded = i >= topology.PhysicalCores
                });
            }

            // Get cache sizes from WMI
            using var cacheSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_CacheMemory");
            var l2CacheSize = 0L;
            var l3CacheSize = 0L;
            
            foreach (ManagementObject obj in cacheSearcher.Get())
            {
                var level = Convert.ToInt32(obj["Level"] ?? 0);
                var size = Convert.ToInt64(obj["MaxCacheSize"] ?? 0); // Size in KB
                
                if (level == 2 && size > 0)
                {
                    l2CacheSize = size * 1024; // Convert KB to bytes
                }
                else if (level == 3 && size > 0)
                {
                    l3CacheSize = size * 1024; // Convert KB to bytes
                }
            }
            
            // Also try to get from Win32_Processor (L2CacheSize, L3CacheSize in KB)
            using var procSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            foreach (ManagementObject obj in procSearcher.Get())
            {
                if (l2CacheSize == 0)
                {
                    var l2 = obj["L2CacheSize"]?.ToString();
                    if (!string.IsNullOrEmpty(l2) && int.TryParse(l2, out var l2Size) && l2Size > 0)
                    {
                        l2CacheSize = l2Size * 1024; // Convert KB to bytes
                    }
                }
                
                if (l3CacheSize == 0)
                {
                    var l3 = obj["L3CacheSize"]?.ToString();
                    if (!string.IsNullOrEmpty(l3) && int.TryParse(l3, out var l3Size) && l3Size > 0)
                    {
                        l3CacheSize = l3Size * 1024; // Convert KB to bytes
                    }
                }
                break;
            }
            
            // Add cache hierarchy (use actual sizes if available, otherwise estimates)
            // L1 cache is typically 32KB per core (instruction + data cache)
            var l1Size = topology.PhysicalCores * 32 * 1024; // Estimate: 32KB per core
            topology.CacheHierarchy.Add(new CacheInfo { Level = 1, Size = l1Size });
            
            if (l2CacheSize > 0)
            {
                topology.CacheHierarchy.Add(new CacheInfo { Level = 2, Size = l2CacheSize });
            }
            else
            {
                // Estimate: 256KB per core
                topology.CacheHierarchy.Add(new CacheInfo { Level = 2, Size = topology.PhysicalCores * 256 * 1024 });
            }
            
            if (l3CacheSize > 0)
            {
                topology.CacheHierarchy.Add(new CacheInfo { Level = 3, Size = l3CacheSize });
            }
            else
            {
                // Estimate: 8MB shared
                topology.CacheHierarchy.Add(new CacheInfo { Level = 3, Size = 8 * 1024 * 1024 });
            }
        }
        catch
        {
            // Use defaults
        }

        return topology;
    }
}

