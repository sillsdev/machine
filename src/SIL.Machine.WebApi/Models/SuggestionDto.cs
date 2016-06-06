using System.Collections.Generic;
using System.Linq;
using SIL.ObjectModel;

namespace SIL.Machine.WebApi.Models
{
	public class SuggestionDto
	{
		public SuggestionDto(string targetSegment, IEnumerable<TargetWordDto> targetWords)
		{
			TargetSegment = targetSegment;
			TargetWords = new ReadOnlyCollection<TargetWordDto>(targetWords.ToArray());
		}

		public string TargetSegment { get; }
		public ReadOnlyCollection<TargetWordDto> TargetWords { get; }
	}
}
