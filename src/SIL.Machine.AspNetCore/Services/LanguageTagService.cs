namespace SIL.Machine.Translation;

public class LanguageTagService : ILanguageTagService
{
    private static readonly Dictionary<string, string> StandardLanguages =
        new() { { "ar", "arb" }, { "ms", "zsm" }, { "lv", "lvs" }, { "ne", "npi" }, { "sw", "swh" }, { "cmn", "zh" } };

    private readonly Dictionary<string, string> _defaultScripts;

    public LanguageTagService()
    {
        // initialise SLDR language tags to retrieve latest langtags.json file
        Sldr.InitializeLanguageTags();
        var cachedAllTagsPath = Path.Combine(Sldr.SldrCachePath, "langtags.json");
        using var stream = new FileStream(cachedAllTagsPath, FileMode.Open);

        var json = JsonNode.Parse(stream);
        _defaultScripts = new Dictionary<string, string>();
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
                        _defaultScripts[t] = script;
                    }
                }
            }

            var tag = (string?)entry["tag"];
            if (tag is not null)
                _defaultScripts[tag] = script;
        }
    }

    public string ConvertToFlores200Code(string languageTag)
    {
        if (!IetfLanguageTag.TryGetParts(languageTag, out string? language, out string? script, out _, out _))
            return languageTag;

        if (!StandardSubtags.RegisteredLanguages.TryGet(language, out LanguageSubtag languageSubtag))
            return languageTag;

        // Normalize to a standard language subtag
        if (StandardLanguages.TryGetValue(language, out string? standardLanguageCode))
            languageSubtag = StandardSubtags.RegisteredLanguages[standardLanguageCode];

        if (script is not null)
            return $"{languageSubtag.Iso3Code}_{script}";

        if (_defaultScripts.TryGetValue(languageTag, out script))
            return $"{languageSubtag.Iso3Code}_{script}";

        return languageTag;
    }
}
