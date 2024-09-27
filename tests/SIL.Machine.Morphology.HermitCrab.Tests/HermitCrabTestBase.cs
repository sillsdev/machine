using System.Text;
using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab;

public abstract class HermitCrabTestBase
{
    protected TraceManager TraceManager { get; set; } = default!;
    protected CharacterDefinitionTable Table1 { get; set; } = default!;
    protected CharacterDefinitionTable Table2 { get; set; } = default!;
    protected CharacterDefinitionTable Table3 { get; set; } = default!;
    protected MprFeature Latinate { get; set; } = default!;
    protected MprFeature Germanic { get; set; } = default!;

    protected Dictionary<string, LexEntry> Entries { get; set; } = default!;

    protected Stratum Surface { get; set; } = default!;
    protected Stratum Allophonic { get; set; } = default!;
    protected Stratum Morphophonemic { get; set; } = default!;
    protected Language Language { get; set; } = default!;

    protected ComplexFeature Head { get; set; } = default!;
    protected ComplexFeature Foot { get; set; } = default!;

    [OneTimeSetUp]
    public void FixtureSetUp()
    {
        TraceManager = new TraceManager();
        var phonologicalFeatSys = new FeatureSystem
        {
            new SymbolicFeature("voc", new FeatureSymbol("voc+", "+"), new FeatureSymbol("voc-", "-")),
            new SymbolicFeature("cons", new FeatureSymbol("cons+", "+"), new FeatureSymbol("cons-", "-")),
            new SymbolicFeature("high", new FeatureSymbol("high+", "+"), new FeatureSymbol("high-", "-")),
            new SymbolicFeature("low", new FeatureSymbol("low+", "+"), new FeatureSymbol("low-", "-")),
            new SymbolicFeature("back", new FeatureSymbol("back+", "+"), new FeatureSymbol("back-", "-")),
            new SymbolicFeature("round", new FeatureSymbol("round+", "+"), new FeatureSymbol("round-", "-")),
            new SymbolicFeature("vd", new FeatureSymbol("vd+", "+"), new FeatureSymbol("vd-", "-")),
            new SymbolicFeature("asp", new FeatureSymbol("asp+", "+"), new FeatureSymbol("asp-", "-")),
            new SymbolicFeature("del_rel", new FeatureSymbol("del_rel+", "+"), new FeatureSymbol("del_rel-", "-")),
            new SymbolicFeature("ATR", new FeatureSymbol("ATR+", "+"), new FeatureSymbol("ATR-", "-")),
            new SymbolicFeature("strident", new FeatureSymbol("strident+", "+"), new FeatureSymbol("strident-", "-")),
            new SymbolicFeature("cont", new FeatureSymbol("cont+", "+"), new FeatureSymbol("cont-", "-")),
            new SymbolicFeature("nasal", new FeatureSymbol("nasal+", "+"), new FeatureSymbol("nasal-", "-")),
            new SymbolicFeature(
                "poa",
                new FeatureSymbol("bilabial"),
                new FeatureSymbol("labiodental"),
                new FeatureSymbol("alveolar"),
                new FeatureSymbol("velar")
            )
        };
        phonologicalFeatSys.Freeze();

        var syntacticFeatSys = new SyntacticFeatureSystem
        {
            new SymbolicFeature("foo", new FeatureSymbol("foo+", "+"), new FeatureSymbol("foo-", "-")),
            new SymbolicFeature("baz", new FeatureSymbol("baz+", "+"), new FeatureSymbol("baz-", "-")),
            new SymbolicFeature("num", new FeatureSymbol("sg"), new FeatureSymbol("pl")),
            new SymbolicFeature(
                "pers",
                new FeatureSymbol("1"),
                new FeatureSymbol("2"),
                new FeatureSymbol("3"),
                new FeatureSymbol("4")
            ),
            new SymbolicFeature("tense", new FeatureSymbol("past"), new FeatureSymbol("pres")),
            new SymbolicFeature("evidential", new FeatureSymbol("witnessed")),
            new SymbolicFeature("aspect", new FeatureSymbol("perf"), new FeatureSymbol("impf")),
            new SymbolicFeature("mood", new FeatureSymbol("active"), new FeatureSymbol("passive")),
            new SymbolicFeature("fum", new FeatureSymbol("fum+", "+"), new FeatureSymbol("fum-", "-")),
            new SymbolicFeature("bar", new FeatureSymbol("bar+", "+"), new FeatureSymbol("bar-", "-"))
        };
        syntacticFeatSys.AddPartsOfSpeech(
            new FeatureSymbol("N", "Noun"),
            new FeatureSymbol("V", "Verb"),
            new FeatureSymbol("TV", "Transitive Verb"),
            new FeatureSymbol("IV", "Intransitive Verb"),
            new FeatureSymbol("A", "Adjective")
        );
        Head = syntacticFeatSys.AddHeadFeature();
        Foot = syntacticFeatSys.AddFootFeature();
        syntacticFeatSys.Freeze();

        Table1 = new CharacterDefinitionTable() { Name = "table1" };
        AddSegDef(Table1, phonologicalFeatSys, "a", "cons-", "voc+", "high-", "low+", "back+", "round-", "vd+");
        AddSegDef(Table1, phonologicalFeatSys, "i", "cons-", "voc+", "high+", "low-", "back-", "round-", "vd+");
        AddSegDef(Table1, phonologicalFeatSys, "u", "cons-", "voc+", "high+", "low-", "back+", "round+", "vd+");
        AddSegDef(Table1, phonologicalFeatSys, "o", "cons-", "voc+", "high-", "low-", "back+", "round+", "vd+");
        AddSegDef(Table1, phonologicalFeatSys, "y", "cons-", "voc+", "high+", "low-", "back-", "round+", "vd+");
        AddSegDef(Table1, phonologicalFeatSys, "ɯ", "cons-", "voc+", "high+", "low-", "back+", "round-", "vd+");
        AddSegDef(
            Table1,
            phonologicalFeatSys,
            "p",
            "cons+",
            "voc-",
            "bilabial",
            "vd-",
            "asp-",
            "strident-",
            "cont-",
            "nasal-"
        );
        AddSegDef(
            Table1,
            phonologicalFeatSys,
            "t",
            "cons+",
            "voc-",
            "alveolar",
            "vd-",
            "asp-",
            "del_rel-",
            "strident-",
            "cont-",
            "nasal-"
        );
        AddSegDef(
            Table1,
            phonologicalFeatSys,
            "k",
            "cons+",
            "voc-",
            "velar",
            "vd-",
            "asp-",
            "strident-",
            "cont-",
            "nasal-"
        );
        AddSegDef(
            Table1,
            phonologicalFeatSys,
            "ts",
            "cons+",
            "voc-",
            "alveolar",
            "vd-",
            "asp-",
            "del_rel+",
            "strident+",
            "cont-",
            "nasal-"
        );
        AddSegDef(
            Table1,
            phonologicalFeatSys,
            "pʰ",
            "cons+",
            "voc-",
            "bilabial",
            "vd-",
            "asp+",
            "strident-",
            "cont-",
            "nasal-"
        );
        AddSegDef(
            Table1,
            phonologicalFeatSys,
            "tʰ",
            "cons+",
            "voc-",
            "alveolar",
            "vd-",
            "asp+",
            "del_rel-",
            "strident-",
            "cont-",
            "nasal-"
        );
        AddSegDef(
            Table1,
            phonologicalFeatSys,
            "kʰ",
            "cons+",
            "voc-",
            "velar",
            "vd-",
            "asp+",
            "strident-",
            "cont-",
            "nasal-"
        );
        AddSegDef(
            Table1,
            phonologicalFeatSys,
            "tsʰ",
            "cons+",
            "voc-",
            "alveolar",
            "vd-",
            "asp+",
            "del_rel+",
            "strident+",
            "cont-",
            "nasal-"
        );
        AddSegDef(Table1, phonologicalFeatSys, "b", "cons+", "voc-", "bilabial", "vd+", "cont-", "nasal-");
        AddSegDef(Table1, phonologicalFeatSys, "d", "cons+", "voc-", "alveolar", "vd+", "strident-", "cont-", "nasal-");
        AddSegDef(Table1, phonologicalFeatSys, "g", "cons+", "voc-", "velar", "vd+", "cont-", "nasal-");
        AddSegDef(Table1, phonologicalFeatSys, "m", "cons+", "voc-", "bilabial", "vd+", "cont-", "nasal+");
        AddSegDef(Table1, phonologicalFeatSys, "n", "cons+", "voc-", "alveolar", "vd+", "strident-", "cont-", "nasal+");
        AddSegDef(Table1, phonologicalFeatSys, "ŋ", "cons+", "voc-", "velar", "vd+", "cont-", "nasal+");
        AddSegDef(
            Table1,
            phonologicalFeatSys,
            "s",
            "cons+",
            "voc-",
            "alveolar",
            "vd-",
            "asp-",
            "del_rel-",
            "strident+",
            "cont+"
        );
        AddSegDef(
            Table1,
            phonologicalFeatSys,
            "z",
            "cons+",
            "voc-",
            "alveolar",
            "vd+",
            "asp-",
            "del_rel-",
            "strident+",
            "cont+"
        );
        AddSegDef(
            Table1,
            phonologicalFeatSys,
            "f",
            "cons+",
            "voc-",
            "labiodental",
            "vd-",
            "asp-",
            "strident+",
            "cont+"
        );
        AddSegDef(
            Table1,
            phonologicalFeatSys,
            "v",
            "cons+",
            "voc-",
            "labiodental",
            "vd+",
            "asp-",
            "strident+",
            "cont+"
        );

        Table2 = new CharacterDefinitionTable() { Name = "table2" };
        AddSegDef(Table2, phonologicalFeatSys, "a", "cons-", "voc+", "high-", "low+", "back+", "round-", "vd+");
        AddSegDef(Table2, phonologicalFeatSys, "i", "cons-", "voc+", "high+", "low-", "back-", "round-", "vd+");
        AddSegDef(Table2, phonologicalFeatSys, "u", "cons-", "voc+", "high+", "low-", "back+", "round+", "vd+");
        AddSegDef(Table2, phonologicalFeatSys, "y", "cons-", "voc+", "high+", "low-", "back-", "round+", "vd+");
        AddSegDef(Table2, phonologicalFeatSys, "o", "cons-", "voc+", "high-", "low-", "back+", "round+", "vd+");
        AddSegDef(Table2, phonologicalFeatSys, "p", "cons+", "voc-", "bilabial", "vd-");
        AddSegDef(Table2, phonologicalFeatSys, "t", "cons+", "voc-", "alveolar", "vd-", "del_rel-", "strident-");
        AddSegDef(Table2, phonologicalFeatSys, "k", "cons+", "voc-", "velar", "vd-");
        AddSegDef(Table2, phonologicalFeatSys, "ts", "cons+", "voc-", "alveolar", "vd-", "del_rel+", "strident+");
        AddSegDef(Table2, phonologicalFeatSys, "b", "cons+", "voc-", "bilabial", "vd+");
        AddSegDef(Table2, phonologicalFeatSys, "d", "cons+", "voc-", "alveolar", "vd+", "strident-");
        AddSegDef(Table2, phonologicalFeatSys, "g", "cons+", "voc-", "velar", "vd+");
        AddSegDef(Table2, phonologicalFeatSys, "m", "cons+", "voc-", "bilabial", "vd+", "cont-", "nasal+");
        AddSegDef(Table2, phonologicalFeatSys, "n", "cons+", "voc-", "alveolar", "vd+", "cont-", "nasal+");
        AddSegDef(Table2, phonologicalFeatSys, "ŋ", "cons+", "voc-", "velar", "vd+", "cont-", "nasal+");
        AddSegDef(
            Table2,
            phonologicalFeatSys,
            "s",
            "cons+",
            "voc-",
            "alveolar",
            "vd-",
            "asp-",
            "del_rel-",
            "strident+",
            "cont+"
        );
        AddSegDef(
            Table2,
            phonologicalFeatSys,
            "z",
            "cons+",
            "voc-",
            "alveolar",
            "vd+",
            "asp-",
            "del_rel-",
            "strident+",
            "cont+"
        );
        AddSegDef(
            Table2,
            phonologicalFeatSys,
            "f",
            "cons+",
            "voc-",
            "labiodental",
            "vd-",
            "asp-",
            "strident+",
            "cont+"
        );
        AddSegDef(
            Table2,
            phonologicalFeatSys,
            "v",
            "cons+",
            "voc-",
            "labiodental",
            "vd+",
            "asp-",
            "strident+",
            "cont+"
        );
        AddBdryDef(Table2, "+");
        AddBdryDef(Table2, "#");
        AddBdryDef(Table2, "!");
        AddBdryDef(Table2, ".");
        AddBdryDef(Table2, "$");

        Table3 = new CharacterDefinitionTable() { Name = "table3" };
        AddSegDef(
            Table3,
            phonologicalFeatSys,
            "a",
            "cons-",
            "voc+",
            "high-",
            "low+",
            "back+",
            "round-",
            "vd+",
            "ATR+",
            "cont+"
        );
        AddSegDef(
            Table3,
            phonologicalFeatSys,
            "a̘",
            "cons-",
            "voc+",
            "high-",
            "low+",
            "back+",
            "round-",
            "vd+",
            "ATR-",
            "cont+"
        );
        AddSegDef(
            Table3,
            phonologicalFeatSys,
            "i",
            "cons-",
            "voc+",
            "high+",
            "low-",
            "back-",
            "round-",
            "vd+",
            "cont+"
        );
        AddSegDef(
            Table3,
            phonologicalFeatSys,
            "u",
            "cons-",
            "voc+",
            "high+",
            "low-",
            "back+",
            "round+",
            "vd+",
            "cont+"
        );
        AddSegDef(
            Table3,
            phonologicalFeatSys,
            "y",
            "cons-",
            "voc+",
            "high+",
            "low-",
            "back-",
            "round+",
            "vd+",
            "cont+"
        );
        AddSegDef(
            Table3,
            phonologicalFeatSys,
            "ɯ",
            "cons-",
            "voc+",
            "high+",
            "low-",
            "back+",
            "round-",
            "vd+",
            "cont+"
        );
        AddSegDef(
            Table3,
            phonologicalFeatSys,
            "o",
            "cons-",
            "voc+",
            "high-",
            "low-",
            "back+",
            "round+",
            "vd+",
            "cont+"
        );
        AddSegDef(Table3, phonologicalFeatSys, "p", "cons+", "voc-", "bilabial", "vd-", "cont-", "nasal-");
        AddSegDef(
            Table3,
            phonologicalFeatSys,
            "t",
            "cons+",
            "voc-",
            "alveolar",
            "vd-",
            "del_rel-",
            "strident-",
            "cont-",
            "nasal-"
        );
        AddSegDef(Table3, phonologicalFeatSys, "k", "cons+", "voc-", "velar", "vd-", "cont-", "nasal-");
        AddSegDef(
            Table3,
            phonologicalFeatSys,
            "ts",
            "cons+",
            "voc-",
            "alveolar",
            "vd-",
            "del_rel+",
            "strident+",
            "cont-",
            "nasal-"
        );
        AddSegDef(Table3, phonologicalFeatSys, "b", "cons+", "voc-", "bilabial", "vd+", "cont-", "nasal-");
        AddSegDef(Table3, phonologicalFeatSys, "d", "cons+", "voc-", "alveolar", "vd+", "strident-", "cont-", "nasal-");
        AddSegDef(Table3, phonologicalFeatSys, "g", "cons+", "voc-", "velar", "vd+", "cont-", "nasal-");
        AddSegDef(Table3, phonologicalFeatSys, "m", "cons+", "voc-", "bilabial", "vd+", "cont-", "nasal+");
        AddSegDef(Table3, phonologicalFeatSys, "n", "cons+", "voc-", "alveolar", "vd+", "strident-", "cont-", "nasal+");
        AddSegDef(Table3, phonologicalFeatSys, "ŋ", "cons+", "voc-", "velar", "vd+", "cont-", "nasal+");
        AddSegDef(
            Table3,
            phonologicalFeatSys,
            "s",
            "cons+",
            "voc-",
            "alveolar",
            "vd-",
            "asp-",
            "del_rel-",
            "strident+",
            "cont+"
        );
        AddSegDef(
            Table3,
            phonologicalFeatSys,
            "z",
            "cons+",
            "voc-",
            "alveolar",
            "vd+",
            "asp-",
            "del_rel-",
            "strident+",
            "cont+"
        );
        AddSegDef(
            Table3,
            phonologicalFeatSys,
            "f",
            "cons+",
            "voc-",
            "labiodental",
            "vd-",
            "asp-",
            "strident+",
            "cont+"
        );
        AddSegDef(
            Table3,
            phonologicalFeatSys,
            "v",
            "cons+",
            "voc-",
            "labiodental",
            "vd+",
            "asp-",
            "strident+",
            "cont+"
        );
        AddBdryDef(Table3, "+");
        AddBdryDef(Table3, "#");
        AddBdryDef(Table3, "!");
        AddBdryDef(Table3, ".");

        Latinate = new MprFeature { Name = "latinate" };
        Germanic = new MprFeature { Name = "germanic" };

        Morphophonemic = new Stratum(Table3)
        {
            Name = "Morphophonemic",
            MorphologicalRuleOrder = MorphologicalRuleOrder.Unordered
        };
        Allophonic = new Stratum(Table1)
        {
            Name = "Allophonic",
            MorphologicalRuleOrder = MorphologicalRuleOrder.Unordered
        };
        Surface = new Stratum(Table1) { Name = "Surface", MorphologicalRuleOrder = MorphologicalRuleOrder.Unordered };

        Entries = new Dictionary<string, LexEntry>();
        var fs = FeatureStruct
            .New(syntacticFeatSys)
            .Symbol("N")
            .Feature(Head)
            .EqualTo(head => head.Symbol("foo+").Symbol("baz-"))
            .Feature(Foot)
            .EqualTo(foot => foot.Symbol("fum-").Symbol("bar+"))
            .Value;
        AddEntry("1", fs, Allophonic, "pʰit");
        fs = FeatureStruct
            .New(syntacticFeatSys)
            .Symbol("N")
            .Feature(Head)
            .EqualTo(head => head.Symbol("foo+").Symbol("baz-"))
            .Feature(Foot)
            .EqualTo(foot => foot.Symbol("fum-").Symbol("bar+"))
            .Value;
        AddEntry("2", fs, Allophonic, "pit");

        AddEntry("5", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "pʰut");
        AddEntry("6", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "kʰat");
        AddEntry("7", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "kʰut");

        AddEntry("8", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "dat");
        AddEntry("9", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Allophonic, "dat");

        AddEntry("10", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "ga̘p");
        AddEntry("11", FeatureStruct.New(syntacticFeatSys).Symbol("A").Value, Morphophonemic, "gab");
        AddEntry("12", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Morphophonemic, "ga+b");

        AddEntry("13", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "bubabu");
        AddEntry("14", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "bubabi");
        AddEntry("15", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "bɯbabu");
        AddEntry("16", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "bibabi");
        AddEntry("17", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "bubi");
        AddEntry("18", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "bibu");
        AddEntry("19", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Morphophonemic, "b+ubu");
        AddEntry("20", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "bubababi");
        AddEntry("21", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "bibababu");
        AddEntry("22", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "bubabababi");
        AddEntry("23", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "bibabababu");
        AddEntry("24", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "bubui");
        AddEntry("25", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "buibu");
        AddEntry("26", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "buibui");
        AddEntry("27", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "buiibuii");
        AddEntry("28", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "buitibuiti");
        AddEntry("29", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "iibubu");

        AddEntry("30", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Morphophonemic, "bu+ib");
        AddEntry("31", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Morphophonemic, "buib");

        AddEntry("32", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "sag");
        AddEntry("33", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "sas");
        AddEntry("34", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "saz");
        AddEntry("35", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "sat");
        AddEntry("36", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "sasibo");
        AddEntry("37", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "sasibut");
        AddEntry("38", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "sasibud");

        AddEntry("39", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "ab+ba");
        AddEntry("40", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "abba");

        AddEntry("41", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Allophonic, "pip");
        AddEntry("42", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "bubibi");
        AddEntry("43", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "bubibu");

        AddEntry("44", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "gigigi");

        AddEntry("45", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "nbinding");

        AddEntry("46", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "bupu");

        AddEntry("47", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "tag");
        AddEntry("48", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "pag");
        AddEntry("49", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "ktb");
        AddEntry("50", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "suupu");
        AddEntry("51", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Morphophonemic, "miu");
        AddEntry("52", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Morphophonemic, "pu");
        AddEntry("53", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Morphophonemic, "mi");
        AddEntry("54", FeatureStruct.New().Value, Morphophonemic, "pi");
        AddEntry("55", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Morphophonemic, "mim+ɯɯ");

        fs = FeatureStruct
            .New(syntacticFeatSys)
            .Symbol("V")
            .Feature(Head)
            .EqualTo(head => head.Feature("num").EqualTo("pl"))
            .Value;
        AddEntry("Perc0", fs, Morphophonemic, "ssag");
        fs = FeatureStruct
            .New(syntacticFeatSys)
            .Symbol("V")
            .Feature(Head)
            .EqualTo(head => head.Feature("pers").EqualTo("1").Feature("num").EqualTo("pl"))
            .Value;
        AddEntry("Perc1", fs, Morphophonemic, "ssag");
        fs = FeatureStruct
            .New(syntacticFeatSys)
            .Symbol("V")
            .Feature(Head)
            .EqualTo(head => head.Feature("pers").EqualTo("3").Feature("num").EqualTo("pl"))
            .Value;
        AddEntry("Perc2", fs, Morphophonemic, "ssag");
        fs = FeatureStruct
            .New(syntacticFeatSys)
            .Symbol("V")
            .Feature(Head)
            .EqualTo(head => head.Feature("pers").EqualTo("2", "3").Feature("num").EqualTo("pl"))
            .Value;
        AddEntry("Perc3", fs, Morphophonemic, "ssag");
        fs = FeatureStruct
            .New(syntacticFeatSys)
            .Symbol("V")
            .Feature(Head)
            .EqualTo(head => head.Feature("pers").EqualTo("1", "3").Feature("num").EqualTo("pl"))
            .Value;
        AddEntry("Perc4", fs, Morphophonemic, "ssag");

        var seeFamily = new LexFamily { Name = "SEE" };
        seeFamily.Entries.Add(
            AddEntry("bl1", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "si")
        );
        fs = FeatureStruct
            .New(syntacticFeatSys)
            .Symbol("V")
            .Feature(Head)
            .EqualTo(head => head.Feature("tense").EqualTo("past"))
            .Value;
        seeFamily.Entries.Add(AddEntry("bl2", fs, Morphophonemic, "sau"));
        fs = FeatureStruct
            .New(syntacticFeatSys)
            .Symbol("V")
            .Feature(Head)
            .EqualTo(head => head.Feature("tense").EqualTo("pres"))
            .Value;
        seeFamily.Entries.Add(AddEntry("bl3", fs, Morphophonemic, "sis"));

        LexEntry entry = AddEntry("pos1", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "ba");
        entry.MprFeatures.Add(Latinate);
        entry = AddEntry("pos2", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Morphophonemic, "ba");
        entry.MprFeatures.Add(Germanic);

        var vowel = FeatureStruct.New(phonologicalFeatSys).Symbol(HCFeatureSystem.Segment).Symbol("voc+").Value;
        entry = AddEntry(
            "free",
            FeatureStruct.New(syntacticFeatSys).Symbol("V").Value,
            Morphophonemic,
            "tap",
            "taz",
            "tas"
        );
        entry
            .Allomorphs[0]
            .Environments.Add(
                new AllomorphEnvironment(
                    ConstraintType.Require,
                    null,
                    Pattern<Word, ShapeNode>.New().Annotation(vowel).Value
                )
            );

        entry = AddEntry(
            "disj",
            FeatureStruct.New(syntacticFeatSys).Symbol("V").Value,
            Morphophonemic,
            "baz",
            "bat",
            "bad",
            "bas"
        );
        var unroundedVowel = FeatureStruct
            .New(phonologicalFeatSys)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("voc+")
            .Symbol("round-")
            .Value;
        entry
            .Allomorphs[0]
            .Environments.Add(
                new AllomorphEnvironment(
                    ConstraintType.Require,
                    null,
                    Pattern<Word, ShapeNode>.New().Annotation(unroundedVowel).Value
                )
            );
        entry
            .Allomorphs[1]
            .Environments.Add(
                new AllomorphEnvironment(
                    ConstraintType.Require,
                    null,
                    Pattern<Word, ShapeNode>.New().Annotation(vowel).Value
                )
            );
        entry
            .Allomorphs[2]
            .Environments.Add(
                new AllomorphEnvironment(
                    ConstraintType.Require,
                    null,
                    Pattern<Word, ShapeNode>.New().Annotation(vowel).Value
                )
            );

        entry = AddEntry(
            "stemname",
            FeatureStruct
                .New(syntacticFeatSys)
                .Symbol("V")
                .Feature(Head)
                .EqualTo(head => head.Feature("tense").EqualTo("pres"))
                .Value,
            Morphophonemic,
            "san",
            "sad",
            "sap"
        );
        entry.Allomorphs[1].StemName = new StemName(
            FeatureStruct
                .New(syntacticFeatSys)
                .Symbol("V")
                .Feature(Head)
                .EqualTo(head => head.Feature("pers").EqualTo("1"))
                .Value,
            FeatureStruct
                .New(syntacticFeatSys)
                .Symbol("V")
                .Feature(Head)
                .EqualTo(head => head.Feature("pers").EqualTo("2"))
                .Value
        )
        {
            Name = "sn1"
        };
        entry.Allomorphs[2].StemName = new StemName(
            FeatureStruct
                .New(syntacticFeatSys)
                .Symbol("V")
                .Feature(Head)
                .EqualTo(head => head.Feature("pers").EqualTo("1"))
                .Value,
            FeatureStruct
                .New(syntacticFeatSys)
                .Symbol("V")
                .Feature(Head)
                .EqualTo(head => head.Feature("pers").EqualTo("3"))
                .Value
        )
        {
            Name = "sn2"
        };

        AddEntry(
            "synfs",
            FeatureStruct
                .New(syntacticFeatSys)
                .Symbol("V")
                .Feature(Head)
                .EqualTo(head => head.Feature("tense").EqualTo("pres"))
                .Value,
            Morphophonemic,
            "bag"
        );

        entry = AddEntry("bound", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "dag");
        entry.PrimaryAllomorph.IsBound = true;

        Language = new Language
        {
            Name = "Test",
            PhonologicalFeatureSystem = phonologicalFeatSys,
            SyntacticFeatureSystem = syntacticFeatSys,
            Strata = { Morphophonemic, Allophonic, Surface }
        };
    }

