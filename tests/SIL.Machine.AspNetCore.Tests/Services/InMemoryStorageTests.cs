namespace SIL.Machine.AspNetCore.Services;

[TestFixture]
public class InMemoryStorageTests
{
    [Test]
    public async Task ExistsAsync()
    {
        using InMemoryStorage fs = new();
        using (StreamWriter sw = new(await fs.OpenWriteAsync("file1")))
        {
            string input = "Hello";
            sw.WriteLine(input);
        }
        bool exists = await fs.ExistsAsync("file1");
        Assert.That(exists, Is.True);
    }

    [Test]
    public async Task OpenReadAsync()
    {
        using InMemoryStorage fs = new();
        string input;
        using (StreamWriter sw = new(await fs.OpenWriteAsync("file1")))
        {
            input = "Hello";
            sw.WriteLine(input);
        }
        string? output;
        using (StreamReader sr = new(await fs.OpenReadAsync("file1")))
        {
            output = sr.ReadLine();
        }
        Assert.That(input, Is.EqualTo(output), $"{input} | {output}");
    }

    [Test]
    public async Task ListFilesAsync_Recurse()
    {
        using InMemoryStorage fs = new();
        using (StreamWriter sw = new(await fs.OpenWriteAsync("test/file1")))
        {
            string input = "Hello";
            sw.WriteLine(input);
        }
        using (StreamWriter sw = new(await fs.OpenWriteAsync("test/test/file2")))
        {
            string input2 = "Hola";
            sw.WriteLine(input2);
        }
        IReadOnlyCollection<string> files = await fs.ListFilesAsync("test", recurse: true);
        Assert.That(files, Is.EquivalentTo(new[] { "test/file1", "test/test/file2" }));
    }

    [Test]
    public async Task ListFilesAsync_DoNotRecurse()
    {
        using InMemoryStorage fs = new();
        using (StreamWriter sw = new(await fs.OpenWriteAsync("test/file1")))
        {
            string input = "Hello";
            sw.WriteLine(input);
        }
        using (StreamWriter sw = new(await fs.OpenWriteAsync("test/test/file2")))
        {
            string input2 = "Hola";
            sw.WriteLine(input2);
        }
        IReadOnlyCollection<string> files = await fs.ListFilesAsync("test", recurse: false);
        Assert.That(files, Is.EquivalentTo(new[] { "test/file1" }));
    }

    [Test]
    public async Task DeleteAsync()
    {
        using InMemoryStorage fs = new();
        using (StreamWriter sw = new(await fs.OpenWriteAsync("test/file1")))
        {
            string input = "Hello";
            sw.WriteLine(input);
        }
        using (StreamWriter sw = new(await fs.OpenWriteAsync("test/test/file2")))
        {
            string input2 = "Hola";
            sw.WriteLine(input2);
        }
        await fs.DeleteAsync("test", recurse: true);
        var files = await fs.ListFilesAsync("test", recurse: true);
        Assert.That(files, Is.Empty);
    }
}
