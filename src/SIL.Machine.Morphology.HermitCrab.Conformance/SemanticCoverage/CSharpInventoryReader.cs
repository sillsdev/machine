#pragma warning disable IDE0011, IDE0200
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

internal static class CSharpInventoryReader
{
    private const string Profile = "sil.machine.hc-semantic-catalog/v1";
    private const string XElementMetadata = "System.Xml.Linq.XElement";
    private const string XContainerMetadata = "System.Xml.Linq.XContainer";
    private const string XNameMetadata = "System.Xml.Linq.XName";
    private const string HcRuleMetadata = "SIL.Machine.Morphology.HermitCrab.IHCRule";
    private const string PhonRuleMetadata = "SIL.Machine.Morphology.HermitCrab.IPhonologicalRule";
    private const string MorphRuleMetadata = "SIL.Machine.Morphology.HermitCrab.IMorphologicalRule";
    private const string MachineRuleMetadata = "SIL.Machine.Rules.IRule`2";

    private static readonly HashSet<string> XmlNames = new(StringComparer.Ordinal)
    {
        "Element",
        "Elements",
        "Attribute",
    };
    private static readonly HashSet<SyntaxKind> DeclarationKinds = new()
    {
        SyntaxKind.ClassDeclaration,
        SyntaxKind.StructDeclaration,
        SyntaxKind.InterfaceDeclaration,
        SyntaxKind.RecordDeclaration,
        SyntaxKind.RecordStructDeclaration,
        SyntaxKind.EnumDeclaration,
        SyntaxKind.DelegateDeclaration,
        SyntaxKind.MethodDeclaration,
        SyntaxKind.ConstructorDeclaration,
        SyntaxKind.DestructorDeclaration,
        SyntaxKind.OperatorDeclaration,
        SyntaxKind.ConversionOperatorDeclaration,
        SyntaxKind.PropertyDeclaration,
        SyntaxKind.IndexerDeclaration,
        SyntaxKind.EventDeclaration,
        SyntaxKind.EventFieldDeclaration,
        SyntaxKind.FieldDeclaration,
        SyntaxKind.VariableDeclarator,
        SyntaxKind.GetAccessorDeclaration,
        SyntaxKind.SetAccessorDeclaration,
        SyntaxKind.AddAccessorDeclaration,
        SyntaxKind.RemoveAccessorDeclaration,
        SyntaxKind.InitAccessorDeclaration,
        SyntaxKind.EnumMemberDeclaration,
        SyntaxKind.LocalFunctionStatement,
    };

    public static SemanticInventory Read(IReadOnlyList<CSharpInventoryInput> inputs) =>
        Read(inputs, Array.Empty<string>());

    public static SemanticInventory Read(
        IReadOnlyList<CSharpInventoryInput> inputs,
        IReadOnlyCollection<string> completeProjects
    )
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(completeProjects);
        SourceInput[] sources = inputs
            .Select(input => input ?? throw new ArgumentException("C# source input cannot be null.", nameof(inputs)))
            .Select(input => new SourceInput(
                NormalizePath(input.RelativePath),
                NormalizeSource(input.SourceText),
                input.AuditedScopes
            ))
            .OrderBy(source => source.Path, StringComparer.Ordinal)
            .ToArray();
        string[] duplicates = sources
            .GroupBy(source => source.Path, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        if (duplicates.Length != 0)
            throw new ArgumentException($"Duplicate C# source paths: {string.Join(", ", duplicates)}", nameof(inputs));

        ValidateScopePatterns(sources);
        IReadOnlyList<CensusConfiguration> configurations = Configurations();
        CSharpCompilationProfile compilationProfile = CSharpCompilationProfile.Create(completeProjects);
        var units = new List<CensusUnit>(configurations.Count);
        foreach (CensusConfiguration configuration in configurations)
        {
            var parseOptions = CSharpParseOptions.Default.WithPreprocessorSymbols(configuration.Symbols);
            SyntaxTree[] trees = sources
                .Select(source => CSharpSyntaxTree.ParseText(source.Text, parseOptions, source.Path))
                .ToArray();
            foreach (SyntaxTree tree in trees)
            {
                Diagnostic? error = tree.GetDiagnostics()
                    .FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
                if (error is not null)
                    throw SyntaxError(error);
            }

            CSharpCompilation compilation = CSharpCompilation.Create(
                "HermitCrabSemanticCoverage_" + HashSources(sources)[..16],
                trees,
                compilationProfile.CreateMetadataReferences(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );
            units.Add(new CensusUnit(configuration, compilation, sources, trees));
        }

        return Census(units, configurations, sources, compilationProfile.Fingerprint(), completeProjects.Count != 0);
    }

    private static SemanticInventory Census(
        IReadOnlyList<CensusUnit> units,
        IReadOnlyList<CensusConfiguration> configurations,
        SourceInput[] allSources,
        string compilationFingerprint,
        bool referencesAreExact
    )
    {
        var known = new HashSet<string>(StringComparer.Ordinal);
        var models = new Dictionary<string, Availability<InventorySurface>>(StringComparer.Ordinal);
        var xml = new Dictionary<string, Availability<ResolvedXml>>(StringComparer.Ordinal);
        var decisions = new Dictionary<string, Availability<ResolvedDecision>>(StringComparer.Ordinal);
        var diagnostics = new Dictionary<string, Availability<InventoryDiagnostic>>(StringComparer.Ordinal);

        foreach (CensusUnit unit in units)
        {
            CensusConfiguration configuration = unit.Configuration;
            CSharpCompilation compilation = unit.Compilation;
            SourceInput[] sources = unit.Sources;
            var context = new SemanticContext(compilation, sources, unit.Trees, referencesAreExact);
            context.BuildDeclarations();
            known.UnionWith(context.KnownSymbolIds());
            Diagnostic[] semanticErrors = compilation
                .GetDiagnostics()
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .ToArray();
            if (semanticErrors.Any(context.IsActionableCompilationError))
                context.AddCompilationErrors(semanticErrors);
            else
                context.BuildExecutionClosure();

            var configured = new List<InventorySurface>();
            var configuredIds = new HashSet<string>(StringComparer.Ordinal);
            context.CollectModelSurfaces(configured, configuredIds);
            foreach (InventorySurface surface in configured)
                Merge(models, surface.Id, surface, configuration.Name);
            foreach (ResolvedXml candidate in context.CollectXmlCandidates())
                Merge(xml, candidate.Key, candidate, configuration.Name);
            foreach (ResolvedDecision candidate in context.CollectDecisionCandidates())
                Merge(decisions, candidate.Key, candidate, configuration.Name);
            foreach (InventoryDiagnostic diagnostic in context.Diagnostics)
            {
                string key = $"{diagnostic.Code}\0{diagnostic.SubjectId}\0{diagnostic.Message}\0{diagnostic.Location}";
                Merge(diagnostics, key, diagnostic, configuration.Name);
            }
        }

        ValidateScopesResolve(allSources, known);
        var surfaces = new List<InventorySurface>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (
            Availability<InventorySurface> entry in models.Values.OrderBy(item => item.Value.Id, StringComparer.Ordinal)
        )
            Add(surfaces, ids, entry.Value with { Configurations = entry.Names });
        EmitXml(surfaces, ids, xml.Values);
        EmitDecisions(surfaces, ids, decisions.Values);
        InventoryDiagnostic[] orderedDiagnostics = diagnostics
            .Values.OrderBy(entry => entry.Value.Code, StringComparer.Ordinal)
            .ThenBy(entry => entry.Value.SubjectId, StringComparer.Ordinal)
            .ThenBy(entry => entry.Value.Location, StringComparer.Ordinal)
            .ThenBy(entry => entry.Value.Message, StringComparer.Ordinal)
            .Select(entry => entry.Value with { Configurations = entry.Names })
            .ToArray();
        return new SemanticInventory(
            Profile,
            HashSources(allSources, configurations, compilationFingerprint, orderedDiagnostics),
            InventorySurfaceFactory.Sort(surfaces),
            orderedDiagnostics
        );
    }

    private sealed record CensusUnit(
        CensusConfiguration Configuration,
        CSharpCompilation Compilation,
        SourceInput[] Sources,
        SyntaxTree[] Trees
    );

    /// <summary>Censuses the compilations the pinned compiler actually produced.</summary>
    /// <remarks>Every node of <paramref name="graph"/> is already built from captured compiler
    /// inputs with owned projects bound as compilation references, so no reference set is
    /// approximated here and no error id is tolerated.</remarks>
    internal static SemanticInventory ReadFromGraph(
        RoslynCompilationGraph graph,
        RepositoryCompilationGraph captured,
        string repositoryRoot,
        IReadOnlyList<string> censusedProjectIds,
        IReadOnlyList<string> auditedScopes
    )
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(captured);
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentNullException.ThrowIfNull(censusedProjectIds);
        ArgumentNullException.ThrowIfNull(auditedScopes);
        if (censusedProjectIds.Count == 0)
            throw new ArgumentException("At least one censused project is required.", nameof(censusedProjectIds));
        if (auditedScopes.Count == 0)
            throw new ArgumentException("At least one audited scope is required.", nameof(auditedScopes));

        string root = System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(repositoryRoot));
        var configurations = new List<CensusConfiguration>();
        var units = new List<CensusUnit>();
        var allSources = new Dictionary<string, SourceInput>(StringComparer.Ordinal);

        foreach (BuildProfile profile in captured.Profiles)
        {
            CensusConfiguration configuration = ConfigurationFor(profile);
            configurations.Add(configuration);
            bool scopesAssigned = false;
            foreach (string projectId in censusedProjectIds)
            {
                RepositoryGraphNode node =
                    captured.Nodes.FirstOrDefault(item =>
                        StringComparer.Ordinal.Equals(item.ProjectId, projectId)
                        && StringComparer.Ordinal.Equals(item.Profile.Id, profile.Id)
                    )
                    ?? throw new ArgumentException(
                        $"The compilation graph has no node for project '{projectId}' in profile '{profile.Id}'.",
                        nameof(censusedProjectIds)
                    );

                CSharpCompilation compilation = graph[node.Key].Compilation;
                SyntaxTree[] trees = compilation.SyntaxTrees.ToArray();
                var sources = new SourceInput[trees.Length];
                for (int index = 0; index < trees.Length; index++)
                {
                    IReadOnlyList<string> scopes =
                        !scopesAssigned && index == 0 ? auditedScopes : Array.Empty<string>();
                    sources[index] = new SourceInput(
                        RepositoryRelativePath(trees[index].FilePath, root),
                        NormalizeSource(trees[index].GetText().ToString()),
                        scopes
                    );
                    allSources[sources[index].Path] = sources[index];
                }

                scopesAssigned = true;
                units.Add(new CensusUnit(configuration, compilation, sources, trees));
            }
        }

