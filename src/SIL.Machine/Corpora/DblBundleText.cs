using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public class DblBundleText : UsxTextBase
	{
		private readonly string _bundleFileName;
		private readonly string _path;

		public DblBundleText(ITokenizer<string, int> wordTokenizer, string id, string bundleFileName, string path,
			ScrVers versification = null)
			: base(wordTokenizer, id, versification)
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
