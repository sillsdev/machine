using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
    public class QuotationMarkUpdateSettings
    {
        private readonly QuotationMarkUpdateStrategy _defaultChapterAction;
        private readonly List<QuotationMarkUpdateStrategy> _chapterActions;

        public QuotationMarkUpdateSettings(
            QuotationMarkUpdateStrategy defaultChapterStrategy = QuotationMarkUpdateStrategy.ApplyFull,
            List<QuotationMarkUpdateStrategy> chapterStrategies = null
        )
        {
            _defaultChapterAction = defaultChapterStrategy;
            _chapterActions = chapterStrategies ?? new List<QuotationMarkUpdateStrategy>();
        }

        public QuotationMarkUpdateStrategy GetActionForChapter(int chapterNumber)
        {
            if (chapterNumber <= _chapterActions.Count)
            {
                return _chapterActions[chapterNumber - 1];
            }
            return _defaultChapterAction;
        }
    }
}
