using System.Collections.Generic;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;

namespace SIL.Machine.WebApi.Services
{
	public enum TextCorpusType
	{
		Source,
		Target
	}

	public interface ITextCorpusFactory
	{
		ITextCorpus Create(IEnumerable<string> projects, ITokenizer<string, int> wordTokenizer, TextCorpusType type);
	}
}
