using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreFreqWindows.Config;
using CoreFreqWindows.Models;

namespace CoreFreqWindows.Services;

/// <summary>
/// Service for continuous data logging to CSV files.
/// </summary>
public class DataLoggingService : IDisposable
{
    private readonly DataCollectionService _dataCollectionService;
    private readonly DataLoggingSettings _settings;
    private Timer? _loggingTimer;
    private StreamWriter? _logWriter;
    private readonly object _lockObject = new();
    private bool _disposed = false;
    private string? _currentLogFile;

    public DataLoggingService(DataCollectionService dataCollectionService, DataLoggingSettings settings)
    {
        _dataCollectionService = dataCollectionService;
        _settings = settings;
    }

    public void Start()
    {
        if (!_settings.Enabled)
            return;

        // Ensure log directory exists
        var logPath = _settings.Path;
        var directory = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Replace date placeholder
        _currentLogFile = logPath.Replace("{Date}", DateTime.Now.ToString("yyyyMMdd"));
        
        // Initialize log file with header
        InitializeLogFile();

        // Start periodic logging
        _loggingTimer = new Timer(LogData, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(_settings.Interval));
    }

    public void Stop()
    {
        _loggingTimer?.Dispose();
        _loggingTimer = null;
        
        lock (_lockObject)
        {
            _logWriter?.Flush();
            _logWriter?.Close();
            _logWriter = null;
        }
    }

    private void InitializeLogFile()
    {
        lock (_lockObject)
        {
            try
            {
                if (string.IsNullOrEmpty(_currentLogFile))
                    return;
                    
                var fileExists = File.Exists(_currentLogFile);
                _logWriter = new StreamWriter(_currentLogFile, append: true, System.Text.Encoding.UTF8);

                // Write header if new file
                if (!fileExists)
                {
                    _logWriter.WriteLine("Timestamp,CPU Model,Physical Cores,Logical Cores," +
                                       "Package Temp (°C),Package Power (W),Package Voltage (V),Total Utilization (%)," +
                                       "Core ID,Core Frequency (MHz),Core Temperature (°C),Core Utilization (%),Core Power (W),Core Voltage (V)");
                    _logWriter.Flush();
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"Failed to initialize log file: {ex.Message}");
            }
        }
    }

    private void LogData(object? state)
    {
        if (_disposed || _logWriter == null)
            return;

        try
        {
            var snapshot = _dataCollectionService.GetSnapshot();
            var timestamp = snapshot.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");

            lock (_lockObject)
            {
                if (_logWriter == null)
                    return;

                // Check if we need to rotate log file (new day)
                var newLogFile = _settings.Path.Replace("{Date}", DateTime.Now.ToString("yyyyMMdd"));
                if (newLogFile != _currentLogFile)
                {
                    _logWriter?.Flush();
                    _logWriter?.Close();
                    _currentLogFile = newLogFile;
                    InitializeLogFile();
                }

                // Log package data
                var packageLine = $"{timestamp},\"{snapshot.SystemInfo.CpuId.BrandString}\"," +
                                 $"{snapshot.SystemInfo.PhysicalCores},{snapshot.SystemInfo.LogicalCores}," +
                                 $"{snapshot.Package.Temperature?.ToString("F2") ?? ""}," +
                                 $"{snapshot.Package.Power?.ToString("F2") ?? ""}," +
                                 $"{snapshot.Package.Voltage?.ToString("F3") ?? ""}," +
                                 $"{snapshot.Package.TotalUtilization.ToString("F2")}";

                // Log each core
                foreach (var core in snapshot.Package.Cores.OrderBy(c => c.CoreId))
                {
                    var coreLine = $",,,,," + // Skip package fields for core rows
                                  $"{core.CoreId}," +
                                  $"{core.CurrentFrequency?.ToString("F2") ?? ""}," +
                                  $"{core.Temperature?.ToString("F2") ?? ""}," +
                                  $"{core.Utilization?.ToString("F2") ?? ""}," +
                                  $"{core.Power?.ToString("F2") ?? ""}," +
                                  $"{core.Voltage?.ToString("F3") ?? ""}";

                    _logWriter?.WriteLine(packageLine + coreLine);
                }

                _logWriter?.Flush();
            }
        }
        catch (Exception ex)
        {
            // Log error but don't crash
            System.Diagnostics.Debug.WriteLine($"Failed to log data: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Stop();
    }
}

