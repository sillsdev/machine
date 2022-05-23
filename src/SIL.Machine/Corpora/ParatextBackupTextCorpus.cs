using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SIL.IO;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public class ParatextBackupTextCorpus : ScriptureTextCorpus
    {
        public ParatextBackupTextCorpus(string fileName, bool includeMarkers = false)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using (ZipArchive archive = ZipFile.OpenRead(fileName))
            {
                ZipArchiveEntry settingsEntry = archive.GetEntry("Settings.xml");
                if (settingsEntry == null)
                    settingsEntry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".ssf"));
                if (settingsEntry == null)
                {
                    throw new ArgumentException(
                        "The project backup does not contain a settings file.",
                        nameof(fileName)
                    );
                }
                XDocument settingsDoc;
                using (Stream stream = settingsEntry.Open())
                {
                    settingsDoc = XDocument.Load(stream);
                }
                var encodingStr = (string)settingsDoc.Root.Element("Encoding") ?? "65001";
                if (!int.TryParse(encodingStr, out int codePage))
                {
                    throw new NotImplementedException(
                        $"The project uses a legacy encoding that requires TECKit, map file: {encodingStr}."
                    );
                }
                var encoding = Encoding.GetEncoding(codePage);

                var scrVersType = (int?)settingsDoc.Root.Element("Versification") ?? (int)ScrVersType.English;
                Versification = new ScrVers((ScrVersType)scrVersType);
                ZipArchiveEntry customVersEntry = archive.GetEntry("custom.vrs");
                if (customVersEntry != null)
                {
                    var guid = (string)settingsDoc.Root.Element("Guid");
                    string versName = ((ScrVersType)scrVersType).ToString() + "-" + guid;
                    if (Scripture.Versification.Table.Implementation.Exists(versName))
                    {
                        Versification = new ScrVers(versName);
                    }
                    else
                    {
                        using (var reader = new StreamReader(customVersEntry.Open()))
                        {
                            Versification = Scripture.Versification.Table.Implementation.Load(
                                reader,
                                "custom.vrs",
                                Versification,
                                versName
                            );
                        }
                    }
                }

                var stylesheetName = (string)settingsDoc.Root.Element("StyleSheet") ?? "usfm.sty";
                ZipArchiveEntry stylesheetEntry = archive.GetEntry(stylesheetName);
                if (stylesheetEntry == null && stylesheetName != "usfm_sb.sty")
                    stylesheetEntry = archive.GetEntry("usfm.sty");
                ZipArchiveEntry customStylesheetEntry = archive.GetEntry("custom.sty");

                UsfmStylesheet stylesheet;
                using (var stylesheetTempFile = TempFile.CreateAndGetPathButDontMakeTheFile())
                using (var customStylesheetTempFile = TempFile.CreateAndGetPathButDontMakeTheFile())
                {
                    string stylesheetPath = "usfm.sty";
                    if (stylesheetEntry != null)
                    {
                        stylesheetEntry.ExtractToFile(stylesheetTempFile.Path);
                        stylesheetPath = stylesheetTempFile.Path;
                    }
                    string customStylesheetPath = null;
                    if (customStylesheetEntry != null)
                    {
                        customStylesheetEntry.ExtractToFile(customStylesheetTempFile.Path);
                        customStylesheetPath = customStylesheetTempFile.Path;
                    }
                    stylesheet = new UsfmStylesheet(stylesheetPath, customStylesheetPath);
                }

                string prefix = "";
                string suffix = ".SFM";
                XElement namingElem = settingsDoc.Root.Element("Naming");
                if (namingElem != null)
                {
                    var prePart = (string)namingElem.Attribute("PrePart");
                    if (!string.IsNullOrEmpty(prePart))
                        prefix = prePart;
                    var postPart = (string)namingElem.Attribute("PostPart");
                    if (!string.IsNullOrEmpty(postPart))
                        suffix = postPart;
                }

                var regex = new Regex($"^{Regex.Escape(prefix)}.*{Regex.Escape(suffix)}$");

                foreach (ZipArchiveEntry sfmEntry in archive.Entries.Where(e => regex.IsMatch(e.FullName)))
                {
                    AddText(
                        new UsfmZipText(
                            stylesheet,
                            encoding,
                            fileName,
                            sfmEntry.FullName,
                            Versification,
                            includeMarkers
                        )
                    );
                }
            }
        }

        public override ScrVers Versification { get; }
    }
}
