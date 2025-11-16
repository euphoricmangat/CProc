using Spectre.Console;
using CoreFreqWindows.Models;
using CoreFreqWindows.UI;
using CoreFreqWindows.Utils;

namespace CoreFreqWindows.UI.Views;

public class DashboardView : BaseView
{
    private readonly ColorScheme _colorScheme;

    public DashboardView(ColorScheme colorScheme)
    {
        _colorScheme = colorScheme;
    }

    public override string Name => "Dashboard";

    public override void Render(MonitoringSnapshot snapshot)
    {
        // System Info Section
        RenderSystemInfo(snapshot);

        AnsiConsole.WriteLine();

        // CPU Cores Table
        RenderCoresTable(snapshot);

        AnsiConsole.WriteLine();

        // Package Info
        RenderPackageInfo(snapshot);
    }

    private void RenderSystemInfo(MonitoringSnapshot snapshot)
    {
        var table = new Table();
        table.Border = TableBorder.Rounded;
        table.Title = new TableTitle("[cyan1]System Information[/]");

        table.AddColumn("[white]Property[/]");
        table.AddColumn("[white]Value[/]");

        table.AddRow("CPU", snapshot.SystemInfo.CpuId.BrandString);
        table.AddRow("Physical Cores", snapshot.SystemInfo.PhysicalCores.ToString());
        table.AddRow("Logical Cores", snapshot.SystemInfo.LogicalCores.ToString());
        table.AddRow("Architecture", snapshot.SystemInfo.Architecture);

        AnsiConsole.Write(table);
    }

    private void RenderCoresTable(MonitoringSnapshot snapshot)
    {
        var table = new Table();
        table.Border = TableBorder.Rounded;
        table.Title = new TableTitle("[cyan1]CPU Cores[/]");

        table.AddColumn("[white]Core[/]");
        table.AddColumn("[white]Frequency[/]");
        table.AddColumn("[white]Temperature[/]");
        table.AddColumn("[white]Utilization[/]");

        foreach (var core in snapshot.Package.Cores.OrderBy(c => c.CoreId))
        {
            var freqColor = GetFrequencyColor(core.CurrentFrequency);
            var tempColor = GetTemperatureColor(core.Temperature);
            var utilColor = GetUtilizationColor(core.Utilization);

            var freqText = UnitConverter.FormatFrequency(core.CurrentFrequency);
            var tempText = UnitConverter.FormatTemperature(core.Temperature);
            var utilText = UnitConverter.FormatPercentage(core.Utilization);

            table.AddRow(
                $"[white]{core.CoreId}[/]",
                $"[{freqColor}]{freqText}[/]",
                $"[{tempColor}]{tempText}[/]",
                $"[{utilColor}]{utilText}[/]"
            );
        }

        AnsiConsole.Write(table);
    }

    private void RenderPackageInfo(MonitoringSnapshot snapshot)
    {
        var table = new Table();
        table.Border = TableBorder.Rounded;
        table.Title = new TableTitle("[cyan1]Package Information[/]");

        table.AddColumn("[white]Property[/]");
        table.AddColumn("[white]Value[/]");

        var packageTemp = UnitConverter.FormatTemperature(snapshot.Package.Temperature);
        var packagePower = UnitConverter.FormatPower(snapshot.Package.Power);
        var totalUtil = UnitConverter.FormatPercentage(snapshot.Package.TotalUtilization);

        table.AddRow("Package Temperature", $"[{GetTemperatureColor(snapshot.Package.Temperature)}]{packageTemp}[/]");
        table.AddRow("Package Power", $"[white]{packagePower}[/]");
        table.AddRow("Total CPU Load", $"[{GetUtilizationColor(snapshot.Package.TotalUtilization)}]{totalUtil}[/]");

        AnsiConsole.Write(table);
    }

    private string GetFrequencyColor(double? frequency)
    {
        if (!frequency.HasValue)
            return _colorScheme.Inactive.ToString();

        // High frequency = good (green), low = yellow/red
        return frequency.Value > 3000 ? _colorScheme.Low.ToString() :
               frequency.Value > 2000 ? _colorScheme.Medium.ToString() :
               _colorScheme.High.ToString();
    }

    private string GetTemperatureColor(double? temperature)
    {
        if (!temperature.HasValue)
            return _colorScheme.Inactive.ToString();

        return temperature.Value switch
        {
            >= 80 => _colorScheme.High.ToString(),
            >= 70 => _colorScheme.Medium.ToString(),
            _ => _colorScheme.Low.ToString()
        };
    }

    private string GetUtilizationColor(double? utilization)
    {
        if (!utilization.HasValue)
            return _colorScheme.Inactive.ToString();

        return utilization.Value switch
        {
            >= 80 => _colorScheme.High.ToString(),
            >= 50 => _colorScheme.Medium.ToString(),
            _ => _colorScheme.Low.ToString()
        };
    }
}

