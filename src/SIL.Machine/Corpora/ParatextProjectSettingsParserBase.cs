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
                if (Versification.Table.Implementation.Exists(versName))
                {
                    versification = new ScrVers(versName);
                }
                else
                {
                    using (var reader = new StreamReader(Open("custom.vrs")))
                    {
                        versification = Versification.Table.Implementation.Load(
                            reader,
                            "custom.vrs",
                            versification,
                            versName
                        );
                    }
                }
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

            string biblicalTerms = settingsDoc.Root.Element("BiblicalTermsListSetting")?.Value;
            if (biblicalTerms == null)
                // Default to Major::BiblicalTerms.xml to mirror Paratext behavior
                biblicalTerms = "Major::BiblicalTerms.xml";

            string[] parts = biblicalTerms?.Split(new[] { ':' }, 3);

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
                parts[2]
            );
        }

        protected abstract bool Exists(string fileName);
        protected abstract string Find(string extension);
        protected abstract Stream Open(string fileName);
        protected abstract UsfmStylesheet CreateStylesheet(string fileName);
    }
}
