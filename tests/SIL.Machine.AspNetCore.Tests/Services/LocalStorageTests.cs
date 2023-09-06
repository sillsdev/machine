namespace SIL.Machine.AspNetCore.Services;

[TestFixture]
public class LocalStorageTests
{
    [Test]
    public async Task ExistsAsync()
    {
        using var tmpDir = new TempDirectory("test");
        using LocalStorage fs = new(tmpDir.Path);
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
        using var tmpDir = new TempDirectory("test");
        using LocalStorage fs = new(tmpDir.Path);
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
        using var tmpDir = new TempDirectory("test");
        using LocalStorage fs = new(tmpDir.Path);
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
        using var tmpDir = new TempDirectory("test");
        using LocalStorage fs = new(tmpDir.Path);
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
    public async Task DeleteFileAsync()
    {
        using var tmpDir = new TempDirectory("test");
        using LocalStorage fs = new(tmpDir.Path);
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
        IReadOnlyCollection<string> files = await fs.ListFilesAsync("test", recurse: true);
        Assert.That(files, Is.Empty);
    }
}
