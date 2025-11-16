namespace CoreFreqWindows.Utils;

public static class StringFormatter
{
    public static string FormatNumber(double? value, int decimals = 2)
    {
        if (!value.HasValue)
            return "N/A";
        return value.Value.ToString($"F{decimals}");
    }

    public static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:F2} {sizes[order]}";
    }

    public static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        if (text.Length <= maxLength)
            return text;
        return text.Substring(0, maxLength - 3) + "...";
    }

    public static string PadRight(string text, int width)
    {
        if (text.Length >= width)
            return text;
        return text + new string(' ', width - text.Length);
    }

    public static string PadLeft(string text, int width)
    {
        if (text.Length >= width)
            return text;
        return new string(' ', width - text.Length) + text;
    }
}

