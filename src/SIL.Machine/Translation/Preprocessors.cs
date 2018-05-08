namespace SIL.Machine.Translation
{
	public static class Preprocessors
	{
		public static string Lowercase(string str)
		{
#if BRIDGE_NET
			return str.ToLower();
#else
			return str.ToLowerInvariant();
#endif
		}

		public static string Null(string str)
		{
			return str;
		}
	}
}
