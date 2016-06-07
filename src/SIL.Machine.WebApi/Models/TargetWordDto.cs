using System.Collections.Generic;

namespace SIL.Machine.WebApi.Models
{
	public class TargetWordDto
	{
		public int[] Range { get; set; }
		public IList<SourceWordDto> SourceWords { get; set; }
	}
}
