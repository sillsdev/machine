#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>A stable identity for a definition owned by one admitted repository project.</summary>
internal sealed record OwnedSymbolKey
{
    private OwnedSymbolKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("An owned symbol key is required.", nameof(value));
        Value = value;
    }

    internal string Value { get; }

    internal static OwnedSymbolKey Create(string projectId, ISymbol symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentNullException.ThrowIfNull(symbol);
        if (symbol.ContainingAssembly is not IAssemblySymbol assembly)
            throw new CompilerInputException("external-symbol", "The symbol has no containing assembly.");

        if (symbol is IMethodSymbol { MethodKind: MethodKind.LocalFunction or MethodKind.AnonymousFunction })
        {
            throw new CompilerInputException(
                "owned-symbol-context-required",
                "Local functions and lambdas require compilation context."
            );
        }
        ISymbol canonical = symbol is INamedTypeSymbol ? symbol : symbol.OriginalDefinition;
        return Create(projectId, assembly.Identity, CanonicalSymbolId(canonical));
    }

    internal static OwnedSymbolKey Create(string projectId, AssemblyIdentity assembly, string canonicalSymbolId) =>
        new($"owned:{projectId}/{AssemblyIdentity(assembly)}/{canonicalSymbolId}");

    internal static string CanonicalId(ISymbol symbol) => CanonicalSymbolId(symbol);

    internal static string CanonicalIdForParameter(IParameterSymbol parameter) => ParameterType(parameter);

    private static string AssemblyIdentity(AssemblyIdentity identity)
    {
        string culture = string.IsNullOrEmpty(identity.CultureName) ? "neutral" : identity.CultureName;
        string token = identity.PublicKeyToken.IsDefaultOrEmpty
            ? "null"
            : Convert.ToHexString(identity.PublicKeyToken.ToArray()).ToLowerInvariant();
        return $"{identity.Name},Version={identity.Version},Culture={culture},PublicKeyToken={token}";
    }

    private static string CanonicalSymbolId(ISymbol symbol)
    {
        return symbol switch
        {
            INamespaceSymbol ns => NamespaceId(ns),
            INamedTypeSymbol type => TypeId(type),
            IMethodSymbol method => MethodId(method),
            IPropertySymbol property => PropertyId(property),
            IFieldSymbol field => $"{TypeId(field.ContainingType!)}.{field.Name}",
            IEventSymbol @event => $"{TypeId(@event.ContainingType!)}.{@event.Name}",
            _ => throw new CompilerInputException(
                "unsupported-symbol-shape",
                $"Symbol kind '{symbol.Kind}' is not supported by the owned-symbol catalog."
            ),
        };
    }

    private static string NamespaceId(INamespaceSymbol ns)
    {
        return ns.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", string.Empty, StringComparison.Ordinal);
    }

    private static string TypeId(INamedTypeSymbol type)
    {
        INamedTypeSymbol definition = type.OriginalDefinition;
        string name = definition.Name + (definition.Arity == 0 ? string.Empty : $"`{definition.Arity}");
        string id =
            type.ContainingType is not null ? $"{TypeId(type.ContainingType)}.{name}"
            : definition.ContainingNamespace is { IsGlobalNamespace: false } ns ? $"{NamespaceId(ns)}.{name}"
            : name;

        if (SymbolEqualityComparer.Default.Equals(type, definition) || type.TypeArguments.Length == 0)
            return id;
        return $"{id}<{string.Join(",", type.TypeArguments.Select(TypeDisplay))}>";
    }

    private static string MethodId(IMethodSymbol method)
    {
        IMethodSymbol definition = method.OriginalDefinition;
        if (
            definition.AssociatedSymbol is IPropertySymbol property
            && definition.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet
        )
        {
            return $"{PropertyId(property)}/{(definition.MethodKind == MethodKind.PropertyGet ? "get" : "set")}";
        }
        if (
            definition.AssociatedSymbol is IEventSymbol @event
            && definition.MethodKind is MethodKind.EventAdd or MethodKind.EventRemove
        )
        {
            return $"{TypeId(@event.ContainingType!)}.{@event.Name}/{(definition.MethodKind == MethodKind.EventAdd ? "add" : "remove")}";
        }
        string prefix = definition.ContainingType is null ? string.Empty : TypeId(definition.ContainingType) + ".";
        string name = definition.MethodKind switch
        {
            MethodKind.Constructor => ".ctor",
            MethodKind.StaticConstructor => ".cctor",
            MethodKind.Destructor => ".dtor",
            MethodKind.UserDefinedOperator => "operator-" + definition.MetadataName,
            MethodKind.Conversion => "conversion-" + definition.MetadataName,
            _ => definition.Name,
        };
        if (definition.Arity > 0)
            name += "`" + definition.Arity;

        string result = $"{prefix}{name}({string.Join(",", definition.Parameters.Select(ParameterType))})";
        // Both implicit and explicit conversions have the same metadata name and
        // parameter list. Their target type is therefore part of their identity.
        return definition.MethodKind == MethodKind.Conversion
            ? result + "->" + TypeDisplay(definition.ReturnType)
            : result;
    }

    private static string PropertyId(IPropertySymbol property)
    {
        string prefix = property.ContainingType is null ? string.Empty : TypeId(property.ContainingType) + ".";
        return property.IsIndexer
            ? $"{prefix}this({string.Join(",", property.Parameters.Select(ParameterType))})"
            : prefix + property.Name;
    }

    private static string ParameterType(IParameterSymbol parameter)
    {
        string modifier = parameter.IsParams
            ? "params "
            : parameter.RefKind switch
            {
                RefKind.Ref => "ref ",
                RefKind.Out => "out ",
                RefKind.In => "in ",
                RefKind.RefReadOnlyParameter => "ref readonly ",
                _ => string.Empty,
            };
        return modifier + TypeDisplay(parameter.Type);
    }

    private static string TypeDisplay(ITypeSymbol type)
    {
        if (type is ITypeParameterSymbol typeParameter)
            return (typeParameter.TypeParameterKind == TypeParameterKind.Method ? "!!" : "!") + typeParameter.Ordinal;
        if (type is IArrayTypeSymbol array)
            return TypeDisplay(array.ElementType) + (array.Rank == 1 ? "[]" : $"[{new string(',', array.Rank - 1)}]");
        if (type is IPointerTypeSymbol pointer)
            return TypeDisplay(pointer.PointedAtType) + "*";
        if (type is INamedTypeSymbol named)
            return TypeId(named);
        if (type is IDynamicTypeSymbol)
            return "dynamic";

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
        return special
            ?? type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .Replace("global::", string.Empty, StringComparison.Ordinal);
    }
}

