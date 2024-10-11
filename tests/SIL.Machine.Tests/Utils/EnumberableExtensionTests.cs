using NUnit.Framework;

namespace SIL.Machine.Utils;

[TestFixture]
public class EnumerableExtensionTests
{
    [Test]
    public void ZipMany_None()
    {
        var seqs = new List<IEnumerable<int>>();
        IEnumerable<int> result = seqs.ZipMany(x => x.Sum());
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ZipMany_Two()
    {
        var seqs = new List<IEnumerable<int>> { new[] { 1, 2, 3 }, new[] { 4, 5, 6 } };
        IEnumerable<int> result = seqs.ZipMany(x => x.Sum());
        Assert.That(result, Is.EqualTo(new[] { 5, 7, 9 }));
    }
}
