using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public class DblBundleText : UsxTextBase
	{
		private readonly string _bundleFileName;
		private readonly string _path;

		public DblBundleText(ITokenizer<string, int, string> wordTokenizer, string id, string bundleFileName,
			string path, ScrVers versification = null, bool mergeSegments = false)
			: base(wordTokenizer, id, versification, mergeSegments)
		{
			_bundleFileName = bundleFileName;
			_path = path;
		}

		protected override IStreamContainer CreateStreamContainer()
		{
			return new ZipEntryStreamContainer(_bundleFileName, _path);
		}
	}
}
