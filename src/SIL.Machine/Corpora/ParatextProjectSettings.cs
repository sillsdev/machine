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
    }
}
