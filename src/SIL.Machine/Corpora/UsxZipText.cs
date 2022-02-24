using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public class UsxZipText : UsxTextBase
	{
		private readonly string _archiveFileName;
		private readonly string _path;

		public UsxZipText(ITokenizer<string, int, string> wordTokenizer, string id, string archiveFileName,
			string path, ScrVers versification = null)
			: base(wordTokenizer, id, versification)
		{
			_archiveFileName = archiveFileName;
			_path = path;
		}

		protected override IStreamContainer CreateStreamContainer()
		{
			return new ZipEntryStreamContainer(_archiveFileName, _path);
		}
	}
}
