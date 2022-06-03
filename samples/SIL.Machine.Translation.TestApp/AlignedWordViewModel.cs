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
        public AlignedWordViewModel(Range<int> range, WordTranslationLevel level)
        {
            Range = range;
            Level = level;
        }

        public Range<int> Range { get; }

        public WordTranslationLevel Level { get; }
    }
}
