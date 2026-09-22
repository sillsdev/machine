using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Matching;

// Minimal, hand-built reproductions of two cases found by a differential fuzz
// (TraversalDedupDifferentialFuzzTests) comparing master against the
// add-allMatches-to-Traverse branch's traversal-dedup change. Both settings
// use AllSubmatches = false and Nondeterministic = false, matching the fuzz.
public class TraversalDedupMinimalCasesTests : PhoneticTestsBase
{
    private FeatureStruct Ann(string voice, string high, string back)
    {
        return FeatureStruct
            .New(PhoneticFeatSys)
            .Feature("voice")
            .EqualTo(voice)
            .Feature("high")
            .EqualTo(high)
            .Feature("back")
            .EqualTo(back)
            .Value;
    }

    [Test]
    public void NondeterministicTraversal_DedupOnVariableBindingLosesMatch()
    {
        // Pattern: high=$v0+, anchored to both ends (runs NondeterministicFsaTraversalMethod
        // because of the variable). The only way to cover the whole input [0,5) with a
        // single consistent value of v0 is the run of high- annotations: [0,2)+[2,4)+[4,5).
        // Reaching it requires abandoning, at annotation index 0, the parallel instance that
        // consumed the high+ annotation [0,1) (which binds v0=+ but then dead-ends, since no
        // annotation starts at offset 1). Both instances reach the same (State, AnnotationIndex)
        // with different VariableBindings (v0=+ vs v0=-); the traversal-dedup change keys on
        // (State, AnnotationIndex) alone and can keep the v0=+ instance, which can never
        // complete the anchored match, discarding the one that would have succeeded.
        Pattern<AnnotatedStringData, int> pattern = Pattern<AnnotatedStringData, int>
            .New()
            .Annotation(FeatureStruct.New(PhoneticFeatSys).Feature("high").EqualToVariable("v0").Value)
            .OneOrMore.Value;

        var data = new AnnotatedStringData(new string('a', 5));
        data.Annotations.Add(0, 2, Ann("voice-", "high-", "back-"), false);
        data.Annotations.Add(0, 2, Ann("voice-", "high-", "back-"), false); // duplicate span+values, as the fuzz produced
        data.Annotations.Add(0, 1, Ann("voice-", "high+", "back+"), false);
        data.Annotations.Add(2, 4, Ann("voice-", "high-", "back+"), false);
        data.Annotations.Add(4, 5, Ann("voice+", "high-", "back+"), false);

        var matcher = new Matcher<AnnotatedStringData, int>(
            pattern,
            new MatcherSettings<int>
            {
                AnchoredToStart = true,
                AnchoredToEnd = true,
                Direction = Direction.LeftToRight,
                AllSubmatches = false,
                Nondeterministic = false,
            }
        );

        Match<AnnotatedStringData, int> match = matcher.Match(data);

        Assert.That(match.Success, Is.True, $"success={match.Success};range={DescribeRange(match.Range)}");
        Assert.That(match.Range, Is.EqualTo(Range<int>.Create(0, 5)));
        Assert.That(((FeatureSymbol)match.VariableBindings["v0"]).ID, Is.EqualTo("high-"));
    }

    [Test]
    public void DeterministicTraversal_DedupOnRegistersShortensMatch()
    {
        // Pattern: (g0(back=back+) | g1(high=high+ back=back+)), anchored to start only
        // (runs DeterministicFsaTraversalMethod: IsDeterministic=True, GroupCount=2).
        // The annotation [0,2) satisfies both g0's sole constraint and g1's first constraint,
        // so two lineages both consume it as their first step: one has already closed g0 and
        // can stop at offset 2, the other still has g1 open, waiting for back=back+ at [2,4).
        // They converge on the same (State, AnnotationIndex) with different open-group
        // registers. Deduping on (State, AnnotationIndex) alone keeps only one lineage's
        // registers - master keeps both and finds the longer g1 match [0,4); the branch's
        // surviving lineage yields only the short, earlier-completing g0 match [0,1).
        Pattern<AnnotatedStringData, int> pattern = Pattern<AnnotatedStringData, int>
            .New()
            .Group("g0", g0 => g0.Annotation(FeatureStruct.New(PhoneticFeatSys).Feature("back").EqualTo("back+").Value))
            .Or.Group(
                "g1",
                g1 =>
                    g1.Annotation(FeatureStruct.New(PhoneticFeatSys).Feature("high").EqualTo("high+").Value)
                        .Annotation(FeatureStruct.New(PhoneticFeatSys).Feature("back").EqualTo("back+").Value)
            )
            .Value;

        var data = new AnnotatedStringData(new string('a', 4));
        data.Annotations.Add(0, 1, Ann("voice-", "high+", "back+"), false);
        data.Annotations.Add(0, 2, Ann("voice-", "high+", "back+"), false);
        data.Annotations.Add(2, 4, Ann("voice+", "high-", "back+"), false);

        var matcher = new Matcher<AnnotatedStringData, int>(
            pattern,
            new MatcherSettings<int>
            {
                AnchoredToStart = true,
                AnchoredToEnd = false,
                Direction = Direction.LeftToRight,
                AllSubmatches = false,
                Nondeterministic = false,
            }
        );

        Match<AnnotatedStringData, int> match = matcher.Match(data);

        Assert.That(match.Success, Is.True);
        Assert.That(
            match.Range,
            Is.EqualTo(Range<int>.Create(0, 4)),
            $"success={match.Success};range={DescribeRange(match.Range)};"
                + $"g0={DescribeCapture(match.GroupCaptures["g0"])};g1={DescribeCapture(match.GroupCaptures["g1"])}"
        );
        Assert.That(match.GroupCaptures["g0"].Success, Is.False);
        Assert.That(match.GroupCaptures["g1"].Success, Is.True);
        Assert.That(match.GroupCaptures["g1"].Range, Is.EqualTo(Range<int>.Create(0, 4)));
    }

    private static string DescribeRange(Range<int> range)
    {
        return range == Range<int>.Null ? "<null>" : $"[{range.Start},{range.End})";
    }

    private static string DescribeCapture(GroupCapture<int> capture)
    {
        return capture.Success ? DescribeRange(capture.Range) : "<uncaptured>";
    }
}
