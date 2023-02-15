namespace SIL.Machine.AspNetCore.Services;

public class S3FileStorage : DisposableBase, IFileStorage
{
    private readonly HttpClient _http;

    public S3FileStorage(Uri endpoint, DelegatingHandler authHandler)
    {
        _http = new HttpClient(authHandler) { BaseAddress = endpoint };
    }

    public async Task<bool> Exists(IOPath path, CancellationToken cancellationToken = default)
    {
        using Stream? s = await OpenRead(path, cancellationToken);
        return s != null;
    }

    public async Task<IReadOnlyCollection<IOEntry>> Ls(
        IOPath? path = null,
        bool recurse = false,
        CancellationToken cancellationToken = default
    )
    {
        if (path != null && !path.IsFolder)
            throw new ArgumentException("path needs to be a folder", nameof(path));

        string? delimiter = recurse ? null : "/";
        string? prefix = IOPath.IsRoot(path) ? null : path?.NLWTS;

        // call https://docs.aws.amazon.com/AmazonS3/latest/API/API_ListObjectsV2.html
        string uri = "/?list-type=2";
        if (delimiter != null)
            uri += "&delimiter=" + delimiter;
        if (prefix != null)
            uri += "&prefix=" + prefix;

        HttpResponseMessage response = await _http.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, uri),
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        string xml = await response.Content.ReadAsStringAsync(cancellationToken);

        List<IOEntry> result = ParseListObjectV2Response(xml, out _).ToList();

        if (recurse)
            AssumeImplicitFolders(path, result);