/// <summary>Bridges profile-local Roslyn symbols to original owned definitions.</summary>
internal sealed class OwnedSymbolBridge
{
    private static readonly IReadOnlyDictionary<string, string> ProjectByAssemblyName = new Dictionary<string, string>(
        StringComparer.Ordinal
    )
    {
        ["SIL.Machine"] = "machine",
        ["SIL.Machine.Morphology.HermitCrab"] = "hc",
        ["hc"] = "hc-tool",
        ["hc-conformance"] = "hc-conformance",
    };

    private readonly IReadOnlyDictionary<string, ProfileIndex> _profiles;
    private readonly IReadOnlyDictionary<IAssemblySymbol, string> _assemblyProfiles = new Dictionary<
        IAssemblySymbol,
        string
    >(ReferenceEqualityComparer.Instance);

    private OwnedSymbolBridge(
        IReadOnlyDictionary<string, ProfileIndex> profiles,
        IReadOnlyDictionary<IAssemblySymbol, string> assemblyProfiles
    )
    {
        _profiles = profiles;
        _assemblyProfiles = assemblyProfiles;
    }

    internal static OwnedSymbolBridge Create(RoslynCompilationGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        var profiles = new Dictionary<string, ProfileIndex>(StringComparer.Ordinal);
        var assemblyProfiles = new Dictionary<IAssemblySymbol, string>(ReferenceEqualityComparer.Instance);

        foreach (
            RoslynCompilationNode node in graph
                .Nodes.Values.OrderBy(item => item.Key.ProfileId, StringComparer.Ordinal)
                .ThenBy(item => item.Key.ProjectId, StringComparer.Ordinal)
        )
        {
            if (!profiles.TryGetValue(node.Key.ProfileId, out ProfileIndex? profile))
            {
                profile = new ProfileIndex();
                profiles.Add(node.Key.ProfileId, profile);
            }

            RegisterAssembly(node.Compilation.Assembly, node.Key.ProfileId, assemblyProfiles);
            foreach (
                IAssemblySymbol assembly in node.Compilation.SourceModule.ReferencedAssemblySymbols.Where(assembly =>
                    ProjectByAssemblyName.ContainsKey(assembly.Identity.Name)
                )
            )
            {
                RegisterAssembly(assembly, node.Key.ProfileId, assemblyProfiles);
            }

            string? expectedProject = ProjectByAssemblyName.GetValueOrDefault(node.Compilation.Assembly.Identity.Name);
            if (
                expectedProject is null
                || !string.Equals(expectedProject, node.Key.ProjectId, StringComparison.Ordinal)
            )
            {
                throw new CompilerInputException(
                    "unknown-owned-assembly",
                    $"Project '{node.Key.ProjectId}' has unexpected assembly '{node.Compilation.Assembly.Identity.Name}'."
                );
            }

            foreach (ISymbol symbol in Definitions(node.Compilation.Assembly.GlobalNamespace))
            {
                OwnedSymbolKey key = OwnedSymbolKey.Create(node.Key.ProjectId, symbol);
                if (!profile.Definitions.TryAdd(key.Value, symbol))
                {
                    throw new CompilerInputException(
                        "ambiguous-owned-key",
                        $"Duplicate owned symbol key '{key.Value}'."
                    );
                }
                profile.SymbolKeys.Add(symbol, key);
            }
            RegisterLocalExecutables(node, profile);
        }

        return new OwnedSymbolBridge(profiles, assemblyProfiles);
    }

