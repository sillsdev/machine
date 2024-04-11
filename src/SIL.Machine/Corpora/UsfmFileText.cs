using System;
using System.IO;
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
            string fileName,
            ScrVers versification = null,
            bool includeMarkers = false,
            bool includeAllText = false
        )
            : base(GetId(fileName, encoding), stylesheet, encoding, versification, includeMarkers, includeAllText)
        {
            _fileName = fileName;
        }

        protected override IStreamContainer CreateStreamContainer()
        {
            return new FileStreamContainer(_fileName);
        }

        private static string GetId(string fileName, Encoding encoding)
        {
            using (var reader = new StreamReader(fileName, encoding))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.StartsWith("\\id "))
                    {
                        string id = line.Substring(4);
                        int index = id.IndexOf(" ");
                        if (index != -1)
                            id = id.Substring(0, index);
                        return id.Trim();
                    }
                }
            }
            throw new InvalidOperationException("The USFM does not contain an 'id' marker.");
        }
    }
}
