#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>Single authority for the references and identity used by the C# census.</summary>
internal sealed class CSharpCompilationProfile
{
    private readonly IReadOnlyList<ReferenceIdentity> _references;

    private CSharpCompilationProfile(IReadOnlyList<ReferenceIdentity> references, IReadOnlyList<string> unresolved)
    {
        _references = references;
        _unresolvedReferences = unresolved;
    }

    private readonly IReadOnlyList<string> _unresolvedReferences;

    public static CSharpCompilationProfile Create() => Create(Array.Empty<string>());

    /// <summary>Builds the census reference set, omitting every project in <paramref name="completeProjects"/>.</summary>
    /// <remarks>A project whose sources are compiled in full must not also be referenced as a built
    /// assembly, or every type it declares exists twice. A partially censused project must keep its
    /// reference, because the files the source set omits are only there.</remarks>
    public static CSharpCompilationProfile Create(IReadOnlyCollection<string> completeProjects)
    {
        ArgumentNullException.ThrowIfNull(completeProjects);
        var censusedAssemblies = new HashSet<string>(completeProjects, StringComparer.OrdinalIgnoreCase);
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var assemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unresolved = new SortedSet<string>(StringComparer.Ordinal);
        var queue = new Queue<Assembly>(new[]
        {
            typeof(object).Assembly,
            typeof(System.Xml.Linq.XElement).Assembly,
            typeof(XmlLanguageLoader).Assembly,
            typeof(SIL.Machine.Rules.IRule<,>).Assembly,
            typeof(CSharpInventoryReader).Assembly,
        });

        while (queue.Count != 0)
        {
            Assembly assembly = queue.Dequeue();
            string identity = assembly.FullName ?? assembly.GetName().Name ?? "<unknown>";
            if (!assemblies.Add(identity))
                continue;

            if (!string.IsNullOrEmpty(assembly.Location) && File.Exists(assembly.Location)
                && !censusedAssemblies.Contains(assembly.GetName().Name ?? string.Empty))
            {
                paths.Add(Path.GetFullPath(assembly.Location));
            }

            foreach (AssemblyName name in assembly.GetReferencedAssemblies())
            {
                try
                {
                    queue.Enqueue(Assembly.Load(name));
                }
                catch (FileNotFoundException) { unresolved.Add(name.FullName ?? name.Name ?? "<unknown>"); }
                catch (FileLoadException) { unresolved.Add(name.FullName ?? name.Name ?? "<unknown>"); }
                catch (BadImageFormatException) { unresolved.Add(name.FullName ?? name.Name ?? "<unknown>"); }
            }
        }

        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trustedPlatformAssemblies)
        {
            foreach (string path in trustedPlatformAssemblies.Split(Path.PathSeparator))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                if (File.Exists(path) && IsFrameworkReference(name))
                    paths.Add(Path.GetFullPath(path));
            }
        }

        var references = paths
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(ReferenceIdentity.Read)
            .OrderBy(reference => reference.Identity, StringComparer.Ordinal)
            .ThenBy(reference => reference.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (references.Length == 0)
            throw new InvalidOperationException("The C# census compilation profile contains no metadata references.");
        return new CSharpCompilationProfile(references, unresolved.ToArray());
    }

    public IEnumerable<MetadataReference> CreateMetadataReferences() =>
        _references.Select(reference => MetadataReference.CreateFromFile(reference.Path));

    public string Fingerprint()
    {
        var text = new StringBuilder()
            .Append("runtime=").Append(RuntimeInformation.FrameworkDescription).Append('\n')
            .Append("runtime-version=").Append(Environment.Version).Append('\n')
            .Append("compiler=").Append(typeof(Microsoft.CodeAnalysis.CSharp.CSharpCompilation).Assembly.FullName).Append('\n')
            .Append("analyzer-implementation=").Append(typeof(CSharpInventoryReader).Assembly.FullName)
            .Append("|mvid=").Append(ModuleVersionId(typeof(CSharpInventoryReader).Assembly)).Append('\n');
        foreach (ReferenceIdentity reference in _references)
            text.Append("metadata-reference=").Append(reference.Identity).Append("|mvid=").Append(reference.Mvid).Append('\n');
        foreach (string unresolved in _unresolvedReferences)
            text.Append("unresolved-reference=").Append(unresolved).Append('\n');
        return text.ToString();
    }

    private static bool IsFrameworkReference(string name) =>
        name is "mscorlib" or "netstandard" or "System.Private.CoreLib" ||
        name.StartsWith("System.", StringComparison.Ordinal) ||
        name.StartsWith("Microsoft.", StringComparison.Ordinal);

    private static Guid ModuleVersionId(Assembly assembly) => assembly.ManifestModule.ModuleVersionId;

    private sealed record ReferenceIdentity(string Path, string Identity, Guid Mvid)
    {
        public static ReferenceIdentity Read(string path)
        {
            AssemblyName assemblyName;
            Guid mvid;
            try
            {
                assemblyName = AssemblyName.GetAssemblyName(path);
                using var stream = File.OpenRead(path);
                using var pe = new PEReader(stream);
                MetadataReader metadata = pe.GetMetadataReader();
                mvid = metadata.GetGuid(metadata.GetModuleDefinition().Mvid);
            }
            catch (Exception ex) when (ex is IOException or BadImageFormatException or UnauthorizedAccessException)
            {
                throw new InvalidOperationException($"Cannot inspect C# metadata reference '{System.IO.Path.GetFileName(path)}'.", ex);
            }

            return new ReferenceIdentity(path, assemblyName.FullName ?? assemblyName.Name ?? System.IO.Path.GetFileName(path), mvid);
        }
    }
}
