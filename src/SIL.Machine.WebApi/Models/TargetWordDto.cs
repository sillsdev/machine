using System.Collections.Generic;
using System.Linq;
using SIL.ObjectModel;

namespace SIL.Machine.WebApi.Models
{
	public class TargetWordDto
	{
		public TargetWordDto(RangeDto range, IEnumerable<SourceWordDto> wordPairs)
		{
			Range = range;
			SourceWords = new ReadOnlyCollection<SourceWordDto>(wordPairs.ToArray());
		}

		public RangeDto Range { get; }
		public ReadOnlyCollection<SourceWordDto> SourceWords { get; }
	}
}
