namespace SIL.Machine.Morphology.HermitCrab.Conformance;

internal static class ConformanceMorpherFactory
{
    internal static Morpher Create(Language language, bool useMemoization = true) =>
        new(new TraceManager(), language, maxDegreeOfParallelism: useMemoization ? 1 : 0);

    internal static Morpher CreateTracing(Language language) =>
        new(new TraceManager { IsTracing = true }, language, maxDegreeOfParallelism: 1);
}
