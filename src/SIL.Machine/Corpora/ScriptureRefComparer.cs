using System.Collections.Generic;
using SIL.Extensions;
using SIL.Machine.Corpora;

public class ScriptureRefComparer : IComparer<ScriptureRef>, IEqualityComparer<ScriptureRef>
{
    public static ScriptureRefComparer Default { get; } = new ScriptureRefComparer(compareSegments: true);
    public static ScriptureRefComparer IgnoreSegments { get; } = new ScriptureRefComparer(compareSegments: false);
    private readonly bool _compareSegments;

    public ScriptureRefComparer(bool compareSegments = true)
    {
        _compareSegments = compareSegments;
    }

    public int Compare(ScriptureRef x, ScriptureRef y)
    {
        return x.CompareTo(y, _compareSegments);
    }

    public bool Equals(ScriptureRef x, ScriptureRef y)
    {
        return x.CompareTo(y, _compareSegments) == 0;
    }

    public int GetHashCode(ScriptureRef obj)
    {
        int hashCode = 23;
        hashCode =
            hashCode * 31
            + (_compareSegments ? obj.VerseRef.BBBCCCVVVS.GetHashCode() : obj.VerseRef.BBBCCCVVV.GetHashCode());
        hashCode = hashCode * 31 + obj.Versification.GetHashCode();
        // Using ToRelaxed is necessary to maintain equality across relaxed refs, Equals properly handles relaxed ref comparison
        hashCode = hashCode * 31 + obj.ToRelaxed().Path.GetSequenceHashCode();
        return hashCode;
    }
}
