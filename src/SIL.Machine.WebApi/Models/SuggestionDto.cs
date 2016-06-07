using System.Collections.Generic;

namespace SIL.Machine.WebApi.Models
{
	public class SuggestionDto
	{
		public IList<string> Suggestion { get; set; }
		public IList<TargetWordDto> Alignment { get; set; }
	}
}
