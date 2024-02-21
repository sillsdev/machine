﻿using System.Text;
using NUnit.Framework;

namespace SIL.Machine.Corpora;

[TestFixture]
public class UsfmFileTextCorpusTests
{
    [Test]
    public void Texts()
    {
        var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath);

        Assert.That(corpus.Texts.Select(t => t.Id), Is.EquivalentTo(new[] { "LEV", "1CH", "MAT", "MRK" }));
    }

    [Test]
    public void TryGetText()
    {
        var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath);

        Assert.That(corpus.TryGetText("MAT", out IText mat), Is.True);
        Assert.That(mat.GetRows(), Is.Not.Empty);
        Assert.That(corpus.TryGetText("LUK", out _), Is.False);
    }
}