    internal OwnedSymbolKey KeyFor(string profileId, ISymbol symbol)
    {
        ProfileIndex profile = Profile(profileId);
        ArgumentNullException.ThrowIfNull(symbol);
        if (symbol.ContainingAssembly is not IAssemblySymbol assembly)
            throw new CompilerInputException("external-symbol", "The symbol has no containing assembly.");
        string? project = ProjectByAssemblyName.GetValueOrDefault(assembly.Identity.Name);
        if (project is null)
        {
            throw new CompilerInputException(
                "external-symbol",
                $"Assembly '{assembly.Identity.Name}' is not an admitted owned project."
            );
        }
        if (!_assemblyProfiles.TryGetValue(assembly, out string? actualProfile))
        {
            throw new CompilerInputException(
                "unknown-symbol-profile",
                "The symbol does not belong to a captured compilation profile."
            );
        }
        if (!string.Equals(profileId, actualProfile, StringComparison.Ordinal))
        {
            throw new CompilerInputException(
                "cross-profile-symbol",
                $"The symbol belongs to profile '{actualProfile}', not '{profileId}'."
            );
        }

        if (profile.SymbolKeys.TryGetValue(symbol, out OwnedSymbolKey? contextualKey))
            return contextualKey;
        if (
            symbol is IMethodSymbol { MethodKind: MethodKind.LocalFunction or MethodKind.AnonymousFunction } contextual
            && profile.ContextualKeys.TryGetValue(ContextLookupKey(contextual), out contextualKey)
        )
        {
            return contextualKey;
        }
        OwnedSymbolKey key = OwnedSymbolKey.Create(project, symbol);
        if (!profile.Definitions.ContainsKey(key.Value))
        {
            if (
                symbol is INamedTypeSymbol type
                && !SymbolEqualityComparer.Default.Equals(type, type.OriginalDefinition)
            )
            {
                INamedTypeSymbol ownedConstruction = ConstructOwnedType(profile, project, type);
                lock (profile.Definitions)
                {
                    if (!profile.Definitions.TryGetValue(key.Value, out ISymbol? existing))
                    {
                        profile.Definitions.Add(key.Value, ownedConstruction);
                        profile.SymbolKeys[ownedConstruction] = key;
                    }
                    else if (!SymbolEqualityComparer.Default.Equals(existing, ownedConstruction))
                    {
                        throw new CompilerInputException(
                            "ambiguous-owned-key",
                            $"Duplicate owned symbol key '{key.Value}'."
                        );
                    }
                }
                profile.SymbolKeys[symbol] = key;
                return key;
            }
            throw new CompilerInputException("missing-owned-key", $"No owned definition exists for '{key.Value}'.");
        }
        return key;
    }

