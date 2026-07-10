using System;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>
/// Thrown by an <see cref="IEngine"/> when the ENGINE UNDER TEST itself failed while loading a
/// grammar or parsing a word -- a genuine engine crash, as opposed to a harness-level failure
/// (the adapter process couldn't be started, timed out, or exited 0 without producing an output
/// file; see <see cref="AdapterEngine"/>). <see cref="Runner"/> treats this exception specially: a
/// fixture whose manifest declares <c>expectCrash: true</c> (see <see cref="FixtureManifest"/>)
/// PASSes when this is thrown instead of the normal signature diff; any other fixture FAILs,
/// showing the carried diagnostics.
/// </summary>
public class EngineCrashException : Exception
{
    public EngineCrashException(string message)
        : base(message) { }

    public EngineCrashException(string message, Exception innerException)
        : base(message, innerException) { }
}
