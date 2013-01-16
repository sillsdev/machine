using System.Collections.Generic;
using NUnit.Framework;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Test
{
	[TestFixture]
	public abstract class PhoneticTestBase
	{
		protected SpanFactory<int> SpanFactory;
		protected FeatureSystem PhoneticFeatSys;
		protected FeatureSystem WordFeatSys;
		protected FeatureSystem TypeFeatSys;
		protected SymbolicFeature Type;
		protected FeatureSymbol Word;
		protected FeatureSymbol NP;
		protected FeatureSymbol VP;
		protected FeatureSymbol Seg;
		protected FeatureSymbol Bdry;
		protected Dictionary<char, FeatureStruct> Characters;

		[TestFixtureSetUp]
		public virtual void FixtureSetUp()
		{
			SpanFactory = new IntegerSpanFactory();

			PhoneticFeatSys = new FeatureSystem
			                  	{
			                  		new SymbolicFeature("son") {PossibleSymbols = {{"son+", "+"}, {"son-", "-"}, {"son?", "?", true}}},
									new SymbolicFeature("syl") {PossibleSymbols = {{"syl+", "+"}, {"syl-", "-"}, {"syl?", "?", true}}},
									new SymbolicFeature("cons") {PossibleSymbols = {{"cons+", "+"}, {"cons-", "-"}, {"cons?", "?", true}}},
									new SymbolicFeature("high") {PossibleSymbols = {{"high+", "+"}, {"high-", "-"}, {"high?", "?", true}}},
									new SymbolicFeature("back") {PossibleSymbols = {{"back+", "+"}, {"back-", "-"}, {"back?", "?", true}}},
									new SymbolicFeature("front") {PossibleSymbols = {{"front+", "+"}, {"front-", "-"}, {"front?", "?", true}}},
									new SymbolicFeature("low") {PossibleSymbols = {{"low+", "+"}, {"low-", "-"}, {"low?", "?", true}}},
									new SymbolicFeature("rnd") {PossibleSymbols = {{"rnd+", "+"}, {"rnd-", "-"}, {"rnd?", "?", true}}},
									new SymbolicFeature("ant") {PossibleSymbols = {{"ant+", "+"}, {"ant-", "-"}, {"ant?", "?", true}}},
									new SymbolicFeature("cor") {PossibleSymbols = {{"cor+", "+"}, {"cor-", "-"}, {"cor?", "?", true}}},
									new SymbolicFeature("voice") {PossibleSymbols = {{"voice+", "+"}, {"voice-", "-"}, {"voice?", "?", true}}},
									new SymbolicFeature("cont") {PossibleSymbols = {{"cont+", "+"}, {"cont-", "-"}, {"cont?", "?", true}}},
									new SymbolicFeature("nas") {PossibleSymbols = {{"nas+", "+"}, {"nas-", "-"}, {"nas?", "?", true}}},
									new SymbolicFeature("str") {PossibleSymbols = {{"str+", "+"}, {"str-", "-"}, {"str?", "?", true}}},
									new StringFeature("strRep")
			                  	};

			WordFeatSys = new FeatureSystem
			              	{
			              		new SymbolicFeature("POS") {PossibleSymbols = {"noun", "verb", "adj", "adv", "det"}}
			              	};

			Word = new FeatureSymbol("Word");
			NP = new FeatureSymbol("NP");
			VP = new FeatureSymbol("VP");
			Seg = new FeatureSymbol("Seg");
			Bdry = new FeatureSymbol("Bdry");

			Type = new SymbolicFeature("Type") {PossibleSymbols = {Word, NP, VP, Seg, Bdry}};

			TypeFeatSys = new FeatureSystem {Type};

			Characters = new Dictionary<char, FeatureStruct>
				{
					{'b', FeatureStruct.New(PhoneticFeatSys)
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
							.Symbol("str-").Value},
					{'d', FeatureStruct.New(PhoneticFeatSys)
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
							.Symbol("str-").Value},
					{'g', FeatureStruct.New(PhoneticFeatSys)
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
							.Symbol("str-").Value},
					{'p', FeatureStruct.New(PhoneticFeatSys)
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
							.Symbol("str-").Value},
					{'t', FeatureStruct.New(PhoneticFeatSys)
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
							.Symbol("str-").Value},
					{'q', FeatureStruct.New(PhoneticFeatSys)
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
							.Symbol("str-").Value},
					{'c', FeatureStruct.New(PhoneticFeatSys)
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
							.Symbol("str-").Value},
					{'k', FeatureStruct.New(PhoneticFeatSys)
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
							.Symbol("str-").Value},
					{'x', FeatureStruct.New(PhoneticFeatSys)
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
							.Symbol("str-").Value},
					{'j', FeatureStruct.New(PhoneticFeatSys)
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
							.Symbol("str+").Value},
					{'s', FeatureStruct.New(PhoneticFeatSys)
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
							.Symbol("str+").Value},
					{'z', FeatureStruct.New(PhoneticFeatSys)
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
							.Symbol("str+").Value},
					{'f', FeatureStruct.New(PhoneticFeatSys)
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
							.Symbol("str+").Value},
					{'v', FeatureStruct.New(PhoneticFeatSys)
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
							.Symbol("str+").Value},
					{'w', FeatureStruct.New(PhoneticFeatSys)
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
							.Symbol("cor-").Value},
					{'y', FeatureStruct.New(PhoneticFeatSys)
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
							.Symbol("cor-").Value},
					{'h', FeatureStruct.New(PhoneticFeatSys)
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
							.Symbol("str-").Value},
					{'r', FeatureStruct.New(PhoneticFeatSys)
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
							.Symbol("str-").Value},
					{'l', FeatureStruct.New(PhoneticFeatSys)
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
							.Symbol("str-").Value},
					{'m', FeatureStruct.New(PhoneticFeatSys)
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
							.Symbol("str-").Value},
					{'n', FeatureStruct.New(PhoneticFeatSys)
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
							.Symbol("str-").Value},
					{'N', FeatureStruct.New(PhoneticFeatSys)
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
							.Symbol("str-").Value},
					{'a', FeatureStruct.New(PhoneticFeatSys)
							.Symbol(Seg)
							.Symbol("son+")
							.Symbol("syl+")
							.Symbol("cons-")
							.Symbol("high-")
							.Symbol("back-")
							.Symbol("front+")
							.Symbol("low+")
							.Symbol("rnd-").Value},
					{'e', FeatureStruct.New(PhoneticFeatSys)
							.Symbol(Seg)
							.Symbol("son+")
							.Symbol("syl+")
							.Symbol("cons-")
							.Symbol("high-")
							.Symbol("back-")
							.Symbol("front+")
							.Symbol("low-")
							.Symbol("rnd-").Value},
					{'i', FeatureStruct.New(PhoneticFeatSys)
							.Symbol(Seg)
							.Symbol("son+")
							.Symbol("syl+")
							.Symbol("cons-")
							.Symbol("high+")
							.Symbol("back-")
							.Symbol("front+")
							.Symbol("low-")
							.Symbol("rnd-").Value},
					{'o', FeatureStruct.New(PhoneticFeatSys)
							.Symbol(Seg)
							.Symbol("son+")
							.Symbol("syl+")
							.Symbol("cons-")
							.Symbol("high-")
							.Symbol("back+")
							.Symbol("front-")
							.Symbol("low+")
							.Symbol("rnd-").Value},
					{'u', FeatureStruct.New(PhoneticFeatSys)
							.Symbol(Seg)
							.Symbol("son+")
							.Symbol("syl+")
							.Symbol("cons-")
							.Symbol("high+")
							.Symbol("back+")
							.Symbol("front-")
							.Symbol("low-")
							.Symbol("rnd+").Value},
					{'+', FeatureStruct.New(PhoneticFeatSys)
							.Symbol(Bdry)
							.Feature("strRep").EqualTo("+").Value},
					{',', FeatureStruct.New(PhoneticFeatSys)
							.Symbol(Bdry)
							.Feature("strRep").EqualTo(",").Value},
					{' ', FeatureStruct.New(PhoneticFeatSys)
							.Symbol(Bdry)
							.Feature("strRep").EqualTo(" ").Value},
					{'.', FeatureStruct.New(PhoneticFeatSys)
							.Symbol(Bdry)
							.Feature("strRep").EqualTo(".").Value}
				};
		}

		protected StringData CreateStringData(string str)
		{
			var stringData = new StringData(SpanFactory, str);
			for (int i = 0; i < str.Length; i++)
			{
				FeatureStruct fs = Characters[str[i]];
				stringData.Annotations.Add(i, i + 1, fs.DeepClone());
			}
			return stringData;
		}
	}
}
