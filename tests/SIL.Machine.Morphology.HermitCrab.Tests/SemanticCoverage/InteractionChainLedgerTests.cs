using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class InteractionChainLedgerTests
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

    // Pins the four headline counts for the chain layer, derived from SemanticInterfaceDirection's
    // engine-checked judgment rather than InterfaceInventoryLedger's name-prefix heuristic. That
    // heuristic classifies LexicalEntry.ruleFeatures, LexicalEntry.partOfSpeech, and Allomorph.stemName
    // as "ref" (none starts with output/assigned/required/excluded/obligatory/head/nonHead), but each
    // one seeds a derivation payload another declared attribute later gates on -- ruleFeatures and
    // partOfSpeech into MorphologicalPhonologicalRuleFeature and PartOfSpeech respectively (both already-
    // known junctions, giving each a THIRD writer), and Allomorph.stemName into a StemName junction the
    // prefix heuristic cannot see at all, since it never asks the writer/reader question of a payload
    // type it has not already paired a writer and reader for by name. 3 writers x 8 readers (MPR) + 3 x 5
    // (PartOfSpeech) + 1 x 1 (StemName) = 40. Two chains are both exercised and hazardous:
    // MorphologicalOutput.MPRFeatures -> MorphologicalInput.requiredMPRFeatures (the original canonical
    // example, via edge-cases/mpr-overwrite-order-dependence among others) and the newly-visible
    // LexicalEntry.ruleFeatures -> MorphologicalInput.requiredMPRFeatures (via
    // languages/fusional-realizational-morphology among others) -- the lexicon-sourced hazard a real
    // field grammar actually exercises.
    [Test]
    public void RealCorpusProducesTheDeclaredJunctionAndChainCounts()
    {
        string root = RepositoryRoot();
        IReadOnlyList<ChainJunction> junctions = InteractionChainLedger.ComputeJunctions(root);
        IReadOnlyList<InteractionChainLedger.Row> rows = InteractionChainLedger.Compute(root);

        int exercised = rows.Count(r => r.Exercised);
        int hazardous = rows.Count(r => r.Hazardous);

        TestContext.Out.WriteLine(
            $"junctions={junctions.Count} chains={rows.Count} exercised={exercised} hazardous={hazardous}"
        );
        foreach (ChainJunction junction in junctions)
        {
            TestContext.Out.WriteLine(
                $"  {junction.PayloadType}: {junction.Writers.Count} writer(s), {junction.Readers.Count} reader(s)"
            );
        }
        foreach (InteractionChainLedger.Row row in rows.Where(r => r.Hazardous))
        {
            TestContext.Out.WriteLine(
                $"hazardous: {row.WriterElement}.{row.WriterAttribute} -> {row.PayloadType} -> "
                    + $"{row.ReaderElement}.{row.ReaderAttribute} ({string.Join(",", row.ExercisingFixtures)})"
            );
        }

        Assert.That(junctions, Has.Count.EqualTo(3));
        Assert.That(
            junctions.Select(j => j.PayloadType),
            Is.EquivalentTo(new[] { "MorphologicalPhonologicalRuleFeature", "PartOfSpeech", "StemName" })
        );

        ChainJunction mprFeature = junctions.Single(j => j.PayloadType == "MorphologicalPhonologicalRuleFeature");
        Assert.That(mprFeature.Writers, Has.Count.EqualTo(3));
        Assert.That(mprFeature.Readers, Has.Count.EqualTo(8));

        ChainJunction partOfSpeech = junctions.Single(j => j.PayloadType == "PartOfSpeech");
        Assert.That(partOfSpeech.Writers, Has.Count.EqualTo(3));
        Assert.That(partOfSpeech.Readers, Has.Count.EqualTo(5));

        ChainJunction stemName = junctions.Single(j => j.PayloadType == "StemName");
        Assert.That(stemName.Writers, Has.Count.EqualTo(1));
        Assert.That(stemName.Readers, Has.Count.EqualTo(1));

        Assert.That(rows, Has.Count.EqualTo(40));
        Assert.That(exercised, Is.EqualTo(26));
        Assert.That(rows.Count - exercised, Is.EqualTo(14));
        Assert.That(hazardous, Is.EqualTo(4));

        InteractionChainLedger.Row[] hazards = rows.Where(r => r.Hazardous).ToArray();
        // Two MPR writers x two readers. The PhonologicalSubrule pair joined when the exception-feature
        // obligation cells were covered: an MPR gate on a sound rule is order-hazardous for the same
        // reason the MorphologicalInput one is -- an Overwrite group can destroy the payload before the
        // gate reads it, so the outcome depends on rule order rather than on the payload alone.
        Assert.That(
            hazards.Select(h => (h.WriterElement, h.WriterAttribute, h.ReaderElement)),
            Is.EquivalentTo(
                new[]
                {
                    ("LexicalEntry", "ruleFeatures", "MorphologicalInput"),
                    ("LexicalEntry", "ruleFeatures", "PhonologicalSubrule"),
                    ("MorphologicalOutput", "MPRFeatures", "MorphologicalInput"),
                    ("MorphologicalOutput", "MPRFeatures", "PhonologicalSubrule"),
                }
            )
        );
        Assert.That(hazards.All(h => h.PayloadType == "MorphologicalPhonologicalRuleFeature"), Is.True);
        Assert.That(hazards.All(h => h.ReaderAttribute == "requiredMPRFeatures"), Is.True);
        Assert.That(
            hazards
                .Single(h => h.WriterElement == "MorphologicalOutput" && h.ReaderElement == "MorphologicalInput")
                .ExercisingFixtures,
            Contains.Item("edge-cases/mpr-overwrite-order-dependence")
        );
    }

    // Every (element, attribute) InterfaceInventoryLedger declares from the DTD must have an explicit
    // entry in SemanticInterfaceDirection -- Classify throws rather than silently defaulting to Ref for
    // an unrecognized pair, exactly so a DTD change that adds a new IDREF/IDREFS attribute forces someone
    // to make the write/read/ref call rather than have it fall out of a naming accident.
    [Test]
    public void EveryDeclaredInterfaceHasAnExplicitSemanticDirection()
    {
        string root = RepositoryRoot();
        IReadOnlyList<InterfaceInventoryLedger.Row> edgeRows = InterfaceInventoryLedger.Compute(root);

        Assert.That(edgeRows, Has.Count.EqualTo(60));
        Assert.DoesNotThrow(() =>
        {
            foreach (InterfaceInventoryLedger.Row row in edgeRows)
                SemanticInterfaceDirection.Classify(row.Element, row.Attribute);
        });
    }

    // The three reclassifications a real field grammar's evidence forced: each was "ref" under
    // InterfaceInventoryLedger's own name-prefix heuristic (none starts with output/assigned) but is a
    // genuine write once checked against the engine.
    [Test]
    public void LexiconSeedingAttributesAreClassifiedAsWritesNotRefs()
    {
        Assert.That(
            SemanticInterfaceDirection.Classify("LexicalEntry", "ruleFeatures"),
            Is.EqualTo(InterfaceDirection.Write)
        );
        Assert.That(
            SemanticInterfaceDirection.Classify("LexicalEntry", "partOfSpeech"),
            Is.EqualTo(InterfaceDirection.Write)
        );
        Assert.That(SemanticInterfaceDirection.Classify("Allomorph", "stemName"), Is.EqualTo(InterfaceDirection.Write));
    }

    // A declared interface appears in its own denominator whether or not any fixture exercises it -- that
    // is what stops an unexercised construct from being silently absent rather than visibly uncovered.
    // The two PhonologicalSubrule MPR readers were the original examples and are now EXERCISED, by the
    // words that closed the exception-feature obligation cells, so they can only carry the first half of
    // the claim. CompoundingRule.nonHeadProdRestrictionsMprFeatures still carries both: no grammar
    // declares it, and it must still appear as a reader.
    [Test]
    public void DeclaredInterfacesAppearInTheirOwnChainsWhetherExercisedOrNot()
    {
        string root = RepositoryRoot();
        IReadOnlyList<InteractionChainLedger.Row> rows = InteractionChainLedger.Compute(root);

        (string Element, string Attribute)[] mustAppearAsReaders =
        {
            ("PhonologicalSubrule", "requiredMPRFeatures"),
            ("PhonologicalSubrule", "excludedMPRFeatures"),
            ("CompoundingRule", "nonHeadProdRestrictionsMprFeatures"),
        };
        foreach ((string element, string attribute) in mustAppearAsReaders)
        {
            InteractionChainLedger.Row[] matches = rows.Where(r =>
                    r.ReaderElement == element && r.ReaderAttribute == attribute
                )
                .ToArray();
            Assert.That(matches, Is.Not.Empty, $"{element}.{attribute} should appear as a reader");
        }

        InteractionChainLedger.Row[] nonHead = rows.Where(r =>
                r.ReaderElement == "CompoundingRule" && r.ReaderAttribute == "nonHeadProdRestrictionsMprFeatures"
            )
            .ToArray();
        Assert.That(
            nonHead.All(r => !r.Exercised),
            Is.True,
            "no grammar declares nonHeadProdRestrictionsMprFeatures, so it must still be unexercised"
        );

        InteractionChainLedger.Row[] writerMatches = rows.Where(r =>
                r.WriterElement == "CompoundingRule" && r.WriterAttribute == "outputProdRestrictionsMprFeatures"
            )
            .ToArray();
        Assert.That(writerMatches, Is.Not.Empty);
        Assert.That(writerMatches.All(r => !r.Exercised), Is.True);
    }

    // Mirrors InterfaceInventoryLedgerTests.CheckedInInterfaceInventoryLedgerIsUpToDate: regenerate the
    // ledger from the DTD plus the real corpus plus the engine-verified supplement, and require the
    // checked-in file to match byte for byte.
    [Test]
    public void CheckedInInteractionChainLedgerIsUpToDate()
    {
        string root = RepositoryRoot();
        IReadOnlyList<InteractionChainLedger.Row> rows = InteractionChainLedger.Compute(root);

        string fresh = InteractionChainLedger.ToText(rows);
        string checkedIn = File.ReadAllText(
            Path.Combine(root, InteractionChainLedger.RelativePath.Replace('/', Path.DirectorySeparatorChar))
        );

        Assert.That(
            fresh.ReplaceLineEndings("\n"),
            Is.EqualTo(checkedIn.ReplaceLineEndings("\n")),
            "regenerate with: hc-conformance --write-interaction-chains --repository-root ."
        );
    }
}
