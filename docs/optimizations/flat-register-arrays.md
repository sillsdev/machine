# Flat register arrays in the FST traversal

**Status:** retained (`SIL.Machine.FiniteState`: `Register<TOffset>[]` indexed by `RegisterArray.Idx(r, k)`
replaces `Register<TOffset>[,]` internally; the public `FstResult.Registers` stays two-dimensional and is
materialized once per accepting state). **Portable:** no; a .NET-specific allocation path.

## The observation

11.5% of the heaviest Sena word's CPU profile was inside `System.Array.CreateInstanceMDArray`, the slow
generic allocator for multi-dimensional arrays, called from `Matcher.AllMatches` (8.6%) and traversal
setup (2.3%): one `new Register<TOffset>[n, 2]` per match call and per traversal instance.

## The change

Mechanical: flat arrays of length `registerCount * 2`, a two-constant index helper, `Array.Copy` on the
flat struct array, and `RegistersEqualityComparer` comparing the flat layout. No behaviour change.

## Measured

`CreateInstanceMDArray` 11.5% → 0.2% of the profile. Counts unchanged by construction. Together with the
other allocation changes it contributed to the heaviest word's wall time; not measured in isolation.

`RegistersEqualityComparerTests` covers equality and hashing of the flattened layout, while the existing FSA
and FST suites cover accepting results and register materialization. The complete conformance-inclusive run
passed 603 tests with one expected host-privilege skip.
