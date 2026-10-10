#nullable enable
using System.Diagnostics;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// The only place this assembly (and the test assembly it grants internals to) may construct a child
/// <see cref="ProcessStartInfo"/>; <c>ProcessStartInfoConstructionGateTests</c> fails naming any other
/// file that does. Xplat Code Coverage attaches a CLR profiler to the current process via
/// CORECLR_*/COR_* environment variables, which ProcessStartInfo inherits into a child by default, and
/// a profiled child that then crashes or is killed has been observed to destabilize the coverage
/// channel the parent test host shares -- reproduced twice as a trace-less test host crash under
/// `--collect:"Xplat Code Coverage"` before this factory existed.
/// </summary>
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

    internal static ProcessStartInfo CreateStartInfo(string fileName)
    {
        var startInfo = new ProcessStartInfo { FileName = fileName, UseShellExecute = false };
        foreach (string name in ProfilerVariableNames)
            startInfo.EnvironmentVariables.Remove(name);
        return startInfo;
    }
}
