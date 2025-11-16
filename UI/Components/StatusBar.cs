using Spectre.Console;
using CoreFreqWindows.UI;
using CoreFreqWindows.Config;

namespace CoreFreqWindows.UI.Components;

public class StatusBar
{
    private readonly ColorScheme _colorScheme;
    private string _currentView = "Dashboard";

    public StatusBar(ColorScheme colorScheme)
    {
        _colorScheme = colorScheme;
    }

    public void SetCurrentView(string viewName)
    {
        _currentView = viewName;
    }

    public void Render()
    {
        var statusText = "[[F1]] Help [[F2]] Freq [[F3]] Temp [[F4]] Voltage [[F5]] Power [[F6]] Topology [[F7]] System [[F8]] Sensors [[Q]] Quit";
        
        AnsiConsole.MarkupLine($"[{_colorScheme.Border}]{new string('─', 80)}[/]");
        AnsiConsole.MarkupLine($"[{_colorScheme.Normal}]{statusText}[/]");
    }
}

