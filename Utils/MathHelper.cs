namespace CoreFreqWindows.Utils;

public static class MathHelper
{
    public static double? Min(double? a, double? b)
    {
        if (!a.HasValue) return b;
        if (!b.HasValue) return a;
        return Math.Min(a.Value, b.Value);
    }

    public static double? Max(double? a, double? b)
    {
        if (!a.HasValue) return b;
        if (!b.HasValue) return a;
        return Math.Max(a.Value, b.Value);
    }

    public static double? Average(IEnumerable<double?> values)
    {
        var validValues = values.Where(v => v.HasValue).Select(v => v!.Value).ToList();
        if (validValues.Count == 0)
            return null;
        return validValues.Average();
    }

    public static double? Sum(IEnumerable<double?> values)
    {
        var validValues = values.Where(v => v.HasValue).Select(v => v!.Value).ToList();
        if (validValues.Count == 0)
            return null;
        return validValues.Sum();
    }

    public static void UpdateMinMax(ref double? min, ref double? max, double? value)
    {
        if (!value.HasValue)
            return;

        if (!min.HasValue || value.Value < min.Value)
            min = value.Value;

        if (!max.HasValue || value.Value > max.Value)
            max = value.Value;
    }
}