    [TearDown]
    public void TestCleanup()
    {
        foreach (Stratum stratum in Language.Strata)
        {
            stratum.PhonologicalRules.Clear();
            stratum.MorphologicalRules.Clear();
            stratum.AffixTemplates.Clear();
        }
    }

    public LexEntry AddEntry(string gloss, FeatureStruct syntacticFS, Stratum stratum, params string[] forms)
    {
        var entry = new LexEntry
        {
            Id = gloss,
            SyntacticFeatureStruct = syntacticFS,
            Gloss = gloss,
            IsPartial = syntacticFS.IsEmpty
        };
        foreach (string form in forms)
            entry.Allomorphs.Add(new RootAllomorph(new Segments(stratum.CharacterDefinitionTable, form, true)));
        stratum.Entries.Add(entry);
        Entries[gloss] = entry;
        return entry;
    }

    private static void AddSegDef(
        CharacterDefinitionTable table,
        FeatureSystem phoneticFeatSys,
        string strRep,
        params string[] symbols
    )
    {
        var fs = new FeatureStruct();
        foreach (string symbolID in symbols)
        {
            FeatureSymbol symbol = phoneticFeatSys.GetSymbol(symbolID);
            fs.AddValue(symbol.Feature, new SymbolicFeatureValue(symbol));
        }
        table.AddSegment(strRep, fs);
    }

