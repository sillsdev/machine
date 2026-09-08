using NUnit.Framework;

namespace SIL.Machine.FiniteState;

[TestFixture]
public class RegistersEqualityComparerTests
{
    // Flat register array layout: index = registerIndex * 2 + (0 = start, 1 = end).
    private static Register<int>[] CreateRegisters(int registerCount)
    {
        return new Register<int>[registerCount * 2];
    }

    private RegistersEqualityComparer<int> _comparer = default!;

    [SetUp]
    public void SetUp()
    {
        _comparer = new RegistersEqualityComparer<int>(EqualityComparer<int>.Default);
    }

    [Test]
    public void Equals_EqualRegisters_ReturnsTrue()
    {
        Register<int>[] x = CreateRegisters(2);
        x[0] = new Register<int>(1, true);
        x[1] = new Register<int>(5, false);
        x[2] = new Register<int>(2, true);
        x[3] = new Register<int>(4, false);

        Register<int>[] y = CreateRegisters(2);
        y[0] = new Register<int>(1, true);
        y[1] = new Register<int>(5, false);
        y[2] = new Register<int>(2, true);
        y[3] = new Register<int>(4, false);

        Assert.That(_comparer.Equals(x, y), Is.True);
        Assert.That(_comparer.GetHashCode(x), Is.EqualTo(_comparer.GetHashCode(y)));
    }

    [Test]
    public void Equals_UnequalOffset_ReturnsFalse()
    {
        Register<int>[] x = CreateRegisters(1);
        x[0] = new Register<int>(1, true);
        x[1] = new Register<int>(5, false);

        Register<int>[] y = CreateRegisters(1);
        y[0] = new Register<int>(1, true);
        y[1] = new Register<int>(9, false);

        Assert.That(_comparer.Equals(x, y), Is.False);
    }

    [Test]
    public void Equals_UnequalIsStart_ReturnsFalse()
    {
        Register<int>[] x = CreateRegisters(1);
        x[0] = new Register<int>(1, true);
        x[1] = new Register<int>(5, false);

        Register<int>[] y = CreateRegisters(1);
        y[0] = new Register<int>(1, false);
        y[1] = new Register<int>(5, false);

        Assert.That(_comparer.Equals(x, y), Is.False);
    }

    [Test]
    public void Equals_BothUnset_ReturnsTrue()
    {
        Register<int>[] x = CreateRegisters(1);
        Register<int>[] y = CreateRegisters(1);

        Assert.That(_comparer.Equals(x, y), Is.True);
        Assert.That(_comparer.GetHashCode(x), Is.EqualTo(_comparer.GetHashCode(y)));
    }

    [Test]
    public void Equals_OneUnsetOneSet_ReturnsFalse()
    {
        Register<int>[] x = CreateRegisters(1);
        Register<int>[] y = CreateRegisters(1);
        y[0] = new Register<int>(1, true);

        Assert.That(_comparer.Equals(x, y), Is.False);
    }
}
