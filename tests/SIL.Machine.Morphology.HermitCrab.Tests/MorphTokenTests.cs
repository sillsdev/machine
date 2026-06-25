using NUnit.Framework;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// The packed 32-bit analysis token (HERMITCRAB_FST_PLAN.md §8): 8-bit MorphOp + 24-bit morpheme
/// index, with the derivation array being self-describing (morpheme order = array order; root =
/// the Root token's position).
/// </summary>
public class MorphTokenTests
{
    [Test]
    public void Encode_RoundTripsOpAndMorphemeId()
    {
        foreach (MorphOp op in System.Enum.GetValues(typeof(MorphOp)))
        {
            foreach (int id in new[] { 0, 1, 42, MorphToken.MaxMorphemeId })
            {
                uint token = MorphToken.Encode(op, id);
                Assert.That(MorphToken.GetOp(token), Is.EqualTo(op), $"op for id {id}");
                Assert.That(MorphToken.GetMorphemeId(token), Is.EqualTo(id), $"id for op {op}");
            }
        }
    }

    [Test]
    public void Encode_IdOutOfRange_Throws()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => MorphToken.Encode(MorphOp.Root, MorphToken.MaxMorphemeId + 1)
        );
        Assert.Throws<System.ArgumentOutOfRangeException>(() => MorphToken.Encode(MorphOp.Root, -1));
    }

    [Test]
    public void Encode_DistinctInputsGiveDistinctTokens()
    {
        // Different op, same id → different token.
        Assert.That(MorphToken.Encode(MorphOp.Prefix, 7), Is.Not.EqualTo(MorphToken.Encode(MorphOp.Suffix, 7)));
        // Same op, different id → different token.
        Assert.That(MorphToken.Encode(MorphOp.Suffix, 7), Is.Not.EqualTo(MorphToken.Encode(MorphOp.Suffix, 8)));
    }

    [Test]
    public void Derivation_ArrayIsSelfDescribing()
    {
        // prefix m10 · root m20 · suffix m30  — a whole WordAnalysis in 12 bytes.
        uint[] derivation =
        {
            MorphToken.Encode(MorphOp.Prefix, 10),
            MorphToken.Encode(MorphOp.Root, 20),
            MorphToken.Encode(MorphOp.Suffix, 30),
        };

        // Morphemes in order = the array's morpheme indices in array order.
        Assert.That(System.Array.ConvertAll(derivation, MorphToken.GetMorphemeId), Is.EqualTo(new[] { 10, 20, 30 }));
        // RootMorphemeIndex falls out of the op codes — no separate field needed.
        Assert.That(MorphToken.RootIndex(derivation), Is.EqualTo(1));
    }

    [Test]
    public void RootIndex_NoRoot_ReturnsMinusOne()
    {
        uint[] derivation = { MorphToken.Encode(MorphOp.Prefix, 1), MorphToken.Encode(MorphOp.Suffix, 2) };
        Assert.That(MorphToken.RootIndex(derivation), Is.EqualTo(-1));
    }
}
