namespace SIL.Machine.AspNetCore.Services;

[TestFixture]
public class FileStorageTests
{
    [Test]
    public async Task ExistsFileInMemoryAsync()
    {
        using InMemoryStorage fs = new InMemoryStorage();
        Stream ws = await fs.OpenWrite("file1");
        StreamWriter sw = new(ws);
        string input = "Hello";
        sw.WriteLine(input);
        sw.Dispose();
        bool exists = await fs.Exists("file1");
        Assert.True(exists);
    }

    [Test]
    public async Task CreateFileReadFileInMemoryAsync()
    {
        using InMemoryStorage fs = new InMemoryStorage();
        Stream ws = await fs.OpenWrite("file1");
        StreamWriter sw = new(ws);
        string input = "Hello";
        sw.WriteLine(input);
        sw.Dispose();
        Stream rs = await fs.OpenRead("file1");
        StreamReader sr = new(rs);
        string? output = sr.ReadLine();
        sr.Dispose();
        Assert.That(input, Is.EqualTo(output), $"{input} | {output}");
    }

    [Test]
    public async Task CreateFilesListFilesRecursiveInMemoryAsync()
    {
        using InMemoryStorage fs = new InMemoryStorage();
        Stream ws = await fs.OpenWrite("test/file1");
        StreamWriter sw = new(ws);
        string input = "Hello";
        sw.WriteLine(input);
        sw.Dispose();
        ws = await fs.OpenWrite("test/test/file2");
        sw = new(ws);
        string input2 = "Hola";
        sw.WriteLine(input2);
        sw.Dispose();
        var files = await fs.Ls("test", recurse: true);
        Assert.That(files.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task CreateFilesListFilesNotRecursiveInMemoryAsync()
    {
        using InMemoryStorage fs = new InMemoryStorage();
        Stream ws = await fs.OpenWrite("test/file1");
        StreamWriter sw = new(ws);
        string input = "Hello";
        sw.WriteLine(input);
        sw.Dispose();
        ws = await fs.OpenWrite("test/test/file2");
        sw = new(ws);
        string input2 = "Hola";
        sw.WriteLine(input2);
        sw.Dispose();
        var files = await fs.Ls("test", recurse: false);
        Assert.That(files.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task CreateFileRemoveFileInMemoryAsync()
    {
        using InMemoryStorage fs = new InMemoryStorage();
        Stream ws = await fs.OpenWrite("test/file1");
        StreamWriter sw = new(ws);
        string input = "Hello";
        sw.WriteLine(input);
        sw.Dispose();
        ws = await fs.OpenWrite("test/test/file2");
        sw = new(ws);
        string input2 = "Hola";
        sw.WriteLine(input2);
        sw.Dispose();
        await fs.Rm("test", recurse: true);
        var files = await fs.Ls("test", recurse: true);
        Assert.That(files.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task ExistsFileLocalAsync()
    {
        var tmpDir = new TempDirectory("test");
        using FileStorage fs = new LocalStorage(tmpDir.Path);
        Stream ws = await fs.OpenWrite("file1");
        StreamWriter sw = new(ws);
        string input = "Hello";
        sw.WriteLine(input);
        sw.Dispose();
        bool exists = await fs.Exists("file1");
        Assert.True(exists);
    }

    [Test]
    public async Task CreateFileReadFileLocalAsync()
    {
        var tmpDir = new TempDirectory("test");
        using FileStorage fs = new LocalStorage(tmpDir.Path);
        Stream ws = await fs.OpenWrite("file1");
        StreamWriter sw = new(ws);
        string input = "Hello";
        sw.WriteLine(input);
        sw.Dispose();
        Stream rs = await fs.OpenRead("file1");
        StreamReader sr = new(rs);
        string? output = sr.ReadLine();
        sr.Dispose();
        Assert.That(input, Is.EqualTo(output), $"{input} | {output}");
    }

    [Test]
    public async Task CreateFilesListFilesRecursiveLocalAsync()
    {
        var tmpDir = new TempDirectory("test");
        using FileStorage fs = new LocalStorage(tmpDir.Path);
        Stream ws = await fs.OpenWrite("test/file1");
        StreamWriter sw = new(ws);
        string input = "Hello";
        sw.WriteLine(input);
        sw.Dispose();
        ws = await fs.OpenWrite("test/test/file2");
        sw = new(ws);
        string input2 = "Hola";
        sw.WriteLine(input2);
        sw.Dispose();
        var files = await fs.Ls("test", recurse: true);
        Assert.That(files.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task CreateFilesListFilesNotRecursiveLocalAsync()
    {
        var tmpDir = new TempDirectory("test");
        using FileStorage fs = new LocalStorage(tmpDir.Path);
        Stream ws = await fs.OpenWrite("test/file1");
        StreamWriter sw = new(ws);
        string input = "Hello";
        sw.WriteLine(input);
        sw.Dispose();
        ws = await fs.OpenWrite("test/test/file2");
        sw = new(ws);
        string input2 = "Hola";
        sw.WriteLine(input2);
        sw.Dispose();
        var files = await fs.Ls("test", recurse: false);
        Assert.That(files.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task CreateFileRemoveFileLocalAsync()
    {
        var tmpDir = new TempDirectory("test");
        using FileStorage fs = new LocalStorage(tmpDir.Path);
        Stream ws = await fs.OpenWrite("test/file1");
        StreamWriter sw = new(ws);
        string input = "Hello";
        sw.WriteLine(input);
        sw.Dispose();
        ws = await fs.OpenWrite("test/test/file2");
        sw = new(ws);
        string input2 = "Hola";
        sw.WriteLine(input2);
        sw.Dispose();
        await fs.Rm("test", recurse: true);
        var files = await fs.Ls("test", recurse: true);
        Assert.That(files.Count, Is.EqualTo(0));
    }
}
