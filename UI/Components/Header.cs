using Spectre.Console;
using CoreFreqWindows.Config;
using CoreFreqWindows.Models;

namespace CoreFreqWindows.UI.Components;

public class Header
{
    private readonly ColorScheme _colorScheme;

    public Header(ColorScheme colorScheme)
    {
        _colorScheme = colorScheme;
    }

    public void Render(SystemInfo systemInfo)
    {
        var cpuName = systemInfo.CpuId.BrandString;
        if (string.IsNullOrEmpty(cpuName))
            cpuName = "Unknown CPU";

        var headerText = $"{Constants.ApplicationName} v{Constants.ApplicationVersion} | CPU: {cpuName}";
        
        AnsiConsole.MarkupLine($"[{_colorScheme.Header}]{headerText}[/]");
        AnsiConsole.WriteLine();
    }
}

