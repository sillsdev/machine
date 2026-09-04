# Partial Morpheme Health Check Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an actionable `GrammarHealthChecker` warning for every distinct HermitCrab morpheme whose published `IsPartial` flag is true.

**Architecture:** Extend the existing diagnostic-only checker with one private enumeration pass over lexical entries, ordinary morphemic rules, and template-slot rules. Deduplicate the model objects by reference, then emit one stable-coded warning whose subject is the original morpheme; do not infer partiality from POS, slots, or feature structures.

**Tech Stack:** C#, .NET 10 test project, netstandard2.0 library, NUnit 4, CSharpier

---

### Task 1: Specify partial-morpheme findings with failing tests

**Files:**
- Modify: `tests/SIL.Machine.Morphology.HermitCrab.Tests/GrammarHealthCheckerTests.cs`

- [ ] **Step 1: Add the morphological-rules import**

```csharp
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
```

- [ ] **Step 2: Add a failing partial lexical-entry test**

```csharp
[Test]
public void Check_PartialLexicalEntry_ReportsActionableWarning()
{
    var table = new CharacterDefinitionTable { Name = "table1" };
    var stratum = new Stratum(table) { Name = "Surface" };
    var entry = new LexEntry { Id = "entry1", IsPartial = true };
    stratum.Entries.Add(entry);
    var language = new Language();
    language.Strata.Add(stratum);

    GrammarHealthFinding finding = GrammarHealthChecker.Check(language).Single();

    Assert.That(finding.Code, Is.EqualTo(GrammarHealthCodes.PartialMorpheme));
    Assert.That(finding.Severity, Is.EqualTo(GrammarHealthSeverity.Warning));
    Assert.That(finding.Message, Does.Contain("entry1"));
    Assert.That(finding.Message, Does.Contain("partially analyzed"));
    Assert.That(finding.Message, Does.Contain("final-template pruning"));
    Assert.That(finding.Subjects, Is.EqualTo(new object[] { entry }));
}
```

- [ ] **Step 3: Add a failing ordinary-rule test**

```csharp
[Test]
public void Check_PartialOrdinaryRule_ReportsRule()
{
    var table = new CharacterDefinitionTable { Name = "table1" };
    var stratum = new Stratum(table) { Name = "Surface" };
    var rule = new AffixProcessRule { Name = "plural", IsPartial = true };
    stratum.MorphologicalRules.Add(rule);
    var language = new Language();
    language.Strata.Add(stratum);

    GrammarHealthFinding finding = GrammarHealthChecker.Check(language).Single();

    Assert.That(finding.Code, Is.EqualTo(GrammarHealthCodes.PartialMorpheme));
    Assert.That(finding.Message, Does.Contain("plural"));
    Assert.That(finding.Subjects, Is.EqualTo(new object[] { rule }));
}
```

- [ ] **Step 4: Add a failing template-rule deduplication test**

```csharp
[Test]
public void Check_PartialTemplateRuleReferencedTwice_ReportsOnce()
{
    var table = new CharacterDefinitionTable { Name = "table1" };
    var stratum = new Stratum(table) { Name = "Surface" };
    var rule = new AffixProcessRule { Name = "subject", IsPartial = true };
    var template = new AffixTemplate { Name = "verb" };
    template.Slots.Add(new AffixTemplateSlot(rule));
    template.Slots.Add(new AffixTemplateSlot(rule));
    stratum.AffixTemplates.Add(template);
    var language = new Language();
    language.Strata.Add(stratum);

    IList<GrammarHealthFinding> findings = GrammarHealthChecker.Check(language);

    Assert.That(findings, Has.Count.EqualTo(1));
    Assert.That(findings[0].Code, Is.EqualTo(GrammarHealthCodes.PartialMorpheme));
    Assert.That(findings[0].Subjects, Is.EqualTo(new object[] { rule }));
}
```

- [ ] **Step 5: Run the focused tests and verify RED**

Run:

```powershell
dotnet test tests\SIL.Machine.Morphology.HermitCrab.Tests\SIL.Machine.Morphology.HermitCrab.Tests.csproj --configuration Release --filter "FullyQualifiedName~GrammarHealthCheckerTests"
```

