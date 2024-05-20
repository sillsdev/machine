using System.Collections.Generic;
using System.Reflection;
using System.Text;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public class UsfmZipText : UsfmTextBase
    {
        private readonly string _archiveFileName;
        private readonly string _path;

        public UsfmZipText(
            UsfmStylesheet stylesheet,
            Encoding encoding,
            string id,
            string archiveFileName,
            string path,
            ScrVers versification = null,
            bool includeMarkers = false,
            bool includeAllText = false
        )
            : base(id, stylesheet, encoding, versification, includeMarkers, includeAllText)
        {
            _archiveFileName = archiveFileName;
            _path = path;
        }

        protected override IStreamContainer CreateStreamContainer()
        {
            return new ZipEntryStreamContainer(_archiveFileName, _path);
        }

        protected override IEnumerable<TextRow> GetVersesInDocOrder()
        {
            try
            {
                return base.GetVersesInDocOrder();
            }
            catch (UsfmParserException e)
            {
                e.GetType()
                    .GetField("_message", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(e, $"{_archiveFileName} - {_path}: {e.Message}");
                throw e;
            }
        }
    }
}
