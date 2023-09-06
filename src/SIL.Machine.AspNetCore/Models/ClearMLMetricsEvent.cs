namespace SIL.Machine.AspNetCore.Models;

public class ClearMLMetricsEvent
{
    public string Metric { get; set; } = default!;
    public string Variant { get; set; } = default!;
    public double Value { get; set; }
    public double MinValue { get; set; }
    public int MinValueIteration { get; set; }
    public double MaxValue { get; set; }
    public int MaxValueIteration { get; set; }
}
