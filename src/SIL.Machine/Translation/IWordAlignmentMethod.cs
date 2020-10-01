using System;
using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface IWordAlignmentMethod : IWordAligner
	{
		Func<IReadOnlyList<string>, int, IReadOnlyList<string>, int, double> ScoreSelector { get; set; }
	}
}
