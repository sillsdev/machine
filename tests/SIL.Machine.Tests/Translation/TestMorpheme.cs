using SIL.Machine.Morphology;

namespace SIL.Machine.Tests.Translation
{
	/// <summary>
	/// This class contains information about a morpheme.
	/// </summary>
	public class TestMorpheme : IMorpheme
	{
		private readonly string _id;
		private readonly string _category;
		private readonly string _gloss;
		private readonly MorphemeType _morphemeType;

		public TestMorpheme(string id, string category, string gloss, MorphemeType morphemeType)
		{
			_id = id;
			_category = category;
			_gloss = gloss;
			_morphemeType = morphemeType;
		}

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
}
