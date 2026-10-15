# Copy-on-write syntactic feature structures in engine clones

**Status:** retained (`Word.CloneForEngine`, `EnsureOwnSyntacticFeatureStruct`).
**Portable:** partly; immutable sharing is general, while the ownership plumbing is specific to this engine.

## Invariant

HermitCrab freezes a `Word` before it becomes an input to the analysis or synthesis graph. Internal clones may
therefore share that frozen word's syntactic feature structure until a rule needs to mutate it. The public
`Word.Clone()` contract remains a deep, mutable clone and never exposes copy-on-write sharing to callers.

## Implementation

Internal engine sites use `CloneForEngine()`. A per-`Morpher` setting is copied onto new words and controls
whether those clones share a frozen syntactic feature structure. The three in-place analysis mutation sites
call `EnsureOwnSyntacticFeatureStruct()` before modification; assignments of a fresh structure need no guard.
The opt-out is internal and captured per `Morpher`, preventing one parser or test from changing another
existing parser's behavior.

## Evidence

The heaviest Sena profile attributed roughly 15% of CPU to freezing and hashing a newly cloned syntactic
feature structure while constructing analysis keys. Sharing removes that repeated deep clone/freeze on the
engine path. The final five-language measurement is branch-wide rather than an isolated toggle result, so no
standalone speedup is assigned here.

## Correctness coverage and limits

`WordSyntacticFsSharingTests` covers copy-on-write, nested/shared structures, public-clone mutability, and
isolation between morphers. The broader HermitCrab suite exercises integration behavior and passed both with
sharing enabled and with `HC_SHARE_SYNTACTIC_FS=false`; the full conformance-inclusive run passed 603 tests
with one expected host skip. Sharing is intentionally unavailable for mutable source words.
