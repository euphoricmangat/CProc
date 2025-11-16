using Spectre.Console;

namespace CoreFreqWindows.UI;

public class ColorScheme
{
    public Color Header { get; set; } = Color.Cyan1;
    public Color Normal { get; set; } = Color.White;
    public Color High { get; set; } = Color.Red1;
    public Color Medium { get; set; } = Color.Yellow1;
    public Color Low { get; set; } = Color.Green1;
    public Color Inactive { get; set; } = Color.Grey;
    public Color Border { get; set; } = Color.Grey;

    public static ColorScheme Default => new();
}

