using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public abstract class ScriptureTextCorpus : DictionaryTextCorpus
    {
        public abstract ScrVers Versification { get; }
    }
}
