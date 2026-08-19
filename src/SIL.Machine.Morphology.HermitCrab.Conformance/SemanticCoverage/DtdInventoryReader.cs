#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

internal static class DtdInventoryReader
{
    private const string Profile = "sil.machine.hc-semantic-catalog/v1";

    private static readonly HashSet<string> AttributeTypes = new(StringComparer.Ordinal)
    {
        "CDATA", "ID", "IDREF", "IDREFS", "NMTOKEN", "NMTOKENS", "ENTITY", "ENTITIES",
    };

    public static SemanticInventory Read(string dtdPath, string dtdText)
    {
        ArgumentException.ThrowIfNullOrEmpty(dtdPath);
        ArgumentNullException.ThrowIfNull(dtdText);

        var parser = new Parser(dtdPath, dtdText);
        IReadOnlyList<InventorySurface> surfaces = parser.Parse();
        return new SemanticInventory(
            Profile,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dtdText))).ToLowerInvariant(),
            InventorySurfaceFactory.Sort(surfaces)
        );
    }

    private sealed class Parser(string path, string text)
    {
        private readonly string _path = path;
        private readonly string _text = text;
        private readonly List<InventorySurface> _surfaces = new();
        private readonly HashSet<string> _elementNames = new(StringComparer.Ordinal);
        private readonly HashSet<string> _attributeNames = new(StringComparer.Ordinal);
        private readonly HashSet<string> _surfaceIds = new(StringComparer.Ordinal);
        private int _index;

        public IReadOnlyList<InventorySurface> Parse()
        {
            while (true)
            {
                SkipWhitespaceAndComments();
                if (End)
                {
                    return _surfaces;
                }

                int declarationStart = _index;
                if (!StartsWith("<!"))
                {
                    Fail(declarationStart, "unexpected text outside a DTD declaration");
                }

                if (StartsWithDeclaration("<!ELEMENT"))
                {
                    ParseElementDeclaration(declarationStart);
                }
                else if (StartsWithDeclaration("<!ATTLIST"))
                {
                    ParseAttributeListDeclaration(declarationStart);
                }
                else if (StartsWithDeclaration("<!DOCTYPE"))
                {
                    ParseDoctypeDeclaration(declarationStart);
                }
                else
                {
                    Fail(declarationStart, "unsupported DTD declaration");
                }
            }
        }

        private bool End => _index >= _text.Length;

        private void ParseElementDeclaration(int start)
        {
            _index += "<!ELEMENT".Length;
            RequireDtdWhitespace("element name");
            string elementName = ReadName(start, "element name");
            if (!_elementNames.Add(elementName))
            {
                Fail(start, $"duplicate element declaration '{elementName}'");
            }

            RequireDtdWhitespace("element content model");
            ContentNode model = ParseContentModel(start);
            SkipWhitespace();
            RequireEndOfDeclaration(start, "ELEMENT");
            AddSurface(
                new InventorySurface(
                    $"dtd:element/{CanonicalIdCodec.Encode(elementName)}",
                    "element",
                    elementName,
                    null,
                    Location(start),
                    model.Kind
                )
            );

            EmitContentNode(elementName, model, null, "r", start);
        }

        private ContentNode ParseContentModel(int start)
        {
            if (TryReadKeyword("EMPTY"))
            {
                return ContentNode.Special("empty", 1, 1);
            }

            if (TryReadKeyword("ANY"))
            {
                return ContentNode.Special("any", 1, 1);
            }

            if (Peek('('))
            {
                ContentNode group = ParseGroup(start);
                (int min, int max) = ReadOccurrenceSuffix();
                ContentNode model = group with
                {
                    MinOccurs = min,
                    MaxOccurs = max,
                    GroupMinOccurs = min,
                    GroupMaxOccurs = max,
                };
                ValidateContentModel(model, start);
                return model;
            }

            Fail(_index, "expected EMPTY, ANY, or a parenthesized content model");
            return null!;
        }

        private void ValidateContentModel(ContentNode model, int declarationStart)
        {
            if (!ContainsPcdata(model))
            {
                return;
            }

            if (model.Kind == "pcdata" && model.MinOccurs == 1 && model.MaxOccurs == 1)
            {
                return;
            }

            if (model.Kind != "choice" || model.MinOccurs != 0 || model.MaxOccurs != int.MaxValue ||
                model.Children.Count < 2 || model.Children[0].Kind != "pcdata" ||
                model.Children[0].MinOccurs != 1 || model.Children[0].MaxOccurs != 1)
            {
                Fail(declarationStart, "invalid mixed content model; expected (#PCDATA) or (#PCDATA | Name ...)*");
            }

            var names = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 1; index < model.Children.Count; index++)
            {
                ContentNode child = model.Children[index];
                if (child.Kind != "element" || child.Name is null || child.MinOccurs != 1 || child.MaxOccurs != 1 ||
                    !names.Add(child.Name))
                {
                    Fail(declarationStart, "invalid mixed content model; expected unique unquantified element names after #PCDATA");
                }
            }
        }

        private static bool ContainsPcdata(ContentNode node) =>
            node.Kind == "pcdata" || node.Children.Any(ContainsPcdata);

        private ContentNode ParseGroup(int declarationStart)
        {
            Require('(', declarationStart, "content model group");
            SkipWhitespace();

            var children = new List<ContentNode>();
            char? separator = null;
            while (true)
            {
                if (End)
                {
                    Fail(declarationStart, "unterminated content model group");
                }

                ContentNode child = ParseContentTerm(declarationStart);
                children.Add(child);
                SkipWhitespace();

                if (TryConsume(')'))
                {
                    break;
                }

                if (End)
                {
                    Fail(declarationStart, "unterminated content model group");
                }

                char next = Current;
                if (next is not (',' or '|'))
                {
                    Fail(_index, "expected ',' or '|' in content model group");
                }

                if (separator is null)
                {
                    separator = next;
                }
                else if (separator != next)
                {
                    Fail(_index, "content model group cannot mix ',' and '|' separators");
                }

                _index++;
                SkipWhitespace();
            }

            const int Min = 1;
            const int Max = 1;
            if (separator is null && children.Count == 1 && children[0].Kind == "pcdata" && Min == 1 && Max == 1)
            {
                return children[0];
            }

            string kind = separator == '|' ? "choice" : "sequence";
            return ContentNode.Group(kind, children, Min, Max);
        }

        private ContentNode ParseContentTerm(int declarationStart)
        {
            ContentNode term;
            if (Peek('('))
            {
                term = ParseGroup(declarationStart);
            }
            else if (StartsWith("#PCDATA"))
            {
                _index += "#PCDATA".Length;
                term = ContentNode.Special("pcdata", 1, 1);
            }
            else
            {
                string name = ReadName(declarationStart, "content model element");
                term = ContentNode.Element(name, 1, 1);
            }

            (int min, int max) = ReadOccurrenceSuffix();
            if (term.IsGroup)
            {
                return term with
                {
                    MinOccurs = min,
                    MaxOccurs = max,
                    GroupMinOccurs = min,
                    GroupMaxOccurs = max,
                };
            }

            return term with
            {
                MinOccurs = min,
                MaxOccurs = max,
            };
        }

        private void EmitContentNode(
            string parent,
            ContentNode node,
            string? groupId,
            string path,
            int declarationStart,
            int containingGroupMin = 1,
            int containingGroupMax = 1
        )
        {
            string cardinality = Cardinality(node.MinOccurs, node.MaxOccurs);
            if (node.IsGroup)
            {
                string id =
                    $"dtd:content/{CanonicalIdCodec.Encode(parent)}/{path}.{node.Kind}@{cardinality}";
                AddSurface(
                    new InventorySurface(
                        id,
                        "content-group",
                        parent,
                        parent,
                        Location(declarationStart),
                        $"kind={node.Kind};path={path};min={FormatMax(node.MinOccurs)};" +
                        $"max={FormatMax(node.MaxOccurs)};parent={parent}"
                    )
                );

                for (int index = 0; index < node.Children.Count; index++)
                {
                    EmitContentNode(
                        parent,
                        node.Children[index],
                        id,
                        $"{path}.{index}",
                        declarationStart,
                        node.MinOccurs,
                        node.MaxOccurs
                    );
                }

                return;
            }

            if (node.Kind is "pcdata" or "empty" or "any")
            {
                string id =
                    $"dtd:content/{CanonicalIdCodec.Encode(parent)}/{path}.{node.Kind}@{cardinality}";
                AddSurface(
                    new InventorySurface(
                        id,
                        "special-content",
                        node.Kind,
                        parent,
                        Location(declarationStart),
                        $"kind={node.Kind};path={path};min={FormatMax(node.MinOccurs)};" +
                        $"max={FormatMax(node.MaxOccurs)};parent={parent}"
                    )
                );
                return;
            }

            string placementId =
                $"dtd:placement/{CanonicalIdCodec.Encode(parent)}/{path}/" +
                $"{CanonicalIdCodec.Encode(node.Name!)}/{cardinality}";
            AddSurface(
                new InventorySurface(
                    placementId,
                    "placement",
                    node.Name!,
                    parent,
                    Location(declarationStart),
                    $"group={groupId ?? "none"};path={path};min={FormatMax(node.MinOccurs)};" +
                    $"max={FormatMax(node.MaxOccurs)};groupMin={FormatMax(containingGroupMin)};" +
                    $"groupMax={FormatMax(containingGroupMax)}"
                )
            );
        }

        private void AddSurface(InventorySurface surface)
        {
            if (!_surfaceIds.Add(surface.Id))
            {
                Fail(_index, $"duplicate generated surface ID {surface.Id}");
            }

            _surfaces.Add(surface);
        }

        private void ParseAttributeListDeclaration(int start)
        {
            _index += "<!ATTLIST".Length;
            RequireDtdWhitespace("ATTLIST element name");
            string elementName = ReadName(start, "ATTLIST element name");

            if (Peek('>'))
            {
                _index++;
                return;
            }

            RequireDtdWhitespace("first attribute declaration");
            SkipWhitespace();

            while (true)
            {
                if (TryConsume('>'))
                {
                    return;
                }

                if (End)
                {
                    Fail(start, "unterminated ATTLIST declaration");
                }

                string attributeName = ReadName(start, "attribute name");
                string attributeKey = $"{elementName}/{attributeName}";
                if (!_attributeNames.Add(attributeKey))
                {
                    Fail(start, $"duplicate attribute declaration '{attributeKey}'");
                }

                RequireDtdWhitespace("attribute type");
                AttributeType type = ParseAttributeType(start);
                RequireDtdWhitespace("attribute default declaration");

                AttributeDefaultMode defaultModeKind;
                string defaultValue;
                if (TryReadKeyword("#REQUIRED"))
                {
                    defaultModeKind = AttributeDefaultMode.Required;
                    defaultValue = "#REQUIRED";
                }
                else if (TryReadKeyword("#IMPLIED"))
                {
                    defaultModeKind = AttributeDefaultMode.Implied;
                    defaultValue = "#IMPLIED";
                }
                else if (TryReadKeyword("#FIXED"))
                {
                    defaultModeKind = AttributeDefaultMode.FixedValue;
                    RequireDtdWhitespace("fixed attribute default");
                    defaultValue = ReadQuoted(start, "fixed attribute default");
                }
                else
                {
                    defaultModeKind = AttributeDefaultMode.DefaultValue;
                    defaultValue = ReadQuoted(start, "attribute default");
                }

                if (type.EnumValues.Count > 0 &&
                    defaultModeKind is AttributeDefaultMode.DefaultValue or AttributeDefaultMode.FixedValue &&
                    !type.EnumValues.Contains(defaultValue, StringComparer.Ordinal))
                {
                    Fail(start, $"attribute default '{defaultValue}' is not an enumeration member");
                }

                string encodedElementName = CanonicalIdCodec.Encode(elementName);
                string encodedAttributeName = CanonicalIdCodec.Encode(attributeName);
                string encodedType = CanonicalIdCodec.Encode(type.Display);
                bool fixedValue = defaultModeKind is AttributeDefaultMode.FixedValue;
                string defaultMode = defaultModeKind switch
                {
                    AttributeDefaultMode.Required => "required",
                    AttributeDefaultMode.Implied => "implied",
                    AttributeDefaultMode.FixedValue => "fixed",
                    AttributeDefaultMode.DefaultValue => "default",
                    _ => throw new InvalidOperationException(),
                };

                AddSurface(
                    new InventorySurface(
                        $"dtd:attribute/{encodedElementName}/{encodedAttributeName}",
                        "attribute",
                        attributeName,
                        elementName,
                        Location(start),
                        $"type={type.Display};default={defaultValue};fixed={fixedValue.ToString().ToLowerInvariant()}"
                    )
                );
                AddSurface(
                    new InventorySurface(
                        $"dtd:attribute-type/{encodedElementName}/{encodedAttributeName}/{encodedType}",
                        "attribute-type",
                        type.Display,
                        elementName,
                        Location(start),
                        $"attribute={attributeName};type={type.Display}"
                    )
                );

                string encodedDefaultValue =
                    defaultModeKind is AttributeDefaultMode.Required or AttributeDefaultMode.Implied
                        ? string.Empty
                        : $"/{CanonicalIdCodec.Encode(defaultValue)}";
                AddSurface(
                    new InventorySurface(
                        $"dtd:attribute-default/{encodedElementName}/{encodedAttributeName}/{defaultMode}{encodedDefaultValue}",
                        "attribute-default",
                        defaultMode,
                        elementName,
                        Location(start),
                        $"attribute={attributeName};default={defaultValue};fixed={fixedValue.ToString().ToLowerInvariant()}"
                    )
                );

                foreach (string value in type.EnumValues)
                {
                    AddSurface(
                        new InventorySurface(
                            $"dtd:enum/{encodedElementName}/{encodedAttributeName}/{CanonicalIdCodec.Encode(value)}",
                            "enum",
                            value,
                            elementName,
                            Location(start),
                            $"attribute={attributeName};type=enumeration"
                        )
                    );
                }

                if (defaultModeKind is not (AttributeDefaultMode.Required or AttributeDefaultMode.Implied))
                {
                    AddSurface(
                        new InventorySurface(
                            $"dtd:default/{encodedElementName}/{encodedAttributeName}/{CanonicalIdCodec.Encode(defaultValue)}",
                            "default",
                            defaultValue,
                            elementName,
                            Location(start),
                            $"attribute={attributeName};fixed={fixedValue.ToString().ToLowerInvariant()}"
                        )
                    );
                }

                if (End)
                {
                    Fail(start, "unterminated ATTLIST declaration");
                }

                if (TryConsume('>'))
                {
                    return;
                }

                RequireDtdWhitespace("next attribute declaration or '>'");
            }
        }

        private AttributeType ParseAttributeType(int declarationStart)
        {
            if (TryConsume('('))
            {
                var values = new List<string>();
                while (true)
                {
                    SkipWhitespace();
                    string value = ReadEnumerationValue(declarationStart, "enumeration value");
                    if (values.Contains(value, StringComparer.Ordinal))
                    {
                        Fail(declarationStart, $"duplicate enumeration value '{value}'");
                    }

                    values.Add(value);
                    SkipWhitespace();
                    if (TryConsume(')'))
                    {
                        break;
                    }

                    Require('|', declarationStart, "attribute enumeration");
                }

                return new AttributeType("enumeration", values);
            }

            string type = ReadName(declarationStart, "attribute type");
            if (!AttributeTypes.Contains(type))
            {
                Fail(declarationStart, $"unsupported attribute type '{type}'");
            }

            return new AttributeType(type, Array.Empty<string>());
        }

        private void ParseDoctypeDeclaration(int start)
        {
            Fail(start, "DOCTYPE declarations are unsupported at this seam because referenced declarations are not loaded");
        }

        private void RequireEndOfDeclaration(int declarationStart, string declarationKind)
        {
            SkipWhitespace();
            if (!TryConsume('>'))
            {
                if (End)
                {
                    Fail(declarationStart, $"unterminated {declarationKind} declaration");
                }

                Fail(_index, $"unexpected content in {declarationKind} declaration");
            }
        }

        private string ReadName(int _, string description)
        {
            SkipWhitespace();
            int start = _index;
            if (End || !(char.IsLetter(Current) || Current is '_' or ':'))
            {
                Fail(start, $"expected {description}");
            }

            _index++;
            while (!End && (char.IsLetterOrDigit(Current) || Current is '_' or '-' or '.' or ':'))
            {
                _index++;
            }

            return _text[start.._index];
        }

        private string ReadEnumerationValue(int _, string description)
        {
            SkipWhitespace();
            int start = _index;
            if (End || !(char.IsLetterOrDigit(Current) || Current is '_' or ':' or '-' or '.'))
            {
                Fail(start, $"expected {description}");
            }

            _index++;
            while (!End && (char.IsLetterOrDigit(Current) || Current is '_' or '-' or '.' or ':'))
            {
                _index++;
            }

            return _text[start.._index];
        }

        private string ReadQuoted(int _, string description)
        {
            SkipWhitespace();
            if (End || Current is not ('"' or '\''))
            {
                Fail(_index, $"expected quoted {description}");
            }

            char quote = Current;
            _index++;
            int start = _index;
            while (!End && Current != quote)
            {
                _index++;
            }

            if (End)
            {
                Fail(start, $"unterminated quoted {description}");
            }

            string value = _text[start.._index];
            _index++;
            return value;
        }

        private void RequireDtdWhitespace(string description)
        {
            if (End || !IsDtdWhitespace(Current))
            {
                Fail(_index, $"expected XML DTD whitespace before {description}");
            }

            SkipWhitespace();
        }

        private bool TryReadKeyword(string keyword)
        {
            SkipWhitespace();
            if (!StartsWith(keyword))
            {
                return false;
            }

            int end = _index + keyword.Length;
            if (end < _text.Length && (char.IsLetterOrDigit(_text[end]) || _text[end] is '_' or '-' or ':'))
            {
                return false;
            }

            _index = end;
            return true;
        }

        private (int Min, int Max) ReadOccurrenceSuffix()
        {
            if (End)
            {
                return (1, 1);
            }

            return Current switch
            {
                '?' => ConsumeOccurrence(0, 1),
                '*' => ConsumeOccurrence(0, int.MaxValue),
                '+' => ConsumeOccurrence(1, int.MaxValue),
                _ => (1, 1),
            };
        }

        private (int Min, int Max) ConsumeOccurrence(int min, int max)
        {
            _index++;
            return (min, max);
        }

        private static string Cardinality(int min, int max) =>
            (min, max) switch
            {
                (0, 1) => "optional",
                (0, int.MaxValue) => "zero-or-more",
                (1, int.MaxValue) => "one-or-more",
                _ => "one",
            };

        private static string FormatMax(int value) => value == int.MaxValue ? "unbounded" : value.ToString();

        private void Require(char expected, int _, string description)
        {
            SkipWhitespace();
            if (!TryConsume(expected))
            {
                Fail(_index, $"expected '{expected}' in {description}");
            }
        }

        private bool TryConsume(char value)
        {
            if (!End && Current == value)
            {
                _index++;
                return true;
            }

            return false;
        }

        private bool Peek(char value) => !End && Current == value;

        private bool StartsWith(string value) =>
            _text.AsSpan(_index).StartsWith(value, StringComparison.Ordinal);

        private bool StartsWithDeclaration(string keyword)
        {
            int end = _index + keyword.Length;
            return StartsWith(keyword) && end < _text.Length && IsDtdWhitespace(_text[end]);
        }

        private char Current => _text[_index];

        private static bool IsDtdWhitespace(char value) => value is ' ' or '\t' or '\r' or '\n';

        private void SkipWhitespace()
        {
            while (!End && IsDtdWhitespace(Current))
            {
                _index++;
            }
        }

        private void SkipWhitespaceAndComments()
        {
            while (true)
            {
                SkipWhitespace();
                if (!StartsWith("<!--"))
                {
                    return;
                }

                int commentStart = _index;
                int end = _text.IndexOf("-->", _index + 4, StringComparison.Ordinal);
                if (end < 0)
                {
                    Fail(commentStart, "unterminated DTD comment");
                }

                string body = _text[(_index + 4)..end];
                if (body.Contains("--", StringComparison.Ordinal) || body.EndsWith("-", StringComparison.Ordinal))
                {
                    Fail(commentStart, "invalid DTD comment: comments cannot contain '--' or end with '-'");
                }

                _index = end + 3;
            }
        }

        private string Location(int offset)
        {
            (int line, int column) = GetLineAndColumn(offset);
            return $"{_path}:{line}:{column}";
        }

        private void Fail(int offset, string message)
        {
            (int line, int column) = GetLineAndColumn(offset);
            throw new SemanticCoverageParseException(_path, line, column, message);
        }

        private (int Line, int Column) GetLineAndColumn(int offset)
        {
            int line = 1;
            int column = 1;
            int boundedOffset = Math.Clamp(offset, 0, _text.Length);
            for (int index = 0; index < boundedOffset; index++)
            {
                if (_text[index] == '\n')
                {
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }
            }

            return (line, column);
        }

        private enum AttributeDefaultMode
        {
            Required,
            Implied,
            DefaultValue,
            FixedValue,
        }

        private sealed record AttributeType(string Display, IReadOnlyList<string> EnumValues);

        private sealed record ContentNode(
            string Kind,
            string? Name,
            IReadOnlyList<ContentNode> Children,
            int MinOccurs,
            int MaxOccurs,
            int GroupMinOccurs = 1,
            int GroupMaxOccurs = 1
        )
        {
            public bool IsGroup => Children.Count > 0;

            public static ContentNode Group(string kind, IReadOnlyList<ContentNode> children, int min, int max) =>
                new(kind, null, children, min, max, min, max);

            public static ContentNode Element(string name, int min, int max) =>
                new("element", name, Array.Empty<ContentNode>(), min, max);

            public static ContentNode Special(string kind, int min, int max) =>
                new(kind, null, Array.Empty<ContentNode>(), min, max);
        }
    }
}
