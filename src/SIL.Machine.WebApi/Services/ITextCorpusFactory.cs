using System.Threading.Tasks;
using SIL.Machine.Corpora;

namespace SIL.Machine.WebApi.Services
{
	public enum TextCorpusType
	{
		Source,
		Target
	}

	public interface ITextCorpusFactory
	{
		Task<ITextCorpus> CreateAsync(string engineId, TextCorpusType type);
	}
}
