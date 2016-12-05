using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SIL.Machine.Translation;

namespace SIL.Machine.WebApi.Models
{
	public class AlignedWordPairDto
	{
		public int SourceIndex { get; set; }
		public int TargetIndex { get; set; }
		public TranslationSources Sources { get; set; }
	}
}
