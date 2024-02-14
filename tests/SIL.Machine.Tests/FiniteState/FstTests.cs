using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState;

[TestFixture]
public class FstTests : PhoneticTestsBase
{
    private PhoneticFstOperations _operations = default!;

    public override void FixtureSetUp()
    {
        base.FixtureSetUp();
        _operations = new PhoneticFstOperations(_characters);
    }

    [Test]
    public void IsDeterminizable()
    {
        var featSys = new FeatureSystem
        {
            new StringFeature("A"),
            new StringFeature("B"),
            new StringFeature("C"),
            new StringFeature("D"),
            new StringFeature("E"),
            new StringFeature("F")
        };

        var fst = new Fst<AnnotatedStringData, int>(_operations);
        fst.StartState = fst.CreateState();
        State<AnnotatedStringData, int> s1 = fst.StartState.Arcs.Add(
            FeatureStruct.New(featSys).Feature("A").EqualTo("true").Value,
            FeatureStruct.New(featSys).Feature("E").EqualTo("true").Value,
            fst.CreateState()
        );
        State<AnnotatedStringData, int> s2 = fst.StartState.Arcs.Add(
            FeatureStruct.New(featSys).Feature("A").EqualTo("true").Value,
            FeatureStruct.New(featSys).Feature("F").EqualTo("true").Value,
            fst.CreateState()
        );
        State<AnnotatedStringData, int> s3 = s1.Arcs.Add(
            FeatureStruct.New(featSys).Feature("B").EqualTo("true").Value,
            FeatureStruct.New(featSys).Feature("D").EqualTo("true").Value,
            fst.CreateState()
        );
        s2.Arcs.Add(
            FeatureStruct.New(featSys).Feature("C").EqualTo("true").Value,
            FeatureStruct.New(featSys).Feature("D").EqualTo("true").Value,
            s3
        );
        State<AnnotatedStringData, int> s4 = s3.Arcs.Add(
            FeatureStruct.New(featSys).Feature("A").EqualTo("true").Value,
            FeatureStruct.New(featSys).Feature("A").EqualTo("true").Value,
            fst.CreateAcceptingState()
        );
        Assert.That(fst.IsDeterminizable, Is.True);

        s4.Arcs.Add(
            FeatureStruct.New(featSys).Feature("A").EqualTo("true").Value,
            FeatureStruct.New(featSys).Feature("A").EqualTo("true").Value,
            s4
        );

        Assert.That(fst.IsDeterminizable, Is.False);
    }

    [Test]
    public void Determinize()
    {
        var featSys = new FeatureSystem
        {
            new StringFeature("A"),
            new StringFeature("B"),
            new StringFeature("C"),
            new StringFeature("D"),
            new StringFeature("E"),
            new StringFeature("F")
        };

        var nfst = new Fst<AnnotatedStringData, int>(_operations);
        nfst.StartState = nfst.CreateState();
        State<AnnotatedStringData, int> s1 = nfst.StartState.Arcs.Add(
            FeatureStruct.New(featSys).Feature("A").EqualTo("true").Value,
            FeatureStruct.New(featSys).Feature("C").EqualTo("true").Value,
            nfst.CreateState()
        );
        State<AnnotatedStringData, int> sa = s1.Arcs.Add(
            FeatureStruct.New(featSys).Feature("D").EqualTo("true").Value,
            FeatureStruct.New(featSys).Feature("E").EqualTo("true").Value,
            nfst.CreateAcceptingState()
        );

        State<AnnotatedStringData, int> s2 = nfst.StartState.Arcs.Add(
            FeatureStruct.New(featSys).Feature("A").EqualTo("true").Value,
            FeatureStruct.New(featSys).Feature("B").EqualTo("true").Value,
            nfst.CreateState()
        );
        State<AnnotatedStringData, int> s3 = s2.Arcs.Add(
            FeatureStruct.New(featSys).Value,
            FeatureStruct.New(featSys).Value,
            nfst.CreateState()
        );
        s3.Arcs.Add(
            FeatureStruct.New(featSys).Feature("B").EqualTo("true").Value,
            FeatureStruct.New(featSys).Feature("B").EqualTo("true").Value,
            sa
        );

        Fst<AnnotatedStringData, int> dfst;
        Assert.That(nfst.TryDeterminize(out dfst), Is.True);
    }

