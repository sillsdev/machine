namespace SIL.Machine.Morphology.HermitCrab.Conformance;

internal static class ConformanceMorpherFactory
{
    internal static Morpher Create(Language language) => new(new TraceManager(), language, maxDegreeOfParallelism: 1);

    internal static Morpher CreateTracing(Language language) =>
        new(new TraceManager { IsTracing = true }, language, maxDegreeOfParallelism: 1);
}
