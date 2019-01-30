using SIL.Machine.Tokenization;

namespace SIL.Machine.WebApi
{
	public static class WebApiUtils
	{
		public static StringTokenizer CreateSegmentTokenizer(string type)
		{
			switch (type)
			{
				case "line":
					return new LineSegmentTokenizer();

				case "latin":
				default:
					return new LatinSentenceTokenizer();
			}
		}
	}
}