    internal ISymbol Resolve(string profileId, OwnedSymbolKey key)
    {
        ProfileIndex profile = Profile(profileId);
        ArgumentNullException.ThrowIfNull(key);
        ParsedKey parsed = Parse(key.Value);
        string assemblySimpleName = parsed.AssemblyIdentity.Split(',')[0];
        if (
            !ProjectByAssemblyName.TryGetValue(assemblySimpleName, out string? expectedProject)
            || !string.Equals(expectedProject, parsed.ProjectId, StringComparison.Ordinal)
        )
        {
            throw new CompilerInputException(
                "external-symbol",
                $"Owned key '{key.Value}' names an unadmitted assembly."
            );
        }
        if (!profile.Definitions.TryGetValue(key.Value, out ISymbol? symbol))
        {
            throw new CompilerInputException(
                "missing-owned-key",
                $"Owned key '{key.Value}' is not present in profile '{profileId}'."
            );
        }
        return symbol;
    }

    private static INamedTypeSymbol ConstructOwnedType(ProfileIndex profile, string projectId, INamedTypeSymbol type)
    {
        OwnedSymbolKey openKey = OwnedSymbolKey.Create(projectId, type.OriginalDefinition);
        if (
            !profile.Definitions.TryGetValue(openKey.Value, out ISymbol? openSymbol)
            || openSymbol is not INamedTypeSymbol openType
        )
        {
            throw new CompilerInputException(
                "missing-owned-key",
                $"No owned generic definition exists for '{openKey.Value}'."
            );
        }

        if (
            type.ContainingType is not null
            && !SymbolEqualityComparer.Default.Equals(type.ContainingType, type.ContainingType.OriginalDefinition)
        )
        {
            INamedTypeSymbol ownedContaining = ConstructOwnedType(profile, projectId, type.ContainingType);
            INamedTypeSymbol[] matches = ownedContaining.GetTypeMembers(type.Name, type.Arity).ToArray();
            if (matches.Length != 1)
            {
                throw new CompilerInputException(
                    "ambiguous-owned-key",
                    $"Constructed containing type has {matches.Length} nested matches for '{type.MetadataName}'."
                );
            }
            openType = matches[0];
        }

        return type.IsUnboundGenericType
            ? openType.ConstructUnboundGenericType()
            : openType.Construct(type.TypeArguments.ToArray());
    }

    private ProfileIndex Profile(string profileId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        if (!_profiles.TryGetValue(profileId, out ProfileIndex? profile))
            throw new CompilerInputException("unknown-profile", $"Unknown compilation profile '{profileId}'.");
        return profile;
    }

    private static IEnumerable<ISymbol> Definitions(INamespaceSymbol ns)
    {
        if (!ns.IsGlobalNamespace)
            yield return ns;
        foreach (INamespaceSymbol child in ns.GetNamespaceMembers().OrderBy(item => item.Name, StringComparer.Ordinal))
        {
            foreach (ISymbol symbol in Definitions(child))
                yield return symbol;
        }
        foreach (
            INamedTypeSymbol type in ns.GetTypeMembers().OrderBy(item => item.MetadataName, StringComparer.Ordinal)
        )
        {
            foreach (ISymbol symbol in Definitions(type))
                yield return symbol;
        }
    }

