using System.Collections.Generic;
using System.Threading.Tasks;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.Services
{
	public interface IEngineService
	{
		Task<TranslationResult> TranslateAsync(string engineId, IReadOnlyList<string> segment);

		Task<IEnumerable<TranslationResult>> TranslateAsync(string engineId, int n, IReadOnlyList<string> segment);

		Task<WordGraph> GetWordGraphAsync(string engineId, IReadOnlyList<string> segment);

		Task<bool> TrainSegmentAsync(string engineId, IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment, bool sentenceStart);

		Task<bool> AddProjectAsync(Project project);

		Task<bool> RemoveProjectAsync(string projectId);

		Task<Build> StartBuildAsync(string engineId);
		Task<Build> StartBuildByProjectIdAsync(string projectId);

		Task CancelBuildAsync(string engineId);
	}
}
