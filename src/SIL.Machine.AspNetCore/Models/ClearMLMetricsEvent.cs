namespace SIL.Machine.AspNetCore.Models;

public record ClearMLMetricsEvent
{
    public required string Metric { get; init; }
    public required string Variant { get; init; }
    public required double Value { get; init; }
    public required double MinValue { get; init; }
    public required int MinValueIteration { get; init; }
    public required double MaxValue { get; init; }
    public required int MaxValueIteration { get; init; }
}
