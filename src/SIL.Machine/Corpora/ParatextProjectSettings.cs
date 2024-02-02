using System.Globalization;
using System.Text;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public class ParatextProjectSettings
    {
        public ParatextProjectSettings(
            string name,
            string fullName,
            Encoding encoding,
            ScrVers versification,
            UsfmStylesheet stylesheet,
            string fileNamePrefix,
            string fileNameForm,
            string fileNameSuffix,
            string biblicalTermsListType,
            string biblicalTermsProjectName,
            string biblicalTermsFileName
        )
        {
            Name = name;
            FullName = fullName;
            Encoding = encoding;
            Versification = versification;
            Stylesheet = stylesheet;
            FileNamePrefix = fileNamePrefix;
            FileNameForm = fileNameForm;
            FileNameSuffix = fileNameSuffix;
            BiblicalTermsListType = biblicalTermsListType;
            BiblicalTermsProjectName = biblicalTermsProjectName;
            BiblicalTermsFileName = biblicalTermsFileName;
        }

        public string Name { get; }
        public string FullName { get; }
        public Encoding Encoding { get; }
        public ScrVers Versification { get; }
        public UsfmStylesheet Stylesheet { get; }
        public string FileNamePrefix { get; }
        public string FileNameForm { get; }
        public string FileNameSuffix { get; }
        public string BiblicalTermsListType { get; }
        public string BiblicalTermsProjectName { get; }
        public string BiblicalTermsFileName { get; }

        public string GetBookFileName(string bookId)
        {
            string bookPart;
            if (FileNameForm == "MAT")
                bookPart = bookId;
            else if (FileNameForm == "40" || FileNameForm == "41")
                bookPart = GetBookFileNameDigits(bookId);
            else
                bookPart = GetBookFileNameDigits(bookId) + bookId;
            return FileNamePrefix + bookPart + FileNameSuffix;
        }

        private static string GetBookFileNameDigits(string bookId)
        {
            int bookNum = Canon.BookIdToNumber(bookId);

            if (bookNum < 10)
                return "0" + bookNum;
            if (bookNum < 40)
                return bookNum.ToString(CultureInfo.InvariantCulture);
            if (bookNum < 100)
                return (bookNum + 1).ToString(CultureInfo.InvariantCulture);
            if (bookNum < 110)
                return "A" + (bookNum - 100);
            if (bookNum < 120)
                return "B" + (bookNum - 110);
            return "C" + (bookNum - 120);
        }
    }
}
