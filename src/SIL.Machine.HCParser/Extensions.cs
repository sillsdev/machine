using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab
{
	internal static class Extensions
	{
		public static IList<MorphInfo> GetMorphInfos(this Word parse)
		{
			return parse.Morphs.Select(m => new MorphInfo(parse, m)).ToArray();
		}

		public static void WriteParse(this TextWriter outWriter, IList<MorphInfo> parse)
		{
			outWriter.Write("Morphs: ");
			bool firstItem = true;
			foreach (MorphInfo morph in parse)
			{
				int len = Math.Max(morph.Form.Length, morph.Gloss.Length);
				if (len > 0)
				{
					if (!firstItem)
						outWriter.Write(" ");
					outWriter.Write(morph.Form.PadRight(len));
					firstItem = false;
				}
			}
			outWriter.WriteLine();
			outWriter.Write("Gloss:  ");
			firstItem = true;
			foreach (MorphInfo morph in parse)
			{
				int len = Math.Max(morph.Form.Length, morph.Gloss.Length);
				if (len > 0)
				{
					if (!firstItem)
						outWriter.Write(" ");
					outWriter.Write(morph.Gloss.PadRight(len));
					firstItem = false;
				}
			}
			outWriter.WriteLine();
		}
	}
}
