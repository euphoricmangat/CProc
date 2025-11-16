using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using CoreFreqWindows.Config;
using CoreFreqWindows.Models;
using CoreFreqWindows.Services;
using CoreFreqWindows.Utils;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using OxyColor = OxyPlot.OxyColor;

namespace CoreFreqWindows.GUI;

/// <summary>
/// Main WPF window for CoreFreq Windows application.
/// Displays real-time CPU monitoring data including frequencies, temperatures, utilization,
/// and trend graphs. Supports keyboard shortcuts and context menu actions.
/// </summary>
public partial class MainWindow : Window
{
    private readonly DataCollectionService _dataService;
    private readonly UpdateService _updateService;
    private readonly DispatcherTimer _updateTimer;
    private AppSettings _settings;
    private readonly ObservableCollection<CoreViewModel> _cores;
    private Forms.NotifyIcon? _notifyIcon;
    private bool _minimizeToTray;
    private double? _lastTemperature;
    private double? _temperatureAlertThreshold;
    private readonly PlotModel _utilizationModel;
    private readonly PlotModel _temperatureModel;
    private readonly LineSeries _utilizationSeries;
    private readonly LineSeries _temperatureSeries;
    private TopologyView? TopologyViewControl;
    private SystemInfoView? SystemInfoViewControl;
    private const int MaxChartPoints = 60; // 60 seconds of data

    /// <summary>
    /// Gets the utilization chart model for data binding.
    /// </summary>
    public PlotModel UtilizationModel => _utilizationModel;
    
    /// <summary>
    /// Gets the temperature chart model for data binding.
    /// </summary>
    public PlotModel TemperatureModel => _temperatureModel;

