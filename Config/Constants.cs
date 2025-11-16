namespace CoreFreqWindows.Config;

public static class Constants
{
    public const string ApplicationName = "CoreFreq for Windows";
    public const string ApplicationVersion = "1.0.0";
    public const int MinimumTerminalWidth = 80;
    public const int MinimumTerminalHeight = 24;
    public const int RecommendedTerminalWidth = 120;
    public const int RecommendedTerminalHeight = 40;
    public const int DefaultUpdateInterval = 1000;
    public const string DefaultView = "dashboard";
    
    // Update interval limits
    public const int MinUpdateInterval = 100;
    public const int MaxUpdateInterval = 10000;
    
    // Color codes
    public const string ColorHeader = "cyan1";
    public const string ColorNormal = "white";
    public const string ColorHigh = "red1";
    public const string ColorMedium = "yellow1";
    public const string ColorLow = "green1";
    public const string ColorInactive = "grey";
    public const string ColorBorder = "grey";
}