    private static IEnumerable<ISymbol> Definitions(INamedTypeSymbol type)
    {
        yield return type;
        foreach (
            INamedTypeSymbol nested in type.GetTypeMembers().OrderBy(item => item.MetadataName, StringComparer.Ordinal)
        )
        {
            foreach (ISymbol symbol in Definitions(nested))
                yield return symbol;
        }
        foreach (
            ISymbol member in type.GetMembers()
                .OrderBy(item => item.MetadataName, StringComparer.Ordinal)
                .ThenBy(item => item.Kind)
        )
        {
            if (member is INamedTypeSymbol)
                continue;
            yield return member;
        }
    }

    private static void RegisterAssembly(
        IAssemblySymbol assembly,
        string profileId,
        IDictionary<IAssemblySymbol, string> assemblyProfiles
    )
    {
        if (
            assemblyProfiles.TryGetValue(assembly, out string? existing)
            && !string.Equals(existing, profileId, StringComparison.Ordinal)
        )
        {
            throw new CompilerInputException(
                "ambiguous-symbol-profile",
                $"Assembly '{assembly.Identity.Name}' appears in multiple profiles."
            );
        }
        assemblyProfiles[assembly] = profileId;
    }

    private static void RegisterLocalExecutables(RoslynCompilationNode node, ProfileIndex profile)
    {
        var locals = new List<IMethodSymbol>();
        var lambdas = new List<IMethodSymbol>();
        foreach (SyntaxTree tree in node.Compilation.SyntaxTrees.OrderBy(tree => tree.FilePath, StringComparer.Ordinal))
        {
            SemanticModel model = node.Compilation.GetSemanticModel(tree);
            foreach (
                LocalFunctionStatementSyntax local in tree.GetRoot()
                    .DescendantNodes()
                    .OfType<LocalFunctionStatementSyntax>()
            )
            {
                if (model.GetDeclaredSymbol(local) is IMethodSymbol symbol)
                    locals.Add(symbol);
            }
            foreach (
                AnonymousFunctionExpressionSyntax lambda in tree.GetRoot()
                    .DescendantNodes()
                    .OfType<AnonymousFunctionExpressionSyntax>()
            )
            {
                if (model.GetOperation(lambda) is IAnonymousFunctionOperation operation)
                    lambdas.Add(operation.Symbol);
            }
        }

        var localIds = new Dictionary<IMethodSymbol, string>(SymbolEqualityComparer.Default);
        IMethodSymbol[] uniqueLocals = locals
            .Cast<ISymbol>()
            .Distinct(SymbolEqualityComparer.Default)
            .Cast<IMethodSymbol>()
            .ToArray();
        IMethodSymbol[] uniqueLambdas = lambdas
            .Cast<ISymbol>()
            .Distinct(SymbolEqualityComparer.Default)
            .Cast<IMethodSymbol>()
            .ToArray();
        foreach (
            IGrouping<int, IMethodSymbol> level in uniqueLocals.GroupBy(LocalNestingDepth).OrderBy(group => group.Key)
        )
        {
            foreach (
                IGrouping<string, IMethodSymbol> collision in level.GroupBy(
                    method => LocalBaseId(method, localIds),
                    StringComparer.Ordinal
                )
            )
            {
                IMethodSymbol[] ordered = collision
                    .OrderBy(SourcePath, StringComparer.Ordinal)
                    .ThenBy(SourceStart)
                    .ToArray();
                for (int index = 0; index < ordered.Length; index++)
                {
                    string suffix = ordered.Length > 1 ? $"#{index}" : string.Empty;
                    localIds.Add(ordered[index], collision.Key + suffix);
                }
            }
        }

        foreach (IMethodSymbol local in uniqueLocals)
            RegisterContextual(node, profile, local, localIds[local]);
        foreach (IMethodSymbol lambda in uniqueLambdas)
            RegisterContextual(node, profile, lambda, ContextualId(lambda, localIds));
    }

