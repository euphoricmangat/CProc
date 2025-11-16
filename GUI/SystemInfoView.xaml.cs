using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CoreFreqWindows.Models;
using CoreFreqWindows.Services;

namespace CoreFreqWindows.GUI;

/// <summary>
/// Enhanced system information view showing detailed CPU specifications, cache hierarchy, and BIOS information.
/// </summary>
public partial class SystemInfoView : UserControl
{
    private readonly DataCollectionService? _dataService;
    private readonly ObservableCollection<CacheViewModel> _cacheInfo;

    public SystemInfoView()
    {
        InitializeComponent();
        _cacheInfo = new ObservableCollection<CacheViewModel>();
        CacheDataGrid.ItemsSource = _cacheInfo;
    }
    
    public SystemInfoView(DataCollectionService dataService) : this()
    {
        _dataService = dataService;
        UpdateSystemInfo();
    }

    public void UpdateSystemInfo()
    {
        if (_dataService == null)
            return;

        var snapshot = _dataService.GetSnapshot();
        var systemInfo = snapshot?.SystemInfo;
        var topology = snapshot?.Topology;

        if (systemInfo == null)
        {
            SetDefaultValues();
            return;
        }

        // Update CPU information
        CpuBrandText.Text = systemInfo.CpuId.BrandString;
        CpuVendorText.Text = systemInfo.CpuId.Vendor;
        CpuFamilyText.Text = !string.IsNullOrEmpty(systemInfo.CpuId.Family) ? systemInfo.CpuId.Family : "N/A";
        PhysicalCoresText.Text = systemInfo.PhysicalCores.ToString();
        LogicalCoresText.Text = systemInfo.LogicalCores.ToString();
        ArchitectureText.Text = systemInfo.Architecture;

        // Update cache hierarchy from topology
        _cacheInfo.Clear();
        if (topology?.CacheHierarchy != null)
        {
            foreach (var cache in topology.CacheHierarchy.OrderBy(c => c.Level))
            {
                _cacheInfo.Add(new CacheViewModel
                {
                    Level = $"L{cache.Level}",
                    Size = FormatBytes(cache.Size),
                    Associativity = cache.Associativity > 0 ? cache.Associativity.ToString() : "N/A",
                    LineSize = cache.LineSize > 0 ? $"{cache.LineSize} B" : "N/A"
                });
            }
        }

        // Update system information
        OsText.Text = systemInfo.OperatingSystem;
        OsVersionText.Text = systemInfo.OsVersion;
        MemoryText.Text = FormatBytes(systemInfo.TotalMemory);
        
        // Motherboard info
        var mbInfo = !string.IsNullOrEmpty(systemInfo.Smbios.Product) 
            ? $"{systemInfo.Smbios.Manufacturer} {systemInfo.Smbios.Product}".Trim()
            : "N/A";
        MotherboardText.Text = mbInfo;

        // Update BIOS information
        BiosVendorText.Text = !string.IsNullOrEmpty(systemInfo.Smbios.BiosVendor) 
            ? systemInfo.Smbios.BiosVendor 
            : "N/A";
        BiosVersionText.Text = !string.IsNullOrEmpty(systemInfo.Smbios.BiosVersion) 
            ? systemInfo.Smbios.BiosVersion 
            : "N/A";
        BiosDateText.Text = systemInfo.Smbios.BiosDate.HasValue 
            ? systemInfo.Smbios.BiosDate.Value.ToString("yyyy-MM-dd") 
            : "N/A";
    }

    private void SetDefaultValues()
    {
        CpuBrandText.Text = "N/A";
        CpuVendorText.Text = "N/A";
        CpuFamilyText.Text = "N/A";
        PhysicalCoresText.Text = "N/A";
        LogicalCoresText.Text = "N/A";
        ArchitectureText.Text = "N/A";
        OsText.Text = "N/A";
        OsVersionText.Text = "N/A";
        MemoryText.Text = "N/A";
        MotherboardText.Text = "N/A";
        BiosVendorText.Text = "N/A";
        BiosVersionText.Text = "N/A";
        BiosDateText.Text = "N/A";
        _cacheInfo.Clear();
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
            return "N/A";

        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

