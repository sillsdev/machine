using System.Collections.Generic;
using System.Linq;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public abstract class ScriptureTextCorpus : DictionaryTextCorpus
	{
		public abstract ScrVers Versification { get; }

		public override IEnumerable<TextCorpusRow> GetRows(ITextCorpusView basedOn = null)
		{
			ScrVers basedOnVersification = null;
			if (basedOn != null && basedOn.Source is ScriptureTextCorpus scriptureTextCorpus)
				basedOnVersification = scriptureTextCorpus.Versification;
			return Texts.Cast<ScriptureText>().SelectMany(t => t.GetRows(basedOnVersification));
		}
	}
}
