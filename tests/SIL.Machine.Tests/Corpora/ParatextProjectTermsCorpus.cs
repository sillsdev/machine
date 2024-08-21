namespace SIL.Machine.Corpora;

public class ParatextProjectTermsCorpus : DictionaryTextCorpus
{
    public ParatextProjectTermsCorpus(
        IDictionary<string, string> files,
        ParatextProjectSettings settings,
        IEnumerable<string> termCategories,
        bool useTermGlosses = true
    )
    {
        IEnumerable<(string, IEnumerable<string>)> glosses = new MemoryParatextTermsParser(files).Parse(
            settings,
            termCategories,
            useTermGlosses
        );
        string textId =
            $"{settings.BiblicalTermsListType}:{settings.BiblicalTermsProjectName}:{settings.BiblicalTermsFileName}";

        IText text = new MemoryText(
            textId,
            glosses.Select(kvp => new TextRow(textId, kvp.Item1) { Segment = kvp.Item2.ToList() })
        );
        AddText(text);
    }
}
