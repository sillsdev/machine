#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>Validates a fixture grammar against the pinned DTD with no external reach.</summary>
/// <remarks>The only admitted external subset is the exact system identifier
/// <c>HermitCrabInput.dtd</c>, remapped to the repository's canonical DTD whatever directory the
/// fixture lives in. Network access and filesystem fallback are refused rather than attempted.</remarks>
public static class GrammarValidation
{
    public const string AdmittedSystemId = "HermitCrabInput.dtd";

    /// <summary>Validates <paramref name="grammarPath"/>, returning every validation message.</summary>
    public static IReadOnlyList<string> Validate(string grammarPath, string dtdPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(grammarPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(dtdPath);

        var messages = new List<string>();
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Parse,
            ValidationType = ValidationType.DTD,
            XmlResolver = new PinnedDtdResolver(Path.GetFullPath(dtdPath)),
        };
        settings.ValidationEventHandler += (_, args) => messages.Add($"{args.Severity}: {args.Message}");

        // The document is opened here so the resolver is consulted only for the DTD, never for the
        // grammar's own URI.
        using var grammar = new FileStream(grammarPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using XmlReader reader = XmlReader.Create(
            grammar,
            settings,
            new Uri(Path.GetFullPath(grammarPath)).AbsoluteUri
        );
        while (reader.Read())
        {
            // Validation is reported through the event handler; the read loop only drives it.
        }

        return messages;
    }

    private sealed class PinnedDtdResolver(string dtdPath) : XmlResolver
    {
        private readonly string _dtdPath = dtdPath;

        // Called for the document's own base URI as well as for external subsets, so admission is
        // decided in GetEntity, which is where content would actually be opened.
        public override Uri ResolveUri(Uri? baseUri, string? relativeUri) =>
            string.Equals(relativeUri, AdmittedSystemId, StringComparison.Ordinal)
                ? new Uri(_dtdPath)
                : base.ResolveUri(baseUri, relativeUri);

        public override object GetEntity(Uri absoluteUri, string? role, Type? typeOfObjectToReturn)
        {
            ArgumentNullException.ThrowIfNull(absoluteUri);
            if (
                !absoluteUri.IsFile
                || !string.Equals(Path.GetFullPath(absoluteUri.LocalPath), _dtdPath, StringComparison.OrdinalIgnoreCase)
            )
            {
                throw new XmlException(
                    $"External entity '{absoluteUri}' is not admitted; only '{AdmittedSystemId}' may be referenced."
                );
            }

            return new FileStream(_dtdPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
    }
}