    [Test]
    public void Compose()
    {
        var featSys = new FeatureSystem { new StringFeature("value") };

        var fst1 = new Fst<AnnotatedStringData, int>(_operations);
        fst1.StartState = fst1.CreateState();
        State<AnnotatedStringData, int> s1 = fst1.StartState.Arcs.Add(
            FeatureStruct.New(featSys).Feature("value").EqualTo("a").Value,
            FeatureStruct.New(featSys).Feature("value").EqualTo("x").Value,
            fst1.CreateAcceptingState()
        );
        s1.Arcs.Add(
            FeatureStruct.New(featSys).Feature("value").EqualTo("b").Value,
            FeatureStruct.New(featSys).Feature("value").EqualTo("y").Value,
            s1
        );

        var fst2 = new Fst<AnnotatedStringData, int>(_operations);
        fst2.StartState = fst2.CreateAcceptingState();
        fst2.StartState.Arcs.Add(FeatureStruct.New(featSys).Feature("value").EqualTo("x").Value, null, fst2.StartState);
        fst2.StartState.Arcs.Add(
            FeatureStruct.New(featSys).Feature("value").EqualTo("y").Value,
            FeatureStruct.New(featSys).Feature("value").EqualTo("z").Value,
            fst2.StartState
        );

        Fst<AnnotatedStringData, int> composedFsa = fst1.Compose(fst2);
        var writer = new StringWriter();
        composedFsa.ToGraphViz(writer);
        Assert.That(
            writer.ToString().Replace("\r\n", "\n"),
            Is.EqualTo(
                @"digraph G {
  0 [shape=""diamond"", color=""green""];
  0 -> 1 [label=""[value:\""a\""],1:ε""];
  1 [shape=""circle"", color=""red"", peripheries=""2""];
  1 -> 1 [label=""[value:\""b\""],1:([value:\""z\""],∪)""];
}
".Replace("\r\n", "\n")
            )
        );
    }

    [Test]
    public void Transduce()
    {
        var fst = new Fst<AnnotatedStringData, int>(_operations) { UseUnification = false };
        fst.StartState = fst.CreateAcceptingState();
        fst.StartState.Arcs.Add(FeatureStruct.New(_phoneticFeatSys).Symbol("nas-", "nas?").Value, fst.StartState);
        fst.StartState.Arcs.Add(
            FeatureStruct.New(_phoneticFeatSys).Symbol("nas+").Symbol("cor+", "cor-").Value,
            fst.StartState
        );
        State<AnnotatedStringData, int> s1 = fst.StartState.Arcs.Add(
            FeatureStruct.New(_phoneticFeatSys).Symbol("cor?").Symbol("nas+").Value,
            FeatureStruct.New(_phoneticFeatSys).Symbol("cor-").Value,
            fst.CreateState()
        );
        s1.Arcs.Add(FeatureStruct.New(_phoneticFeatSys).Symbol("cor-").Value, fst.StartState);
        State<AnnotatedStringData, int> s2 = fst.StartState.Arcs.Add(
            FeatureStruct.New(_phoneticFeatSys).Symbol("cor?").Symbol("nas+").Value,
            FeatureStruct.New(_phoneticFeatSys).Symbol("cor+").Value,
            fst.CreateAcceptingState()
        );
        s2.Arcs.Add(
            FeatureStruct.New(_phoneticFeatSys).Symbol("cor?").Symbol("nas+").Value,
            FeatureStruct.New(_phoneticFeatSys).Symbol("cor+").Value,
            s2
        );
        s2.Arcs.Add(
            FeatureStruct.New(_phoneticFeatSys).Symbol("nas-", "nas?").Symbol("cor+", "cor?").Value,
            fst.StartState
        );
        s2.Arcs.Add(FeatureStruct.New(_phoneticFeatSys).Symbol("nas+").Symbol("cor+").Value, fst.StartState);
        s2.Arcs.Add(
            FeatureStruct.New(_phoneticFeatSys).Symbol("cor?").Symbol("nas+").Value,
            FeatureStruct.New(_phoneticFeatSys).Symbol("cor-").Value,
            s1
        );

        Fst<AnnotatedStringData, int> dfst = fst.Determinize();

        AnnotatedStringData data = CreateStringData("caNp");
        FstResult<AnnotatedStringData, int> result;
        Assert.That(dfst.Transduce(data, data.Annotations.First, null, true, true, true, out result), Is.True);
        Assert.That(result.Output.String, Is.EqualTo("camp"));

        data = CreateStringData("caN");
        Assert.That(dfst.Transduce(data, data.Annotations.First, null, true, true, true, out result), Is.True);
        Assert.That(result.Output.String, Is.EqualTo("can"));

        data = CreateStringData("carp");
        Assert.That(dfst.Transduce(data, data.Annotations.First, null, true, true, true, out result), Is.True);
        Assert.That(result.Output.String, Is.EqualTo("carp"));

        fst = new Fst<AnnotatedStringData, int>(_operations) { UseUnification = false };
        fst.StartState = fst.CreateAcceptingState();
        s1 = fst
            .StartState.Arcs.Add(FeatureStruct.New(_phoneticFeatSys).Symbol("cons+").Value, fst.CreateState())
            .Arcs.Add(FeatureStruct.New(_phoneticFeatSys).Symbol("cons-").Value, fst.CreateState());
        s2 = s1.Arcs.Add(FeatureStruct.New(_phoneticFeatSys).Symbol("nas+").Value, null, fst.CreateState());
        State<AnnotatedStringData, int> s3 = s1.Arcs.Add(
            FeatureStruct.New(_phoneticFeatSys).Symbol("voice-").Value,
            fst.CreateState()
        );
        s3.Arcs.Add(null, FeatureStruct.New(_phoneticFeatSys).Symbol(_bdry).Feature("strRep").EqualTo(".").Value, s2);
        s3.Arcs.Add(
            null,
            FeatureStruct.New(_phoneticFeatSys).Symbol(_bdry).Feature("strRep").EqualTo("+").Value,
            fst.CreateState()
        )
            .Arcs.Add(null, FeatureStruct.New(_phoneticFeatSys).Symbol(_bdry).Feature("strRep").EqualTo(".").Value, s2);
        s2.Arcs.Add(FeatureStruct.New(_phoneticFeatSys).Symbol("cons+").Value, fst.CreateAcceptingState());

        dfst = fst.Determinize();

        data = CreateStringData("camp");
        Assert.That(dfst.Transduce(data, data.Annotations.First, null, true, true, true, out result), Is.True);
        Assert.That(result.Output.String, Is.EqualTo("cap"));

        data = CreateStringData("casp");
        IEnumerable<FstResult<AnnotatedStringData, int>> results;
        Assert.That(dfst.Transduce(data, data.Annotations.First, null, true, true, true, out results), Is.True);
        FstResult<AnnotatedStringData, int>[] resultsArray = results.ToArray();
        Assert.That(resultsArray.Length, Is.EqualTo(2));
        Assert.That(resultsArray.Select(r => r.Output.String), Is.EquivalentTo(new[] { "cas+.p", "cas.p" }));
    }
}
