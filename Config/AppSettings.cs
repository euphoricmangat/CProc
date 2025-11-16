namespace CoreFreqWindows.Config;

public class AppSettings
{
    public int UpdateInterval { get; set; } = Constants.DefaultUpdateInterval;
    public string DefaultView { get; set; } = Constants.DefaultView;
    public string TemperatureUnit { get; set; } = "C"; // C or F
    public string FrequencyUnit { get; set; } = "GHz"; // MHz or GHz
    public string ColorTheme { get; set; } = "default";
    
    public LoggingSettings Logging { get; set; } = new();
}

public class LoggingSettings
{
    public LogLevelSettings LogLevel { get; set; } = new();
    public FileLoggingSettings File { get; set; } = new();
    public DataLoggingSettings DataLogging { get; set; } = new();
}

public class LogLevelSettings
{
    public string Default { get; set; } = "Information";
    public string Microsoft { get; set; } = "Warning";
}

public class FileLoggingSettings
{
    public bool Enabled { get; set; } = false;
    public string Path { get; set; } = "logs/corefreq-{Date}.log";
}

public class DataLoggingSettings
{
    public bool Enabled { get; set; } = false;
    public string Path { get; set; } = "data/sensors-{Date}.csv";
    public int Interval { get; set; } = 1000;
}

