using System.Runtime.CompilerServices;

// Lets the conformance harness (SIL.Machine.Morphology.HermitCrab.Conformance, assembly name
// "hc-conformance" -- see its .csproj's <AssemblyName>) call this project's internal
// Program.SplitCommandLine, so both share one command-line tokenizer instead of the harness
// maintaining a second, divergent copy.
[assembly: InternalsVisibleTo("hc-conformance")]
