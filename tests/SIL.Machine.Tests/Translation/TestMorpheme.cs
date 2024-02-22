using SIL.Machine.Morphology;

namespace SIL.Machine.Translation;

/// <summary>
/// This class contains information about a morpheme.
/// </summary>
public class TestMorpheme(string id, string category, string gloss, MorphemeType morphemeType) : IMorpheme
{
    private readonly string _id = id;
    private readonly string _category = category;
    private readonly string _gloss = gloss;
    private readonly MorphemeType _morphemeType = morphemeType;

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public string Id
    {
        get { return _id; }
    }

    /// <summary>
    /// Gets the category or part of speech.
    /// </summary>
    public string Category
    {
        get { return _category; }
    }

    /// <summary>
    /// Gets the gloss.
    /// </summary>
    public string Gloss
    {
        get { return _gloss; }
    }

    /// <summary>
    /// Gets the morpheme type.
    /// </summary>
    public MorphemeType MorphemeType
    {
        get { return _morphemeType; }
    }

    public override string ToString()
    {
        return _id;
    }
}
