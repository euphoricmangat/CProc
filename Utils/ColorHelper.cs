using Spectre.Console;

namespace CoreFreqWindows.Utils;

public static class ColorHelper
{
    public static Color GetColorForValue(double? value, double? min, double? max, bool invert = false)
    {
        if (!value.HasValue || !min.HasValue || !max.HasValue || min == max)
            return Color.Grey;

        var normalized = (value.Value - min.Value) / (max.Value - min.Value);
        if (invert)
            normalized = 1.0 - normalized;

        return normalized switch
        {
            >= 0.8 => Color.Red,
            >= 0.5 => Color.Yellow,
            >= 0.2 => Color.Green,
            _ => Color.Blue
        };
    }

    public static Color GetColorForTemperature(double? temp)
    {
        if (!temp.HasValue)
            return Color.Grey;

        return temp.Value switch
        {
            >= 80 => Color.Red,
            >= 70 => Color.Yellow,
            >= 50 => Color.Green,
            _ => Color.Blue
        };
    }

    public static Color GetColorForUtilization(double? utilization)
    {
        if (!utilization.HasValue)
            return Color.Grey;

        return utilization.Value switch
        {
            >= 80 => Color.Red,
            >= 50 => Color.Yellow,
            >= 20 => Color.Green,
            _ => Color.Blue
        };
    }

    public static Style GetStyle(string colorName)
    {
        return colorName.ToLower() switch
        {
            "cyan" or "cyan1" => new Style(Color.Cyan1),
            "blue" or "blue1" => new Style(Color.Blue1),
            "red" or "red1" => new Style(Color.Red1),
            "yellow" or "yellow1" => new Style(Color.Yellow1),
            "green" or "green1" => new Style(Color.Green1),
            "white" => new Style(Color.White),
            "grey" or "gray" => new Style(Color.Grey),
            _ => new Style(Color.White)
        };
    }
}

