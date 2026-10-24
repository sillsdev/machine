using System;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// A mutant grammar was refused before any word was parsed: it would not serialize, or the loader or the
/// <see cref="Morpher"/> constructor threw. This, and only this, is load-failure evidence.
/// </summary>
public sealed class GrammarRejectedException(string message) : Exception(message);
