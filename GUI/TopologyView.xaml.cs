using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CoreFreqWindows.Models;
using CoreFreqWindows.Services;

namespace CoreFreqWindows.GUI;

/// <summary>
/// Topology view showing CPU topology, core mapping, and cache hierarchy.
/// </summary>
public partial class TopologyView : UserControl
{
    private readonly DataCollectionService? _dataService;
    private readonly ObservableCollection<CoreMappingViewModel> _coreMappings;
    private readonly ObservableCollection<CacheViewModel> _cacheInfo;

    public TopologyView()
    {
        InitializeComponent();
        _coreMappings = new ObservableCollection<CoreMappingViewModel>();
        _cacheInfo = new ObservableCollection<CacheViewModel>();
    }
    
    public TopologyView(DataCollectionService dataService) : this()
    {
        _dataService = dataService;
        
        CoreMappingDataGrid.ItemsSource = _coreMappings;
        CacheDataGrid.ItemsSource = _cacheInfo;
        
        UpdateTopology();
    }

    public void UpdateTopology()
    {
        if (_dataService == null)
            return;

        var snapshot = _dataService.GetSnapshot();
        var topology = snapshot?.Topology;

        if (topology == null)
        {
            PhysicalCoresText.Text = "N/A";
            LogicalCoresText.Text = "N/A";
            SmtText.Text = "N/A";
            NumaNodesText.Text = "N/A";
            _coreMappings.Clear();
            _cacheInfo.Clear();
            TopologyCanvas.Children.Clear();
            return;
        }

        // Update overview
        PhysicalCoresText.Text = topology.PhysicalCores.ToString();
        LogicalCoresText.Text = topology.LogicalCores.ToString();
        SmtText.Text = topology.HasHyperThreading ? "Enabled" : "Disabled";
        NumaNodesText.Text = topology.NumaNodes.ToString();

        // Update core mapping
        _coreMappings.Clear();
        if (topology.CoreTopology != null)
        {
            foreach (var coreTopology in topology.CoreTopology.OrderBy(c => c.ThreadId))
            {
                _coreMappings.Add(new CoreMappingViewModel
                {
                    LogicalCore = coreTopology.ThreadId,
                    PhysicalCore = coreTopology.CoreId,
                    Package = coreTopology.PackageId,
                    NumaNode = coreTopology.NodeId,
                    IsSmt = coreTopology.IsHyperThreaded ? "Yes" : "No"
                });
            }
        }

        // Update cache hierarchy
        _cacheInfo.Clear();
        if (topology.CacheHierarchy != null)
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

        // Draw topology visualization
        DrawTopologyVisualization(topology);
    }

    private void DrawTopologyVisualization(TopologyData topology)
    {
        TopologyCanvas.Children.Clear();

        if (topology == null || topology.PhysicalCores <= 0)
            return;

        var physicalCores = topology.PhysicalCores;
        var logicalCores = topology.LogicalCores;
        var coresPerRow = Math.Min(8, physicalCores);
        var rows = (int)Math.Ceiling((double)physicalCores / coresPerRow);

        var coreWidth = 60;
        var coreHeight = 40;
        var spacing = 10;
        var startX = 20;
        var startY = 20;

        // Draw physical cores
        for (int i = 0; i < physicalCores; i++)
        {
            var row = i / coresPerRow;
            var col = i % coresPerRow;
            var x = startX + col * (coreWidth + spacing);
            var y = startY + row * (coreHeight + spacing);

            // Physical core rectangle
            var coreRect = new System.Windows.Shapes.Rectangle
            {
                Width = coreWidth,
                Height = coreHeight,
                Fill = new SolidColorBrush(Color.FromRgb(88, 166, 255)),
                Stroke = new SolidColorBrush(Color.FromRgb(139, 148, 158)),
                StrokeThickness = 1
            };
            Canvas.SetLeft(coreRect, x);
            Canvas.SetTop(coreRect, y);
            TopologyCanvas.Children.Add(coreRect);

            // Core label
            var label = new TextBlock
            {
                Text = $"P{i}",
                Foreground = Brushes.White,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Canvas.SetLeft(label, x + coreWidth / 2 - 10);
            Canvas.SetTop(label, y + coreHeight / 2 - 8);
            TopologyCanvas.Children.Add(label);

            // SMT threads (if any)
            if (topology.HasHyperThreading && i + physicalCores < logicalCores)
            {
                var smtRect = new System.Windows.Shapes.Rectangle
                {
                    Width = coreWidth - 10,
                    Height = coreHeight - 10,
                    Fill = new SolidColorBrush(Color.FromRgb(139, 148, 158)),
                    Stroke = new SolidColorBrush(Color.FromRgb(88, 166, 255)),
                    StrokeThickness = 1,
                    Opacity = 0.7
                };
                Canvas.SetLeft(smtRect, x + 5);
                Canvas.SetTop(smtRect, y + 5);
                TopologyCanvas.Children.Add(smtRect);

                var smtLabel = new TextBlock
                {
                    Text = $"L{i + physicalCores}",
                    Foreground = Brushes.White,
                    FontSize = 9,
                    Opacity = 0.8
                };
                Canvas.SetLeft(smtLabel, x + coreWidth / 2 - 8);
                Canvas.SetTop(smtLabel, y + coreHeight / 2 + 2);
                TopologyCanvas.Children.Add(smtLabel);
            }
        }

        // Legend
        var legendY = startY + rows * (coreHeight + spacing) + 20;
        var legendX = startX;

        var legendTitle = new TextBlock
        {
            Text = "Legend:",
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            FontSize = 12
        };
        Canvas.SetLeft(legendTitle, legendX);
        Canvas.SetTop(legendTitle, legendY);
        TopologyCanvas.Children.Add(legendTitle);

        var pCoreLabel = new TextBlock
        {
            Text = "P# = Physical Core",
            Foreground = new SolidColorBrush(Color.FromRgb(88, 166, 255)),
            FontSize = 10
        };
        Canvas.SetLeft(pCoreLabel, legendX);
        Canvas.SetTop(pCoreLabel, legendY + 20);
        TopologyCanvas.Children.Add(pCoreLabel);

        var lCoreLabel = new TextBlock
        {
            Text = "L# = Logical Core (SMT)",
            Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)),
            FontSize = 10
        };
        Canvas.SetLeft(lCoreLabel, legendX);
        Canvas.SetTop(lCoreLabel, legendY + 40);
        TopologyCanvas.Children.Add(lCoreLabel);
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
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

public class CoreMappingViewModel
{
    public int LogicalCore { get; set; }
    public int PhysicalCore { get; set; }
    public int Package { get; set; }
    public int NumaNode { get; set; }
    public string IsSmt { get; set; } = "No";
}

public class CacheViewModel
{
    public string Level { get; set; } = "";
    public string Size { get; set; } = "";
    public string Associativity { get; set; } = "";
    public string LineSize { get; set; } = "";
}

