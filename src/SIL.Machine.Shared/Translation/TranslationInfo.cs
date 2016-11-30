using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public class TranslationInfo
	{
		public IList<string> Target { get; } = new List<string>();
		public IList<double> TargetConfidences { get; } = new List<double>();
		public IList<PhraseInfo> Phrases { get; } = new List<PhraseInfo>();
		public ISet<int> TargetUnknownWords { get; } = new HashSet<int>();
		public double Score { get; set; }
	}
}
