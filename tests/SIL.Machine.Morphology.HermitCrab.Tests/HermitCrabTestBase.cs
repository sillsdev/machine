using System.Text;
using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab;

public abstract class HermitCrabTestBase
{
    protected TraceManager _traceManager = default!;
    protected CharacterDefinitionTable _table1 = default!;
    protected CharacterDefinitionTable _table2 = default!;
    protected CharacterDefinitionTable _table3 = default!;
    protected MprFeature _latinate = default!;
    protected MprFeature _germanic = default!;

    protected Dictionary<string, LexEntry> _entries = default!;

    protected Stratum _surface = default!;
    protected Stratum _allophonic = default!;
    protected Stratum _morphophonemic = default!;
    protected Language _language = default!;

    protected ComplexFeature _head = default!;
    protected ComplexFeature _foot = default!;

    [OneTimeSetUp]
    public void FixtureSetUp()
    {
        _traceManager = new TraceManager();
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
        _head = syntacticFeatSys.AddHeadFeature();
        _foot = syntacticFeatSys.AddFootFeature();
        syntacticFeatSys.Freeze();

        _table1 = new CharacterDefinitionTable() { Name = "table1" };
        AddSegDef(_table1, phonologicalFeatSys, "a", "cons-", "voc+", "high-", "low+", "back+", "round-", "vd+");
        AddSegDef(_table1, phonologicalFeatSys, "i", "cons-", "voc+", "high+", "low-", "back-", "round-", "vd+");
        AddSegDef(_table1, phonologicalFeatSys, "u", "cons-", "voc+", "high+", "low-", "back+", "round+", "vd+");
        AddSegDef(_table1, phonologicalFeatSys, "o", "cons-", "voc+", "high-", "low-", "back+", "round+", "vd+");
        AddSegDef(_table1, phonologicalFeatSys, "y", "cons-", "voc+", "high+", "low-", "back-", "round+", "vd+");
        AddSegDef(_table1, phonologicalFeatSys, "ɯ", "cons-", "voc+", "high+", "low-", "back+", "round-", "vd+");
        AddSegDef(
            _table1,
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
            _table1,
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
            _table1,
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
            _table1,
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
            _table1,
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
            _table1,
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
            _table1,
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
            _table1,
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
        AddSegDef(_table1, phonologicalFeatSys, "b", "cons+", "voc-", "bilabial", "vd+", "cont-", "nasal-");
        AddSegDef(
            _table1,
            phonologicalFeatSys,
            "d",
            "cons+",
            "voc-",
            "alveolar",
            "vd+",
            "strident-",
            "cont-",
            "nasal-"
        );
        AddSegDef(_table1, phonologicalFeatSys, "g", "cons+", "voc-", "velar", "vd+", "cont-", "nasal-");
        AddSegDef(_table1, phonologicalFeatSys, "m", "cons+", "voc-", "bilabial", "vd+", "cont-", "nasal+");
        AddSegDef(
            _table1,
            phonologicalFeatSys,
            "n",
            "cons+",
            "voc-",
            "alveolar",
            "vd+",
            "strident-",
            "cont-",
            "nasal+"
        );
        AddSegDef(_table1, phonologicalFeatSys, "ŋ", "cons+", "voc-", "velar", "vd+", "cont-", "nasal+");
        AddSegDef(
            _table1,
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
            _table1,
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
            _table1,
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
            _table1,
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

        _table2 = new CharacterDefinitionTable() { Name = "table2" };
        AddSegDef(_table2, phonologicalFeatSys, "a", "cons-", "voc+", "high-", "low+", "back+", "round-", "vd+");
        AddSegDef(_table2, phonologicalFeatSys, "i", "cons-", "voc+", "high+", "low-", "back-", "round-", "vd+");
        AddSegDef(_table2, phonologicalFeatSys, "u", "cons-", "voc+", "high+", "low-", "back+", "round+", "vd+");
        AddSegDef(_table2, phonologicalFeatSys, "y", "cons-", "voc+", "high+", "low-", "back-", "round+", "vd+");
        AddSegDef(_table2, phonologicalFeatSys, "o", "cons-", "voc+", "high-", "low-", "back+", "round+", "vd+");
        AddSegDef(_table2, phonologicalFeatSys, "p", "cons+", "voc-", "bilabial", "vd-");
        AddSegDef(_table2, phonologicalFeatSys, "t", "cons+", "voc-", "alveolar", "vd-", "del_rel-", "strident-");
        AddSegDef(_table2, phonologicalFeatSys, "k", "cons+", "voc-", "velar", "vd-");
        AddSegDef(_table2, phonologicalFeatSys, "ts", "cons+", "voc-", "alveolar", "vd-", "del_rel+", "strident+");
        AddSegDef(_table2, phonologicalFeatSys, "b", "cons+", "voc-", "bilabial", "vd+");
        AddSegDef(_table2, phonologicalFeatSys, "d", "cons+", "voc-", "alveolar", "vd+", "strident-");
        AddSegDef(_table2, phonologicalFeatSys, "g", "cons+", "voc-", "velar", "vd+");
        AddSegDef(_table2, phonologicalFeatSys, "m", "cons+", "voc-", "bilabial", "vd+", "cont-", "nasal+");
        AddSegDef(_table2, phonologicalFeatSys, "n", "cons+", "voc-", "alveolar", "vd+", "cont-", "nasal+");
        AddSegDef(_table2, phonologicalFeatSys, "ŋ", "cons+", "voc-", "velar", "vd+", "cont-", "nasal+");
        AddSegDef(
            _table2,
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
            _table2,
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
            _table2,
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
            _table2,
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
        AddBdryDef(_table2, "+");
        AddBdryDef(_table2, "#");
        AddBdryDef(_table2, "!");
        AddBdryDef(_table2, ".");
        AddBdryDef(_table2, "$");

        _table3 = new CharacterDefinitionTable() { Name = "table3" };
        AddSegDef(
            _table3,
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
            _table3,
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
            _table3,
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
            _table3,
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
            _table3,
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
            _table3,
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
            _table3,
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
        AddSegDef(_table3, phonologicalFeatSys, "p", "cons+", "voc-", "bilabial", "vd-", "cont-", "nasal-");
        AddSegDef(
            _table3,
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
        AddSegDef(_table3, phonologicalFeatSys, "k", "cons+", "voc-", "velar", "vd-", "cont-", "nasal-");
        AddSegDef(
            _table3,
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
        AddSegDef(_table3, phonologicalFeatSys, "b", "cons+", "voc-", "bilabial", "vd+", "cont-", "nasal-");
        AddSegDef(
            _table3,
            phonologicalFeatSys,
            "d",
            "cons+",
            "voc-",
            "alveolar",
            "vd+",
            "strident-",
            "cont-",
            "nasal-"
        );
        AddSegDef(_table3, phonologicalFeatSys, "g", "cons+", "voc-", "velar", "vd+", "cont-", "nasal-");
        AddSegDef(_table3, phonologicalFeatSys, "m", "cons+", "voc-", "bilabial", "vd+", "cont-", "nasal+");
        AddSegDef(
            _table3,
            phonologicalFeatSys,
            "n",
            "cons+",
            "voc-",
            "alveolar",
            "vd+",
            "strident-",
            "cont-",
            "nasal+"
        );
        AddSegDef(_table3, phonologicalFeatSys, "ŋ", "cons+", "voc-", "velar", "vd+", "cont-", "nasal+");
        AddSegDef(
            _table3,
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
            _table3,
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
            _table3,
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
            _table3,
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
        AddBdryDef(_table3, "+");
        AddBdryDef(_table3, "#");
        AddBdryDef(_table3, "!");
        AddBdryDef(_table3, ".");

        _latinate = new MprFeature { Name = "latinate" };
        _germanic = new MprFeature { Name = "germanic" };

        _morphophonemic = new Stratum(_table3)
        {
            Name = "Morphophonemic",
            MorphologicalRuleOrder = MorphologicalRuleOrder.Unordered
        };
        _allophonic = new Stratum(_table1)
        {
            Name = "Allophonic",
            MorphologicalRuleOrder = MorphologicalRuleOrder.Unordered
        };
        _surface = new Stratum(_table1) { Name = "Surface", MorphologicalRuleOrder = MorphologicalRuleOrder.Unordered };

        _entries = new Dictionary<string, LexEntry>();
        var fs = FeatureStruct
            .New(syntacticFeatSys)
            .Symbol("N")
            .Feature(_head)
            .EqualTo(head => head.Symbol("foo+").Symbol("baz-"))
            .Feature(_foot)
            .EqualTo(foot => foot.Symbol("fum-").Symbol("bar+"))
            .Value;
        AddEntry("1", fs, _allophonic, "pʰit");
        fs = FeatureStruct
            .New(syntacticFeatSys)
            .Symbol("N")
            .Feature(_head)
            .EqualTo(head => head.Symbol("foo+").Symbol("baz-"))
            .Feature(_foot)
            .EqualTo(foot => foot.Symbol("fum-").Symbol("bar+"))
            .Value;
        AddEntry("2", fs, _allophonic, "pit");

        AddEntry("5", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _allophonic, "pʰut");
        AddEntry("6", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _allophonic, "kʰat");
        AddEntry("7", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _allophonic, "kʰut");

        AddEntry("8", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _allophonic, "dat");
        AddEntry("9", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, _allophonic, "dat");

        AddEntry("10", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, _morphophonemic, "ga̘p");
        AddEntry("11", FeatureStruct.New(syntacticFeatSys).Symbol("A").Value, _morphophonemic, "gab");
        AddEntry("12", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _morphophonemic, "ga+b");

        AddEntry("13", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _allophonic, "bubabu");
        AddEntry("14", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _allophonic, "bubabi");
        AddEntry("15", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _allophonic, "bɯbabu");
        AddEntry("16", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _allophonic, "bibabi");
        AddEntry("17", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _allophonic, "bubi");
        AddEntry("18", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _allophonic, "bibu");
        AddEntry("19", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _morphophonemic, "b+ubu");
        AddEntry("20", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _allophonic, "bubababi");
        AddEntry("21", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _allophonic, "bibababu");
        AddEntry("22", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _allophonic, "bubabababi");
        AddEntry("23", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _allophonic, "bibabababu");
        AddEntry("24", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _allophonic, "bubui");
        AddEntry("25", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _allophonic, "buibu");
        AddEntry("26", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _allophonic, "buibui");
        AddEntry("27", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _allophonic, "buiibuii");
        AddEntry("28", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _allophonic, "buitibuiti");
        AddEntry("29", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _allophonic, "iibubu");

        AddEntry("30", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _morphophonemic, "bu+ib");
        AddEntry("31", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _morphophonemic, "buib");

        AddEntry("32", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, _morphophonemic, "sag");
        AddEntry("33", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, _morphophonemic, "sas");
        AddEntry("34", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, _morphophonemic, "saz");
        AddEntry("35", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, _morphophonemic, "sat");
        AddEntry("36", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, _morphophonemic, "sasibo");
        AddEntry("37", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, _morphophonemic, "sasibut");
        AddEntry("38", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, _morphophonemic, "sasibud");

        AddEntry("39", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, _morphophonemic, "ab+ba");
        AddEntry("40", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, _morphophonemic, "abba");

        AddEntry("41", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, _allophonic, "pip");
        AddEntry("42", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, _morphophonemic, "bubibi");
        AddEntry("43", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, _morphophonemic, "bubibu");

        AddEntry("44", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, _morphophonemic, "gigigi");

        AddEntry("45", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, _morphophonemic, "nbinding");

        AddEntry("46", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _allophonic, "bupu");

        AddEntry("47", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, _morphophonemic, "tag");
        AddEntry("48", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, _morphophonemic, "pag");
        AddEntry("49", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, _morphophonemic, "ktb");
        AddEntry("50", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _allophonic, "suupu");
        AddEntry("51", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _morphophonemic, "miu");
        AddEntry("52", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _morphophonemic, "pu");
        AddEntry("53", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _morphophonemic, "mi");
        AddEntry("54", FeatureStruct.New().Value, _morphophonemic, "pi");
        AddEntry("55", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _morphophonemic, "mim+ɯɯ");

        fs = FeatureStruct
            .New(syntacticFeatSys)
            .Symbol("V")
            .Feature(_head)
            .EqualTo(head => head.Feature("num").EqualTo("pl"))
            .Value;
        AddEntry("Perc0", fs, _morphophonemic, "ssag");
        fs = FeatureStruct
            .New(syntacticFeatSys)
            .Symbol("V")
            .Feature(_head)
            .EqualTo(head => head.Feature("pers").EqualTo("1").Feature("num").EqualTo("pl"))
            .Value;
        AddEntry("Perc1", fs, _morphophonemic, "ssag");
        fs = FeatureStruct
            .New(syntacticFeatSys)
            .Symbol("V")
            .Feature(_head)
            .EqualTo(head => head.Feature("pers").EqualTo("3").Feature("num").EqualTo("pl"))
            .Value;
        AddEntry("Perc2", fs, _morphophonemic, "ssag");
        fs = FeatureStruct
            .New(syntacticFeatSys)
            .Symbol("V")
            .Feature(_head)
            .EqualTo(head => head.Feature("pers").EqualTo("2", "3").Feature("num").EqualTo("pl"))
            .Value;
        AddEntry("Perc3", fs, _morphophonemic, "ssag");
        fs = FeatureStruct
            .New(syntacticFeatSys)
            .Symbol("V")
            .Feature(_head)
            .EqualTo(head => head.Feature("pers").EqualTo("1", "3").Feature("num").EqualTo("pl"))
            .Value;
        AddEntry("Perc4", fs, _morphophonemic, "ssag");

        var seeFamily = new LexFamily { Name = "SEE" };
        seeFamily.Entries.Add(
            AddEntry("bl1", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, _morphophonemic, "si")
        );
        fs = FeatureStruct
            .New(syntacticFeatSys)
            .Symbol("V")
            .Feature(_head)
            .EqualTo(head => head.Feature("tense").EqualTo("past"))
            .Value;
        seeFamily.Entries.Add(AddEntry("bl2", fs, _morphophonemic, "sau"));
        fs = FeatureStruct
            .New(syntacticFeatSys)
            .Symbol("V")
            .Feature(_head)
            .EqualTo(head => head.Feature("tense").EqualTo("pres"))
            .Value;
        seeFamily.Entries.Add(AddEntry("bl3", fs, _morphophonemic, "sis"));

        LexEntry entry = AddEntry("pos1", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, _morphophonemic, "ba");
        entry.MprFeatures.Add(_latinate);
        entry = AddEntry("pos2", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, _morphophonemic, "ba");
        entry.MprFeatures.Add(_germanic);

        var vowel = FeatureStruct.New(phonologicalFeatSys).Symbol(HCFeatureSystem.Segment).Symbol("voc+").Value;
        entry = AddEntry(
            "free",
            FeatureStruct.New(syntacticFeatSys).Symbol("V").Value,
            _morphophonemic,
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
            _morphophonemic,
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
                .Feature(_head)
                .EqualTo(head => head.Feature("tense").EqualTo("pres"))
                .Value,
            _morphophonemic,
            "san",
            "sad",
            "sap"
        );
        entry.Allomorphs[1].StemName = new StemName(
            FeatureStruct
                .New(syntacticFeatSys)
                .Symbol("V")
                .Feature(_head)
                .EqualTo(head => head.Feature("pers").EqualTo("1"))
                .Value,
            FeatureStruct
                .New(syntacticFeatSys)
                .Symbol("V")
                .Feature(_head)
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
                .Feature(_head)
                .EqualTo(head => head.Feature("pers").EqualTo("1"))
                .Value,
            FeatureStruct
                .New(syntacticFeatSys)
                .Symbol("V")
                .Feature(_head)
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
                .Feature(_head)
                .EqualTo(head => head.Feature("tense").EqualTo("pres"))
                .Value,
            _morphophonemic,
            "bag"
        );

        entry = AddEntry("bound", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, _morphophonemic, "dag");
        entry.PrimaryAllomorph.IsBound = true;

        _language = new Language
        {
            Name = "Test",
            PhonologicalFeatureSystem = phonologicalFeatSys,
            SyntacticFeatureSystem = syntacticFeatSys,
            Strata = { _morphophonemic, _allophonic, _surface }
        };
    }

    [TearDown]
    public void TestCleanup()
    {
        foreach (Stratum stratum in _language.Strata)
        {
            stratum.PhonologicalRules.Clear();
            stratum.MorphologicalRules.Clear();
            stratum.AffixTemplates.Clear();
        }
    }

    private LexEntry AddEntry(string gloss, FeatureStruct syntacticFS, Stratum stratum, params string[] forms)
    {
        var entry = new LexEntry
        {
            Id = gloss,
            SyntacticFeatureStruct = syntacticFS,
            Gloss = gloss,
            IsPartial = syntacticFS.IsEmpty
        };
        foreach (string form in forms)
            entry.Allomorphs.Add(new RootAllomorph(new Segments(stratum.CharacterDefinitionTable, form)));
        stratum.Entries.Add(entry);
        _entries[gloss] = entry;
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

    protected FeatureStruct GetFeatureFromChar(CharacterDefinitionTable table, string strRep)
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
            _language.PhonologicalFeatureSystem.GetFeature<SymbolicFeature>(featureID),
            variable,
            agree
        );
    }
}
