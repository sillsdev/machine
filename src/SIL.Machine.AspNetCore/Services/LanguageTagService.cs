namespace SIL.Machine.Translation;

public class LanguageTagService : ILanguageTagService
{
    private static readonly Dictionary<string, string> StandardLanguages =
        new()
        {
            { "ar", "arb" },
            { "ms", "zsm" },
            { "lv", "lvs" },
            { "ne", "npi" },
            { "sw", "swh" },
            { "cmn", "zh" }
        };

    private readonly Dictionary<string, string> _defaultScripts;

    private static readonly Regex LangTagPattern = new Regex(
        "(?'language'[a-zA-Z]{2,8})([_-](?'script'[a-zA-Z]{4}))?",
        RegexOptions.ExplicitCapture
    );

    public LanguageTagService()
    {
        // initialise SLDR language tags to retrieve latest langtags.json file
        _defaultScripts = InitializeDefaultScripts();
    }

    private static Dictionary<string, string> InitializeDefaultScripts()
    {
        Sldr.InitializeLanguageTags();
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
                        t is not null
                        && IetfLanguageTag.TryGetParts(t, out _, out string? s, out _, out _)
                        && s is null
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

    public string ConvertToFlores200Code(string languageTag)
    {
        // Try to find a pattern of {language code}_{script}
        Match langTagMatch = LangTagPattern.Match(languageTag);
        if (!langTagMatch.Success)
            return languageTag;
        string parsedLanguage = langTagMatch.Groups["language"].Value;
        string languageSubtag = parsedLanguage;
        string iso639_3Code = parsedLanguage;

        // Best attempt to convert language to a registered ISO 639-3 code
        // Uses https://www.iana.org/assignments/language-subtag-registry/language-subtag-registry for mapping

        // If they gave us the ISO code, revert it to the 2 character code
        if (StandardSubtags.TryGetLanguageFromIso3Code(languageSubtag, out LanguageSubtag tempSubtag))
            languageSubtag = tempSubtag.Code;

        // There are a few extra conversions not in SIL Writing Systems that we need to handle
        if (StandardLanguages.TryGetValue(languageSubtag, out string? tempName))
            languageSubtag = tempName;

        if (StandardSubtags.RegisteredLanguages.TryGet(languageSubtag, out LanguageSubtag? languageSubtagObj))
            iso639_3Code = languageSubtagObj.Iso3Code;

        // Use default script unless there is one parsed out of the language tag
        Group scriptGroup = langTagMatch.Groups["script"];
        string? script = null;

        if (scriptGroup.Success)
            script = scriptGroup.Value;
        else if (_defaultScripts.TryGetValue(languageTag, out string? tempScript2))
            script = tempScript2;
        else if (_defaultScripts.TryGetValue(languageSubtag, out string? tempScript))
            script = tempScript;

        if (script is not null)
            return $"{iso639_3Code}_{script}";
        else
            return languageTag;
    }
}
