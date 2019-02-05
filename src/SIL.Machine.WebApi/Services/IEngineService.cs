using System.Collections.Generic;
using System.Threading.Tasks;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.DataAccess;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.Services
{
	public interface IEngineService
	{
		Task<TranslationResult> TranslateAsync(EngineLocatorType locatorType, string locator,
			IReadOnlyList<string> segment);

		Task<IEnumerable<TranslationResult>> TranslateAsync(EngineLocatorType locatorType, string locator, int n,
			IReadOnlyList<string> segment);

		Task<HybridInteractiveTranslationResult> InteractiveTranslateAsync(EngineLocatorType locatorType,
			string locator, IReadOnlyList<string> segment);

		Task<bool> TrainSegmentAsync(EngineLocatorType locatorType, string locator,
			IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment);

		Task<Project> AddProjectAsync(string projectId, string sourceLanguageTag, string targetLanguageTag,
			string sourceSegmentType, string targetSegmentType, bool isShared);

		Task<bool> RemoveProjectAsync(string projectId);

		Task<Build> StartBuildAsync(EngineLocatorType locatorType, string locator);

		Task<bool> CancelBuildAsync(BuildLocatorType locatorType, string locator);
	}
}
