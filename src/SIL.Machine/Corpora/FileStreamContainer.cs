using SIL.ObjectModel;
using System.IO;

namespace SIL.Machine.Corpora
{
	public class FileStreamContainer : DisposableBase, IStreamContainer
	{
		private readonly string _fileName;

		public FileStreamContainer(string fileName)
		{
			_fileName = fileName;
		}

		public Stream OpenStream()
		{
			return new FileStream(_fileName, FileMode.Open, FileAccess.Read);
		}
	}
}
