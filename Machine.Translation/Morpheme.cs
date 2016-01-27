namespace SIL.Machine.Translation
{
	public enum MorphType
	{
		Stem,
		Affix
	}

	public class Morpheme
	{
		private readonly string _id;
		private readonly string _category;
		private readonly string _gloss;
		private readonly MorphType _morphType;

		public Morpheme(string id, string category, string gloss, MorphType morphType)
		{
			_id = id;
			_category = category;
			_gloss = gloss;
			_morphType = morphType;
		}

		public string Id
		{
			get { return _id; }
		}

		public string Category
		{
			get { return _category; }
		}

		public string Gloss
		{
			get { return _gloss; }
		}

		public MorphType MorphType
		{
			get { return _morphType; }
		}
	}
}
