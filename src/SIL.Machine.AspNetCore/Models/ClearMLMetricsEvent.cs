namespace SIL.Machine.AspNetCore.Models;

public record ClearMLMetricsEvent
{
    public string? Metric { get; init; }
    public string? Variant { get; init; }
    public required double Value { get; init; }
    public double? MinValue { get; init; }
    public int? MinValueIteration { get; init; }
    public double? MaxValue { get; init; }
    public int? MaxValueIteration { get; init; }
}
