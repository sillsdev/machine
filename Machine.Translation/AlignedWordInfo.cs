namespace SIL.Machine.Translation
{
	public enum AlignedWordType
	{
		Normal,
		Transferred,
		NotTranslated
	}

	public class AlignedWordInfo
	{
		private readonly int _index;
		private readonly double _confidence;
		private readonly AlignedWordType _type;

		public AlignedWordInfo(int index, double confidence, AlignedWordType type)
		{
			_index = index;
			_confidence = confidence;
			_type = type;
		}

		public int Index
		{
			get { return _index; }
		}

		public double Confidence
		{
			get { return _confidence; }
		}

		public AlignedWordType Type
		{
			get { return _type; }
		}
	}
}
