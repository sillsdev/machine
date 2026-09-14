using System.IO.Compression;
using NUnit.Framework;

namespace SIL.Machine.Utils;

[TestFixture]
public class ZipArchiveEntryExtensionTests
{
    [Test]
    public async Task OpenBoundedStream_ReturnsReadableStream()
    {
        byte[] contentBytes = [.. "Hello World"u8];
        byte[] zipBytes = await CreateInMemoryZipFileAsync("test.txt", contentBytes);
        using var memoryStream = new MemoryStream(zipBytes);
        await using var archive = new ZipArchive(memoryStream);
        ZipArchiveEntry? entry = archive.GetEntry("test.txt");

        // SUT
        await using BoundedStream stream = entry.OpenBoundedStream(maxUncompressedSize: 100);
        using var reader = new StreamReader(stream);
        string content = await reader.ReadToEndAsync();

        Assert.That(content, Is.EqualTo("Hello World"));
    }

    [Test]
    public async Task OpenBoundedStream_ThrowsInvalidDataExceptionWhenHeaderSizeExceeded()
    {
        byte[] payload = new byte[200];
        byte[] zipBytes = await CreateInMemoryZipFileAsync("large.txt", payload);
        using var memoryStream = new MemoryStream(zipBytes);
        await using var archive = new ZipArchive(memoryStream);
        ZipArchiveEntry? entry = archive.GetEntry("large.txt");

        // SUT
        Assert.Throws<InvalidDataException>(() => entry.OpenBoundedStream(maxUncompressedSize: 100));
    }

    [Test]
    public async Task OpenBoundedStream_ThrowsInvalidDataExceptionWhenCompressionRatioExceeded()
    {
        byte[] highlyCompressibleData = new byte[10_000];
        byte[] zipBytes = await CreateInMemoryZipFileAsync(
            "bomb.txt",
            highlyCompressibleData,
            CompressionLevel.SmallestSize
        );
        using var memoryStream = new MemoryStream(zipBytes);
        await using var archive = new ZipArchive(memoryStream);
        ZipArchiveEntry? entry = archive.GetEntry("bomb.txt");

        // SUT
        Assert.Throws<InvalidDataException>(() =>
            entry.OpenBoundedStream(maxUncompressedSize: 20_000, maxCompressionRatio: 2.0)
        );
    }

    [Test]
    public void BoundedStream_ThrowsIOExceptionWhenRuntimeExpansionExceedsLimit()
    {
        byte[] rawData = [.. "1234567890"u8];
        using var memoryStream = new MemoryStream(rawData);
        using var boundedStream = new BoundedStream(memoryStream, maxSize: 5);

        byte[] buffer = new byte[10];

        // SUT
        boundedStream.ReadExactly(buffer, 0, 4);
        Assert.Throws<IOException>(() => boundedStream.ReadExactly(buffer, 0, 4));
    }

    private static async Task<byte[]> CreateInMemoryZipFileAsync(
        string fileName,
        ReadOnlyMemory<byte> content,
        CompressionLevel level = CompressionLevel.Fastest,
        CancellationToken cancellationToken = default
    )
    {
        using var ms = new MemoryStream();
        await using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            ZipArchiveEntry entry = archive.CreateEntry(fileName, level);
            await using Stream entryStream = await entry.OpenAsync(cancellationToken);
            await entryStream.WriteAsync(content, cancellationToken);
        }
        return ms.ToArray();
    }
}
