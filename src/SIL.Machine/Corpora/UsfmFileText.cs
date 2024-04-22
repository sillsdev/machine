using System.Text;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public class UsfmFileText : UsfmTextBase
    {
        private readonly string _fileName;

        public UsfmFileText(
            UsfmStylesheet stylesheet,
            Encoding encoding,
            string id,
            string fileName,
            ScrVers versification = null,
            bool includeMarkers = false,
            bool includeAllText = false
        )
            : base(id, stylesheet, encoding, versification, includeMarkers, includeAllText)
        {
            _fileName = fileName;
        }

        protected override IStreamContainer CreateStreamContainer()
        {
            return new FileStreamContainer(_fileName);
        }
    }
}
