---
name: machine-library-review
description: Review shipped machine library code for compatibility, deterministic language behavior, async contracts, resources, and focused tests.
applyTo: "src/SIL.Machine/**/*.cs,src/SIL.Machine.Translation.Thot/**/*.cs,src/SIL.Machine.Tokenization.SentencePiece/**/*.cs,src/SIL.Machine.Translation.TensorFlow/**/*.cs"
---

- Treat code in these projects as shipped library code. Check public/protected API
  shape, overloads, optional parameters, return types, XML documentation, target
  framework, package references, and assembly/package versioning when touched.
- The current library target is `netstandard2.0`; do not introduce a target or API that
  silently removes existing consumers. Do not demand nullable reference types for the
  whole library, but review any changed annotations or `#nullable` contract carefully
  because test projects enable nullable while shipped libraries do not.
- Existing public asynchronous APIs use `Task` and `CancellationToken`; existing
  corpus/tokenizer APIs are synchronous. Verify cancellation propagation, ordering,
  disposal, and library-context behavior in changed async code. Treat a new
  `IAsyncEnumerable` surface as an explicit compatibility decision.
- Use explicit culture/comparison semantics for token, marker, identifier, serialized,
  and protocol logic. Preserve genuinely linguistic culture-aware behavior. Add a
  culture regression test when the changed code can vary by culture.
- For file, stream, archive, process, or native-DLL changes, check bounds, paths,
  disposal, platform selection, and failure propagation.
- Add or update focused tests for changed decisions and boundary cases. Report commands
  and gaps; do not use a coverage percentage as the sole proof.
- Run `dotnet csharpier check .`, the relevant test project, and the release build as
  appropriate. Respect `.editorconfig` and CSharpier's 120-column width.