        SourceInput[] ordered = allSources.Values.OrderBy(source => source.Path, StringComparer.Ordinal).ToArray();
        ValidateScopePatterns(ordered);
        return Census(units, configurations, ordered, captured.Hashes.GraphHash, referencesAreExact: true);
    }

    private static CensusConfiguration ConfigurationFor(BuildProfile profile)
    {
        string[] symbols = profile.AdditionalSymbols.OrderBy(symbol => symbol, StringComparer.Ordinal).ToArray();
        return new CensusConfiguration(symbols.Length == 0 ? "base" : string.Join("+", symbols), symbols);
    }

    private static string RepositoryRelativePath(string path, string repositoryRoot)
    {
        string full = System.IO.Path.GetFullPath(path);
        return NormalizePath(
            full.StartsWith(repositoryRoot, StringComparison.OrdinalIgnoreCase)
                ? System.IO.Path.GetRelativePath(repositoryRoot, full)
                : full
        );
    }

    // HermitCrab compiles under these symbols, so a single-configuration census cannot
    // see a decision that exists only when one is defined.
    private static readonly string[] ConditionalSymbols = { "OUTPUT_ANALYSES", "SINGLE_THREADED" };

    private static IReadOnlyList<CensusConfiguration> Configurations()
    {
        var configurations = new List<CensusConfiguration>();
        for (int mask = 0; mask < 1 << ConditionalSymbols.Length; mask++)
        {
            string[] symbols = ConditionalSymbols
                .Where((_, index) => (mask & (1 << index)) != 0)
                .OrderBy(symbol => symbol, StringComparer.Ordinal)
                .ToArray();
            configurations.Add(
                new CensusConfiguration(symbols.Length == 0 ? "base" : string.Join("+", symbols), symbols)
            );
        }
        return configurations.OrderBy(configuration => configuration.Name, StringComparer.Ordinal).ToArray();
    }

    private static void ValidateScopePatterns(IEnumerable<SourceInput> sources)
    {
        foreach (SourceInput source in sources)
        foreach (string scope in source.Scopes)
            if (ScopeValidation.HasPattern(scope))
                throw new ArgumentException($"Audited scope '{scope}' must be exact; patterns are not allowed.");
    }

    // A scope naming a symbol that exists in only one configuration is still exact.
    private static void ValidateScopesResolve(IEnumerable<SourceInput> sources, HashSet<string> known)
    {
        foreach (SourceInput source in sources)
        foreach (string scope in source.Scopes)
            if (!known.Contains(scope))
                throw new ArgumentException($"{source.Path}: unknown audited source scope '{scope}'.");
    }

    private static void Add(List<InventorySurface> surfaces, HashSet<string> ids, InventorySurface surface)
    {
        if (!ids.Add(surface.Id))
            throw new InvalidOperationException($"Duplicate generated surface ID '{surface.Id}'.");
        surfaces.Add(surface);
    }

    private static void Merge<T>(Dictionary<string, Availability<T>> merged, string key, T value, string configuration)
    {
        if (merged.TryGetValue(key, out Availability<T>? existing))
            existing.Add(configuration);
        else
            merged.Add(key, new Availability<T>(value, configuration));
    }

    private static void EmitXml(
        List<InventorySurface> surfaces,
        HashSet<string> ids,
        IEnumerable<Availability<ResolvedXml>> entries
    )
    {
        var counters = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (
            Availability<ResolvedXml> entry in entries
                .OrderBy(item => item.Value.Parent, StringComparer.Ordinal)
                .ThenBy(item => item.Value.Method, StringComparer.Ordinal)
                .ThenBy(item => item.Value.Path, StringComparer.Ordinal)
                .ThenBy(item => item.Value.SpanStart)
        )
        {
            ResolvedXml candidate = entry.Value;
            string group = $"{candidate.Parent}\0{candidate.Method}";
            int ordinal = counters.TryGetValue(group, out int used) ? used : 0;
            counters[group] = ordinal + 1;
            string id = candidate.Kind switch
            {
                "xml-read" =>
                    $"loader:{candidate.Parent}/{candidate.Method}/{CanonicalIdCodec.Encode(candidate.Constant!)}#{ordinal}",
                "xml-all-elements" => $"loader:{candidate.Parent}/{candidate.Method}/xml-all-elements#{ordinal}",
                "dynamic-xml-access" => $"source:dynamic-xml-access/{candidate.Parent}/{candidate.Method}#{ordinal}",
                _ => $"source:unresolved-xml-access/{candidate.Parent}/{candidate.Method}#{ordinal}",
            };
            Add(
                surfaces,
                ids,
                new InventorySurface(
                    id,
                    candidate.Kind,
                    candidate.Constant ?? candidate.Method,
                    candidate.Parent,
                    candidate.Location,
                    candidate.Value,
                    entry.Names
                )
            );
        }
    }

    private static void EmitDecisions(
        List<InventorySurface> surfaces,
        HashSet<string> ids,
        IEnumerable<Availability<ResolvedDecision>> entries
    )
    {
        var counters = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (
            Availability<ResolvedDecision> entry in entries
                .OrderBy(item => item.Value.Parent, StringComparer.Ordinal)
                .ThenBy(item => item.Value.Kind, StringComparer.Ordinal)
                .ThenBy(item => item.Value.Path, StringComparer.Ordinal)
                .ThenBy(item => item.Value.SpanStart)
                .ThenBy(item => item.Value.Branch, StringComparer.Ordinal)
        )
        {
            ResolvedDecision candidate = entry.Value;
            string group = $"{candidate.Parent}\0{candidate.Kind}";
            int ordinal = counters.TryGetValue(group, out int used) ? used : 0;
            counters[group] = ordinal + 1;
            Add(
                surfaces,
                ids,
                new InventorySurface(
                    $"decision:{candidate.Parent}/{candidate.Kind}/{candidate.Branch}#{ordinal}-{candidate.Fingerprint}",
                    // The branch is part of the canonical ID and name, but the surface kind denotes
                    // the audited construct. This keeps the two outcomes of one if/switch decision
                    // together for catalog policies while retaining both obligations exactly once.
                    $"decision-{candidate.Kind}",
                    $"{candidate.Parent}/{candidate.Kind}/{candidate.Branch}",
                    candidate.Parent,
                    candidate.Location,
                    null,
                    entry.Names
                )
            );
        }
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    private static string NormalizeSource(string source) =>
        source.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static string HashSources(IEnumerable<SourceInput> sources) =>
        HashSources(sources, Array.Empty<CensusConfiguration>(), string.Empty, Array.Empty<InventoryDiagnostic>());

    // The configuration set is part of what the census covered, so widening it must
    // change the hash the staleness gate compares.
    private static string HashSources(
        IEnumerable<SourceInput> sources,
        IReadOnlyList<CensusConfiguration> configurations,
        string compilationFingerprint,
        IReadOnlyList<InventoryDiagnostic> diagnostics
    )
    {
        var text = new StringBuilder();
        foreach (SourceInput source in sources.OrderBy(item => item.Path, StringComparer.Ordinal))
        {
            text.Append(source.Path).Append('\0').Append(source.Text).Append('\0');
            foreach (string scope in source.Scopes.OrderBy(value => value, StringComparer.Ordinal))
                text.Append(scope).Append('\0');
        }
        foreach (CensusConfiguration configuration in configurations)
            text.Append(configuration.Name).Append('\0');
        text.Append("compilation\0").Append(compilationFingerprint).Append('\0');
        foreach (
            InventoryDiagnostic diagnostic in diagnostics
                .OrderBy(item => item.Code, StringComparer.Ordinal)
                .ThenBy(item => item.SubjectId, StringComparer.Ordinal)
                .ThenBy(item => item.Location, StringComparer.Ordinal)
                .ThenBy(item => item.Message, StringComparer.Ordinal)
        )
        {
            text.Append("diagnostic\0")
                .Append(diagnostic.Code)
                .Append('\0')
                .Append(diagnostic.SubjectId)
                .Append('\0')
                .Append(diagnostic.Message)
                .Append('\0')
                .Append(diagnostic.Configurations)
                .Append('\0')
                .Append(diagnostic.Location)
                .Append('\0');
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString()))).ToLowerInvariant();
    }

    private static FormatException SyntaxError(Diagnostic diagnostic)
    {
        FileLinePositionSpan span = diagnostic.Location.GetLineSpan();
        string path = string.IsNullOrEmpty(span.Path) ? "<source>" : span.Path;
        return new FormatException(
            $"{path}:{span.StartLinePosition.Line + 1}:{span.StartLinePosition.Character + 1}: {diagnostic.GetMessage()}"
        );
    }

    private sealed record CensusConfiguration(string Name, IReadOnlyList<string> Symbols);

    private sealed class Availability<T>(T value, string configuration)
    {
        private readonly SortedSet<string> _configurations = new(StringComparer.Ordinal) { configuration };

        public T Value { get; } = value;

        public string Names => string.Join(",", _configurations);

        public void Add(string name) => _configurations.Add(name);
    }

    private sealed record ResolvedXml(
        string Parent,
        string Method,
        string? Constant,
        string Kind,
        string Path,
        int SpanStart,
        string Location,
        string? Value
    )
    {
        public string Key => $"{Path}\0{SpanStart}\0{Parent}\0{Method}\0{Kind}\0{Constant}";
    }

    private sealed record ResolvedDecision(
        string Parent,
        string Kind,
        string Branch,
        string Path,
        int SpanStart,
        string Fingerprint,
        string Location
    )
    {
        public string Key => $"{Path}\0{SpanStart}\0{Parent}\0{Kind}\0{Branch}\0{Fingerprint}";
    }

    private sealed record SourceInput(string Path, string Text, IReadOnlyList<string> Scopes);

    private sealed record Declaration(ISymbol Symbol, SyntaxNode Node, SourceInput Source, SemanticModel Model);

    private sealed record DelegateTarget(ISymbol Symbol, string Location);

    private sealed record XmlCandidate(
        string Parent,
        string Method,
        string? Constant,
        string Kind,
        SyntaxNode Node,
        SourceInput Source,
        string? Value
    );

    private sealed record DecisionCandidate(
        string Parent,
        string Kind,
        string Branch,
        SyntaxNode Node,
        SourceInput Source
    );

    private sealed class SemanticContext
    {
        private readonly CSharpCompilation _compilation;
        private readonly SourceInput[] _sources;
        private readonly Dictionary<SyntaxTree, SourceInput> _sourceByTree = new();
        private readonly Dictionary<SyntaxTree, SemanticModel> _models = new();
        private readonly List<Declaration> _declarations = new();
        private readonly Dictionary<string, ISymbol> _symbolsById = new(StringComparer.Ordinal);
        private readonly Dictionary<ISymbol, string> _symbolIds = new(SymbolEqualityComparer.Default);
        private readonly Dictionary<INamedTypeSymbol, string> _typeIds = new(SymbolEqualityComparer.Default);
        private readonly Dictionary<IMethodSymbol, int> _localFunctionOrdinals = new(SymbolEqualityComparer.Default);
        private readonly Dictionary<string, int> _anonymousFunctionOrdinals = new(StringComparer.Ordinal);
        private readonly List<XmlCandidate> _xml = new();
        private readonly List<DecisionCandidate> _decisions = new();
        private readonly HashSet<string> _reachable = new(StringComparer.Ordinal);
        private readonly List<InventoryDiagnostic> _diagnostics = new();
        private readonly Dictionary<ISymbol, DelegateTarget> _localDelegates = new(SymbolEqualityComparer.Default);
        private readonly Dictionary<string, DelegateTarget> _parameterDelegates = new(StringComparer.Ordinal);
        private readonly HashSet<ILocalSymbol> _escapedLocals = new(SymbolEqualityComparer.Default);
        private readonly bool _referencesAreExact;

        /// <summary>Errors a partial source set provokes by resolving against built assemblies
        /// instead of the files it omits. Meaningless as defect signal there, and never tolerated
        /// once the source set carries whole projects.</summary>
        private static readonly HashSet<string> ApproximationOnlyErrorIds = new(StringComparer.Ordinal)
        {
            "CS0122",
            "CS0126",
            "CS0200",
            "CS0246",
            "CS0311",
            "CS0535",
            "CS1061",
            "CS1503",
        };

        private bool HasAuditedScopes => _sources.Any(source => source.Scopes.Count != 0);

        public IReadOnlyList<InventoryDiagnostic> Diagnostics => _diagnostics;

        public SemanticContext(
            CSharpCompilation compilation,
            SourceInput[] sources,
            SyntaxTree[] trees,
            bool referencesAreExact
        )
        {
            _compilation = compilation;
            _sources = sources;
            _referencesAreExact = referencesAreExact;
            for (int i = 0; i < trees.Length; i++)
            {
                _sourceByTree.Add(trees[i], sources[i]);
                _models.Add(trees[i], compilation.GetSemanticModel(trees[i], ignoreAccessibility: false));
            }
        }

        public void BuildDeclarations()
        {
            foreach (
                (SyntaxTree tree, SourceInput source) in _sourceByTree.OrderBy(
                    pair => pair.Value.Path,
                    StringComparer.Ordinal
                )
            )
            {
                SemanticModel model = _models[tree];
                foreach (SyntaxNode node in tree.GetRoot().DescendantNodesAndSelf())
                {
                    if (!DeclarationKinds.Contains(node.Kind()))
                        continue;
                    ISymbol? symbol = model.GetDeclaredSymbol(node);
                    if (symbol is null || !IsSourceSymbol(symbol))
                        continue;
                    _declarations.Add(new Declaration(symbol, node, source, model));
                }

                // Anonymous functions are executable source symbols too. They are registered
                // separately because Roslyn does not expose them through GetDeclaredSymbol on
                // every compiler version, while their IOperation always carries the symbol.
                foreach (
                    AnonymousFunctionExpressionSyntax lambda in tree.GetRoot()
                        .DescendantNodes()
                        .OfType<AnonymousFunctionExpressionSyntax>()
                )
                {
                    if (
                        model.GetOperation(lambda) is not IAnonymousFunctionOperation operation
                        || !IsSourceSymbol(operation.Symbol)
                    )
                        continue;
                    if (
                        !_declarations.Any(item => SymbolEqualityComparer.Default.Equals(item.Symbol, operation.Symbol))
                    )
                        _declarations.Add(new Declaration(operation.Symbol, lambda, source, model));
                }
            }
            // Expression-bodied and auto properties expose compiler-generated accessors whose
            // declared syntax is the property itself. Register those source accessors against
            // that declaration so a property reference can enter the getter body exactly like an
            // explicit accessor declaration.
            foreach (Declaration declaration in _declarations.ToArray())
            {
                switch (declaration.Symbol)
                {
                    case IPropertySymbol property:
                        AddSyntheticAccessor(property.GetMethod, declaration);
                        AddSyntheticAccessor(property.SetMethod, declaration);
                        break;
                    case IEventSymbol @event:
                        AddSyntheticAccessor(@event.AddMethod, declaration);
                        AddSyntheticAccessor(@event.RemoveMethod, declaration);
                        break;
                }
            }
            AssignLocalFunctionOrdinals();
            // Register every source symbol, not just types. Exact audits may target an
            // overload, accessor, operator, destructor, or local function.
            foreach (
                ISymbol symbol in _declarations
                    .Select(item => item.Symbol)
                    .Distinct(SymbolEqualityComparer.Default)
                    .OrderBy(CanonicalSymbolId, StringComparer.Ordinal)
            )
            {
                string id = CanonicalSymbolId(symbol);
                if (_symbolsById.TryGetValue(id, out ISymbol? existing))
                {
                    if (AreMergedPartialSymbols(existing, symbol))
                        continue;
                    throw new InvalidOperationException($"Duplicate canonical C# symbol ID '{id}'.");
                }

                _symbolsById.Add(id, symbol);
            }
            ValidateRelevantRuleBases();
        }

        private void AddSyntheticAccessor(IMethodSymbol? accessor, Declaration propertyDeclaration)
        {
            if (
                accessor is null
                || !IsSourceSymbol(accessor)
                || _declarations.Any(item => SymbolEqualityComparer.Default.Equals(item.Symbol, accessor))
            )
                return;
            _declarations.Add(propertyDeclaration with { Symbol = accessor });
        }

        private void ValidateRelevantRuleBases()
        {
            foreach (
                (SyntaxTree tree, SourceInput source) in _sourceByTree.OrderBy(
                    pair => pair.Value.Path,
                    StringComparer.Ordinal
                )
            )
            {
                SemanticModel model = _models[tree];
                foreach (BaseTypeSyntax baseType in tree.GetRoot().DescendantNodes().OfType<BaseTypeSyntax>())
                {
                    string terminal = baseType.Type.ToString().Split('.').Last().Split('<').First();
                    bool relevant =
                        terminal is "IHCRule" or "IPhonologicalRule" or "IMorphologicalRule" or "IRule"
                        || IsUnresolvedAlias(
                            model,
                            baseType.Type,
                            "IHCRule",
                            "IPhonologicalRule",
                            "IMorphologicalRule",
                            "IRule"
                        );
                    if (relevant && IsUnresolvedSymbol(model.GetSymbolInfo(baseType.Type).Symbol))
                        throw new FormatException(
                            $"{source.Path}: unresolved relevant rule interface '{baseType.Type}'."
                        );
                }
            }
        }

        private void AssignLocalFunctionOrdinals()
        {
            IMethodSymbol[] localFunctions = _declarations
                .Select(declaration => declaration.Symbol)
                .OfType<IMethodSymbol>()
                .Where(method => method.MethodKind == MethodKind.LocalFunction)
                .Distinct(SymbolEqualityComparer.Default)
                .Cast<IMethodSymbol>()
                .ToArray();
            foreach (
                IGrouping<int, IMethodSymbol> level in localFunctions
                    .GroupBy(LocalFunctionNestingDepth)
                    .OrderBy(group => group.Key)
            )
            {
                foreach (
                    IGrouping<string, IMethodSymbol> collision in level
                        .GroupBy(LocalFunctionBaseId, StringComparer.Ordinal)
                        .Where(group => group.Count() > 1)
                )
                {
                    int ordinal = 0;
                    foreach (
                        IMethodSymbol method in collision
                            .OrderBy(LocalFunctionSourcePath, StringComparer.Ordinal)
                            .ThenBy(LocalFunctionSourceStart)
                    )
                    {
                        _localFunctionOrdinals.Add(method, ordinal++);
                    }
                }
            }

            IMethodSymbol[] anonymousFunctions = _declarations
                .Select(declaration => declaration.Symbol)
                .OfType<IMethodSymbol>()
                .Where(method => method.MethodKind == MethodKind.AnonymousFunction)
                .Distinct(SymbolEqualityComparer.Default)
                .Cast<IMethodSymbol>()
                .ToArray();
            foreach (
                IGrouping<int, IMethodSymbol> level in anonymousFunctions
                    .Where(method => method.ContainingSymbol is not null)
                    .GroupBy(AnonymousFunctionNestingDepth)
                    .OrderBy(group => group.Key)
            )
            {
                foreach (
                    IGrouping<string, IMethodSymbol> group in level.GroupBy(
                        method => CanonicalSymbolId(method.ContainingSymbol!),
                        StringComparer.Ordinal
                    )
                )
                {
                    int ordinal = 0;
                    foreach (
                        IMethodSymbol method in group
                            .OrderBy(LocalFunctionSourcePath, StringComparer.Ordinal)
                            .ThenBy(LocalFunctionSourceStart)
                    )
                        _anonymousFunctionOrdinals[AnonymousFunctionKey(method)] = ordinal++;
                }
            }
        }

        private static int AnonymousFunctionNestingDepth(IMethodSymbol method)
        {
            int depth = 0;
            for (
                ISymbol? current = method.ContainingSymbol;
                current is IMethodSymbol containing && containing.MethodKind == MethodKind.AnonymousFunction;
                current = current.ContainingSymbol
            )
                depth++;
            return depth;
        }

        private string AnonymousFunctionKey(IMethodSymbol method) =>
            $"{CanonicalSymbolId(method.ContainingSymbol!)}\0{LocalFunctionSourcePath(method)}\0{LocalFunctionSourceStart(method)}";

        private static int LocalFunctionNestingDepth(IMethodSymbol method)
        {
            int depth = 0;
            for (
                ISymbol? current = method.ContainingSymbol;
                current is IMethodSymbol containing && containing.MethodKind == MethodKind.LocalFunction;
                current = current.ContainingSymbol
            )
                depth++;
            return depth;
        }

        private string LocalFunctionBaseId(IMethodSymbol method) =>
            $"{CanonicalSymbolId(method.ContainingSymbol)}/local/{CallableName(method)}({Parameters(method)})";

        private static string LocalFunctionSourcePath(IMethodSymbol method) =>
            method
                .Locations.Where(location => location.IsInSource)
                .Select(location => location.SourceTree?.FilePath ?? string.Empty)
                .OrderBy(path => path, StringComparer.Ordinal)
                .FirstOrDefault()
            ?? string.Empty;

        private static int LocalFunctionSourceStart(IMethodSymbol method) =>
            method
                .Locations.Where(location => location.IsInSource)
                .Select(location => location.SourceSpan.Start)
                .DefaultIfEmpty(-1)
                .Min();

        private static bool AreMergedPartialSymbols(ISymbol left, ISymbol right)
        {
            if (left is IMethodSymbol leftMethod && right is IMethodSymbol rightMethod)
            {
                return leftMethod.PartialDefinitionPart is not null
                    || leftMethod.PartialImplementationPart is not null
                    || rightMethod.PartialDefinitionPart is not null
                    || rightMethod.PartialImplementationPart is not null;
            }

            if (left is not INamedTypeSymbol || right is not INamedTypeSymbol)
                return false;
            return left.DeclaringSyntaxReferences.Any(IsPartialTypeDeclaration)
                && right.DeclaringSyntaxReferences.Any(IsPartialTypeDeclaration);
        }

        private static bool IsPartialTypeDeclaration(SyntaxReference reference) =>
            reference.GetSyntax() is TypeDeclarationSyntax declaration
            && declaration.Modifiers.Any(SyntaxKind.PartialKeyword);

        public IEnumerable<string> KnownSymbolIds() => _symbolsById.Keys;

        public bool IsActionableCompilationError(Diagnostic diagnostic) =>
            diagnostic.Location.IsInSource
            && diagnostic.Location.SourceTree is not null
            && IsInsideAuditedScope(diagnostic)
            && (_referencesAreExact || !ApproximationOnlyErrorIds.Contains(diagnostic.Id));

        /// <summary>The model for <paramref name="tree"/>, or null when the tree belongs to a
        /// referenced compilation rather than this one.</summary>
        /// <remarks>Owned projects are bound as compilation references, so a symbol reached from
        /// this compilation can carry syntax this context has no model for. Such a symbol is
        /// outside the censused source and is not followed.</remarks>
        private SemanticModel? ModelOrNull(SyntaxTree tree) =>
            _models.TryGetValue(tree, out SemanticModel? model) ? model : null;

        private bool IsInsideAuditedScope(Diagnostic diagnostic)
        {
            if (!diagnostic.Location.IsInSource || diagnostic.Location.SourceTree is null)
                return false;
            SemanticModel? model = ModelOrNull(diagnostic.Location.SourceTree);
            if (model is null)
                return false;
            ISymbol? containing = model.GetEnclosingSymbol(diagnostic.Location.SourceSpan.Start);
            while (containing is not null)
            {
                string containingId = CanonicalSymbolId(containing);
                foreach (string scope in _sources.SelectMany(source => source.Scopes))
                {
                    if (StringComparer.Ordinal.Equals(scope, containingId))
                        return true;
                    if (
                        _symbolsById.TryGetValue(scope, out ISymbol? root)
                        && root is INamedTypeSymbol rootType
                        && IsWithinType(containing, rootType)
                    )
                        return true;
                }

                containing = containing.ContainingSymbol;
            }

            return false;
        }

        public void AddCompilationErrors(IEnumerable<Diagnostic> errors)
        {
            foreach (Diagnostic error in errors)
            {
                if (!IsActionableCompilationError(error))
                    continue;
                string location = error.Location.IsInSource
                    ? Location(
                        error.Location.SourceTree!,
                        error.Location.SourceSpan,
                        _sourceByTree[error.Location.SourceTree!]
                    )
                    : "<compilation>";
                AddDiagnostic("compilation-error", "compilation", error.GetMessage(), location);
            }
        }

        public void BuildExecutionClosure()
        {
            if (!HasAuditedScopes)
                return;
            var pending = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string scope in _sources.SelectMany(source => source.Scopes).Distinct(StringComparer.Ordinal))
            {
                if (_symbolsById.TryGetValue(scope, out ISymbol? symbol))
                    AddReachable(symbol, pending, expandType: symbol is INamedTypeSymbol);
            }

            while (pending.Count != 0)
            {
                string id = pending.Min!;
                pending.Remove(id);
                if (!_symbolsById.TryGetValue(id, out ISymbol? symbol))
                    continue;
                ProcessReachableSymbol(symbol, pending);
            }
        }

        private void ProcessReachableSymbol(ISymbol symbol, SortedSet<string> pending)
        {
            foreach (
                Declaration declaration in _declarations
                    .Where(item =>
                        StringComparer.Ordinal.Equals(CanonicalSymbolId(item.Symbol), CanonicalSymbolId(symbol))
                    )
                    .OrderBy(item => item.Source.Path, StringComparer.Ordinal)
                    .ThenBy(item => item.Node.SpanStart)
            )
            {
                _localDelegates.Clear();
                _escapedLocals.Clear();
                SeedParameterDelegates(symbol);
                IOperation? operation = ExecutableOperation(declaration);
                if (operation is IAnonymousFunctionOperation anonymous)
                    operation = anonymous.Body;
                if (operation is not null)
                    ProcessOperation(operation, CanonicalSymbolId(symbol), pending);
                if (symbol is IMethodSymbol { MethodKind: MethodKind.Constructor } constructorSymbol)
                {
                    ProcessInstanceInitializers(constructorSymbol.ContainingType, CanonicalSymbolId(symbol), pending);
                    AddConstructorEdges(constructorSymbol, pending);
                }
                else if (symbol is IMethodSymbol { MethodKind: MethodKind.StaticConstructor } staticConstructor)
                {
                    ProcessStaticInitializers(staticConstructor.ContainingType, CanonicalSymbolId(symbol), pending);
                }
            }
        }

        private void SeedParameterDelegates(ISymbol symbol)
        {
            if (symbol is not IMethodSymbol method)
                return;
            foreach (IParameterSymbol parameter in method.Parameters)
            {
                if (_parameterDelegates.TryGetValue(ParameterBindingKey(method, parameter), out DelegateTarget? target))
                    _localDelegates[parameter] = target;
            }
        }

        private void ProcessStaticInitializers(INamedTypeSymbol? type, string callerId, SortedSet<string> pending)
        {
            if (type is null)
                return;
            foreach (
                Declaration declaration in _declarations
                    .Where(item =>
                        item.Symbol switch
                        {
                            IFieldSymbol field => field.IsStatic
                                && SymbolEqualityComparer.Default.Equals(
                                    field.ContainingType?.OriginalDefinition,
                                    type.OriginalDefinition
                                ),
                            IPropertySymbol property => property.IsStatic
                                && SymbolEqualityComparer.Default.Equals(
                                    property.ContainingType?.OriginalDefinition,
                                    type.OriginalDefinition
                                ),
                            _ => false,
                        }
                    )
                    .OrderBy(item => item.Source.Path, StringComparer.Ordinal)
                    .ThenBy(item => item.Node.SpanStart)
            )
            {
                AddReachable(declaration.Symbol, pending);
                IOperation? initializer = ExecutableOperation(declaration);
                if (initializer is not null)
                    ProcessOperation(initializer, callerId, pending);
            }
        }

        private void AddStaticInitialization(INamedTypeSymbol? type, SortedSet<string> pending)
        {
            if (type is null)
                return;
            foreach (
                IMethodSymbol constructor in _symbolsById
                    .Values.OfType<IMethodSymbol>()
                    .Where(method =>
                        method.MethodKind == MethodKind.StaticConstructor
                        && SymbolEqualityComparer.Default.Equals(
                            method.ContainingType?.OriginalDefinition,
                            type.OriginalDefinition
                        )
                    )
            )
            {
                AddReachable(constructor, pending);
            }
        }

        private void ProcessInstanceInitializers(INamedTypeSymbol? type, string callerId, SortedSet<string> pending)
        {
            if (type is null)
                return;
            foreach (
                Declaration declaration in _declarations
                    .Where(item =>
                        item.Symbol switch
                        {
                            IFieldSymbol field => !field.IsStatic
                                && SymbolEqualityComparer.Default.Equals(
                                    field.ContainingType?.OriginalDefinition,
                                    type.OriginalDefinition
                                ),
                            IPropertySymbol property => !property.IsStatic
                                && SymbolEqualityComparer.Default.Equals(
                                    property.ContainingType?.OriginalDefinition,
                                    type.OriginalDefinition
                                ),
                            _ => false,
                        }
                    )
                    .OrderBy(item => item.Source.Path, StringComparer.Ordinal)
                    .ThenBy(item => item.Node.SpanStart)
            )
            {
                AddReachable(declaration.Symbol, pending);
                IOperation? initializer = ExecutableOperation(declaration);
                if (initializer is not null)
                    ProcessOperation(initializer, callerId, pending);
            }
        }

        private void AddConstructorEdges(IMethodSymbol constructor, SortedSet<string> pending)
        {
            ConstructorDeclarationSyntax? syntax = constructor
                .DeclaringSyntaxReferences.Select(reference => reference.GetSyntax())
                .OfType<ConstructorDeclarationSyntax>()
                .FirstOrDefault(node => ModelOrNull(node.SyntaxTree) is not null);
            ConstructorInitializerSyntax? initializer = syntax?.Initializer;
            IMethodSymbol? explicitTarget = initializer is null
                ? null
                : ModelOrNull(initializer.SyntaxTree)?.GetSymbolInfo(initializer).Symbol as IMethodSymbol;
            if (explicitTarget is not null)
            {
                // Both this(...) and base(...) constructor initializers are exact calls.
                if (initializer is not null)
                    foreach (ArgumentSyntax argument in initializer.ArgumentList.Arguments)
                        ProcessOperation(
                            ModelOrNull(argument.SyntaxTree)!.GetOperation(argument.Expression)!,
                            CanonicalSymbolId(constructor),
                            pending
                        );
                AddReachable(explicitTarget, pending);
                return;
            }

            INamedTypeSymbol? baseType = constructor.ContainingType?.BaseType;
            if (baseType is null || baseType.SpecialType == SpecialType.System_Object)
                return;
            foreach (
                IMethodSymbol baseConstructor in _symbolsById
                    .Values.OfType<IMethodSymbol>()
                    .Where(candidate =>
                        candidate.MethodKind == MethodKind.Constructor
                        && candidate.Parameters.Length == 0
                        && candidate.ContainingType is not null
                        && SymbolEqualityComparer.Default.Equals(
                            candidate.ContainingType.OriginalDefinition,
                            baseType.OriginalDefinition
                        )
                    )
                    .OrderBy(CanonicalSymbolId, StringComparer.Ordinal)
            )
                AddReachable(baseConstructor, pending);
        }

        private static IOperation? ExecutableOperation(Declaration declaration) =>
            declaration.Node switch
            {
                MethodDeclarationSyntax method when method.Body is not null => declaration.Model.GetOperation(
                    method.Body
                ),
                MethodDeclarationSyntax method when method.ExpressionBody is not null => declaration.Model.GetOperation(
                    method.ExpressionBody.Expression
                ),
                ConstructorDeclarationSyntax constructor when constructor.Body is not null =>
                    declaration.Model.GetOperation(constructor.Body),
                ConstructorDeclarationSyntax constructor when constructor.ExpressionBody is not null =>
                    declaration.Model.GetOperation(constructor.ExpressionBody.Expression),
                DestructorDeclarationSyntax destructor when destructor.Body is not null =>
                    declaration.Model.GetOperation(destructor.Body),
                OperatorDeclarationSyntax op when op.Body is not null => declaration.Model.GetOperation(op.Body),
                OperatorDeclarationSyntax op when op.ExpressionBody is not null => declaration.Model.GetOperation(
                    op.ExpressionBody.Expression
                ),
                ConversionOperatorDeclarationSyntax conversion when conversion.Body is not null =>
                    declaration.Model.GetOperation(conversion.Body),
                ConversionOperatorDeclarationSyntax conversion when conversion.ExpressionBody is not null =>
                    declaration.Model.GetOperation(conversion.ExpressionBody.Expression),
                AccessorDeclarationSyntax accessor when accessor.Body is not null => declaration.Model.GetOperation(
                    accessor.Body
                ),
                AccessorDeclarationSyntax accessor when accessor.ExpressionBody is not null =>
                    declaration.Model.GetOperation(accessor.ExpressionBody.Expression),
                PropertyDeclarationSyntax property when property.ExpressionBody is not null =>
                    declaration.Model.GetOperation(property.ExpressionBody.Expression),
                IndexerDeclarationSyntax indexer when indexer.ExpressionBody is not null =>
                    declaration.Model.GetOperation(indexer.ExpressionBody.Expression),
                PropertyDeclarationSyntax property when property.Initializer is not null =>
                    declaration.Model.GetOperation(property.Initializer.Value),
                LocalFunctionStatementSyntax local when local.Body is not null => declaration.Model.GetOperation(
                    local.Body
                ),
                LocalFunctionStatementSyntax local when local.ExpressionBody is not null =>
                    declaration.Model.GetOperation(local.ExpressionBody.Expression),
                AnonymousFunctionExpressionSyntax lambda => declaration.Model.GetOperation(lambda),
                VariableDeclaratorSyntax variable when variable.Initializer is not null =>
                    declaration.Model.GetOperation(variable.Initializer.Value),
                VariableDeclaratorSyntax variable => declaration.Model.GetOperation(variable),
                _ => null,
            };

        private void ProcessOperation(IOperation operation, string callerId, SortedSet<string> pending)
        {
            // Nested functions are declarations, not execution edges. They are entered only by
            // a delegate invocation or a source-bound method group handled below.
            if (operation.Kind is OperationKind.AnonymousFunction or OperationKind.LocalFunction)
                return;

            switch (operation)
            {
                case IInvocationOperation invocation:
                    if (invocation.Instance is not null)
                        ProcessOperation(invocation.Instance, callerId, pending);
                    foreach (IArgumentOperation argument in invocation.Arguments)
                    {
                        if (argument.Parameter?.Type is ITypeSymbol parameterType && IsDelegateType(parameterType))
                        {
                            if (!IsSourceMethod(invocation.TargetMethod))
                                FollowDelegateValue(argument.Value, callerId, pending, invocation);
                            else if (
                                BindDelegateArgumentToConsumers(
                                    invocation.TargetMethod,
                                    invocation.Instance,
                                    argument.Parameter,
                                    argument.Value,
                                    callerId,
                                    invocation,
                                    pending
                                )
                            )
                            {
                                // The consumed delegate is bound to each concrete implementation.
                            }
                            else
                            {
                                // Even an ignored delegate argument evaluates its factory or
                                // other non-direct expression, but its returned callback is not
                                // itself invoked.
                                if (!IsDirectDelegateValue(argument.Value))
                                    ProcessOperation(argument.Value, callerId, pending);
                            }
                        }
                        else
                            ProcessOperation(argument.Value, callerId, pending);
                    }
                    ResolveInvocation(invocation, callerId, pending);
                    return;

                case IObjectCreationOperation creation:
                    if (creation.Constructor is not null)
                    {
                        AddStaticInitialization(creation.Constructor.ContainingType, pending);
                        AddMethodTarget(creation.Constructor, creation, creation, callerId, pending);
                        if (
                            creation.Constructor.MethodKind == MethodKind.Constructor
                            && !_symbolsById.ContainsKey(CanonicalSymbolId(creation.Constructor))
                        )
                        {
                            ProcessInstanceInitializers(creation.Constructor.ContainingType, callerId, pending);
                            AddConstructorEdges(creation.Constructor, pending);
                        }
                    }
                    foreach (IArgumentOperation argument in creation.Arguments)
                        ProcessOperation(argument.Value, callerId, pending);
                    if (creation.Initializer is not null)
                        ProcessOperation(creation.Initializer, callerId, pending);
                    return;

                case IPropertyReferenceOperation property:
                    if (property.Instance is not null)
                        ProcessOperation(property.Instance, callerId, pending);
                    if (property.Property.IsStatic)
                        AddStaticInitialization(property.Property.ContainingType, pending);
                    foreach (IArgumentOperation argument in property.Arguments)
                        ProcessOperation(argument.Value, callerId, pending);
                    bool writes = IsWriteAccess(property);
                    bool reads = !writes || IsReadWriteAccess(property);
                    if (reads && property.Property.GetMethod is IMethodSymbol getter)
                        AddExactOrDispatchTarget(getter, property.Instance, property, callerId, pending);
                    if (writes && property.Property.SetMethod is IMethodSymbol setter)
                        AddExactOrDispatchTarget(setter, property.Instance, property, callerId, pending);
                    if (
                        (reads && property.Property.GetMethod is null)
                        || (writes && property.Property.SetMethod is null)
                    )
                        AddDiagnostic(
                            "unresolved-interface-dispatch",
                            callerId,
                            "interface property accessor has no concrete source implementation",
                            property
                        );
                    return;

                case IEventReferenceOperation @event:
                    if (@event.Instance is not null)
                        ProcessOperation(@event.Instance, callerId, pending);
                    if (@event.Event.IsStatic)
                        AddStaticInitialization(@event.Event.ContainingType, pending);
                    IMethodSymbol? eventAccessor = EventAccessor(@event);
                    if (eventAccessor is not null)
                        AddExactOrDispatchTarget(eventAccessor, @event.Instance, @event, callerId, pending);
                    return;

                case IFieldReferenceOperation field:
                    if (field.Instance is not null)
                        ProcessOperation(field.Instance, callerId, pending);
                    if (field.Field.IsStatic)
                        AddStaticInitialization(field.Field.ContainingType, pending);
                    AddReachable(field.Field, pending);
                    return;

                case IDynamicMemberReferenceOperation dynamicMember:
                    if (dynamicMember.Instance is not null)
                        ProcessOperation(dynamicMember.Instance, callerId, pending);
                    AddDiagnostic(
                        "unresolved-dynamic-member",
                        callerId,
                        $"dynamic member '{dynamicMember.MemberName}' cannot be closed statically",
                        dynamicMember
                    );
                    return;

                case IDynamicIndexerAccessOperation dynamicIndexer:
                    ProcessOperation(dynamicIndexer.Operation, callerId, pending);
                    foreach (IOperation argument in dynamicIndexer.Arguments)
                        ProcessOperation(argument, callerId, pending);
                    AddDiagnostic(
                        "unresolved-dynamic-member",
                        callerId,
                        "dynamic indexer access cannot be closed statically",
                        dynamicIndexer
                    );
                    return;

                case IDynamicInvocationOperation dynamicInvocation:
                    ProcessOperation(dynamicInvocation.Operation, callerId, pending);
                    foreach (IArgumentOperation argument in dynamicInvocation.Arguments)
                        ProcessOperation(argument.Value, callerId, pending);
                    if (
                        dynamicInvocation.Operation is IDynamicMemberReferenceOperation member
                        && member.Instance is not null
                    )
                        AddDiagnostic(
                            "unresolved-call-dispatch",
                            callerId,
                            "dynamic invocation receiver cannot be closed statically",
                            member.Instance
                        );
                    else
                        AddDiagnostic(
                            "unresolved-call-dispatch",
                            callerId,
                            "dynamic invocation cannot be closed statically",
                            dynamicInvocation
                        );
                    return;

                case IDynamicObjectCreationOperation dynamicCreation:
                    foreach (IArgumentOperation argument in dynamicCreation.Arguments)
                        ProcessOperation(argument.Value, callerId, pending);
                    if (dynamicCreation.Initializer is not null)
                        ProcessOperation(dynamicCreation.Initializer, callerId, pending);
                    AddDiagnostic(
                        "open-construction-dispatch",
                        callerId,
                        "dynamic object construction cannot be closed statically",
                        dynamicCreation
                    );
                    return;

                case ITypeParameterObjectCreationOperation typeParameterCreation:
                    if (typeParameterCreation.Initializer is not null)
                        ProcessOperation(typeParameterCreation.Initializer, callerId, pending);
                    AddDiagnostic(
                        "open-construction-dispatch",
                        callerId,
                        "type-parameter construction cannot be closed statically",
                        typeParameterCreation
                    );
                    return;

                case ISimpleAssignmentOperation assignment:
                    if (assignment.Target is ILocalReferenceOperation local && IsDelegateType(local.Local.Type))
                    {
                        if (!IsDirectDelegateValue(assignment.Value))
                            ProcessOperation(assignment.Value, callerId, pending);
                        RememberDelegate(local.Local, assignment.Value);
                        return;
                    }
                    ProcessOperation(assignment.Target, callerId, pending);
                    ProcessOperation(assignment.Value, callerId, pending);
                    return;

                case IVariableDeclaratorOperation variable when variable.Initializer is not null:
                    if (IsDelegateType(variable.Symbol.Type))
                    {
                        if (!IsDirectDelegateValue(variable.Initializer.Value))
                            ProcessOperation(variable.Initializer.Value, callerId, pending);
                        RememberDelegate(variable.Symbol, variable.Initializer.Value);
                    }
                    else
                        ProcessOperation(variable.Initializer.Value, callerId, pending);
                    return;

                case IDelegateCreationOperation delegateCreation:
                    // Delegate creation produces a value. Its body is entered only when a
                    // consuming callsite binds and invokes the delegate.
                    return;

                case IMethodReferenceOperation:
                case ILocalFunctionOperation:
                case IAnonymousFunctionOperation:
                    return;

                case IConditionalOperation conditional:
                    ProcessOperation(conditional.Condition, callerId, pending);
                    if (TryGetConstantBoolean(conditional.Condition, out bool condition))
                    {
                        IOperation? selected = condition ? conditional.WhenTrue : conditional.WhenFalse;
                        if (selected is not null)
                            ProcessOperation(selected, callerId, pending);
                    }
                    else
                    {
                        if (conditional.WhenTrue is not null)
                            ProcessOperation(conditional.WhenTrue, callerId, pending);
                        if (conditional.WhenFalse is not null)
                            ProcessOperation(conditional.WhenFalse, callerId, pending);
                    }
                    return;

                case IBinaryOperation binary:
                    ProcessOperation(binary.LeftOperand, callerId, pending);
                    ProcessOperation(binary.RightOperand, callerId, pending);
                    if (binary.OperatorMethod is IMethodSymbol binaryMethod)
                        AddMethodTarget(binaryMethod, binary.LeftOperand, binary, callerId, pending);
                    return;

                case IUnaryOperation unary:
                    ProcessOperation(unary.Operand, callerId, pending);
                    if (unary.OperatorMethod is IMethodSymbol unaryMethod)
                        AddMethodTarget(unaryMethod, unary.Operand, unary, callerId, pending);
                    return;

                case IConversionOperation conversion:
                    ProcessOperation(conversion.Operand, callerId, pending);
                    if (conversion.Conversion.MethodSymbol is IMethodSymbol conversionMethod)
                        AddMethodTarget(conversionMethod, conversion.Operand, conversion, callerId, pending);
                    return;

                case IIncrementOrDecrementOperation increment:
                    ProcessOperation(increment.Target, callerId, pending);
                    if (increment.OperatorMethod is IMethodSymbol incrementMethod)
                        AddMethodTarget(incrementMethod, increment.Target, increment, callerId, pending);
                    return;

                case ICompoundAssignmentOperation compound:
                    ProcessOperation(compound.Target, callerId, pending);
                    ProcessOperation(compound.Value, callerId, pending);
                    if (compound.OperatorMethod is IMethodSymbol compoundMethod)
                        AddMethodTarget(compoundMethod, compound.Target, compound, callerId, pending);
                    return;

                default:
                    ReportUnresolvedInvocationSyntax(operation.Syntax, callerId);
                    foreach (IOperation child in operation.ChildOperations)
                        ProcessOperation(child, callerId, pending);
                    return;
            }
        }

        private void ResolveInvocation(IInvocationOperation invocation, string callerId, SortedSet<string> pending)
        {
            IMethodSymbol method = invocation.TargetMethod;
            if (IsDynamicInvocation(invocation))
            {
                AddDiagnostic(
                    "unresolved-call-dispatch",
                    callerId,
                    "dynamic invocation target cannot be closed statically",
                    invocation
                );
                return;
            }
            if (method.MethodKind == MethodKind.DelegateInvoke || method.ContainingType?.TypeKind == TypeKind.Delegate)
            {
                FollowDelegateValue(invocation.Instance, callerId, pending, invocation);
                return;
            }

            if (method is null || method.ContainingType?.TypeKind == TypeKind.Error)
            {
                AddDiagnostic(
                    "unresolved-call-dispatch",
                    callerId,
                    "invocation target cannot be closed statically",
                    invocation
                );
                return;
            }

            if (method.IsStatic && method.MethodKind != MethodKind.StaticConstructor)
                AddStaticInitialization(method.ContainingType, pending);

            if (IsBaseReceiver(invocation))
            {
                // A base-qualified call is an exact nonvirtual edge, even when the
                // selected member is declared virtual. It must not fan out to overrides.
                AddReachable(method, pending);
                return;
            }

            AddMethodTarget(method, invocation.Instance, invocation, callerId, pending);
        }

        private void ReportUnresolvedInvocationSyntax(SyntaxNode node, string callerId)
        {
            foreach (
                InvocationExpressionSyntax syntax in node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>()
            )
            {
                SemanticModel? model = ModelOrNull(syntax.SyntaxTree);
                if (model is null || model.GetSymbolInfo(syntax).Symbol is not null)
                    continue;
                if (IsDynamicSyntaxInvocation(syntax))
                {
                    AddDiagnostic(
                        "unresolved-call-dispatch",
                        callerId,
                        "dynamic invocation target cannot be closed statically",
                        syntax
                    );
                    continue;
                }
                AddDiagnostic(
                    "unresolved-call-dispatch",
                    callerId,
                    "invocation target cannot be closed statically",
                    syntax
                );
            }
        }

        private static bool IsBaseReceiver(IInvocationOperation invocation) =>
            invocation.Syntax
                is InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax { Expression: BaseExpressionSyntax }
                };

        private bool IsDynamicInvocation(IInvocationOperation invocation)
        {
            if (invocation.Instance?.Type?.TypeKind == TypeKind.Dynamic)
                return true;
            if (invocation.Syntax is not InvocationExpressionSyntax syntax)
                return false;
            ExpressionSyntax? receiver = syntax.Expression switch
            {
                MemberAccessExpressionSyntax member => member.Expression,
                MemberBindingExpressionSyntax => syntax
                    .Ancestors()
                    .OfType<ConditionalAccessExpressionSyntax>()
                    .FirstOrDefault()
                    ?.Expression,
                _ => null,
            };
            return receiver is not null
                && ModelOrNull(syntax.SyntaxTree)?.GetTypeInfo(receiver).Type?.TypeKind == TypeKind.Dynamic;
        }

        private bool IsDynamicSyntaxInvocation(InvocationExpressionSyntax syntax)
        {
            ExpressionSyntax? receiver = syntax.Expression switch
            {
                MemberAccessExpressionSyntax member => member.Expression,
                MemberBindingExpressionSyntax => syntax
                    .Ancestors()
                    .OfType<ConditionalAccessExpressionSyntax>()
                    .FirstOrDefault()
                    ?.Expression,
                _ => null,
            };
            return receiver is not null
                && ModelOrNull(syntax.SyntaxTree)?.GetTypeInfo(receiver).Type?.TypeKind == TypeKind.Dynamic;
        }

        private void AddMethodTarget(
            IMethodSymbol method,
            IOperation? receiver,
            IOperation callsite,
            string callerId,
            SortedSet<string> pending
        )
        {
            if (
                method.ContainingType?.TypeKind == TypeKind.Interface
                || method.IsVirtual
                || method.IsOverride
                || method.IsAbstract
            )
            {
                IReadOnlyList<IMethodSymbol> targets = DispatchTargets(method, receiver);
                if (targets.Count == 0)
                {
                    string code =
                        method.ContainingType?.TypeKind == TypeKind.Interface
                            ? "unresolved-interface-dispatch"
                            : "external-virtual-dispatch";
                    AddDiagnostic(code, callerId, "callsite has no concrete source dispatch target", callsite);
                    return;
                }
                foreach (IMethodSymbol target in targets)
                    AddReachable(target, pending);
                if (method.ContainingType?.TypeKind == TypeKind.Interface)
                    MarkConcreteInterfaceReceivers(method, receiver, pending);
                return;
            }

            AddReachable(method, pending);
        }

        private void MarkConcreteInterfaceReceivers(
            IMethodSymbol interfaceMethod,
            IOperation? receiver,
            SortedSet<string> pending
        )
        {
            INamedTypeSymbol? receiverType = receiver?.Type as INamedTypeSymbol;
            IEnumerable<INamedTypeSymbol> types = _symbolsById
                .Values.OfType<INamedTypeSymbol>()
                .Where(type => type.TypeKind is TypeKind.Class or TypeKind.Struct && !type.IsAbstract);
            if (
                receiverType is not null
                && receiverType.TypeKind is TypeKind.Class or TypeKind.Struct
                && !receiverType.IsAbstract
                && _symbolsById.ContainsKey(CanonicalSymbolId(receiverType))
            )
                types = types.Where(type => IsSameOrDerived(type, receiverType));

            foreach (INamedTypeSymbol type in types)
            {
                IMethodSymbol? implementation =
                    type.FindImplementationForInterfaceMember(interfaceMethod) as IMethodSymbol;
                if (
                    implementation is not null
                    && IsSourceSymbol(implementation)
                    && _symbolsById.ContainsKey(CanonicalSymbolId(implementation))
                )
                    AddReachable(type, pending);
            }
        }

        private IReadOnlyList<IMethodSymbol> DispatchTargets(IMethodSymbol method, IOperation? receiver)
        {
            INamedTypeSymbol? receiverType = receiver?.Type as INamedTypeSymbol;
            IEnumerable<INamedTypeSymbol> types = _symbolsById
                .Values.OfType<INamedTypeSymbol>()
                .Where(type => type.TypeKind is TypeKind.Class or TypeKind.Struct && !type.IsAbstract);
            if (
                receiverType is not null
                && receiverType.TypeKind is TypeKind.Class or TypeKind.Struct
                && !receiverType.IsAbstract
                && _symbolsById.ContainsKey(CanonicalSymbolId(receiverType))
            )
                types = types.Where(type => IsSameOrDerived(type, receiverType));

            var targets = new Dictionary<string, IMethodSymbol>(StringComparer.Ordinal);
            foreach (INamedTypeSymbol type in types.OrderBy(CanonicalTypeId, StringComparer.Ordinal))
            {
                IMethodSymbol? target =
                    method.ContainingType?.TypeKind == TypeKind.Interface
                        ? type.FindImplementationForInterfaceMember(method) as IMethodSymbol
                        : FindOverride(type, method);
                if (
                    target is not null
                    && IsSourceSymbol(target)
                    && _symbolsById.ContainsKey(CanonicalSymbolId(target))
                    && !target.IsAbstract
                )
                    targets[CanonicalSymbolId(target)] = target;
            }
            return targets.Values.OrderBy(CanonicalSymbolId, StringComparer.Ordinal).ToArray();
        }

        private static bool IsSameOrDerived(INamedTypeSymbol type, INamedTypeSymbol baseType)
        {
            for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
                if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, baseType.OriginalDefinition))
                    return true;
            return false;
        }

        private static IMethodSymbol? FindOverride(INamedTypeSymbol type, IMethodSymbol method)
        {
            foreach (IMethodSymbol candidate in type.GetMembers(method.Name).OfType<IMethodSymbol>())
            {
                for (IMethodSymbol? current = candidate; current is not null; current = current.OverriddenMethod)
                    if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, method.OriginalDefinition))
                        return candidate;
            }
            return
                method.ContainingType is not null
                && SymbolEqualityComparer.Default.Equals(
                    type.OriginalDefinition,
                    method.ContainingType.OriginalDefinition
                )
                ? method
                : null;
        }

        private void RememberDelegate(ILocalSymbol local, IOperation value)
        {
            if (TryGetDelegateTarget(value, out DelegateTarget? target))
            {
                if (_localDelegates.ContainsKey(local))
                {
                    _localDelegates.Remove(local);
                    _escapedLocals.Add(local);
                }
                else if (!_escapedLocals.Contains(local))
                    _localDelegates[local] = target!;
                return;
            }
            _localDelegates.Remove(local);
            _escapedLocals.Add(local);
        }

        private bool IsDirectDelegateValue(IOperation value) => TryGetDelegateTarget(value, out _);

        private bool IsSourceMethod(IMethodSymbol method) => _symbolsById.ContainsKey(CanonicalSymbolId(method));

        private bool SourceMethodConsumesDelegate(IMethodSymbol method, IParameterSymbol parameter)
        {
            foreach (
                Declaration declaration in _declarations.Where(item =>
                    SymbolEqualityComparer.Default.Equals(item.Symbol, method)
                    || StringComparer.Ordinal.Equals(CanonicalSymbolId(item.Symbol), CanonicalSymbolId(method))
                )
            )
            {
                IOperation? body = ExecutableOperation(declaration);
                if (body is not null && OperationConsumesDelegate(body, parameter))
                    return true;
            }

            return false;
        }

        private static bool OperationConsumesDelegate(IOperation operation, IParameterSymbol parameter)
        {
            if (operation.Kind is OperationKind.AnonymousFunction or OperationKind.LocalFunction)
                return false;
            if (operation is IInvocationOperation invocation)
            {
                if (
                    invocation.Instance is IParameterReferenceOperation instance
                    && SymbolEqualityComparer.Default.Equals(instance.Parameter, parameter)
                )
                    return true;
                if (
                    invocation.Arguments.Any(argument =>
                        argument.Value is IParameterReferenceOperation reference
                        && SymbolEqualityComparer.Default.Equals(reference.Parameter, parameter)
                    )
                )
                    return true;
            }

            return operation.ChildOperations.Any(child => OperationConsumesDelegate(child, parameter));
        }

        private bool BindDelegateArgumentToConsumers(
            IMethodSymbol method,
            IOperation? receiver,
            IParameterSymbol parameter,
            IOperation value,
            string callerId,
            IOperation callsite,
            SortedSet<string> pending
        )
        {
            IMethodSymbol[] targets =
                method.ContainingType?.TypeKind == TypeKind.Interface
                || method.IsVirtual
                || method.IsOverride
                || method.IsAbstract
                    ? DispatchTargets(method, receiver).ToArray()
                    : new[] { method };
            bool consumed = false;
            foreach (IMethodSymbol target in targets)
            {
                if (!SourceMethodConsumesDelegate(target, DelegateParameter(target, parameter)))
                    continue;
                consumed = true;
                if (TryResolveDelegateValue(value, out DelegateTarget? delegateTarget))
                {
                    _parameterDelegates[ParameterBindingKey(target, DelegateParameter(target, parameter))] =
                        delegateTarget!;
                    AddReachable(delegateTarget!.Symbol, pending);
                }
                else
                    AddDelegateDiagnostic(callerId, callsite);
            }

            return consumed;
        }

        private static IParameterSymbol DelegateParameter(IMethodSymbol target, IParameterSymbol sourceParameter) =>
            target.Parameters.FirstOrDefault(parameter => parameter.Ordinal == sourceParameter.Ordinal)
            ?? sourceParameter;

        private void AddExactOrDispatchTarget(
            IMethodSymbol method,
            IOperation? receiver,
            IOperation callsite,
            string callerId,
            SortedSet<string> pending
        )
        {
            if (IsBaseMemberAccess(callsite))
            {
                AddReachable(method, pending);
                return;
            }

            AddMethodTarget(method, receiver, callsite, callerId, pending);
        }

        private bool TryGetDelegateTarget(IOperation value, out DelegateTarget? target)
        {
            switch (value)
            {
                case IDelegateCreationOperation creation:
                    return TryGetDelegateTarget(creation.Target, out target);
                case IAnonymousFunctionOperation anonymous:
                    target = new DelegateTarget(anonymous.Symbol, Location(anonymous));
                    return true;
                case IMethodReferenceOperation method:
                    target = new DelegateTarget(method.Method, Location(method));
                    return true;
                case IConversionOperation conversion:
                    return TryGetDelegateTarget(conversion.Operand, out target);
                case IParenthesizedOperation parenthesized:
                    return TryGetDelegateTarget(parenthesized.Operand, out target);
                default:
                    target = null;
                    return false;
            }
        }

        private void FollowDelegateValue(
            IOperation? value,
            string callerId,
            SortedSet<string> pending,
            IOperation callsite
        )
        {
            if (value is null)
            {
                AddDelegateDiagnostic(callerId, callsite);
                return;
            }
            value = Unwrap(value);
            if (
                value is ILocalReferenceOperation local
                && _localDelegates.TryGetValue(local.Local, out DelegateTarget? target)
            )
            {
                AddReachable(target.Symbol, pending);
                return;
            }
            if (
                value is IParameterReferenceOperation parameter
                && _localDelegates.TryGetValue(parameter.Parameter, out target)
            )
            {
                AddReachable(target.Symbol, pending);
                return;
            }
            if (TryGetDelegateTarget(value, out DelegateTarget? direct))
            {
                AddReachable(direct!.Symbol, pending);
                return;
            }
            AddDelegateDiagnostic(callerId, callsite);
        }

        private bool TryResolveDelegateValue(IOperation value, out DelegateTarget? target)
        {
            value = Unwrap(value);
            if (value is ILocalReferenceOperation local && _localDelegates.TryGetValue(local.Local, out target))
                return true;
            if (
                value is IParameterReferenceOperation parameter
                && _localDelegates.TryGetValue(parameter.Parameter, out target)
            )
                return true;
            return TryGetDelegateTarget(value, out target);
        }

        private string ParameterBindingKey(IMethodSymbol method, IParameterSymbol parameter) =>
            $"{CanonicalSymbolId(method)}\0{parameter.Ordinal}";

        private static IOperation Unwrap(IOperation value)
        {
            while (value is IConversionOperation conversion)
                value = conversion.Operand;
            return value is IParenthesizedOperation parenthesized ? Unwrap(parenthesized.Operand) : value;
        }

        private static bool IsWriteAccess(IOperation operation) =>
            operation.Syntax.Parent is AssignmentExpressionSyntax assignment
                && assignment.Left.Span.Contains(operation.Syntax.SpanStart)
            || operation.Syntax.Parent is PrefixUnaryExpressionSyntax prefix
                && prefix.Kind() is SyntaxKind.PreIncrementExpression or SyntaxKind.PreDecrementExpression
            || operation.Syntax.Parent is PostfixUnaryExpressionSyntax postfix
                && postfix.Kind() is SyntaxKind.PostIncrementExpression or SyntaxKind.PostDecrementExpression;

        private static bool IsReadWriteAccess(IOperation operation) =>
            operation.Syntax.Parent is AssignmentExpressionSyntax assignment
                && assignment.Kind()
                    is SyntaxKind.AddAssignmentExpression
                        or SyntaxKind.SubtractAssignmentExpression
                        or SyntaxKind.MultiplyAssignmentExpression
                        or SyntaxKind.DivideAssignmentExpression
                        or SyntaxKind.ModuloAssignmentExpression
                        or SyntaxKind.AndAssignmentExpression
                        or SyntaxKind.ExclusiveOrAssignmentExpression
                        or SyntaxKind.OrAssignmentExpression
                        or SyntaxKind.LeftShiftAssignmentExpression
                        or SyntaxKind.RightShiftAssignmentExpression
                        or SyntaxKind.CoalesceAssignmentExpression
            || operation.Syntax.Parent is PrefixUnaryExpressionSyntax prefix
                && prefix.Kind() is SyntaxKind.PreIncrementExpression or SyntaxKind.PreDecrementExpression
            || operation.Syntax.Parent is PostfixUnaryExpressionSyntax postfix
                && postfix.Kind() is SyntaxKind.PostIncrementExpression or SyntaxKind.PostDecrementExpression;

        private static bool IsBaseMemberAccess(IOperation operation) =>
            operation.Syntax is MemberAccessExpressionSyntax { Expression: BaseExpressionSyntax }
            || operation.Syntax is MemberBindingExpressionSyntax binding
                && binding.Ancestors().OfType<ConditionalAccessExpressionSyntax>().FirstOrDefault()?.Expression
                    is BaseExpressionSyntax;

        private static bool TryGetConstantBoolean(IOperation operation, out bool value)
        {
            if (operation.ConstantValue is { HasValue: true, Value: bool constant })
            {
                value = constant;
                return true;
            }

            value = false;
            return false;
        }

        private static IMethodSymbol? EventAccessor(IEventReferenceOperation operation)
        {
            if (operation.Syntax.Parent is AssignmentExpressionSyntax assignment)
                return assignment.Kind() == SyntaxKind.SubtractAssignmentExpression
                    ? operation.Event.RemoveMethod
                    : operation.Event.AddMethod;
            return operation.Event.AddMethod;
        }

        private static bool IsDelegateType(ITypeSymbol type) => type.TypeKind == TypeKind.Delegate;

        private void AddDelegateDiagnostic(string callerId, IOperation callsite) =>
            AddDiagnostic(
                "unresolved-delegate-dispatch",
                callerId,
                "mutable or escaped delegate target cannot be closed statically",
                callsite
            );

        private void AddDiagnostic(string code, string subjectId, string message, IOperation callsite) =>
            AddDiagnostic(code, subjectId, message, Location(callsite));

        private void AddDiagnostic(string code, string subjectId, string message, SyntaxNode callsite) =>
            AddDiagnostic(code, subjectId, message, Location(callsite, _sourceByTree[callsite.SyntaxTree]));

        private void AddDiagnostic(string code, string subjectId, string message, string location)
        {
            if (
                _diagnostics.Any(item =>
                    item.Code == code
                    && item.SubjectId == subjectId
                    && item.Location == location
                    && item.Message == message
                )
            )
                return;
            _diagnostics.Add(new InventoryDiagnostic(code, subjectId, message, "", location));
        }

        private void AddReachable(ISymbol symbol, SortedSet<string> pending, bool expandType = false)
        {
            if (!IsSourceSymbol(symbol))
                return;
            string id = CanonicalSymbolId(symbol);
            if (!_symbolsById.ContainsKey(id))
                return;
            if (!_reachable.Add(id))
                return;
            if (symbol.ContainingType is INamedTypeSymbol containingType)
                _reachableTypes.Add(CanonicalTypeId(containingType));
            if (symbol is INamedTypeSymbol type)
                _reachableTypes.Add(CanonicalTypeId(type));
            if (expandType && symbol is INamedTypeSymbol rootType)
            {
                foreach (Declaration declaration in _declarations.Where(item => IsWithinType(item.Symbol, rootType)))
                    AddReachable(declaration.Symbol, pending);
            }
            pending.Add(id);
        }

        private readonly HashSet<string> _reachableTypes = new(StringComparer.Ordinal);

        private bool IsWithinType(ISymbol symbol, INamedTypeSymbol root)
        {
            for (
                INamedTypeSymbol? current = symbol.ContainingType;
                current is not null;
                current = current.ContainingType
            )
                if (StringComparer.Ordinal.Equals(CanonicalTypeId(current), CanonicalTypeId(root)))
                    return true;
            return symbol is INamedTypeSymbol type
                && StringComparer.Ordinal.Equals(CanonicalTypeId(type), CanonicalTypeId(root));
        }

        private bool IsReachable(ISymbol symbol) => !HasAuditedScopes || _reachable.Contains(CanonicalSymbolId(symbol));

        private bool IsReachableType(INamedTypeSymbol type) =>
            !HasAuditedScopes || _reachableTypes.Contains(CanonicalTypeId(type));

        public void CollectModelSurfaces(List<InventorySurface> surfaces, HashSet<string> ids)
        {
            // Callable declarations are the internal symbol graph used to resolve scopes and to
            // attach decisions, XML accesses, and markers to canonical parents. A declaration is
            // not itself a grammar-observable surface: emitting every method would make the
            // catalog denominator a noisy census of implementation details.
            foreach (
                IFieldSymbol member in _symbolsById
                    .Values.OfType<IFieldSymbol>()
                    .Where(field =>
                        field.ContainingType?.TypeKind == TypeKind.Enum && IsReachableType(field.ContainingType)
                    )
                    .OrderBy(CanonicalSymbolId, StringComparer.Ordinal)
            )
            {
                string parent = CanonicalSymbolId(member.ContainingType!);
                Add(
                    surfaces,
                    ids,
                    new InventorySurface(
                        $"model:enum/{parent}/{CanonicalIdCodec.Encode(member.Name)}",
                        "enum-member",
                        member.Name,
                        parent,
                        Location(member),
                        member.ConstantValue?.ToString()
                    )
                );
            }

            foreach (
                INamedTypeSymbol type in _symbolsById
                    .Values.OfType<INamedTypeSymbol>()
                    .Where(type => type.TypeKind != TypeKind.Interface && !type.IsAbstract && IsReachableType(type))
                    .Distinct(SymbolEqualityComparer.Default)
                    .Cast<INamedTypeSymbol>()
                    .OrderBy(CanonicalSymbolId, StringComparer.Ordinal)
            )
            {
                foreach (string rule in RuleNames(type))
                {
                    string typeId = CanonicalSymbolId(type);
                    Add(
                        surfaces,
                        ids,
                        new InventorySurface(
                            $"model:rule/{typeId}/{rule}",
                            "rule-implementation",
                            typeId,
                            null,
                            Location(type),
                            rule
                        )
                    );
                }
            }
        }

        public IReadOnlyList<ResolvedXml> CollectXmlCandidates()
        {
            foreach (
                (SyntaxTree tree, SourceInput source) in _sourceByTree.OrderBy(
                    pair => pair.Value.Path,
                    StringComparer.Ordinal
                )
            )
            {
                SemanticModel model = _models[tree];
                foreach (
                    InvocationExpressionSyntax invocation in tree.GetRoot()
                        .DescendantNodes()
                        .OfType<InvocationExpressionSyntax>()
                )
                {
                    if (HasAuditedScopes && !Audited(model, invocation))
                        continue;
                    string? name = InvocationName(invocation);
                    if (name is not null && XmlNames.Contains(name))
                        CollectXml(model, source, invocation, name);
                }
            }

            return _xml.Select(candidate => new ResolvedXml(
                    candidate.Parent,
                    candidate.Method,
                    candidate.Constant,
                    candidate.Kind,
                    candidate.Source.Path,
                    candidate.Node.SpanStart,
                    Location(candidate.Node, candidate.Source),
                    candidate.Value
                ))
                .ToArray();
        }

        public IReadOnlyList<ResolvedDecision> CollectDecisionCandidates()
        {
            foreach (
                (SyntaxTree tree, SourceInput source) in _sourceByTree.OrderBy(
                    pair => pair.Value.Path,
                    StringComparer.Ordinal
                )
            )
            {
                SemanticModel model = _models[tree];
                foreach (SyntaxNode node in tree.GetRoot().DescendantNodes())
                {
                    if (!Audited(model, node))
                        continue;
                    switch (node)
                    {
                        case IfStatementSyntax:
                            Decision(model, source, node, "if", "true");
                            Decision(model, source, node, "if", "false");
                            break;
                        case SwitchStatementSyntax statement:
                            foreach (
                                SwitchLabelSyntax label in statement.Sections.SelectMany(section => section.Labels)
                            )
                                Decision(
                                    model,
                                    source,
                                    label,
                                    "switch",
                                    label is DefaultSwitchLabelSyntax ? "default" : label.ToString()
                                );
                            break;
                        case SwitchExpressionSyntax expression:
                            foreach (SwitchExpressionArmSyntax arm in expression.Arms)
                                Decision(model, source, arm, "switch-expression", arm.Pattern.ToString());
                            break;
                        case ConditionalExpressionSyntax:
                            Decision(model, source, node, "conditional", "true");
                            Decision(model, source, node, "conditional", "false");
                            break;
                        case ConditionalAccessExpressionSyntax:
                            Decision(model, source, node, "conditional-access", "present");
                            Decision(model, source, node, "conditional-access", "null");
                            break;
                        case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.CoalesceExpression):
                            Decision(model, source, node, "coalesce", "left-nonnull");
                            Decision(model, source, node, "coalesce", "right");
                            break;
                        case CatchClauseSyntax clause:
                            Decision(model, source, node, "catch", "true");
                            Decision(model, source, node, "catch", "false");
                            if (clause.Filter is not null)
                            {
                                Decision(model, source, clause.Filter, "catch-filter", "true");
                                Decision(model, source, clause.Filter, "catch-filter", "false");
                            }
                            break;
                        case ForStatementSyntax
                        or ForEachStatementSyntax
                        or ForEachVariableStatementSyntax
                        or WhileStatementSyntax
                        or DoStatementSyntax:
                            Decision(model, source, node, "loop", "natural-exit");
                            break;
                        case BreakStatementSyntax when NearestLoop(node) is not null:
                            Decision(model, source, node, "loop", "break");
                            break;
                        case ContinueStatementSyntax when NearestLoop(node) is not null:
                            Decision(model, source, node, "loop", "continue");
                            break;
                    }
                }
            }

            return _decisions
                .Select(candidate => new ResolvedDecision(
                    candidate.Parent,
                    candidate.Kind,
                    candidate.Branch,
                    candidate.Source.Path,
                    candidate.Node.SpanStart,
                    Fingerprint(candidate.Node),
                    Location(candidate.Node, candidate.Source)
                ))
                .ToArray();
        }

        // ToString() drops surrounding trivia; a disabled #if block is leading trivia of the
        // next statement, so ToFullString() would fingerprint one node differently per configuration.
        private static string Fingerprint(SyntaxNode node) =>
            Convert
                .ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(node.NormalizeWhitespace().ToString())))
                .ToLowerInvariant()[..16];

        private void CollectXml(SemanticModel model, SourceInput source, InvocationExpressionSyntax node, string name)
        {
            IMethodSymbol? method = model.GetSymbolInfo(node).Symbol as IMethodSymbol;
            ExpressionSyntax? receiver =
                (node.Expression as MemberAccessExpressionSyntax)?.Expression
                ?? (
                    node.Expression is MemberBindingExpressionSyntax
                        ? node.Ancestors().OfType<ConditionalAccessExpressionSyntax>().FirstOrDefault()?.Expression
                        : null
                );
            ITypeSymbol? receiverType = receiver is null ? null : model.GetTypeInfo(receiver).Type;
            bool receiverIsXml = IsXmlReceiver(receiverType);
            bool receiverIsUnresolvedXml =
                receiverType is IErrorTypeSymbol errorType && (errorType.Name is "XElement" or "XContainer")
                || receiver is not null && IsUnresolvedAliasReceiver(model, receiver, "XElement", "XContainer");
            if (!IsXml(method, name))
            {
                if (receiverType is not null && !receiverIsXml && !receiverIsUnresolvedXml)
                    return;
                if (receiverType is null && method is not null)
                    return;
            }
            string? constant = null;
            string kind;
            string? value = null;
            if (IsXml(method, name))
            {
                if (name == "Elements" && node.ArgumentList.Arguments.Count == 0)
                {
                    constant = "xml-all-elements";
                    kind = "xml-all-elements";
                }
                else if (node.ArgumentList.Arguments.Count == 1)
                {
                    constant = ConstantString(model, node.ArgumentList.Arguments[0].Expression);
                    kind = constant is null ? "dynamic-xml-access" : "xml-read";
                    value = constant is null ? node.ArgumentList.Arguments[0].Expression.ToString() : name;
                }
                else
                {
                    kind = "unresolved-xml-access";
                    value = node.ToString();
                }
            }
            else
            {
                kind = "unresolved-xml-access";
                value = node.ToString();
            }
            _xml.Add(new XmlCandidate(ContainingId(model, node), name, constant, kind, node, source, value));
        }

        private void Decision(SemanticModel model, SourceInput source, SyntaxNode node, string kind, string branch)
        {
            string parent = ContainingId(model, node);
            _decisions.Add(new DecisionCandidate(parent, kind, branch, node, source));
        }

        private bool Audited(SemanticModel model, SyntaxNode node)
        {
            if (_sources.All(source => source.Scopes.Count == 0))
                return false;
            ISymbol? containing = model.GetEnclosingSymbol(node.SpanStart);
            return containing is not null && IsReachable(containing);
        }

        private string ContainingId(SemanticModel model, SyntaxNode node)
        {
            for (
                ISymbol? current = model.GetEnclosingSymbol(node.SpanStart);
                current is not null;
                current = current.ContainingSymbol
            )
                if (IsSourceSymbol(current))
                    return CanonicalSymbolId(current);
            return "<global>";
        }

        private string CanonicalSymbolId(ISymbol symbol)
        {
            if (_symbolIds.TryGetValue(symbol, out string? value))
                return value;
            string id = symbol switch
            {
                INamedTypeSymbol type => CanonicalTypeId(type),
                IMethodSymbol method => CanonicalMethodId(method),
                IPropertySymbol property => CanonicalPropertyId(property),
                IFieldSymbol field => $"{CanonicalTypeId(field.ContainingType!)}.{field.Name}",
                IEventSymbol @event => $"{CanonicalTypeId(@event.ContainingType!)}.{@event.Name}",
                _ => CleanGlobal(symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
            };
            _symbolIds[symbol] = id;
            return id;
        }

        private string CanonicalTypeId(INamedTypeSymbol type)
        {
            INamedTypeSymbol definition = type.OriginalDefinition;
            if (!_typeIds.TryGetValue(definition, out string? id))
            {
                string name = definition.Name + (definition.Arity == 0 ? string.Empty : $"`{definition.Arity}");
                id =
                    definition.ContainingType is not null ? $"{CanonicalTypeId(definition.ContainingType)}.{name}"
                    : definition.ContainingNamespace is { IsGlobalNamespace: false } ns
                        ? $"{CleanGlobal(ns.ToDisplayString())}.{name}"
                    : name;
                _typeIds[definition] = id;
            }

            if (SymbolEqualityComparer.Default.Equals(type, definition) || type.TypeArguments.Length == 0)
                return id;

            return $"{id}<{string.Join(",", type.TypeArguments.Select(TypeDisplay))}>";
        }

        private string CanonicalMethodId(IMethodSymbol method)
        {
            IMethodSymbol definition = method.OriginalDefinition;
            if (definition.MethodKind == MethodKind.LocalFunction && definition.ContainingSymbol is not null)
            {
                string id = LocalFunctionBaseId(definition);
                return _localFunctionOrdinals.TryGetValue(definition, out int ordinal) ? $"{id}#{ordinal}" : id;
            }

            if (definition.MethodKind == MethodKind.AnonymousFunction && definition.ContainingSymbol is not null)
            {
                // Disabled preprocessor regions still occupy source spans. A source-location
                // identity therefore remains stable when another configuration adds or removes
                // a gated lambda before this one; ordinal-by-visible-lambdas does not.
                return $"{CanonicalSymbolId(definition.ContainingSymbol)}/lambda@{CanonicalIdCodec.Encode(LocalFunctionSourcePath(definition))}:{LocalFunctionSourceStart(definition)}";
            }

            if (
                definition.AssociatedSymbol is IPropertySymbol property
                && definition.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet
            )
            {
                return $"{CanonicalPropertyId(property)}/{(definition.MethodKind == MethodKind.PropertyGet ? "get" : "set")}";
            }

            if (
                definition.AssociatedSymbol is IEventSymbol @event
                && definition.MethodKind is MethodKind.EventAdd or MethodKind.EventRemove
            )
            {
                return $"{CanonicalEventId(@event)}/{(definition.MethodKind == MethodKind.EventAdd ? "add" : "remove")}";
            }

            return MethodSignature(definition);
        }

        private string MethodSignature(IMethodSymbol method)
        {
            string prefix = method.ContainingType is null ? string.Empty : CanonicalTypeId(method.ContainingType) + ".";
            return $"{prefix}{CallableName(method)}({Parameters(method)})";
        }

        private static string CallableName(IMethodSymbol method)
        {
            string name = method.MethodKind switch
            {
                MethodKind.Constructor => ".ctor",
                MethodKind.StaticConstructor => ".cctor",
                MethodKind.Destructor => ".dtor",
                MethodKind.UserDefinedOperator => "operator-" + method.MetadataName,
                MethodKind.Conversion => "conversion-" + method.MetadataName,
                _ => method.Name,
            };
            if (method.Arity > 0)
                name += "`" + method.Arity;
            return name;
        }

        private string Parameters(IMethodSymbol method) =>
            string.Join(",", method.Parameters.Select(parameter => RefPrefix(parameter) + TypeDisplay(parameter.Type)));

        private string CanonicalPropertyId(IPropertySymbol property)
        {
            string prefix = property.ContainingType is null
                ? string.Empty
                : CanonicalTypeId(property.ContainingType) + ".";
            if (!property.IsIndexer)
                return prefix + property.Name;
            return $"{prefix}this({string.Join(",", property.Parameters.Select(parameter => RefPrefix(parameter) + TypeDisplay(parameter.Type)))})";
        }

        private string CanonicalEventId(IEventSymbol @event) =>
            $"{CanonicalTypeId(@event.ContainingType!)}.{@event.Name}";

        private static string RefPrefix(IParameterSymbol parameter)
        {
            if (parameter.IsParams)
                return "params ";
            return parameter.RefKind switch
            {
                RefKind.Ref => "ref ",
                RefKind.Out => "out ",
                RefKind.In => "in ",
                RefKind.RefReadOnlyParameter => "ref readonly ",
                _ => string.Empty,
            };
        }

        private string TypeDisplay(ITypeSymbol type)
        {
            if (type is ITypeParameterSymbol typeParameter)
            {
                return (typeParameter.TypeParameterKind == TypeParameterKind.Method ? "!!" : "!")
                    + typeParameter.Ordinal;
            }
            if (type is IArrayTypeSymbol array)
            {
                string rank = array.Rank == 1 ? "[]" : $"[{new string(',', array.Rank - 1)}]";
                return TypeDisplay(array.ElementType) + rank;
            }
            if (type is IPointerTypeSymbol pointer)
                return TypeDisplay(pointer.PointedAtType) + "*";
            if (type is INamedTypeSymbol named)
                return CanonicalTypeId(named);

            string? special = type.SpecialType switch
            {
                SpecialType.System_Boolean => "System.Boolean",
                SpecialType.System_Byte => "System.Byte",
                SpecialType.System_Char => "System.Char",
                SpecialType.System_Decimal => "System.Decimal",
                SpecialType.System_Double => "System.Double",
                SpecialType.System_Int16 => "System.Int16",
                SpecialType.System_Int32 => "System.Int32",
                SpecialType.System_Int64 => "System.Int64",
                SpecialType.System_Object => "System.Object",
                SpecialType.System_SByte => "System.SByte",
                SpecialType.System_Single => "System.Single",
                SpecialType.System_String => "System.String",
                SpecialType.System_UInt16 => "System.UInt16",
                SpecialType.System_UInt32 => "System.UInt32",
                SpecialType.System_UInt64 => "System.UInt64",
                _ => null,
            };
            return special ?? CleanGlobal(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        private static string CleanGlobal(string value) => value.Replace("global::", "", StringComparison.Ordinal);

        private IReadOnlyList<string> RuleNames(INamedTypeSymbol type)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            INamedTypeSymbol? hc = _compilation.GetTypeByMetadataName(HcRuleMetadata);
            INamedTypeSymbol? phon = _compilation.GetTypeByMetadataName(PhonRuleMetadata);
            INamedTypeSymbol? morph = _compilation.GetTypeByMetadataName(MorphRuleMetadata);
            INamedTypeSymbol? machine = _compilation.GetTypeByMetadataName(MachineRuleMetadata);
            foreach (INamedTypeSymbol implemented in type.AllInterfaces)
            {
                INamedTypeSymbol original = implemented.OriginalDefinition;
                if (hc is not null && SymbolEqualityComparer.Default.Equals(original, hc))
                    names.Add("IHCRule");
                if (phon is not null && SymbolEqualityComparer.Default.Equals(original, phon))
                    names.Add("IPhonologicalRule");
                if (morph is not null && SymbolEqualityComparer.Default.Equals(original, morph))
                    names.Add("IMorphologicalRule");
                if (machine is not null && SymbolEqualityComparer.Default.Equals(original, machine))
                    names.Add("IRule");
            }
            return names.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        private bool IsXml(IMethodSymbol? method, string name)
        {
            if (method is null || method.Name != name)
                return false;
            INamedTypeSymbol? owner = method.ContainingType?.OriginalDefinition;
            INamedTypeSymbol? element = _compilation.GetTypeByMetadataName(XElementMetadata);
            INamedTypeSymbol? container = _compilation.GetTypeByMetadataName(XContainerMetadata);
            if (name == "Attribute")
            {
                if (element is null || !SymbolEqualityComparer.Default.Equals(owner, element))
                    return false;
            }
            else if (
                name == "Element"
                && (container is null || !SymbolEqualityComparer.Default.Equals(owner, container))
            )
            {
                return false;
            }
            else if (
                name == "Elements"
                && (container is null || !SymbolEqualityComparer.Default.Equals(owner, container))
            )
            {
                return false;
            }
            INamedTypeSymbol? xname = _compilation.GetTypeByMetadataName(XNameMetadata);
            if (name == "Elements" && method.Parameters.Length == 0)
                return true;
            return method.Parameters.Length == 1
                && xname is not null
                && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, xname);
        }

        private static bool IsXmlReceiver(ITypeSymbol? type)
        {
            if (type is null)
                return false;
            string value = CleanGlobal(
                type.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            );
            return value is XElementMetadata or XContainerMetadata;
        }

        private static bool IsUnresolvedAliasReceiver(
            SemanticModel model,
            ExpressionSyntax receiver,
            params string[] targetNames
        )
        {
            ISymbol? receiverSymbol = model.GetSymbolInfo(receiver).Symbol;
            TypeSyntax? declaredType = receiverSymbol
                ?.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax())
                .Select(node =>
                    node switch
                    {
                        ParameterSyntax parameter => parameter.Type,
                        VariableDeclaratorSyntax variable
                            when variable.Parent?.Parent is VariableDeclarationSyntax declaration => declaration.Type,
                        _ => null,
                    }
                )
                .FirstOrDefault(type => type is not null);
            return declaredType is not null && IsUnresolvedAlias(model, declaredType, targetNames);
        }

        private static bool IsUnresolvedAlias(SemanticModel model, SyntaxNode use, params string[] targetNames)
        {
            string? alias = use switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                GenericNameSyntax generic => generic.Identifier.ValueText,
                _ => null,
            };
            return alias is not null
                && IsUnresolvedAlias(model, use, alias, targetNames, new HashSet<string>(StringComparer.Ordinal));
        }

        private static bool IsUnresolvedAlias(
            SemanticModel model,
            SyntaxNode use,
            string alias,
            IReadOnlyCollection<string> targetNames,
            HashSet<string> visited
        )
        {
            if (!visited.Add(alias))
                return false;
            foreach (
                UsingDirectiveSyntax usingDirective in model
                    .SyntaxTree.GetRoot()
                    .DescendantNodes()
                    .OfType<UsingDirectiveSyntax>()
                    .Where(directive =>
                        directive.Alias?.Name.Identifier.ValueText == alias
                        && directive.Name is not null
                        && IsUsingApplicable(directive, use)
                    )
            )
            {
                NameSyntax target = usingDirective.Name!;
                if (
                    IsUnresolvedSymbol(model.GetSymbolInfo(target).Symbol)
                    && targetNames.Contains(TerminalName(target), StringComparer.Ordinal)
                )
                    return true;
                if (
                    target is IdentifierNameSyntax nested
                    && IsUnresolvedAlias(model, use, nested.Identifier.ValueText, targetNames, visited)
                )
                    return true;
            }
            return false;
        }

        private static bool IsUsingApplicable(UsingDirectiveSyntax usingDirective, SyntaxNode use) =>
            usingDirective.Parent is CompilationUnitSyntax
            || usingDirective.Parent is BaseNamespaceDeclarationSyntax owner
                && use.AncestorsAndSelf().Any(ancestor => ReferenceEquals(ancestor, owner));

        private static bool IsUnresolvedSymbol(ISymbol? symbol) =>
            symbol is null or IErrorTypeSymbol || symbol is IAliasSymbol { Target: IErrorTypeSymbol };

        private static string TerminalName(NameSyntax name) =>
            name switch
            {
                QualifiedNameSyntax qualified => TerminalName(qualified.Right),
                AliasQualifiedNameSyntax aliasQualified => TerminalName(aliasQualified.Name),
                GenericNameSyntax generic => generic.Identifier.ValueText,
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                _ => name.ToString().Split('.').Last(),
            };

        private static string? ConstantString(SemanticModel model, ExpressionSyntax? expression)
        {
            if (expression is null)
                return null;
            Optional<object?> constant = model.GetConstantValue(expression);
            return constant.HasValue && constant.Value is string value ? value : null;
        }

        private static string? InvocationName(InvocationExpressionSyntax node) =>
            node.Expression switch
            {
                MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
                MemberBindingExpressionSyntax binding => binding.Name.Identifier.ValueText,
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                GenericNameSyntax generic => generic.Identifier.ValueText,
                _ => null,
            };

        private static bool IsSourceSymbol(ISymbol symbol) =>
            symbol switch
            {
                INamedTypeSymbol => true,
                IMethodSymbol method => method.MethodKind
                    is MethodKind.Ordinary
                        or MethodKind.Constructor
                        or MethodKind.StaticConstructor
                        or MethodKind.Destructor
                        or MethodKind.UserDefinedOperator
                        or MethodKind.Conversion
                        or MethodKind.LocalFunction
                        or MethodKind.AnonymousFunction
                        or MethodKind.PropertyGet
                        or MethodKind.PropertySet
                        or MethodKind.EventAdd
                        or MethodKind.EventRemove,
                IPropertySymbol or IFieldSymbol or IEventSymbol => true,
                _ => false,
            };

        private string Location(IOperation operation) =>
            Location(operation.Syntax, _sourceByTree[operation.Syntax.SyntaxTree]);

        private string Location(ISymbol symbol)
        {
            Location? location = symbol
                .Locations.Where(item => item.IsInSource)
                .OrderBy(item => item.SourceTree?.FilePath, StringComparer.Ordinal)
                .ThenBy(item => item.SourceSpan.Start)
                .FirstOrDefault();
            return location?.SourceTree is null
                ? "<metadata>"
                : Location(location.SourceTree, location.SourceSpan, _sourceByTree[location.SourceTree]);
        }

        private static string Location(SyntaxNode node, SourceInput source) =>
            Location(node.SyntaxTree, node.Span, source);

        private static string Location(SyntaxTree tree, TextSpan span, SourceInput source)
        {
            FileLinePositionSpan line = tree.GetLineSpan(span);
            return $"{source.Path}:{line.StartLinePosition.Line + 1}:{line.StartLinePosition.Character + 1}-{line.EndLinePosition.Line + 1}:{line.EndLinePosition.Character + 1}";
        }

        private static SyntaxNode? NearestLoop(SyntaxNode node) =>
            node.Ancestors()
                .FirstOrDefault(ancestor =>
                    ancestor
                        is ForStatementSyntax
                            or ForEachStatementSyntax
                            or ForEachVariableStatementSyntax
                            or WhileStatementSyntax
                            or DoStatementSyntax
                );
    }
}
