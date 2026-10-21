namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>
/// Builds the oracle <see cref="Morpher"/> for a fixture. The fixture's <c>morpher:</c> block is a
/// required argument, so no caller can run a fixture that overrides a setting at the defaults.
/// </summary>
internal static class ConformanceMorpherFactory
{
    internal static Morpher Create(Language language, MorpherSettings settings, bool useMemoization = true)
    {
        var morpher = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: useMemoization ? 1 : 0);
        settings.ApplyTo(morpher);
        return morpher;
    }

    internal static Morpher CreateTracing(Language language, MorpherSettings settings)
    {
        var morpher = new Morpher(new TraceManager { IsTracing = true }, language, maxDegreeOfParallelism: 1);
        settings.ApplyTo(morpher);
        return morpher;
    }
}
