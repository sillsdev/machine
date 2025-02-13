using System;
using System.IO;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public abstract class VersificationTableBase : Versification.Table
    {
        protected abstract string ProjectName { get; }

        protected override Versification Get(string versName)
        {
            if (!Exists(versName))
            {
                LoadVersification(ref versName);
            }

            return base.Get(versName);
        }

        public override bool VersificationFileExists(string versName)
        {
            string[] parts = versName.Split(new[] { '-' }, 2);
            if (parts.Length == 1)
                return base.VersificationFileExists(versName); // Not a custom versification
            return FileExists("custom.vrs");
        }

        private void LoadVersification(ref string versName)
        {
            string[] parts = versName.Split(new[] { '-' }, 2);
            if (parts.Length > 1)
            {
                bool isValidVersType = Enum.TryParse(parts[0], out ScrVersType versType);
                if (!isValidVersType || versType == ScrVersType.Unknown)
                    versType = ScrVersType.English;

                ScrVers baseVers = new ScrVers(versType);
                if (!FileExists("custom.vrs"))
                {
                    versName = parts[0];
                }
                else
                {
                    using (Stream stream = OpenFile("custom.vrs"))
                    {
                        Load(
                            new StreamReader(stream),
                            ProjectName != null ? Path.Combine(ProjectName, "custom.vrs") : null,
                            baseVers,
                            versName
                        );
                    }
                }
            }
        }

        protected abstract bool FileExists(string fileName);
        protected abstract Stream OpenFile(string fileName);
    }
}
