namespace SIL.Machine.Translation
{
	public class PhraseInfo
	{
		public int SourceStartIndex { get; set; }
		public int SourceEndIndex { get; set; }
		public int TargetCut { get; set; }
		public WordAlignmentMatrix Alignment { get; set; }
	}
}
