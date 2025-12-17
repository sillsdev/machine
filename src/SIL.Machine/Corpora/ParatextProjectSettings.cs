using System.Collections.Generic;
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
            string biblicalTermsFileName,
            string languageCode
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
            LanguageCode = languageCode;
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

        public string LanguageCode { get; }

        public bool IsBookFileName(string fileName, out string bookId)
        {
            bookId = null;

            if (!fileName.StartsWith(FileNamePrefix) || !fileName.EndsWith(FileNameSuffix))
                return false;

            string bookPart = fileName.Substring(
                FileNamePrefix.Length,
                fileName.Length - FileNamePrefix.Length - FileNameSuffix.Length
            );
            if (FileNameForm == "MAT")
            {
                if (bookPart.Length != 3)
                    return false;

                bookId = bookPart;
            }
            else if (FileNameForm == "40" || FileNameForm == "41")
            {
                if (bookPart != "100" && bookPart.Length != 2)
                    return false;

                bookId = Canon.BookNumberToId(GetBookNumber(bookPart));
            }
            else
            {
                if (bookPart.StartsWith("100"))
                {
                    if (bookPart.Length != 6)
                        return false;
                }
                else if (bookPart.Length != 5)
                {
                    return false;
                }

                bookId = bookPart.Length == 5 ? bookPart.Substring(2) : bookPart.Substring(3);
            }
            return true;
        }

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

        public IEnumerable<string> GetAllScriptureBookIds()
        {
            BookSet scriptureBooks = Canon.ScriptureBooks;
            scriptureBooks.SelectAll();
            foreach (string bookId in scriptureBooks.SelectedBookIds)
            {
                yield return bookId;
            }
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

        private static int GetBookNumber(string bookFileNameDigits)
        {
            if (bookFileNameDigits.StartsWith("A"))
                return 100 + int.Parse(bookFileNameDigits.Substring(1), CultureInfo.InvariantCulture);
            if (bookFileNameDigits.StartsWith("B"))
                return 110 + int.Parse(bookFileNameDigits.Substring(1), CultureInfo.InvariantCulture);
            if (bookFileNameDigits.StartsWith("C"))
                return 120 + int.Parse(bookFileNameDigits.Substring(1), CultureInfo.InvariantCulture);

            int bookNum = int.Parse(bookFileNameDigits, CultureInfo.InvariantCulture);
            if (bookNum >= 40)
                return bookNum - 1;
            return bookNum;
        }
    }
}
