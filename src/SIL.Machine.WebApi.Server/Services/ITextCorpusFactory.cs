using System.Collections.Generic;
using SIL.Machine.Corpora;

namespace SIL.Machine.WebApi.Server.Services
{
	public enum TextCorpusType
	{
		Source,
		Target
	}

	public interface ITextCorpusFactory
	{
		ITextCorpus Create(IEnumerable<string> projects, TextCorpusType type);
	}
}
