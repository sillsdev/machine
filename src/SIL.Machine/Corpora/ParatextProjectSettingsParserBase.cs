using System;
using System.IO;
using System.Text;
using System.Xml.Linq;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public abstract class ParatextProjectSettingsParserBase
    {
        public ParatextProjectSettings Parse()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            string settingsFileName = "Settings.xml";
            if (!Exists(settingsFileName))
                settingsFileName = Find(".ssf");
            if (string.IsNullOrEmpty(settingsFileName))
                throw new InvalidOperationException("The project does not contain a settings file.");
            XDocument settingsDoc;
            using (Stream stream = Open(settingsFileName))
            {
                settingsDoc = XDocument.Load(stream);
            }

            string name = settingsDoc.Root.Element("Name").Value;
            string fullName = settingsDoc.Root.Element("FullName").Value;

            var encodingStr = (string)settingsDoc.Root.Element("Encoding") ?? "65001";
            if (!int.TryParse(encodingStr, out int codePage))
            {
                throw new NotImplementedException(
                    $"The project uses a legacy encoding that requires TECKit, map file: {encodingStr}."
                );
            }
            var encoding = Encoding.GetEncoding(codePage);

            var scrVersType = (int?)settingsDoc.Root.Element("Versification") ?? (int)ScrVersType.English;
            var versification = new ScrVers((ScrVersType)scrVersType);
            if (Exists("custom.vrs"))
            {
                var guid = (string)settingsDoc.Root.Element("Guid");
                string versName = ((ScrVersType)scrVersType).ToString() + "-" + guid;
                versification = new ScrVers(versName);
            }

            var stylesheetFileName = (string)settingsDoc.Root.Element("StyleSheet") ?? "usfm.sty";
            if (!Exists(stylesheetFileName) && stylesheetFileName != "usfm_sb.sty")
                stylesheetFileName = "usfm.sty";
            UsfmStylesheet stylesheet = CreateStylesheet(stylesheetFileName);

            string prefix = "";
            string form = "41MAT";
            string suffix = ".SFM";
            XElement namingElem = settingsDoc.Root.Element("Naming");
            if (namingElem != null)
            {
                var prePart = (string)namingElem.Attribute("PrePart");
                if (!string.IsNullOrEmpty(prePart))
                    prefix = prePart;

                var bookNameForm = (string)namingElem.Attribute("BookNameForm");
                if (!string.IsNullOrEmpty(bookNameForm))
                    form = bookNameForm;

                var postPart = (string)namingElem.Attribute("PostPart");
                if (!string.IsNullOrEmpty(postPart))
                    suffix = postPart;
            }

            string biblicalTermsListSetting = settingsDoc.Root.Element("BiblicalTermsListSetting")?.Value;
            if (biblicalTermsListSetting == null)
                // Default to Major::BiblicalTerms.xml to mirror Paratext behavior
                biblicalTermsListSetting = "Major::BiblicalTerms.xml";

            string[] parts = biblicalTermsListSetting.Split(new[] { ':' }, 3);
            if (parts.Length != 3)
            {
                throw new InvalidOperationException(
                    $"The BiblicalTermsListSetting element in Settings.xml in project {fullName}"
                        + $" is not in the expected format (e.g., Major::BiblicalTerms.xml) but is {biblicalTermsListSetting}."
                );
            }
            string languageCode = null;
            string languageIsoCodeSetting = settingsDoc.Root.Element("LanguageIsoCode")?.Value;
            if (languageIsoCodeSetting != null)
            {
                string[] languageIsoCodeSettingParts = settingsDoc.Root.Element("LanguageIsoCode").Value.Split(':');
                if (languageIsoCodeSettingParts.Length > 0)
                {
                    languageCode = languageIsoCodeSettingParts[0];
                }
            }

            return new ParatextProjectSettings(
                name,
                fullName,
                encoding,
                versification,
                stylesheet,
                prefix,
                form,
                suffix,
                parts[0],
                parts[1],
                parts[2],
                languageCode
            );
        }

        protected abstract bool Exists(string fileName);
        protected abstract string Find(string extension);
        protected abstract Stream Open(string fileName);
        protected abstract UsfmStylesheet CreateStylesheet(string fileName);
    }
}
