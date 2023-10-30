namespace SIL.Machine.Translation;

public class LanguageTagService : ILanguageTagService
{
    private static readonly Dictionary<string, string> StandardLanguages =
        new() { { "ar", "arb" }, { "ms", "zsm" }, { "lv", "lvs" }, { "ne", "npi" }, { "sw", "swh" }, { "cmn", "zh" } };

    private readonly Dictionary<string, string> _defaultScripts;

    private readonly Dictionary<string, string> _threeToTwo;

    private readonly Regex _langTagPattern;

    public LanguageTagService()
    {
        // initialise SLDR language tags to retrieve latest langtags.json file
        Sldr.InitializeLanguageTags();
        _defaultScripts = InitializeDefaultScripts();
        _threeToTwo = InitializeThreeToTwo();
        _langTagPattern = new Regex(
            "(?'language'[a-zA-Z]{2,8})([_-](?'script'[a-zA-Z]{4}))?",
            RegexOptions.ExplicitCapture
        );
    }

    private Dictionary<string, string> InitializeDefaultScripts()
    {
        var cachedAllTagsPath = Path.Combine(Sldr.SldrCachePath, "langtags.json");
        using var stream = new FileStream(cachedAllTagsPath, FileMode.Open);

        var json = JsonNode.Parse(stream);
        var tempDefaultScripts = new Dictionary<string, string>();
        foreach (JsonNode? entry in json!.AsArray())
        {
            if (entry is null)
                continue;

            var script = (string?)entry["script"];
            if (script is null)
                continue;

            JsonNode? tags = entry["tags"];
            if (tags is not null)
            {
                foreach (var t in tags.AsArray().Select(v => (string?)v))
                {
                    if (
                        t is not null && IetfLanguageTag.TryGetParts(t, out _, out string? s, out _, out _) && s is null
                    )
                    {
                        tempDefaultScripts[t] = script;
                    }
                }
            }

            var tag = (string?)entry["tag"];
            if (tag is not null)
                tempDefaultScripts[tag] = script;
        }
        return tempDefaultScripts;
    }

    private Dictionary<string, string> InitializeThreeToTwo()
    {
        var temp3to2 = new Dictionary<string, string>();
        foreach (LanguageSubtag l in StandardSubtags.RegisteredLanguages)
        {
            if (l.Iso3Code.Length > 0)
            {
                temp3to2[l.Iso3Code] = l.Code;
            }
        }
        return temp3to2;
    }

    public string ConvertToFlores200Code(string languageTag)
    {
        // Try to find a pattern of {language code}_{script}
        Match langTagMatch = _langTagPattern.Match(languageTag);
        if (!langTagMatch.Success)
            return languageTag;
        string parsed_language = langTagMatch.Groups["language"].Value;
        string language_subtag = parsed_language;
        string iso639_3_code = parsed_language;

        // Best attempt to convert language to a registered ISO 639-3 code
        // Uses https://www.iana.org/assignments/language-subtag-registry/language-subtag-registry for mapping

        // If they gave us the ISO code, revert it to the 2 character code
        if (_threeToTwo.ContainsKey(language_subtag))
            language_subtag = _threeToTwo[language_subtag];

        // There are a few extra conversions not in SIL Writing Systems that we need to handle
        if (StandardLanguages.ContainsKey(language_subtag))
            language_subtag = StandardLanguages[language_subtag];

        if (StandardSubtags.RegisteredLanguages.Contains(language_subtag))
            iso639_3_code = StandardSubtags.RegisteredLanguages[language_subtag].Iso3Code;

        // Use default script unless there is one parsed out of the language tag
        Group scriptGroup = langTagMatch.Groups["script"];
        string? script = null;
        if (_defaultScripts.ContainsKey(language_subtag))
            script = _defaultScripts[language_subtag];

        if (_defaultScripts.ContainsKey(languageTag))
            script = _defaultScripts[languageTag];

        if (scriptGroup.Success)
            script = scriptGroup.Value;

        if (script is not null)
            return $"{iso639_3_code}_{script}";
        else
            return languageTag;
    }
}
