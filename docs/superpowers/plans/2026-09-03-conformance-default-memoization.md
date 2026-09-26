# Conformance Default Memoization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make memoized HermitCrab analysis the authoritative default for every conformance result evaluation while retaining a separate tracing pass for rule evidence.

**Architecture:** A small conformance-owned factory creates the canonical non-tracing, single-threaded `Morpher`. Result-producing paths use that factory. `Runner` performs a second non-authoritative traced parse only after the memoized signature passes.

**Tech Stack:** C# 14, .NET 10, NUnit 4

---

### Task 1: Define and prove the conformance default

**Files:**
- Create: `src/SIL.Machine.Morphology.HermitCrab.Conformance/ConformanceMorpherFactory.cs`
- Create: `tests/SIL.Machine.Morphology.HermitCrab.Tests/ConformanceMorpherFactoryTests.cs`

- [ ] **Step 1: Write a failing discovery test**

Add this NUnit test. It compiles against current production code and must fail because the factory does not exist.

```csharp
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class ConformanceMorpherFactoryTests
{
    [Test]
    public void DefaultMorpherIsMemoizationEligible()
    {
        Type? factoryType = typeof(Runner).Assembly.GetType(
            "SIL.Machine.Morphology.HermitCrab.Conformance.ConformanceMorpherFactory"
        );

        Assert.That(factoryType, Is.Not.Null);
    }
}
```

- [ ] **Step 2: Run the discovery test and verify red**

Run:

```powershell
dotnet test tests\SIL.Machine.Morphology.HermitCrab.Tests\SIL.Machine.Morphology.HermitCrab.Tests.csproj --filter FullyQualifiedName~ConformanceMorpherFactoryTests --no-restore
```

Expected: one failed assertion reporting a null factory type.

- [ ] **Step 3: Add the minimal factory**

Create this factory:

```csharp
namespace SIL.Machine.Morphology.HermitCrab.Conformance;

internal static class ConformanceMorpherFactory
{
    internal static Morpher Create(Language language, bool useMemoization = true) =>
        new(new TraceManager(), language, maxDegreeOfParallelism: useMemoization ? 1 : 0);

    internal static Morpher CreateTracing(Language language) =>
        new(new TraceManager { IsTracing = true }, language, maxDegreeOfParallelism: 1);
}
```

- [ ] **Step 4: Replace discovery test with direct configuration assertions**

Replace the discovery test with direct configuration assertions:

```csharp
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class ConformanceMorpherFactoryTests
{
    private static string RepositoryRoot()
    {
        string? directory = TestContext.CurrentContext.TestDirectory;
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory, "conformance", "constructs.txt")))
                return directory;
            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate the repository root.");
        return string.Empty;
    }

    [Test]
    public void DefaultMorpherIsMemoizationEligible()
    {
        Fixture fixture = Fixture
            .DiscoverAll(Path.Combine(RepositoryRoot(), "conformance"))
            .First();
        Language language = XmlLanguageLoader.Load(fixture.GrammarPath);

        Morpher morpher = ConformanceMorpherFactory.Create(language);

        Assert.Multiple(() =>
        {
            Assert.That(morpher.MaxDegreeOfParallelism, Is.EqualTo(1));
            Assert.That(morpher.TraceManager.IsTracing, Is.False);
        });
    }

    [Test]
    public void DiagnosticMorpherDisablesMemoization()
    {
        Fixture fixture = Fixture.DiscoverAll(Path.Combine(RepositoryRoot(), "conformance")).First();
        Language language = XmlLanguageLoader.Load(fixture.GrammarPath);

        Morpher morpher = ConformanceMorpherFactory.Create(language, useMemoization: false);

        Assert.Multiple(() =>
        {
            Assert.That(morpher.MaxDegreeOfParallelism, Is.EqualTo(0));
            Assert.That(morpher.TraceManager.IsTracing, Is.False);
        });
    }
}
```

- [ ] **Step 5: Run the focused test and verify green**

Run the Step 2 command. Expected: one passing test.

### Task 2: Route authoritative conformance execution through the factory

**Files:**
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/Runner.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SelfCheckEngine.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/Program.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/CounterfactualGate.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/EngineGateWitnessSweep.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab.Conformance/SemanticCoverage/GateObligationLedger.cs`
- Test: `tests/SIL.Machine.Morphology.HermitCrab.Tests/ConformanceCommandLineTests.cs`
- Test: `tests/SIL.Machine.Morphology.HermitCrab.Tests/ConformanceMorpherFactoryTests.cs`
- Test: `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/ConformanceFixtureGateTests.cs`

- [ ] **Step 1: Make result-producing paths use the default factory**

Replace each non-tracing default `new Morpher(new TraceManager(), language)` in `SelfCheckEngine` and `CounterfactualGate` with:

```csharp
ConformanceMorpherFactory.Create(language)
```

In `Runner.RunAllWords`, create both morphers:

```csharp
Morpher resultMorpher = ConformanceMorpherFactory.Create(language, useMemoization);
Morpher tracingMorpher = ConformanceMorpherFactory.CreateTracing(language);
```

Parse authoritative results with `resultMorpher`. After signature, skip, and expected-failure checks pass, call the sequential `CreateTracing` morpher's `ParseWord(word.Word, out object trace, guessRoot).ToList()` only to populate trace evidence. Keep `actualBySignature` based on authoritative `results`. Route tracing-only semantic coverage through `CreateTracing` because `TraceManager` mutates child collections and cannot safely collect a parallel trace.

Add a `useMemoization` parameter, defaulting to `true`, to `SelfCheckEngine` and `Runner.RunSelfCheck`; pass it through private runner methods to the factory. Add a self-check-only `--no-memoization` CLI option that passes `false`, document it in help, and reject its combination with `--adapter`.

- [ ] **Step 2: Run the conformance fixture gate**

Run:

```powershell
dotnet test tests\SIL.Machine.Morphology.HermitCrab.Tests\SIL.Machine.Morphology.HermitCrab.Tests.csproj --filter FullyQualifiedName~ConformanceFixtureGateTests --no-restore
```

Expected: 5 passing tests.

- [ ] **Step 3: Run memoization regression tests**

Run:

```powershell
dotnet test tests\SIL.Machine.Morphology.HermitCrab.Tests\SIL.Machine.Morphology.HermitCrab.Tests.csproj --filter "FullyQualifiedName~MemoizedCombinationRuleCascadeTests|FullyQualifiedName~AnalysisStateKeyTests|FullyQualifiedName~AnalysisStratumRuleTests" --no-restore
```

Expected: all selected tests pass, including `Apply_PositiveReplayMatchesUnmemoizedResultSet_IncludingTrailOrder`.

- [ ] **Step 4: Format and verify the full test project**

Run:

```powershell
dotnet csharpier format src\SIL.Machine.Morphology.HermitCrab.Conformance tests\SIL.Machine.Morphology.HermitCrab.Tests\ConformanceMorpherFactoryTests.cs
dotnet test tests\SIL.Machine.Morphology.HermitCrab.Tests\SIL.Machine.Morphology.HermitCrab.Tests.csproj --no-restore
```

Expected: formatting succeeds and full test project passes.

- [ ] **Step 5: Commit implementation**

Stage only factory, runner, self-check, counterfactual, and test files. Commit with:

```powershell
git commit -m "test: make memoization conformance default"
```
