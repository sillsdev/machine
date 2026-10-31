using System;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// A mutant child ran out of memory under the GC heap limit. It is a <see cref="TimeoutException"/> so
/// every caller that treats a timeout as a resource limit rather than a semantic delta treats this alike.
/// </summary>
public sealed class MemoryLimitExceededException(string message) : TimeoutException(message);
