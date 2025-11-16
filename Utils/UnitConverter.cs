namespace CoreFreqWindows.Utils;

public static class UnitConverter
{
    public static double MhzToGhz(double mhz)
    {
        return mhz / 1000.0;
    }

    public static double GhzToMhz(double ghz)
    {
        return ghz * 1000.0;
    }

    public static double CelsiusToFahrenheit(double celsius)
    {
        return (celsius * 9.0 / 5.0) + 32.0;
    }

    public static double FahrenheitToCelsius(double fahrenheit)
    {
        return (fahrenheit - 32.0) * 5.0 / 9.0;
    }

    public static string FormatFrequency(double? frequencyMhz, string unit = "GHz")
    {
        if (!frequencyMhz.HasValue)
            return "N/A";

        return unit.ToUpper() switch
        {
            "GHZ" => $"{MhzToGhz(frequencyMhz.Value):F2} GHz",
            "MHZ" => $"{frequencyMhz.Value:F0} MHz",
            _ => $"{MhzToGhz(frequencyMhz.Value):F2} GHz"
        };
    }

    public static string FormatTemperature(double? temperatureC, string unit = "C")
    {
        if (!temperatureC.HasValue)
            return "N/A";

        return unit.ToUpper() switch
        {
            "F" => $"{CelsiusToFahrenheit(temperatureC.Value):F1} °F",
            "C" => $"{temperatureC.Value:F1} °C",
            _ => $"{temperatureC.Value:F1} °C"
        };
    }

    public static string FormatVoltage(double? voltage)
    {
        if (!voltage.HasValue)
            return "N/A";
        return $"{voltage.Value:F3} V";
    }

    public static string FormatPower(double? power)
    {
        if (!power.HasValue)
            return "N/A";
        return $"{power.Value:F2} W";
    }

    public static string FormatPercentage(double? percentage)
    {
        if (!percentage.HasValue)
            return "N/A";
        return $"{percentage.Value:F1}%";
    }
}