    private static void AddBdryDef(CharacterDefinitionTable table, string strRep)
    {
        table.AddBoundary(strRep);
    }

    protected FeatureStruct Character(CharacterDefinitionTable table, string strRep)
    {
        return table[strRep].FeatureStruct;
    }

    protected void AssertMorphsEqual(IEnumerable<Word> words, params string[] expected)
    {
        var actual = new HashSet<string>();
        foreach (Word word in words)
        {
            var sb = new StringBuilder();
            bool first = true;
            foreach (Allomorph morph in word.AllomorphsInMorphOrder)
            {
                if (!first)
                    sb.Append(' ');
                sb.Append(morph.Morpheme.Gloss);
                first = false;
            }
            actual.Add(sb.ToString());
        }

        Assert.That(actual, Is.EquivalentTo(expected));
    }

    protected void AssertSyntacticFeatureStructsEqual(IEnumerable<Word> words, FeatureStruct expected)
    {
        Assert.That(
            words,
            Has.All.Property("SyntacticFeatureStruct")
                .EqualTo(expected)
                .Using(FreezableEqualityComparer<FeatureStruct>.Default)
        );
    }

    protected SymbolicFeatureValue Variable(string featureID, string variable, bool agree = true)
    {
        return new SymbolicFeatureValue(
            Language.PhonologicalFeatureSystem.GetFeature<SymbolicFeature>(featureID),
            variable,
            agree
        );
    }
}
