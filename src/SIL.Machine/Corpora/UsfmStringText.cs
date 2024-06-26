using System.Text;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public class UsfmStringText : UsfmTextBase
    {
        private readonly string _usfm;

        public UsfmStringText(
            UsfmStylesheet stylesheet,
            Encoding encoding,
            string id,
            string usfm,
            ScrVers versification = null,
            bool includeMarkers = false,
            bool includeAllText = false
        )
            : base(id, stylesheet, encoding, versification, includeMarkers, includeAllText)
        {
            _usfm = usfm;
        }

        protected override IStreamContainer CreateStreamContainer()
        {
            return new StringStreamContainer(_usfm);
        }
    }
}
