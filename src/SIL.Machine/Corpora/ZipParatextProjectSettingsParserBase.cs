using System.IO;
using SIL.IO;

namespace SIL.Machine.Corpora
{
    public abstract class ZipParatextProjectSettingsParserBase : ParatextProjectSettingsParserBase
    {
        protected override UsfmStylesheet CreateStylesheet(string fileName)
        {
            TempFile stylesheetTempFile = null;
            TempFile customStylesheetTempFile = null;
            try
            {
                string stylesheetPath = fileName;
                if (Exists(fileName))
                {
                    stylesheetTempFile = TempFile.CreateAndGetPathButDontMakeTheFile();
                    using (Stream source = Open(fileName))
                    using (Stream target = File.OpenWrite(stylesheetTempFile.Path))
                    {
                        source.CopyTo(target);
                    }
                    stylesheetPath = stylesheetTempFile.Path;
                }

                string customStylesheetPath = null;
                if (Exists("custom.sty"))
                {
                    customStylesheetTempFile = TempFile.CreateAndGetPathButDontMakeTheFile();
                    using (Stream source = Open("custom.sty"))
                    using (Stream target = File.OpenWrite(customStylesheetTempFile.Path))
                    {
                        source.CopyTo(target);
                    }
                    customStylesheetPath = customStylesheetTempFile.Path;
                }
                return new UsfmStylesheet(stylesheetPath, customStylesheetPath);
            }
            finally
            {
                stylesheetTempFile?.Dispose();
                customStylesheetTempFile?.Dispose();
            }
        }
    }
}
