using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public class UsxFileText : UsxTextBase
	{
		private readonly string _fileName;

		public UsxFileText(string fileName, ScrVers versification = null)
			: base(CorporaUtils.GetUsxId(fileName), versification)
		{
			_fileName = fileName;
		}

		protected override IStreamContainer CreateStreamContainer()
		{
			return new FileStreamContainer(_fileName);
		}
	}
}
