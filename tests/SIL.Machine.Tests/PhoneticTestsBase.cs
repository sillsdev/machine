using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine;

[TestFixture]
public abstract class PhoneticTestsBase
{
    protected FeatureSystem _phoneticFeatSys = default!;
    protected FeatureSystem _wordFeatSys = default!;
    protected FeatureSystem _typeFeatSys = default!;
    protected SymbolicFeature _type = default!;
    protected FeatureSymbol _word = default!;
    protected FeatureSymbol _nP = default!;
    protected FeatureSymbol _vP = default!;
    protected FeatureSymbol _seg = default!;
    protected FeatureSymbol _bdry = default!;
    protected FeatureSymbol _allo = default!;
    protected FeatureSymbol _anchor = default!;
    protected Dictionary<char, FeatureStruct> _characters = default!;

    [OneTimeSetUp]
    public virtual void FixtureSetUp()
    {
        _phoneticFeatSys = new FeatureSystem
        {
            new SymbolicFeature(
                "son",
                new FeatureSymbol("son+", "+"),
                new FeatureSymbol("son-", "-"),
                new FeatureSymbol("son?", "?")
            )
            {
                DefaultSymbolID = "son?"
            },
            new SymbolicFeature(
                "syl",
                new FeatureSymbol("syl+", "+"),
                new FeatureSymbol("syl-", "-"),
                new FeatureSymbol("syl?", "?")
            )
            {
                DefaultSymbolID = "syl?"
            },
            new SymbolicFeature(
                "cons",
                new FeatureSymbol("cons+", "+"),
                new FeatureSymbol("cons-", "-"),
                new FeatureSymbol("cons?", "?")
            )
            {
                DefaultSymbolID = "cons?"
            },
            new SymbolicFeature(
                "high",
                new FeatureSymbol("high+", "+"),
                new FeatureSymbol("high-", "-"),
                new FeatureSymbol("high?", "?")
            )
            {
                DefaultSymbolID = "high?"
            },
            new SymbolicFeature(
                "back",
                new FeatureSymbol("back+", "+"),
                new FeatureSymbol("back-", "-"),
                new FeatureSymbol("back?", "?")
            )
            {
                DefaultSymbolID = "back?"
            },
            new SymbolicFeature(
                "front",
                new FeatureSymbol("front+", "+"),
                new FeatureSymbol("front-", "-"),
                new FeatureSymbol("front?", "?")
            )
            {
                DefaultSymbolID = "front?"
            },
            new SymbolicFeature(
                "low",
                new FeatureSymbol("low+", "+"),
                new FeatureSymbol("low-", "-"),
                new FeatureSymbol("low?", "?")
            )
            {
                DefaultSymbolID = "low?"
            },
            new SymbolicFeature(
                "rnd",
                new FeatureSymbol("rnd+", "+"),
                new FeatureSymbol("rnd-", "-"),
                new FeatureSymbol("rnd?", "?")
            )
            {
                DefaultSymbolID = "rnd?"
            },
            new SymbolicFeature(
                "ant",
                new FeatureSymbol("ant+", "+"),
                new FeatureSymbol("ant-", "-"),
                new FeatureSymbol("ant?", "?")
            )
            {
                DefaultSymbolID = "ant?"
            },
            new SymbolicFeature(
                "cor",
                new FeatureSymbol("cor+", "+"),
                new FeatureSymbol("cor-", "-"),
                new FeatureSymbol("cor?", "?")
            )
            {
                DefaultSymbolID = "cor?"
            },
            new SymbolicFeature(
                "voice",
                new FeatureSymbol("voice+", "+"),
                new FeatureSymbol("voice-", "-"),
                new FeatureSymbol("voice?", "?")
            )
            {
                DefaultSymbolID = "voice?"
            },
            new SymbolicFeature(
                "cont",
                new FeatureSymbol("cont+", "+"),
                new FeatureSymbol("cont-", "-"),
                new FeatureSymbol("cont?", "?")
            )
            {
                DefaultSymbolID = "cont?"
            },
            new SymbolicFeature(
                "nas",
                new FeatureSymbol("nas+", "+"),
                new FeatureSymbol("nas-", "-"),
                new FeatureSymbol("nas?", "?")
            )
            {
                DefaultSymbolID = "nas?"
            },
            new SymbolicFeature(
                "str",
                new FeatureSymbol("str+", "+"),
                new FeatureSymbol("str-", "-"),
                new FeatureSymbol("str?", "?")
            )
            {
                DefaultSymbolID = "str?"
            },
            new StringFeature("strRep")
        };

        _wordFeatSys = new FeatureSystem
        {
            new SymbolicFeature(
                "POS",
                new FeatureSymbol("noun"),
                new FeatureSymbol("verb"),
                new FeatureSymbol("adj"),
                new FeatureSymbol("adv"),
                new FeatureSymbol("det")
            )
        };

        _word = new FeatureSymbol("Word");
        _nP = new FeatureSymbol("NP");
        _vP = new FeatureSymbol("VP");
        _seg = new FeatureSymbol("Seg");
        _bdry = new FeatureSymbol("Bdry");
        _allo = new FeatureSymbol("Allo");
        _anchor = new FeatureSymbol("Anchor");

        _type = new SymbolicFeature("Type", _word, _nP, _vP, _seg, _bdry, _allo, _anchor);

        _typeFeatSys = new FeatureSystem { _type };

        _characters = new Dictionary<char, FeatureStruct>
        {
            {
                'b',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son-")
                    .Symbol("syl-")
                    .Symbol("cons+")
                    .Symbol("high-")
                    .Symbol("ant+")
                    .Symbol("cor-")
                    .Symbol("voice+")
                    .Symbol("cont-")
                    .Symbol("nas-")
                    .Symbol("str-")
                    .Value
            },
            {
                'd',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son-")
                    .Symbol("syl-")
                    .Symbol("cons+")
                    .Symbol("high-")
                    .Symbol("ant+")
                    .Symbol("cor+")
                    .Symbol("voice+")
                    .Symbol("cont-")
                    .Symbol("nas-")
                    .Symbol("str-")
                    .Value
            },
            {
                'g',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son-")
                    .Symbol("syl-")
                    .Symbol("cons+")
                    .Symbol("high+")
                    .Symbol("ant-")
                    .Symbol("cor-")
                    .Symbol("voice+")
                    .Symbol("cont-")
                    .Symbol("nas-")
                    .Symbol("str-")
                    .Value
            },
            {
                'p',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son-")
                    .Symbol("syl-")
                    .Symbol("cons+")
                    .Symbol("high-")
                    .Symbol("ant+")
                    .Symbol("cor-")
                    .Symbol("voice-")
                    .Symbol("cont-")
                    .Symbol("nas-")
                    .Symbol("str-")
                    .Value
            },
            {
                't',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son-")
                    .Symbol("syl-")
                    .Symbol("cons+")
                    .Symbol("high-")
                    .Symbol("ant+")
                    .Symbol("cor+")
                    .Symbol("voice-")
                    .Symbol("cont-")
                    .Symbol("nas-")
                    .Symbol("str-")
                    .Value
            },
            {
                'q',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son-")
                    .Symbol("syl-")
                    .Symbol("cons+")
                    .Symbol("high+")
                    .Symbol("ant-")
                    .Symbol("cor-")
                    .Symbol("voice-")
                    .Symbol("cont-")
                    .Symbol("nas-")
                    .Symbol("str-")
                    .Value
            },
            {
                'c',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son-")
                    .Symbol("syl-")
                    .Symbol("cons+")
                    .Symbol("high+")
                    .Symbol("ant-")
                    .Symbol("cor-")
                    .Symbol("voice-")
                    .Symbol("cont-")
                    .Symbol("nas-")
                    .Symbol("str-")
                    .Value
            },
            {
                'k',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son-")
                    .Symbol("syl-")
                    .Symbol("cons+")
                    .Symbol("high+")
                    .Symbol("ant-")
                    .Symbol("cor-")
                    .Symbol("voice-")
                    .Symbol("cont-")
                    .Symbol("nas-")
                    .Symbol("str-")
                    .Value
            },
            {
                'x',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son-")
                    .Symbol("syl-")
                    .Symbol("cons+")
                    .Symbol("high+")
                    .Symbol("ant-")
                    .Symbol("cor-")
                    .Symbol("voice-")
                    .Symbol("cont-")
                    .Symbol("nas-")
                    .Symbol("str-")
                    .Value
            },
            {
                'j',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son-")
                    .Symbol("syl-")
                    .Symbol("cons+")
                    .Symbol("high+")
                    .Symbol("ant-")
                    .Symbol("cor+")
                    .Symbol("voice+")
                    .Symbol("cont-")
                    .Symbol("nas-")
                    .Symbol("str+")
                    .Value
            },
            {
                's',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son-")
                    .Symbol("syl-")
                    .Symbol("cons+")
                    .Symbol("high-")
                    .Symbol("ant+")
                    .Symbol("cor+")
                    .Symbol("voice-")
                    .Symbol("cont+")
                    .Symbol("nas-")
                    .Symbol("str+")
                    .Value
            },
            {
                'z',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son-")
                    .Symbol("syl-")
                    .Symbol("cons+")
                    .Symbol("high-")
                    .Symbol("ant+")
                    .Symbol("cor+")
                    .Symbol("voice+")
                    .Symbol("cont+")
                    .Symbol("nas-")
                    .Symbol("str+")
                    .Value
            },
            {
                'f',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son-")
                    .Symbol("syl-")
                    .Symbol("cons+")
                    .Symbol("high-")
                    .Symbol("ant+")
                    .Symbol("cor-")
                    .Symbol("voice-")
                    .Symbol("cont+")
                    .Symbol("nas-")
                    .Symbol("str+")
                    .Value
            },
            {
                'v',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son-")
                    .Symbol("syl-")
                    .Symbol("cons+")
                    .Symbol("high-")
                    .Symbol("ant+")
                    .Symbol("cor-")
                    .Symbol("voice+")
                    .Symbol("cont+")
                    .Symbol("nas-")
                    .Symbol("str+")
                    .Value
            },
            {
                'w',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son+")
                    .Symbol("syl-")
                    .Symbol("cons-")
                    .Symbol("high+")
                    .Symbol("back+")
                    .Symbol("front-")
                    .Symbol("low-")
                    .Symbol("rnd+")
                    .Symbol("ant-")
                    .Symbol("cor-")
                    .Value
            },
            {
                'y',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son+")
                    .Symbol("syl-")
                    .Symbol("cons-")
                    .Symbol("high+")
                    .Symbol("back-")
                    .Symbol("front+")
                    .Symbol("low-")
                    .Symbol("rnd-")
                    .Symbol("ant-")
                    .Symbol("cor-")
                    .Value
            },
            {
                'h',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son+")
                    .Symbol("syl-")
                    .Symbol("cons-")
                    .Symbol("high-")
                    .Symbol("back-")
                    .Symbol("front-")
                    .Symbol("low+")
                    .Symbol("ant-")
                    .Symbol("cor-")
                    .Symbol("voice-")
                    .Symbol("cont+")
                    .Symbol("nas-")
                    .Symbol("str-")
                    .Value
            },
            {
                'r',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son+")
                    .Symbol("syl-")
                    .Symbol("cons+")
                    .Symbol("high-")
                    .Symbol("back-")
                    .Symbol("front-")
                    .Symbol("low-")
                    .Symbol("ant-")
                    .Symbol("cor+")
                    .Symbol("voice+")
                    .Symbol("cont+")
                    .Symbol("nas-")
                    .Symbol("str-")
                    .Value
            },
            {
                'l',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son+")
                    .Symbol("syl-")
                    .Symbol("cons+")
                    .Symbol("high-")
                    .Symbol("back-")
                    .Symbol("front-")
                    .Symbol("low-")
                    .Symbol("ant+")
                    .Symbol("cor+")
                    .Symbol("voice+")
                    .Symbol("cont+")
                    .Symbol("nas-")
                    .Symbol("str-")
                    .Value
            },
            {
                'm',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son+")
                    .Symbol("syl-")
                    .Symbol("cons+")
                    .Symbol("high-")
                    .Symbol("low-")
                    .Symbol("ant+")
                    .Symbol("cor-")
                    .Symbol("voice+")
                    .Symbol("cont-")
                    .Symbol("nas+")
                    .Symbol("str-")
                    .Value
            },
            {
                'n',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son+")
                    .Symbol("syl-")
                    .Symbol("cons+")
                    .Symbol("high-")
                    .Symbol("low-")
                    .Symbol("ant+")
                    .Symbol("cor+")
                    .Symbol("voice+")
                    .Symbol("cont-")
                    .Symbol("nas+")
                    .Symbol("str-")
                    .Value
            },
            {
                'N',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son+")
                    .Symbol("syl-")
                    .Symbol("cons+")
                    .Symbol("high-")
                    .Symbol("low-")
                    .Symbol("ant+")
                    .Symbol("cor?")
                    .Symbol("voice+")
                    .Symbol("cont-")
                    .Symbol("nas+")
                    .Symbol("str-")
                    .Value
            },
            {
                'a',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son+")
                    .Symbol("syl+")
                    .Symbol("cons-")
                    .Symbol("high-")
                    .Symbol("back-")
                    .Symbol("front+")
                    .Symbol("low+")
                    .Symbol("rnd-")
                    .Value
            },
            {
                'e',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son+")
                    .Symbol("syl+")
                    .Symbol("cons-")
                    .Symbol("high-")
                    .Symbol("back-")
                    .Symbol("front+")
                    .Symbol("low-")
                    .Symbol("rnd-")
                    .Value
            },
            {
                'i',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son+")
                    .Symbol("syl+")
                    .Symbol("cons-")
                    .Symbol("high+")
                    .Symbol("back-")
                    .Symbol("front+")
                    .Symbol("low-")
                    .Symbol("rnd-")
                    .Value
            },
            {
                'o',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son+")
                    .Symbol("syl+")
                    .Symbol("cons-")
                    .Symbol("high-")
                    .Symbol("back+")
                    .Symbol("front-")
                    .Symbol("low+")
                    .Symbol("rnd-")
                    .Value
            },
            {
                'u',
                FeatureStruct
                    .New(_phoneticFeatSys)
                    .Symbol(_seg)
                    .Symbol("son+")
                    .Symbol("syl+")
                    .Symbol("cons-")
                    .Symbol("high+")
                    .Symbol("back+")
                    .Symbol("front-")
                    .Symbol("low-")
                    .Symbol("rnd+")
                    .Value
            },
            { '+', FeatureStruct.New(_phoneticFeatSys).Symbol(_bdry).Feature("strRep").EqualTo("+").Value },
            { ',', FeatureStruct.New(_phoneticFeatSys).Symbol(_bdry).Feature("strRep").EqualTo(",").Value },
            { ' ', FeatureStruct.New(_phoneticFeatSys).Symbol(_bdry).Feature("strRep").EqualTo(" ").Value },
            { '.', FeatureStruct.New(_phoneticFeatSys).Symbol(_bdry).Feature("strRep").EqualTo(".").Value },
            { '0', FeatureStruct.New(_phoneticFeatSys).Symbol(_bdry).Feature("strRep").EqualTo("0").Value }
        };
    }

    protected AnnotatedStringData CreateStringData(string str)
    {
        var stringData = new AnnotatedStringData(str);
        for (int i = 0; i < str.Length; i++)
        {
            FeatureStruct fs = _characters[str[i]];
            stringData.Annotations.Add(i, i + 1, fs.Clone());
        }
        return stringData;
    }
}