        return result;
    }

    public async Task<Stream?> OpenRead(IOPath path, CancellationToken cancellationToken = default)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));

        // call https://docs.aws.amazon.com/AmazonS3/latest/API/API_GetObject.html
        HttpResponseMessage response = await _http.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, $"/{IOPath.Normalize(path, true)}"),
            cancellationToken
        );

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    public async Task<Stream> OpenWrite(IOPath path, CancellationToken cancellationToken = default)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));

        string npath = IOPath.Normalize(path, true);

        // initiate upload and get upload ID
        var request = new HttpRequestMessage(HttpMethod.Post, $"/{npath}?uploads");
        HttpResponseMessage response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        string xml = await response.Content.ReadAsStringAsync(cancellationToken); // this contains UploadId
        string uploadId = ParseInitiateMultipartUploadResponse(xml);

        return new BufferedStream(new S3WriteStream(this, npath, uploadId), 1024 * 1024 * 5);
    }

    public async Task<T?> ReadAsJson<T>(IOPath path, CancellationToken cancellationToken = default)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));

        string? json = await ReadText(path, null, cancellationToken);
        if (json == null)
            return default;

        return JsonSerializer.Deserialize<T>(json);
    }

    public async Task<string?> ReadText(
        IOPath path,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default
    )
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));

        if (!path.IsFile)
            throw new ArgumentException($"{nameof(path)} needs to be a file", nameof(path));

        Stream? src = await OpenRead(path, cancellationToken).ConfigureAwait(false);
        if (src == null)
            return null;

        var ms = new MemoryStream();
        using (src)
        {
            await src.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        }

        return (encoding ?? Encoding.UTF8).GetString(ms.ToArray());
    }

    public async Task Ren(IOPath oldPath, IOPath newPath, CancellationToken cancellationToken = default)
    {
        if (oldPath is null)
            throw new ArgumentNullException(nameof(oldPath));
        if (newPath is null)
            throw new ArgumentNullException(nameof(newPath));

        // when file moves to a folder
        if (oldPath.IsFile && newPath.IsFolder)
        {
            // now it's a file-to-file rename
            throw new NotImplementedException();
        }
        else if (oldPath.IsFolder && newPath.IsFile)
        {
            throw new ArgumentException($"attempted to rename folder to file", nameof(newPath));
        }
        else if (oldPath.IsFolder)
        {
            // folder-to-folder ren
            throw new NotImplementedException();
        }
        else
        {
            // file-to-file ren
            await RenFile(oldPath, newPath, cancellationToken);
        }
    }

    public async Task Rm(IOPath path, bool recurse = false, CancellationToken cancellationToken = default)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));

        // call https://docs.aws.amazon.com/AmazonS3/latest/API/API_DeleteObject.html
        (
            await _http.SendAsync(new HttpRequestMessage(HttpMethod.Delete, path.NLS), cancellationToken)
        ).EnsureSuccessStatusCode();
    }

    public async Task WriteAsJson(IOPath path, object value, CancellationToken cancellationToken = default)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        string json = JsonSerializer.Serialize(value);
        await WriteText(path, json, null, cancellationToken);
    }

    public async Task WriteText(
        IOPath path,
        string contents,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default
    )
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));

        if (contents is null)
            throw new ArgumentNullException(nameof(contents));

        if (!path.IsFile)
            throw new ArgumentException($"{nameof(path)} needs to be a file", nameof(path));

        using Stream ws = await OpenWrite(path, cancellationToken);
        Stream rs = new MemoryStream((encoding ?? Encoding.UTF8).GetBytes(contents));
        await rs.CopyToAsync(ws, cancellationToken);
    }

    public string UploadPart(string key, string uploadId, int partNumber, byte[] buffer, int count)
    {
        HttpResponseMessage response = _http.Send(CreateUploadPartRequest(key, uploadId, partNumber, buffer, count));
        response.EnsureSuccessStatusCode();
        return response.Headers.GetValues("ETag").First();
    }

    public async Task<string> UploadPartAsync(string key, string uploadId, int partNumber, byte[] buffer, int count)
    {
        HttpResponseMessage response = await _http.SendAsync(
            CreateUploadPartRequest(key, uploadId, partNumber, buffer, count)
        );
        response.EnsureSuccessStatusCode();
        return response.Headers.GetValues("ETag").First();
    }

    public void CompleteMultipartUpload(string key, string uploadId, IEnumerable<string> partTags)
    {
        HttpResponseMessage msg = _http.Send(CreateCompleteMultipartUploadRequest(key, uploadId, partTags));
        if (!msg.IsSuccessStatusCode)
        {
            string body = msg.Content.ReadAsStringAsync().Result;
            Console.WriteLine(body);
        }
    }

    public async Task CompleteMultipartUploadAsync(string key, string uploadId, IEnumerable<string> partTags)
    {
        (
            await _http.SendAsync(CreateCompleteMultipartUploadRequest(key, uploadId, partTags))
        ).EnsureSuccessStatusCode();
    }

    // https://docs.aws.amazon.com/AmazonS3/latest/API/API_UploadPart.html
    private HttpRequestMessage CreateUploadPartRequest(
        string key,
        string uploadId,
        int partNumber,
        byte[] buffer,
        int count
    )
    {
        return new HttpRequestMessage(HttpMethod.Put, $"/{key}?partNumber={partNumber}&uploadId={uploadId}")
        {
            Content = new ByteArrayContent(buffer, 0, count)
        };
    }

    //https://docs.aws.amazon.com/AmazonS3/latest/API/API_CompleteMultipartUpload.html
    private HttpRequestMessage CreateCompleteMultipartUploadRequest(
        string key,
        string uploadId,
        IEnumerable<string> partTags
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/{key}?uploadId={uploadId}");

        var sb = new StringBuilder(
            @"<?xml version=""1.0"" encoding=""UTF-8""?><CompleteMultipartUpload xmlns=""http://s3.amazonaws.com/doc/2006-03-01/"">"
        );
        int partId = 1;
        foreach (string eTag in partTags)
        {
            sb.Append("<Part><ETag>")
                .Append(eTag)
                .Append("</ETag><PartNumber>")
                .Append(partId++)
                .Append("</PartNumber></Part>");
        }
        sb.Append("</CompleteMultipartUpload>");
        request.Content = new StringContent(sb.ToString());
        return request;
    }

    /// <summary>
    /// Parses out XML response. See specs at https://docs.aws.amazon.com/AmazonS3/latest/API/API_ListObjectsV2.html
    /// </summary>
    /// <param name="xml"></param>
    /// <param name="continuationToken"></param>
    /// <returns></returns>
    private static IReadOnlyCollection<IOEntry> ParseListObjectV2Response(string xml, out string? continuationToken)
    {
        continuationToken = null;
        var result = new List<IOEntry>();
        using (var sr = new StringReader(xml))
        {
            using var xr = XmlReader.Create(sr);
            string? en = null;

            while (xr.Read())
            {
                if (xr.NodeType == XmlNodeType.Element)
                {
                    switch (xr.Name)
                    {
                        case "Contents":
                            string? key = null;
                            string? lastMod = null;
                            string? eTag = null;
                            string? size = null;
                            string? storageClass = null;
                            // read all the elements in this
                            while (xr.Read() && !(xr.NodeType == XmlNodeType.EndElement && xr.Name == "Contents"))
                            {
                                if (xr.NodeType == XmlNodeType.Element)
                                    en = xr.Name;
                                else if (xr.NodeType == XmlNodeType.Text)
                                {
                                    switch (en)
                                    {
                                        case "Key":
                                            key = xr.Value;
                                            break;
                                        case "LastModified":
                                            lastMod = xr.Value;
                                            break;
                                        case "ETag":
                                            eTag = xr.Value;
                                            break;
                                        case "Size":
                                            size = xr.Value;
                                            break;
                                        case "StorageClass":
                                            storageClass = xr.Value;
                                            break;
                                    }
                                }
                            }

                            if (key != null && lastMod != null && size != null)
                            {
                                var entry = new IOEntry(key)
                                {
                                    LastModificationTime = DateTimeOffset.Parse(lastMod),
                                    Size = int.Parse(size)
                                };
                                entry.TryAddProperties("ETag", eTag, "StorageClass", storageClass);
                                result.Add(entry);
                            }

                            break;
                        case "CommonPrefixes":
                            while (xr.Read() && !(xr.NodeType == XmlNodeType.EndElement && xr.Name == "CommonPrefixes"))
                            {
                                // <Prefix>foldername/</Prefix>
                                if (xr.NodeType == XmlNodeType.Element)
                                    en = xr.Name;
                                else if (xr.NodeType == XmlNodeType.Text)
                                {
                                    if (en == "Prefix")
                                    {
                                        result.Add(new IOEntry(xr.Value));
                                    }
                                }
                            }
                            break;
                        case "NextContinuationToken":
                            throw new NotImplementedException();
                    }
                }
            }
        }

        return result;
    }

    private static string ParseInitiateMultipartUploadResponse(string xml)
    {
        using var sr = new StringReader(xml);
        using var xr = XmlReader.Create(sr);
        while (xr.Read())
        {
            if (xr.NodeType == XmlNodeType.Element && xr.Name == "UploadId")
            {
                xr.Read();
                return xr.Value;
            }
        }

        throw new Exception("Invalid initiate multipart upload response.");
    }

    private static void AssumeImplicitFolders(string absoluteRoot, List<IOEntry> entries)
    {
        absoluteRoot = IOPath.Normalize(absoluteRoot);

        List<IOEntry> implicitFolders = entries
            .Select(b => b.Path.Full)
            .Select(p => p.Substring(absoluteRoot.Length))
            .Select(p => IOPath.GetParent(p))
            .Where(p => !IOPath.IsRoot(p))
            .Distinct()
            .Select(p => new IOEntry(p + "/"))
            .ToList();

        entries.InsertRange(0, implicitFolders);
    }

    private async Task RenFile(IOPath oldPath, IOPath newPath, CancellationToken cancellationToken = default)
    {
        using Stream? src = await OpenRead(oldPath, cancellationToken);
        if (src != null)
        {
            using (Stream dest = await OpenWrite(newPath, cancellationToken))
            {
                await src.CopyToAsync(dest, cancellationToken);
            }

            await Rm(oldPath, cancellationToken: cancellationToken);
        }
    }
}