    private static void RegisterContextual(
        RoslynCompilationNode node,
        ProfileIndex profile,
        IMethodSymbol symbol,
        string canonicalId
    )
    {
        OwnedSymbolKey key = OwnedSymbolKey.Create(node.Key.ProjectId, node.Compilation.Assembly.Identity, canonicalId);
        if (!profile.Definitions.TryAdd(key.Value, symbol))
            throw new CompilerInputException("ambiguous-owned-key", $"Duplicate owned symbol key '{key.Value}'.");
        profile.SymbolKeys.Add(symbol, key);
        if (!profile.ContextualKeys.TryAdd(ContextLookupKey(symbol), key))
        {
            throw new CompilerInputException(
                "ambiguous-owned-key",
                $"Duplicate contextual symbol location for '{key.Value}'."
            );
        }
    }

    private static string LocalBaseId(IMethodSymbol method, IReadOnlyDictionary<IMethodSymbol, string> localIds) =>
        $"{ContextualId(method.ContainingSymbol!, localIds)}/local/{CallableSignature(method)}";

    private static string ContextualId(ISymbol symbol, IReadOnlyDictionary<IMethodSymbol, string> localIds)
    {
        if (
            symbol is IMethodSymbol { MethodKind: MethodKind.LocalFunction } local
            && localIds.TryGetValue(local, out string? localId)
        )
        {
            return localId;
        }
        if (symbol is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction } lambda)
        {
            return $"{ContextualId(lambda.ContainingSymbol!, localIds)}/lambda@{CanonicalIdCodec.Encode(SourcePath(lambda))}:{SourceStart(lambda)}";
        }
        return OwnedSymbolKey.CanonicalId(symbol.OriginalDefinition);
    }

    private static string CallableSignature(IMethodSymbol method)
    {
        string name = method.Name + (method.Arity == 0 ? string.Empty : $"`{method.Arity}");
        string parameters = string.Join(",", method.Parameters.Select(OwnedSymbolKey.CanonicalIdForParameter));
        return $"{name}({parameters})";
    }

    private static int LocalNestingDepth(IMethodSymbol method)
    {
        int depth = 0;
        for (
            ISymbol? current = method.ContainingSymbol;
            current is IMethodSymbol containing && containing.MethodKind == MethodKind.LocalFunction;
            current = current.ContainingSymbol
        )
        {
            depth++;
        }
        return depth;
    }

    private static string SourcePath(IMethodSymbol method) =>
        method
            .Locations.Where(location => location.IsInSource)
            .Select(location => location.SourceTree?.FilePath ?? string.Empty)
            .OrderBy(path => path, StringComparer.Ordinal)
            .FirstOrDefault()
        ?? string.Empty;

    private static int SourceStart(IMethodSymbol method) =>
        method
            .Locations.Where(location => location.IsInSource)
            .Select(location => location.SourceSpan.Start)
            .DefaultIfEmpty(-1)
            .Min();

    private static string ContextLookupKey(IMethodSymbol method) =>
        $"{method.ContainingAssembly.Identity}\0{method.MethodKind}\0{SourcePath(method)}\0{SourceStart(method)}";

    private sealed class ProfileIndex
    {
        internal Dictionary<string, ISymbol> Definitions { get; } = new(StringComparer.Ordinal);
        internal Dictionary<ISymbol, OwnedSymbolKey> SymbolKeys { get; } = new(ReferenceEqualityComparer.Instance);
        internal Dictionary<string, OwnedSymbolKey> ContextualKeys { get; } = new(StringComparer.Ordinal);
    }

    private readonly record struct ParsedKey(string ProjectId, string AssemblyIdentity, string SymbolId);

    private static ParsedKey Parse(string value)
    {
        if (!value.StartsWith("owned:", StringComparison.Ordinal))
            throw new CompilerInputException("invalid-owned-key", $"Invalid owned symbol key '{value}'.");
        string body = value["owned:".Length..];
        int first = body.IndexOf('/');
        int second = first < 0 ? -1 : body.IndexOf('/', first + 1);
        if (first <= 0 || second <= first + 1 || second == body.Length - 1)
            throw new CompilerInputException("invalid-owned-key", $"Invalid owned symbol key '{value}'.");
        return new ParsedKey(body[..first], body[(first + 1)..second], body[(second + 1)..]);
    }
}
