using NUnit.Framework;
using SIL.Machine.Utils;

namespace SIL.Machine.Tokenization.SentencePiece;

[TestFixture]
public class SentencePieceTokenizerTests
{
    private static string TestFilename => Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "data", "test.txt");

    private string _tempDir;

    private string ModelFilename => Path.Combine(_tempDir, "sp.model");

    [OneTimeSetUp]
    public void CreateModel()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        DirectoryHelper.DeleteDirectoryRobust(_tempDir);
        Directory.CreateDirectory(_tempDir);
        var trainer = new SentencePieceTrainer { VocabSize = 100 };
        trainer.Train(TestFilename, Path.Combine(_tempDir, "sp"));
    }

    [OneTimeTearDown]
    public void DeleteModel()
    {
        DirectoryHelper.DeleteDirectoryRobust(_tempDir);
    }

    [Test]
    public void Tokenize()
    {
        using var processor = new SentencePieceTokenizer(ModelFilename);
        string[] tokens = processor.Tokenize("Other travelling salesmen live a life of luxury.").ToArray();
        Assert.That(tokens, Has.Length.EqualTo(30));
    }

    [Test]
    public void Tokenize_Empty()
    {
        using var processor = new SentencePieceTokenizer(ModelFilename);
        string[] tokens = processor.Tokenize("").ToArray();
        Assert.That(tokens, Is.Empty);
    }
}
