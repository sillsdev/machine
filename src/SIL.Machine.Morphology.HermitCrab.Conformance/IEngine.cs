using System.Collections.Generic;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>
/// Something that can produce batch-TSV rows for a fixture: either the live C# oracle running
/// in-process (<see cref="SelfCheckEngine"/>) or an external adapter process
/// (<see cref="AdapterEngine"/>), per conformance/PROTOCOL.md.
/// </summary>
public interface IEngine
{
    /// <summary>A short, human-readable name for reporting (e.g. "self-check", "adapter").</summary>
    string Name { get; }

    /// <summary>
    /// The capability set this engine declares support for. Self-check mode always reports the
    /// full set (it IS the reference C# oracle); an adapter's set comes from the --capabilities
    /// flag.
    /// </summary>
    IReadOnlySet<string> Capabilities { get; }

    /// <summary>Runs every word in the fixture's words.txt, returning the resulting TSV rows.</summary>
    List<TsvRow> Run(MaterializedFixture fixture);
}
