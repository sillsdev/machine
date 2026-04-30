using System;
using System.IO;
using System.Text;
using System.Xml.Linq;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public abstract class ParatextProjectSettingsParserBase
    {
        private readonly IParatextProjectFileHandler _paratextProjectFileHandler;
        private readonly ParatextProjectSettings _parentParatextProjectSettings;

        public ParatextProjectSettingsParserBase(
            IParatextProjectFileHandler paratextProjectFileHandler,
            ParatextProjectSettings parentParatextProjectSettings = null
        )
        {
            _paratextProjectFileHandler = paratextProjectFileHandler;
            _parentParatextProjectSettings = parentParatextProjectSettings;
        }

        public ParatextProjectSettings Parse()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            string settingsFileName = "Settings.xml";
            if (!_paratextProjectFileHandler.Exists(settingsFileName))
                settingsFileName = _paratextProjectFileHandler.Find(".ssf");
            if (string.IsNullOrEmpty(settingsFileName))
                throw new InvalidOperationException("The project does not contain a settings file.");
            XDocument settingsDoc;
            using (Stream stream = _paratextProjectFileHandler.Open(settingsFileName))
            {
                settingsDoc = XDocument.Load(stream);
            }

            string guid = settingsDoc.Root.Element("Guid").Value;
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
            if (_paratextProjectFileHandler.Exists("custom.vrs"))
            {
                string versName = ((ScrVersType)scrVersType).ToString() + "-" + guid;
                if (Versification.Table.Implementation.Exists(versName))
                {
                    versification = new ScrVers(versName);
                }
                else
                {
                    using (var reader = new StreamReader(_paratextProjectFileHandler.Open("custom.vrs")))
                    {
                        versification = Versification.Table.Implementation.Load(
                            reader,
                            "custom.vrs",
                            versification,
                            versName
                        );
                    }
                    Versification.Table.Implementation.RemoveAllUnknownVersifications();
                }
            }

            var stylesheetFileName = (string)settingsDoc.Root.Element("StyleSheet") ?? "usfm.sty";
            if (!_paratextProjectFileHandler.Exists(stylesheetFileName) && stylesheetFileName != "usfm_sb.sty")
                stylesheetFileName = "usfm.sty";
            UsfmStylesheet stylesheet = _paratextProjectFileHandler.CreateStylesheet(stylesheetFileName);

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
                string[] languageIsoCodeSettingParts = languageIsoCodeSetting.Split(':');
                if (languageIsoCodeSettingParts.Length > 0)
                {
                    languageCode = languageIsoCodeSettingParts[0];
                }
            }

            string translationInfoSetting = settingsDoc.Root.Element("TranslationInfo")?.Value;
            string translationType = "Standard";
            string parentName = null;
            string parentGuid = null;
            string[] translationInfoSettingParts = translationInfoSetting?.Split(':');
            if (translationInfoSettingParts?.Length == 3)
            {
                translationType = translationInfoSettingParts[0];
                parentName = translationInfoSettingParts[1] != string.Empty ? translationInfoSettingParts[1] : null;
                parentGuid = translationInfoSettingParts[2] != string.Empty ? translationInfoSettingParts[2] : null;
            }

            string visibility = settingsDoc.Root.Element("Visibility")?.Value;

            var settings = new ParatextProjectSettings(
                guid: guid,
                name: name,
                fullName: fullName,
                encoding: encoding,
                versification: versification,
                stylesheet: stylesheet,
                fileNamePrefix: prefix,
                fileNameForm: form,
                fileNameSuffix: suffix,
                biblicalTermsListType: parts[0],
                biblicalTermsProjectName: parts[1],
                biblicalTermsFileName: parts[2],
                languageCode: languageCode,
                translationType: translationType,
                visibility: visibility,
                parentGuid: parentGuid,
                parentName: parentName
            );

            if (_parentParatextProjectSettings != null && settings.HasParent)
            {
                settings.Parent = _parentParatextProjectSettings;
            }

            return settings;
        }
    }
}
