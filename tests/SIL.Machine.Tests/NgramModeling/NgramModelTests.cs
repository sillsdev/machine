using NUnit.Framework;
using SIL.Machine.DataStructures;

namespace SIL.Machine.NgramModeling;

[TestFixture]
public class NgramModelTests
{
    [Test]
    public void GetProbability()
    {
        var words = new[] { "#call#", "#stall#", "#hello#", "#the#", "#a#", "#test#", "#income#", "#unproduce#" };

        NgramModel<string, char> model = NgramModel<string, char>.Train(
            2,
            words,
            w => w,
            new MaxLikelihoodSmoother<string, char>()
        );
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
        var words = new[] { "#call#", "#stall#", "#hello#", "#the#", "#a#", "#test#", "#income#", "#unproduce#" };

        NgramModel<string, char>[] models = NgramModel<string, char>
            .TrainAll(10, words, w => w, () => new MaxLikelihoodSmoother<string, char>())
            .ToArray();
        Assert.That(models[0].Ngrams.Count, Is.EqualTo(16));
        Assert.That(models[1].Ngrams.Count, Is.EqualTo(36));
        Assert.That(models[7].Ngrams.Count, Is.EqualTo(5));
        Assert.That(models[8].Ngrams.Count, Is.EqualTo(3));
        Assert.That(models[9].Ngrams.Count, Is.EqualTo(2));
    }

    [Test]
    public void GetProbabilityRightToLeft()
    {
        var words = new[] { "#call#", "#stall#", "#hello#", "#the#", "#a#", "#test#", "#income#", "#unproduce#" };

        NgramModel<string, char> model = NgramModel<string, char>.Train(
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

        NgramModel<string, char> model = NgramModel<string, char>.Train(
            2,
            words,
            w => w,
            new MaxLikelihoodSmoother<string, char>()
        );
        Assert.That(model.GetProbability('a', new Ngram<char>("l")), Is.EqualTo(0));
        Assert.That(model.GetProbability('l', new Ngram<char>("l")), Is.EqualTo(0));
        Assert.That(model.GetProbability('e', new Ngram<char>("l")), Is.EqualTo(0));
        Assert.That(model.GetProbability('t', new Ngram<char>("l")), Is.EqualTo(0));
    }
}
