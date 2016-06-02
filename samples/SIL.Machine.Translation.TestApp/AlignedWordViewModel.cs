using Eto.Forms;
using GalaSoft.MvvmLight;

namespace SIL.Machine.Translation.TestApp
{
	public enum WordTranslationLevel
	{
		Unknown,
		Transfer,
		HighConfidence,
		LowConfidence
	}

	public class AlignedWordViewModel : ViewModelBase
	{
		private readonly Range<int> _range;
		private readonly WordTranslationLevel _level;

		public AlignedWordViewModel(Range<int> range, WordTranslationLevel level)
		{
			_range = range;
			_level = level;
		}

		public Range<int> Range
		{
			get { return _range; }
		}

		public WordTranslationLevel Level
		{
			get { return _level; }
		}
	}
}
