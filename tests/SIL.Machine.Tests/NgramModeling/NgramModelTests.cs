using NUnit.Framework;
using SIL.Machine.DataStructures;

namespace SIL.Machine.NgramModeling;

[TestFixture]
public class NgramModelTests
{
    [Test]
    public void GetProbability()
    {
        string[] words = ["#call#", "#stall#", "#hello#", "#the#", "#a#", "#test#", "#income#", "#unproduce#"];

        var model = NgramModel<string, char>.Train(2, words, w => w, new MaxLikelihoodSmoother<string, char>());
        Assert.That(model.GetProbability('l', new Ngram<char>("a")), Is.EqualTo(0.666).Within(0.001));
        Assert.That(model.GetProbability('#', new Ngram<char>("a")), Is.EqualTo(0.333).Within(0.001));
        Assert.That(model.GetProbability('a', new Ngram<char>("a")), Is.EqualTo(0.0));

        Assert.That(model.GetProbability('l', new Ngram<char>("l")), Is.EqualTo(0.5));
        Assert.That(model.GetProbability('o', new Ngram<char>("l")), Is.EqualTo(0.166).Within(0.001));
        Assert.That(model.GetProbability('#', new Ngram<char>("l")), Is.EqualTo(0.333).Within(0.001));
        Assert.That(model.GetProbability('a', new Ngram<char>("l")), Is.EqualTo(0.0));

        model = NgramModel<string, char>.Train(3, words, w => w, new MaxLikelihoodSmoother<string, char>());
        Assert.That(model.GetProbability('l', new Ngram<char>("at")), Is.EqualTo(0.0));

        Assert.That(model.GetProbability('l', new Ngram<char>("al")), Is.EqualTo(1.0));
        Assert.That(model.GetProbability('t', new Ngram<char>("al")), Is.EqualTo(0.0));
    }

    [Test]
    public void Ngrams()
    {
        string[] words = ["#call#", "#stall#", "#hello#", "#the#", "#a#", "#test#", "#income#", "#unproduce#"];

        NgramModel<string, char>[] models = NgramModel<string, char>
            .TrainAll(10, words, w => w, () => new MaxLikelihoodSmoother<string, char>())
            .ToArray();
        Assert.That(models[0].Ngrams, Has.Count.EqualTo(16));
        Assert.That(models[1].Ngrams, Has.Count.EqualTo(36));
        Assert.That(models[7].Ngrams, Has.Count.EqualTo(5));
        Assert.That(models[8].Ngrams, Has.Count.EqualTo(3));
        Assert.That(models[9].Ngrams, Has.Count.EqualTo(2));
    }

    [Test]
    public void GetProbabilityRightToLeft()
    {
        string[] words = ["#call#", "#stall#", "#hello#", "#the#", "#a#", "#test#", "#income#", "#unproduce#"];

        var model = NgramModel<string, char>.Train(
            2,
            words,
            w => w,
            Direction.RightToLeft,
            new MaxLikelihoodSmoother<string, char>()
        );
        Assert.That(model.GetProbability('a', new Ngram<char>("l")), Is.EqualTo(0.333).Within(0.001));
        Assert.That(model.GetProbability('l', new Ngram<char>("l")), Is.EqualTo(0.5));
        Assert.That(model.GetProbability('e', new Ngram<char>("l")), Is.EqualTo(0.166).Within(0.001));
        Assert.That(model.GetProbability('t', new Ngram<char>("l")), Is.EqualTo(0.0));

        Assert.That(model.GetProbability('c', new Ngram<char>("a")), Is.EqualTo(0.333).Within(0.001));
        Assert.That(model.GetProbability('t', new Ngram<char>("a")), Is.EqualTo(0.333).Within(0.001));
        Assert.That(model.GetProbability('#', new Ngram<char>("a")), Is.EqualTo(0.333).Within(0.001));
        Assert.That(model.GetProbability('l', new Ngram<char>("a")), Is.EqualTo(0.0));
    }

    [Test]
    public void NoSamples()
    {
        string[] words = [];

        var model = NgramModel<string, char>.Train(2, words, w => w, new MaxLikelihoodSmoother<string, char>());
        Assert.That(model.GetProbability('a', new Ngram<char>("l")), Is.EqualTo(0));
        Assert.That(model.GetProbability('l', new Ngram<char>("l")), Is.EqualTo(0));
        Assert.That(model.GetProbability('e', new Ngram<char>("l")), Is.EqualTo(0));
        Assert.That(model.GetProbability('t', new Ngram<char>("l")), Is.EqualTo(0));
    }
}
