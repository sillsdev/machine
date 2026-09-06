#nullable enable
using System.Diagnostics;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

internal static class ChildProcessEnvironment
{
    private static readonly string[] ProfilerVariableNames =
    {
        "CORECLR_ENABLE_PROFILING",
        "CORECLR_PROFILER",
        "CORECLR_PROFILER_PATH",
        "CORECLR_PROFILER_PATH_32",
        "CORECLR_PROFILER_PATH_64",
        "COR_ENABLE_PROFILING",
        "COR_PROFILER",
        "COR_PROFILER_PATH",
    };

    // ProcessStartInfo.EnvironmentVariables inherits the current process's environment, so a coverage
    // collector's CLR-profiler attachment would otherwise reach this child uninvited.
    internal static void StripCoverageProfiler(ProcessStartInfo startInfo)
    {
        foreach (string name in ProfilerVariableNames)
            startInfo.EnvironmentVariables.Remove(name);
    }
}
