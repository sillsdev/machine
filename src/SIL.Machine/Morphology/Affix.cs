using SIL.Machine.NgramModeling;

namespace SIL.Machine.Morphology
{
	public enum AffixType
	{
		Prefix,
		Suffix
	}

	public class Affix<TItem>
	{
		private readonly AffixType _type;
		private readonly Ngram<TItem> _ngram;
		private readonly double _score;

		public Affix(AffixType type, Ngram<TItem> ngram, double score)
		{
			_type = type;
			_ngram = ngram;
			_score = score;
		}

		public AffixType Type
		{
			get { return _type; }
		}

		public Ngram<TItem> Ngram
		{
			get { return _ngram; }
		}

		public double Score
		{
			get { return _score; }
		}

		public override string ToString()
		{
			switch (_type)
			{
				case AffixType.Prefix:
					return _ngram + "-";
				case AffixType.Suffix:
					return "-" + _ngram;
			}
			return _ngram.ToString();
		}
	}
}
