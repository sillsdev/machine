using System.Collections.Generic;

namespace SIL.Machine.WebApi.Models
{
	public interface IEngineService
	{
		IEnumerable<EngineDto> GetAll();
		bool TryGet(string sourceLanguageTag, string targetLanguageTag, out EngineDto engine);
		bool TryTranslate(string sourceLanguageTag, string targetLanguageTag, string segment, out string result);
		bool TryInteractiveTranslate(string sourceLanguageTag, string targetLanguageTag, IReadOnlyList<string> segment, out InteractiveTranslationResultDto result);
		bool TryTrainSegment(string sourceLanguageTag, string targetLanguageTag, SegmentPairDto segmentPair);
	}
}
