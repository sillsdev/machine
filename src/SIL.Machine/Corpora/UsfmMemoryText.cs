using System.Text;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public class UsfmMemoryText : UsfmTextBase
    {
        private readonly string _usfm;
        private readonly Encoding _encoding;

        public UsfmMemoryText(
            UsfmStylesheet stylesheet,
            string id,
            string usfm,
            Encoding encoding = null,
            ScrVers versification = null,
            bool includeMarkers = false,
            bool includeAllText = false
        )
            : base(id, stylesheet, encoding ?? Encoding.UTF8, versification, includeMarkers, includeAllText)
        {
            _usfm = usfm;
            _encoding = encoding ?? Encoding.UTF8;
        }

        protected override IStreamContainer CreateStreamContainer()
        {
            return new MemoryStreamContainer(_usfm, _encoding);
        }
    }
}
