namespace SIL.Machine.WebApi.Services;

public class TextFileTextCorpusFactory : ITextCorpusFactory
{
	private readonly string _textFileDir;

	public TextFileTextCorpusFactory(IOptions<TextFileTextCorpusOptions> options)
	{
		_textFileDir = options.Value.TextFileDir;
	}

	public Task<ITextCorpus> CreateAsync(string engineId, TextCorpusType type)
	{
		var wordTokenizer = new LatinWordTokenizer();
		var texts = new List<IText>();
		string dir = "";
		switch (type)
		{
			case TextCorpusType.Source:
				dir = "source";
				break;
			case TextCorpusType.Target:
				dir = "target";
				break;
		}

		foreach (string file in Directory.EnumerateFiles(Path.Combine(_textFileDir, engineId, dir), "*.txt"))
		{
			var text = new TextFileText(wordTokenizer, $"{engineId}_{Path.GetFileNameWithoutExtension(file)}",
				file);
			texts.Add(text);
		}

		return Task.FromResult<ITextCorpus>(new DictionaryTextCorpus(texts));
	}
}
