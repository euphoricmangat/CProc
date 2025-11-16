using CommandLine;

namespace CoreFreqWindows.Config;

public class CommandLineOptions
{
    [Option('t', "view", HelpText = "Start with specific view (dashboard, frequency, temp, voltage, power, topology, system, sensors)")]
    public string? View { get; set; }

    [Option('d', "dashboard", HelpText = "Start in dashboard mode (default)")]
    public bool Dashboard { get; set; }

    [Option('s', "system-info", HelpText = "Print system info and exit (JSON format)")]
    public bool SystemInfo { get; set; }

    [Option('i', "interval", HelpText = "Update interval in milliseconds (default: 1000)")]
    public int? Interval { get; set; }

    [Option('l', "log", HelpText = "Log data to file")]
    public string? LogFile { get; set; }

    [Option('h', "help", HelpText = "Show help")]
    public bool Help { get; set; }

    [Option('v', "version", HelpText = "Show version")]
    public bool Version { get; set; }

    [Option("debug-sensors", HelpText = "List all available sensors and exit")]
    public bool DebugSensors { get; set; }

    [Option("gui", HelpText = "Launch with GUI (default)")]
    public bool Gui { get; set; } = true;

    [Option("terminal", HelpText = "Launch with terminal UI instead of GUI")]
    public bool Terminal { get; set; }
}

