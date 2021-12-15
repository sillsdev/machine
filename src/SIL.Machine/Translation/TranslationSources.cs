using System;

namespace SIL.Machine.Translation
{
	[Flags]
	public enum TranslationSources
	{
		None = 0x0,
		Smt = 0x1,
		Transfer = 0x2,
		Prefix = 0x4,
		Nmt = 0x8
	}
}