    /// <summary>
    /// Initializes a new instance of the MainWindow class.
    /// </summary>
    /// <param name="dataService">The data collection service for monitoring data.</param>
    /// <param name="updateService">The update service for periodic data updates.</param>
    /// <param name="settings">Application settings.</param>
    public MainWindow(DataCollectionService dataService, UpdateService updateService, AppSettings settings)
    {
        InitializeComponent();
        _dataService = dataService;
        _updateService = updateService;
        _settings = settings;
        _cores = new ObservableCollection<CoreViewModel>();
        CoresDataGrid.ItemsSource = _cores;

        // Initialize OxyPlot charts
        _utilizationModel = new PlotModel
        {
            PlotAreaBorderColor = OxyColor.FromRgb(139, 148, 158),
            TextColor = OxyColor.FromRgb(139, 148, 158),
            Background = OxyColors.Transparent
        };
        _utilizationModel.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Minimum = 0,
            Maximum = 100,
            Title = "%",
            TitleColor = OxyColor.FromRgb(139, 148, 158),
            TextColor = OxyColor.FromRgb(139, 148, 158),
            TicklineColor = OxyColor.FromRgb(48, 54, 61),
            MajorGridlineColor = OxyColor.FromRgb(33, 38, 45),
            MinorGridlineColor = OxyColor.FromRgb(22, 27, 34)
        });
        _utilizationModel.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Time",
            TitleColor = OxyColor.FromRgb(139, 148, 158),
            TextColor = OxyColor.FromRgb(139, 148, 158),
            TicklineColor = OxyColor.FromRgb(48, 54, 61),
            MajorGridlineColor = OxyColor.FromRgb(33, 38, 45)
        });
        _utilizationSeries = new LineSeries
        {
            Title = "CPU Load",
            Color = OxyColor.FromRgb(88, 166, 255),
            StrokeThickness = 2,
            MarkerType = MarkerType.None
        };
        _utilizationModel.Series.Add(_utilizationSeries);

        _temperatureModel = new PlotModel
        {
            PlotAreaBorderColor = OxyColor.FromRgb(139, 148, 158),
            TextColor = OxyColor.FromRgb(139, 148, 158),
            Background = OxyColors.Transparent
        };
        _temperatureModel.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "°C",
            TitleColor = OxyColor.FromRgb(139, 148, 158),
            TextColor = OxyColor.FromRgb(139, 148, 158),
            TicklineColor = OxyColor.FromRgb(48, 54, 61),
            MajorGridlineColor = OxyColor.FromRgb(33, 38, 45),
            MinorGridlineColor = OxyColor.FromRgb(22, 27, 34)
        });
        _temperatureModel.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Time",
            TitleColor = OxyColor.FromRgb(139, 148, 158),
            TextColor = OxyColor.FromRgb(139, 148, 158),
            TicklineColor = OxyColor.FromRgb(48, 54, 61),
            MajorGridlineColor = OxyColor.FromRgb(33, 38, 45)
        });
        _temperatureSeries = new LineSeries
        {
            Title = "Temperature",
            Color = OxyColor.FromRgb(255, 123, 114),
            StrokeThickness = 2,
            MarkerType = MarkerType.None
        };
        _temperatureModel.Series.Add(_temperatureSeries);

        DataContext = this;

        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(_updateService.UpdateInterval)
        };
        _updateTimer.Tick += UpdateTimer_Tick;

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        StateChanged += MainWindow_StateChanged;
        
        InitializeSystemTray();
    }
    
    private void InitializeSystemTray()
    {
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "CoreFreq for Windows",
            Visible = true
        };
        
        _notifyIcon.DoubleClick += (s, e) =>
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        };
        
        var contextMenu = new Forms.ContextMenuStrip();
        contextMenu.Items.Add("Show", null, (s, e) => { Show(); WindowState = WindowState.Normal; Activate(); });
        contextMenu.Items.Add("Settings", null, (s, e) => SettingsButton_Click(s, new System.Windows.RoutedEventArgs()));
        contextMenu.Items.Add(new Forms.ToolStripSeparator());
        contextMenu.Items.Add("Exit", null, (s, e) => Close());
        
        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Set models directly on PlotView controls to ensure they're bound
        UtilizationChart.Model = _utilizationModel;
        TemperatureChart.Model = _temperatureModel;
        
        // Initialize Topology View
        TopologyViewControl = new TopologyView(_dataService);
        TopologyViewContainer.Content = TopologyViewControl;
        
        // Initialize System Info View
        SystemInfoViewControl = new SystemInfoView(_dataService);
        SystemInfoViewContainer.Content = SystemInfoViewControl;
        
        // Initial update
        UpdateUI();
        
        // Start update timer
        _updateTimer.Start();
        StatusText.Text = "Monitoring...";
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        _updateTimer?.Stop();
        _notifyIcon?.Dispose();
    }
    
    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && _minimizeToTray)
        {
            Hide();
        }
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        _updateService.Update();
        UpdateUI();
    }

    private void UpdateUI()
    {
        var snapshot = _dataService.GetSnapshot();

        // Update header
        CpuNameText.Text = $"CPU: {snapshot.SystemInfo.CpuId.BrandString}";

        // Update system info
        CpuText.Text = snapshot.SystemInfo.CpuId.BrandString;
        PhysicalCoresText.Text = snapshot.SystemInfo.PhysicalCores.ToString();
        LogicalCoresText.Text = snapshot.SystemInfo.LogicalCores.ToString();
        ArchitectureText.Text = snapshot.SystemInfo.Architecture;

        // Update cores with color coding
        _cores.Clear();
        foreach (var core in snapshot.Package.Cores.OrderBy(c => c.CoreId))
        {
            var freqColor = GetFrequencyColor(core.CurrentFrequency);
            var tempColor = GetTemperatureColor(core.Temperature);
            
            _cores.Add(new CoreViewModel
            {
                CoreId = core.CoreId,
                FrequencyDisplay = UnitConverter.FormatFrequency(core.CurrentFrequency),
                TemperatureDisplay = UnitConverter.FormatTemperature(core.Temperature),
                UtilizationDisplay = UnitConverter.FormatPercentage(core.Utilization),
                UtilizationValue = core.Utilization ?? 0,
                FrequencyColor = freqColor,
                TemperatureColor = tempColor,
                MinFrequencyDisplay = UnitConverter.FormatFrequency(core.MinFrequency),
                MaxFrequencyDisplay = UnitConverter.FormatFrequency(core.MaxFrequency),
                MinTemperatureDisplay = UnitConverter.FormatTemperature(core.MinTemperature),
                MaxTemperatureDisplay = UnitConverter.FormatTemperature(core.MaxTemperature)
            });
        }

        // Update package info
        PackageTempText.Text = UnitConverter.FormatTemperature(snapshot.Package.Temperature);
        PackagePowerText.Text = UnitConverter.FormatPower(snapshot.Package.Power);
        TotalCpuLoadText.Text = UnitConverter.FormatPercentage(snapshot.Package.TotalUtilization);
        
        // Update system tray icon and check alerts
        UpdateSystemTray(snapshot);
        
        // Update topology view if it exists
        if (TopologyViewControl != null)
        {
            TopologyViewControl.UpdateTopology();
        }
        
        // Update system info view if it exists
        if (SystemInfoViewControl != null)
        {
            SystemInfoViewControl.UpdateSystemInfo();
        }

        // Update charts
        var totalUtil = snapshot.Package.TotalUtilization;
        _utilizationSeries.Points.Add(new DataPoint(_utilizationSeries.Points.Count, totalUtil));
        if (_utilizationSeries.Points.Count > MaxChartPoints)
        {
            _utilizationSeries.Points.RemoveAt(0);
        }
        _utilizationModel.InvalidatePlot(true);

        var packageTemp = snapshot.Package.Temperature ?? 0;
        _temperatureSeries.Points.Add(new DataPoint(_temperatureSeries.Points.Count, packageTemp));
        if (_temperatureSeries.Points.Count > MaxChartPoints)
        {
            _temperatureSeries.Points.RemoveAt(0);
        }
        _temperatureModel.InvalidatePlot(true);
    }
    
    private void ShowTemperatureAlert(double temperature)
    {
        if (_notifyIcon != null && _notifyIcon.Visible)
        {
            _notifyIcon.BalloonTipTitle = "High Temperature Alert";
            _notifyIcon.BalloonTipText = $"CPU temperature is {temperature:F1}°C - Consider checking cooling!";
            _notifyIcon.BalloonTipIcon = Forms.ToolTipIcon.Warning;
            _notifyIcon.ShowBalloonTip(5000);
        }
    }

    private static Brush GetFrequencyColor(double? frequency)
    {
        if (!frequency.HasValue || frequency.Value <= 0)
            return new SolidColorBrush(Color.FromRgb(139, 148, 158)); // Gray

        return frequency.Value switch
        {
            >= 4000 => new SolidColorBrush(Color.FromRgb(35, 134, 54)), // Green - High performance
            >= 3000 => new SolidColorBrush(Color.FromRgb(88, 166, 255)), // Blue - Normal
            >= 2000 => new SolidColorBrush(Color.FromRgb(255, 188, 47)), // Yellow - Low
            _ => new SolidColorBrush(Color.FromRgb(255, 123, 114)) // Red - Very low
        };
    }

    private static Brush GetTemperatureColor(double? temperature)
    {
        if (!temperature.HasValue || temperature.Value <= 0)
            return new SolidColorBrush(Color.FromRgb(139, 148, 158)); // Gray

        return temperature.Value switch
        {
            >= 80 => new SolidColorBrush(Color.FromRgb(255, 123, 114)), // Red - Hot
            >= 70 => new SolidColorBrush(Color.FromRgb(255, 188, 47)), // Yellow - Warm
            >= 50 => new SolidColorBrush(Color.FromRgb(88, 166, 255)), // Blue - Normal
            _ => new SolidColorBrush(Color.FromRgb(35, 134, 54)) // Green - Cool
        };
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(_settings)
        {
            Owner = this
        };
        
        if (settingsWindow.ShowDialog() == true && settingsWindow.SettingsSaved)
        {
            _settings = settingsWindow.UpdatedSettings;
            _minimizeToTray = settingsWindow.MinimizeToTray;
            
            // Update update interval if changed
            if (_updateTimer != null)
            {
                _updateTimer.Interval = TimeSpan.FromMilliseconds(_settings.UpdateInterval);
            }
            
            // Update alert threshold
            if (double.TryParse(settingsWindow.TemperatureAlertThreshold, out var threshold))
            {
                _temperatureAlertThreshold = threshold;
            }
            
            StatusText.Text = "Settings saved";
        }
    }
    
    private void UpdateSystemTray(MonitoringSnapshot snapshot)
    {
        if (_notifyIcon == null) return;
        
        var temp = snapshot.Package.Temperature ?? 0;
        var utilization = snapshot.Package.TotalUtilization;
        
        // Update tooltip with current stats
        _notifyIcon.Text = $"CoreFreq Windows\n" +
                          $"CPU: {utilization:F1}%\n" +
                          $"Temp: {temp:F1}°C\n" +
                          $"Power: {snapshot.Package.Power?.ToString("F1") ?? "N/A"}W";
        
        // Check temperature alert
        if (_temperatureAlertThreshold.HasValue && temp > _temperatureAlertThreshold.Value)
        {
            if (!_lastTemperature.HasValue || _lastTemperature.Value <= _temperatureAlertThreshold.Value)
            {
                // Temperature just crossed threshold
                _notifyIcon.BalloonTipTitle = "Temperature Alert";
                _notifyIcon.BalloonTipText = $"CPU temperature is {temp:F1}°C (threshold: {_temperatureAlertThreshold.Value}°C)";
                _notifyIcon.BalloonTipIcon = Forms.ToolTipIcon.Warning;
                _notifyIcon.ShowBalloonTip(5000);
            }
        }
        
        _lastTemperature = temp;
    }

    private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.R:
                ResetMinMax();
                break;
            case Key.E:
                ExportToJson();
                break;
            case Key.C:
                if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                {
                    ExportToCsv();
                    e.Handled = true;
                }
                break;
            case Key.F1:
                ShowHelp();
                break;
            case Key.Escape:
                Close();
                break;
        }
    }

    private void ResetMinMax_Click(object sender, RoutedEventArgs e)
    {
        ResetMinMax();
    }

    private void ExportToJson_Click(object sender, RoutedEventArgs e)
    {
        ExportToJson();
    }

    private void ExportToCsv_Click(object sender, RoutedEventArgs e)
    {
        ExportToCsv();
    }

    private void ShowHelp_Click(object sender, RoutedEventArgs e)
    {
        ShowHelp();
    }

    private void ResetMinMax()
    {
        _dataService.ClearMinMax();
        UpdateUI();
        StatusText.Text = "Min/Max statistics reset";
    }

    private void ExportToJson()
    {
        try
        {
            var snapshot = _dataService.GetSnapshot();
            var exportData = new
            {
                Timestamp = snapshot.Timestamp,
                CpuModel = snapshot.SystemInfo.CpuId.BrandString,
                PhysicalCores = snapshot.SystemInfo.PhysicalCores,
                LogicalCores = snapshot.SystemInfo.LogicalCores,
                Architecture = snapshot.SystemInfo.Architecture,
                Package = new
                {
                    Temperature = snapshot.Package.Temperature,
                    MinTemperature = snapshot.Package.MinTemperature,
                    MaxTemperature = snapshot.Package.MaxTemperature,
                    Power = snapshot.Package.Power,
                    Voltage = snapshot.Package.Voltage,
                    TotalUtilization = snapshot.Package.TotalUtilization
                },
                Cores = snapshot.Package.Cores.Select(c => new
                {
                    CoreId = c.CoreId,
                    CurrentFrequency = c.CurrentFrequency,
                    MinFrequency = c.MinFrequency,
                    MaxFrequency = c.MaxFrequency,
                    Temperature = c.Temperature,
                    MinTemperature = c.MinTemperature,
                    MaxTemperature = c.MaxTemperature,
                    Utilization = c.Utilization,
                    Power = c.Power,
                    Voltage = c.Voltage,
                    IsActive = c.IsActive
                }).ToArray()
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(exportData, Newtonsoft.Json.Formatting.Indented);
            var fileName = $"corefreq_export_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
            
            System.IO.File.WriteAllText(filePath, json);
            
            StatusText.Text = $"Exported to: {fileName}";
            
            // Show message box
            System.Windows.MessageBox.Show(
                $"Data exported successfully to:\n{filePath}",
                "Export Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Export failed: {ex.Message}";
            System.Windows.MessageBox.Show(
                $"Failed to export data:\n{ex.Message}",
                "Export Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ShowHelp()
    {
        var helpText = "Keyboard Shortcuts:\n\n" +
                      "R - Reset Min/Max statistics\n" +
                      "E - Export data to JSON\n" +
                      "Ctrl+C - Export data to CSV\n" +
                      "F1 - Show this help\n" +
                      "Esc - Close application\n\n" +
                      "Right-click on the cores table for context menu options.";
        
        System.Windows.MessageBox.Show(helpText, "Help - CoreFreq for Windows", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExportToCsv()
    {
        try
        {
            var snapshot = _dataService.GetSnapshot();
            var csv = new System.Text.StringBuilder();
            
            // Header row
            csv.AppendLine("Timestamp,CPU Model,Physical Cores,Logical Cores,Architecture");
            csv.AppendLine($"{snapshot.Timestamp:yyyy-MM-dd HH:mm:ss},\"{snapshot.SystemInfo.CpuId.BrandString}\",{snapshot.SystemInfo.PhysicalCores},{snapshot.SystemInfo.LogicalCores},{snapshot.SystemInfo.Architecture}");
            csv.AppendLine();
            
            // Package information
            csv.AppendLine("Package Information");
            csv.AppendLine("Temperature (°C),Min Temperature (°C),Max Temperature (°C),Power (W),Voltage (V),Total Utilization (%)");
            csv.AppendLine($"{snapshot.Package.Temperature?.ToString("F2") ?? "N/A"}," +
                          $"{snapshot.Package.MinTemperature?.ToString("F2") ?? "N/A"}," +
                          $"{snapshot.Package.MaxTemperature?.ToString("F2") ?? "N/A"}," +
                          $"{snapshot.Package.Power?.ToString("F2") ?? "N/A"}," +
                          $"{snapshot.Package.Voltage?.ToString("F3") ?? "N/A"}," +
                          $"{snapshot.Package.TotalUtilization.ToString("F2")}");
            csv.AppendLine();
            
            // Core data
            csv.AppendLine("Core Data");
            csv.AppendLine("Core ID,Current Frequency (MHz),Min Frequency (MHz),Max Frequency (MHz),Temperature (°C),Min Temperature (°C),Max Temperature (°C),Utilization (%),Power (W),Voltage (V),Is Active");
            
            foreach (var core in snapshot.Package.Cores.OrderBy(c => c.CoreId))
            {
                csv.AppendLine($"{core.CoreId}," +
                              $"{core.CurrentFrequency?.ToString("F2") ?? "N/A"}," +
                              $"{core.MinFrequency?.ToString("F2") ?? "N/A"}," +
                              $"{core.MaxFrequency?.ToString("F2") ?? "N/A"}," +
                              $"{core.Temperature?.ToString("F2") ?? "N/A"}," +
                              $"{core.MinTemperature?.ToString("F2") ?? "N/A"}," +
                              $"{core.MaxTemperature?.ToString("F2") ?? "N/A"}," +
                              $"{core.Utilization?.ToString("F2") ?? "N/A"}," +
                              $"{core.Power?.ToString("F2") ?? "N/A"}," +
                              $"{core.Voltage?.ToString("F3") ?? "N/A"}," +
                              $"{(core.IsActive ? "Yes" : "No")}");
            }

            var fileName = $"corefreq_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
            
            System.IO.File.WriteAllText(filePath, csv.ToString(), System.Text.Encoding.UTF8);
            
            StatusText.Text = $"Exported to: {fileName}";
            
            System.Windows.MessageBox.Show(
                $"Data exported successfully to:\n{filePath}",
                "Export Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Export failed: {ex.Message}";
            System.Windows.MessageBox.Show(
                $"Failed to export data:\n{ex.Message}",
                "Export Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}

public class CoreViewModel : INotifyPropertyChanged
{
    private int _coreId;
    private string _frequencyDisplay = "N/A";
    private string _temperatureDisplay = "N/A";
    private string _utilizationDisplay = "N/A";
    private double _utilizationValue;
    private Brush _frequencyColor = Brushes.Gray;
    private Brush _temperatureColor = Brushes.Gray;

    public int CoreId
    {
        get => _coreId;
        set { _coreId = value; OnPropertyChanged(); }
    }

    public string FrequencyDisplay
    {
        get => _frequencyDisplay;
        set { _frequencyDisplay = value; OnPropertyChanged(); }
    }

    public string TemperatureDisplay
    {
        get => _temperatureDisplay;
        set { _temperatureDisplay = value; OnPropertyChanged(); }
    }

    public string UtilizationDisplay
    {
        get => _utilizationDisplay;
        set { _utilizationDisplay = value; OnPropertyChanged(); }
    }

    public double UtilizationValue
    {
        get => _utilizationValue;
        set { _utilizationValue = value; OnPropertyChanged(); }
    }

    public Brush FrequencyColor
    {
        get => _frequencyColor;
        set { _frequencyColor = value; OnPropertyChanged(); }
    }

    public Brush TemperatureColor
    {
        get => _temperatureColor;
        set { _temperatureColor = value; OnPropertyChanged(); }
    }

    private string _minFrequencyDisplay = "N/A";
    private string _maxFrequencyDisplay = "N/A";
    private string _minTemperatureDisplay = "N/A";
    private string _maxTemperatureDisplay = "N/A";

    public string MinFrequencyDisplay
    {
        get => _minFrequencyDisplay;
        set { _minFrequencyDisplay = value; OnPropertyChanged(); }
    }

    public string MaxFrequencyDisplay
    {
        get => _maxFrequencyDisplay;
        set { _maxFrequencyDisplay = value; OnPropertyChanged(); }
    }

    public string MinTemperatureDisplay
    {
        get => _minTemperatureDisplay;
        set { _minTemperatureDisplay = value; OnPropertyChanged(); }
    }

    public string MaxTemperatureDisplay
    {
        get => _maxTemperatureDisplay;
        set { _maxTemperatureDisplay = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
