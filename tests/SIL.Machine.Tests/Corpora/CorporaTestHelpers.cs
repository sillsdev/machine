using System;
using System.IO;

namespace SIL.Machine.Tests.Corpora
{
	internal static class CorporaTestHelpers
	{
		public static readonly string TestDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Corpora", "TestData");
		public static readonly string UsfmStylesheetPath = Path.Combine(TestDataPath, "usfm.sty");
		public static readonly string UsfmTestProjectPath = Path.Combine(TestDataPath, "Tes");
	}
}