Expected: compilation fails because `GrammarHealthCodes.PartialMorpheme` does not exist. This is the intended red result.

- [ ] **Step 6: Commit the failing specification**

```powershell
git add tests/SIL.Machine.Morphology.HermitCrab.Tests/GrammarHealthCheckerTests.cs
git commit -m "test: specify partial morpheme health findings"
```

### Task 2: Implement the checker from the published model fact

**Files:**
- Modify: `src/SIL.Machine.Morphology.HermitCrab/GrammarHealthChecker.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab/GrammarHealthFinding.cs`

- [ ] **Step 1: Add the stable finding code**

Add to `GrammarHealthCodes`:

```csharp
public const string PartialMorpheme = "hc-partial-morpheme";
```

- [ ] **Step 2: Invoke the new diagnostic pass**

Add after the existing checks in `GrammarHealthChecker.Check`:

```csharp
CheckPartialMorphemes(language, findings);
```

- [ ] **Step 3: Enumerate every morpheme-bearing location and deduplicate references**

Add a private method that creates:

```csharp
var seen = new HashSet<Morpheme>(new ReferenceEqualityComparer<Morpheme>());
```

For every stratum, visit `stratum.Entries`, `stratum.MorphologicalRules.OfType<Morpheme>()`, and every rule in every `AffixTemplateSlot`. Pass each object to a helper that returns immediately when `!morpheme.IsPartial || !seen.Add(morpheme)`.

- [ ] **Step 4: Emit one warning with the original object as subject**

For a `LexEntry`, prefer its non-empty `Id`; for a `MorphemicMorphologicalRule`, prefer its non-empty `Name`; fall back to `Id`, `Gloss`, and finally `"unnamed"`. Emit:

```csharp
new GrammarHealthFinding(
    GrammarHealthSeverity.Warning,
    GrammarHealthCodes.PartialMorpheme,
    string.Format(
        "{0} '{1}' is partially analyzed. Supply its missing category or template/slot analysis; "
            + "leaving it partial can broaden analysis and disable safe final-template pruning.",
        kind,
        name
    ),
    new object[] { morpheme }
)
```

- [ ] **Step 5: Run the focused tests and verify GREEN**

Run the Task 1 test command.

Expected: all `GrammarHealthCheckerTests` pass.

- [ ] **Step 6: Commit the implementation**

```powershell
git add src/SIL.Machine.Morphology.HermitCrab/GrammarHealthChecker.cs src/SIL.Machine.Morphology.HermitCrab/GrammarHealthFinding.cs
git commit -m "feat: report partially analyzed morphemes"
```

### Task 3: Verify, document the PR, and publish the branch

**Files:**
- Modify if formatting requires it: the two production files and one test file above
- External: GitHub pull request #475 body

- [ ] **Step 1: Run CSharpier**

```powershell
dotnet tool restore
dotnet csharpier format src/SIL.Machine.Morphology.HermitCrab/GrammarHealthChecker.cs src/SIL.Machine.Morphology.HermitCrab/GrammarHealthFinding.cs tests/SIL.Machine.Morphology.HermitCrab.Tests/GrammarHealthCheckerTests.cs
```

Expected: exit code 0. Commit any formatting-only changes.

- [ ] **Step 2: Run the full targeted suite**

```powershell
dotnet test tests\SIL.Machine.Morphology.HermitCrab.Tests\SIL.Machine.Morphology.HermitCrab.Tests.csproj --configuration Release
```

Expected: all tests pass with zero warnings.

- [ ] **Step 3: Check the complete branch diff**

```powershell
git diff --check origin/master...HEAD
git status --short --branch
```

Expected: no whitespace errors and a clean worktree.

- [ ] **Step 4: Push the approved PR head**

```powershell
git push origin HEAD:feature/grammar-health-checker
```

Expected: GitHub updates pull request #475.

- [ ] **Step 5: Update the pull-request description**

Add the third checker contract and its new tests to the existing PR description. Preserve the diagnostic-only and netstandard2.0 design statements. Explicitly state that the partial finding advises authors to complete missing category or slot information while #491 retains conservative final-template behavior.
