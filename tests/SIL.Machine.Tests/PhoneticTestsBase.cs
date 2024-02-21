﻿using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine;

[TestFixture]
public abstract class PhoneticTestsBase
{
    protected FeatureSystem PhoneticFeatSys { get; set; } = default!;
    protected FeatureSystem WordFeatSys { get; set; } = default!;
    protected FeatureSystem TypeFeatSys { get; set; } = default!;
    protected SymbolicFeature Type { get; set; } = default!;
    protected FeatureSymbol Word { get; set; } = default!;
    protected FeatureSymbol NP { get; set; } = default!;
    protected FeatureSymbol VP { get; set; } = default!;
    protected FeatureSymbol Seg { get; set; } = default!;
    protected FeatureSymbol Bdry { get; set; } = default!;
    protected FeatureSymbol Allo { get; set; } = default!;
    protected FeatureSymbol Anchor { get; set; } = default!;
    protected Dictionary<char, FeatureStruct> Characters { get; set; } = default!;

    [OneTimeSetUp]
    public virtual void FixtureSetUp()
    {
        PhoneticFeatSys = new FeatureSystem
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

        WordFeatSys = new FeatureSystem
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

        Word = new FeatureSymbol("Word");
        NP = new FeatureSymbol("NP");
        VP = new FeatureSymbol("VP");
        Seg = new FeatureSymbol("Seg");
        Bdry = new FeatureSymbol("Bdry");
        Allo = new FeatureSymbol("Allo");
        Anchor = new FeatureSymbol("Anchor");

        Type = new SymbolicFeature("Type", Word, NP, VP, Seg, Bdry, Allo, Anchor);

        TypeFeatSys = new FeatureSystem { Type };

        Characters = new Dictionary<char, FeatureStruct>
        {
            {
                'b',
                FeatureStruct
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
                    .New(PhoneticFeatSys)
                    .Symbol(Seg)
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
            { '+', FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo("+").Value },
            { ',', FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(",").Value },
            { ' ', FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(" ").Value },
            { '.', FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo(".").Value },
            { '0', FeatureStruct.New(PhoneticFeatSys).Symbol(Bdry).Feature("strRep").EqualTo("0").Value }
        };
    }

    protected AnnotatedStringData CreateStringData(string str)
    {
        var stringData = new AnnotatedStringData(str);
        for (int i = 0; i < str.Length; i++)
        {
            FeatureStruct fs = Characters[str[i]];
            stringData.Annotations.Add(i, i + 1, fs.Clone());
        }
        return stringData;
    }
}
