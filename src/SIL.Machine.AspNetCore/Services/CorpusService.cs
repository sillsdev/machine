namespace SIL.Machine.AspNetCore.Services;

public class CorpusService : ICorpusService
{
    public IEnumerable<ITextCorpus> CreateTextCorpora(IReadOnlyList<CorpusFile> files)
    {
        List<ITextCorpus> corpora = [];

        List<Dictionary<string, IText>> textFileCorpora = [];
        foreach (CorpusFile file in files)
        {
            switch (file.Format)
            {
                case FileFormat.Text:
                    // if there are multiple texts with the same id, then add it to a new corpus or the first
                    // corpus that doesn't contain a text with that id
                    Dictionary<string, IText>? corpus = textFileCorpora.FirstOrDefault(c =>
                        !c.ContainsKey(file.TextId)
                    );
                    if (corpus is null)
                    {
                        corpus = [];
                        textFileCorpora.Add(corpus);
                    }
                    corpus[file.TextId] = new TextFileText(file.TextId, file.Location);
                    break;

                case FileFormat.Paratext:
                    corpora.Add(new ParatextBackupTextCorpus(file.Location, includeAllText: true));
                    break;
            }
        }
        foreach (Dictionary<string, IText> corpus in textFileCorpora)
            corpora.Add(new DictionaryTextCorpus(corpus.Values));

        return corpora;
    }

    public IEnumerable<ITextCorpus> CreateTermCorpora(IReadOnlyList<CorpusFile> files)
    {
        foreach (CorpusFile file in files)
        {
            switch (file.Format)
            {
                case FileFormat.Paratext:
                    yield return new ParatextBackupTermsCorpus(file.Location, ["PN"]);
                    break;
            }
        }
    }
}
