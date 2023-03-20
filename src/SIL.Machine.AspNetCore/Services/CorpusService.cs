namespace SIL.Machine.AspNetCore.Services;

public class CorpusService : ICorpusService
{
    public ITextCorpus CreateTextCorpus(IReadOnlyList<CorpusFile> files)
    {
        if (files.Count == 1 && files[0].Format == FileFormat.Paratext)
            return new ParatextBackupTextCorpus(files[0].Location);

        var texts = new List<IText>();
        foreach (CorpusFile file in files)
        {
            switch (file.Format)
            {
                case FileFormat.Text:
                    texts.Add(new TextFileText(file.TextId, file.Location));
                    break;
            }
        }
        return new DictionaryTextCorpus(texts);
    }
}
