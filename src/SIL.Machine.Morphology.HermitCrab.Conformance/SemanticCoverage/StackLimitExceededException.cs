using System;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// A mutant child overflowed its stack. Like <see cref="MemoryLimitExceededException"/>, it is a resource
/// limit rather than a semantic delta, so it is a <see cref="TimeoutException"/>.
/// </summary>
public sealed class StackLimitExceededException(string message) : TimeoutException(message);
