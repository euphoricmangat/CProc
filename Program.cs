using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommandLine;
using LibreHardwareMonitor.Hardware;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using Spectre.Console;
using System.Windows;
using CoreFreqWindows.Config;
using CoreFreqWindows.Core;
using CoreFreqWindows.GUI;
using CoreFreqWindows.Services;
using CoreFreqWindows.UI;
using CoreFreqWindows.UI.Views;

namespace CoreFreqWindows;

class Program
{
    private static AppSettings? _settings;
    private static HardwareMonitor? _hardwareMonitor;
    private static DataCollectionService? _dataCollectionService;
    private static UpdateService? _updateService;
    private static DataLoggingService? _dataLoggingService;
    private static TerminalUI? _terminalUI;

    [STAThread]
    static int Main(string[] args)
    {
        // Parse command line arguments
        var parserResult = Parser.Default.ParseArguments<CommandLineOptions>(args);
        
        return parserResult.MapResult(
            options => RunApplication(options).GetAwaiter().GetResult(),
            errors => 1
        );
    }

    static async Task<int> RunApplication(CommandLineOptions options)
    {
        try
        {
            // Handle special options
            if (options.Version)
            {
                Console.WriteLine($"{Constants.ApplicationName} v{Constants.ApplicationVersion}");
                return 0;
            }

            if (options.Help)
            {
                ShowHelp();
                return 0;
            }

            // Load configuration
            LoadConfiguration();

            // Apply command line overrides
            if (options.Interval.HasValue)
            {
                _settings!.UpdateInterval = options.Interval.Value;
            }

            // Initialize logging
            InitializeLogging();

            Log.Information("Starting {ApplicationName} v{Version}", Constants.ApplicationName, Constants.ApplicationVersion);

            // Handle system info export
            if (options.SystemInfo)
            {
                await ExportSystemInfo();
                return 0;
            }

            // Handle debug sensors
            if (options.DebugSensors)
            {
                await DebugSensors();
                return 0;
            }

            // Initialize hardware monitoring
            InitializeHardwareMonitoring();

            // Initialize services
            InitializeServices();

            // Launch GUI or Terminal UI based on options
            if (options.Terminal)
            {
                // Terminal mode
                CheckTerminalSize();
                var initialView = GetInitialView(options);
                _terminalUI = new TerminalUI(_dataCollectionService!, _updateService!, initialView);

                using var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                    _terminalUI.Stop();
                };

                await _terminalUI.RunAsync(cts.Token);
            }
            else
            {
                // GUI mode (default)
                // Initialize WPF Application on STA thread
                var app = new Application();
                app.ShutdownMode = ShutdownMode.OnMainWindowClose;
                
                try
                {
                    var mainWindow = new MainWindow(_dataCollectionService!, _updateService!, _settings!);
                    app.MainWindow = mainWindow;
                    mainWindow.Show();
                    
                    Log.Information("GUI window opened");
                    app.Run();
                }
                catch (Exception guiEx)
                {
                    Log.Error(guiEx, "Failed to create GUI window");
                    MessageBox.Show($"Failed to create GUI window: {guiEx.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    throw;
                }
            }

            Log.Information("Application shutdown complete");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            Log.Error(ex, "Fatal error occurred");
            return 1;
        }
        finally
        {
            Cleanup();
        }
    }

    static void LoadConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

        var configuration = builder.Build();
        _settings = new AppSettings();
        configuration.Bind(_settings);

