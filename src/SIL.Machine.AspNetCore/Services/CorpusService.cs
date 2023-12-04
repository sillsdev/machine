namespace SIL.Machine.AspNetCore.Services;

public enum CorpusType
{
    Text,
    Term
}

public class CorpusService : ICorpusService
{
    public IDictionary<CorpusType, ITextCorpus> CreateTextCorpus(IReadOnlyList<CorpusFile> files)
    {
        IDictionary<CorpusType, ITextCorpus> corpora = new Dictionary<CorpusType, ITextCorpus>();
        if (files.Count == 1 && files[0].Format == FileFormat.Paratext)
        {
            corpora[CorpusType.Text] = new ParatextBackupTextCorpus(files[0].Location);
            corpora[CorpusType.Term] = new ParatextBackupTermsCorpus(files[0].Location);
        }
        else
        {
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
            corpora[CorpusType.Text] = new DictionaryTextCorpus(texts);
        }
        return corpora;
    }
}
