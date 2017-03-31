using System.Collections.Generic;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.Services
{
	public enum TextCorpusType
	{
		Source,
		Target
	}

	public interface ITextCorpusFactory
	{
		ITextCorpus Create(IEnumerable<Project> projects, ITokenizer<string, int> wordTokenizer, TextCorpusType type);
	}
}