        // Validate settings
        _settings.UpdateInterval = Math.Max(
            Constants.MinUpdateInterval,
            Math.Min(Constants.MaxUpdateInterval, _settings.UpdateInterval)
        );
    }

    static void InitializeLogging()
    {
        var logConfig = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}{Exception}");

        if (_settings?.Logging.File.Enabled == true)
        {
            var logPath = _settings.Logging.File.Path.Replace("{Date}", DateTime.Now.ToString("yyyy-MM-dd"));
            logConfig.WriteTo.File(logPath, rollingInterval: RollingInterval.Day);
        }

        Log.Logger = logConfig.CreateLogger();
    }

    static void CheckTerminalSize()
    {
        try
        {
            var width = Console.WindowWidth;
            var height = Console.WindowHeight;

            if (width < Constants.MinimumTerminalWidth || height < Constants.MinimumTerminalHeight)
            {
                AnsiConsole.MarkupLine($"[red]Warning: Terminal size ({width}x{height}) is below minimum ({Constants.MinimumTerminalWidth}x{Constants.MinimumTerminalHeight})[/]");
                AnsiConsole.MarkupLine("[yellow]Some content may not display correctly. Please resize your terminal.[/]");
                AnsiConsole.WriteLine();
                Thread.Sleep(2000);
            }
        }
        catch (IOException)
        {
            // Console handle may not be available in some environments
            // Continue anyway - the UI will handle it
        }
    }

    static void InitializeHardwareMonitoring()
    {
        // Don't use AnsiConsole in GUI mode as it may cause issues
        try
        {
            Console.WriteLine("Initializing hardware monitoring...");
            _hardwareMonitor = new HardwareMonitor();
            _hardwareMonitor.Initialize();
            Console.WriteLine("Hardware monitoring initialized");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize hardware monitoring");
            throw new InvalidOperationException(
                "Failed to initialize hardware monitoring. " +
                "Please ensure you have administrator privileges and that LibreHardwareMonitor drivers are available.",
                ex);
        }
    }

    static void InitializeServices()
    {
        var cpuMonitor = new CpuMonitor(_hardwareMonitor!);
        var sensorReader = new SensorReader(_hardwareMonitor!);
        var frequencyReader = new FrequencyReader(_hardwareMonitor!, cpuMonitor);
        var performanceCounters = new PerformanceCounters();
        var systemInfoReader = new SystemInfoReader();

        _dataCollectionService = new DataCollectionService(
            _hardwareMonitor!,
            cpuMonitor,
            sensorReader,
            frequencyReader,
            performanceCounters,
            systemInfoReader
        );

        _updateService = new UpdateService(_dataCollectionService, _settings!.UpdateInterval);

        // Initialize data logging if enabled
        if (_settings.Logging.DataLogging.Enabled)
        {
            _dataLoggingService = new DataLoggingService(_dataCollectionService, _settings.Logging.DataLogging);
            _dataLoggingService.Start();
        }

        // Do initial data collection
        _dataCollectionService.CollectData();
    }

    static BaseView GetInitialView(CommandLineOptions options)
    {
        var viewName = options.View ?? (options.Dashboard ? "dashboard" : _settings!.DefaultView);
        var colorScheme = ColorScheme.Default;

        return viewName.ToLower() switch
        {
            "dashboard" => new DashboardView(colorScheme),
            "frequency" or "freq" => new DashboardView(colorScheme), // Placeholder for Phase 2
            "temp" or "temperature" => new DashboardView(colorScheme), // Placeholder for Phase 2
            "voltage" => new DashboardView(colorScheme), // Placeholder for Phase 2
            "power" => new DashboardView(colorScheme), // Placeholder for Phase 2
            "topology" => new DashboardView(colorScheme), // Placeholder for Phase 3
            "system" => new DashboardView(colorScheme), // Placeholder for Phase 3
            "sensors" => new DashboardView(colorScheme), // Placeholder for Phase 2
            _ => new DashboardView(colorScheme)
        };
    }

    static async Task ExportSystemInfo()
    {
        InitializeHardwareMonitoring();
        InitializeServices();

        var snapshot = _dataCollectionService!.GetSnapshot();
        var json = JsonConvert.SerializeObject(snapshot.SystemInfo, Formatting.Indented);
        Console.WriteLine(json);
    }

    static async Task DebugSensors()
    {
        InitializeHardwareMonitoring();

        var cpu = _hardwareMonitor!.GetCpuHardware();
        if (cpu == null)
        {
            Console.WriteLine("No CPU hardware found.");
            return;
        }

        Console.WriteLine($"CPU Hardware: {cpu.Name}");
        Console.WriteLine($"\nAll Sensors ({cpu.Sensors.Count()} total):\n");
        Console.WriteLine($"{"Type",-15} {"Name",-40} {"Value",-15} {"Min",-10} {"Max",-10} {"Identifier"}");
        Console.WriteLine(new string('-', 120));

        foreach (var sensor in cpu.Sensors.OrderBy(s => s.SensorType).ThenBy(s => s.Name))
        {
            var value = sensor.Value.HasValue ? sensor.Value.Value.ToString("F2") : "N/A";
            var min = sensor.Min.HasValue ? sensor.Min.Value.ToString("F2") : "N/A";
            var max = sensor.Max.HasValue ? sensor.Max.Value.ToString("F2") : "N/A";
            Console.WriteLine($"{sensor.SensorType,-15} {sensor.Name,-40} {value,-15} {min,-10} {max,-10} {sensor.Identifier}");
        }

        Console.WriteLine("\n\nClock Sensors (Frequencies):");
        var clockSensors = cpu.Sensors.Where(s => s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Clock).ToList();
        if (clockSensors.Count == 0)
        {
            Console.WriteLine("  No clock sensors found!");
        }
        else
        {
            foreach (var sensor in clockSensors)
            {
                Console.WriteLine($"  {sensor.Name}: {sensor.Value?.ToString("F2") ?? "N/A"} MHz");
            }
        }

        Console.WriteLine("\nTemperature Sensors:");
        var tempSensors = cpu.Sensors.Where(s => s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Temperature).ToList();
        if (tempSensors.Count == 0)
        {
            Console.WriteLine("  No temperature sensors found!");
        }
        else
        {
            foreach (var sensor in tempSensors)
            {
                Console.WriteLine($"  {sensor.Name}: {sensor.Value?.ToString("F2") ?? "N/A"} °C");
            }
        }
    }

    static void ShowHelp()
    {
        Console.WriteLine($"{Constants.ApplicationName} v{Constants.ApplicationVersion}");
        Console.WriteLine();
        Console.WriteLine("Usage: CoreFreqWin [OPTIONS]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -t, --view <view>     Start with specific view (dashboard, frequency, temp, voltage, power, topology, system, sensors)");
        Console.WriteLine("  -d, --dashboard       Start in dashboard mode (default)");
        Console.WriteLine("  -s, --system-info     Print system info and exit (JSON format)");
        Console.WriteLine("  -i, --interval <ms>   Update interval in milliseconds (default: 1000)");
        Console.WriteLine("  -l, --log <file>     Log data to file");
        Console.WriteLine("  -h, --help           Show help");
        Console.WriteLine("  -v, --version        Show version");
        Console.WriteLine();
        Console.WriteLine("Keyboard Shortcuts:");
        Console.WriteLine("  F1        Help");
        Console.WriteLine("  F2        Frequency view");
        Console.WriteLine("  F3        Temperature view");
        Console.WriteLine("  F4        Voltage view");
        Console.WriteLine("  F5        Power view");
        Console.WriteLine("  F6        Topology view");
        Console.WriteLine("  F7        System info view");
        Console.WriteLine("  F8        All sensors view");
        Console.WriteLine("  Space     Pause/Resume");
        Console.WriteLine("  Q         Quit");
        Console.WriteLine("  Ctrl+C    Quit");
    }

    static void Cleanup()
    {
        _dataLoggingService?.Dispose();
        _hardwareMonitor?.Dispose();
        Log.CloseAndFlush();
    }
}

