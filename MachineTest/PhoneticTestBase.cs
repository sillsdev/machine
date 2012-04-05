using System.Globalization;
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

		[TestFixtureSetUp]
		public void FixtureSetUp()
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
		}

		protected StringData CreateStringData(string str)
		{
			var stringData = new StringData(SpanFactory, str);
			for (int i = 0; i < str.Length; i++)
			{
				FeatureSymbol type = Seg;
				FeatureStruct fs;
				switch (str[i])
				{
					case 'b':
						fs = FeatureStruct.NewMutable(PhoneticFeatSys)
							.Symbol("son-")
							.Symbol("syl-")
							.Symbol("cons+")
							.Symbol("high-")
							.Symbol("ant+")
							.Symbol("cor-")
							.Symbol("voice+")
							.Symbol("cont-")
							.Symbol("nas-")
							.Symbol("str-").Value;
						break;
					case 'd':
						fs = FeatureStruct.NewMutable(PhoneticFeatSys)
							.Symbol("son-")
							.Symbol("syl-")
							.Symbol("cons+")
							.Symbol("high-")
							.Symbol("ant+")
							.Symbol("cor+")
							.Symbol("voice+")
							.Symbol("cont-")
							.Symbol("nas-")
							.Symbol("str-").Value;
						break;
					case 'g':
						fs = FeatureStruct.NewMutable(PhoneticFeatSys)
							.Symbol("son-")
							.Symbol("syl-")
							.Symbol("cons+")
							.Symbol("high+")
							.Symbol("ant-")
							.Symbol("cor-")
							.Symbol("voice+")
							.Symbol("cont-")
							.Symbol("nas-")
							.Symbol("str-").Value;
						break;
					case 'p':
						fs = FeatureStruct.NewMutable(PhoneticFeatSys)
							.Symbol("son-")
							.Symbol("syl-")
							.Symbol("cons+")
							.Symbol("high-")
							.Symbol("ant+")
							.Symbol("cor-")
							.Symbol("voice-")
							.Symbol("cont-")
							.Symbol("nas-")
							.Symbol("str-").Value;
						break;
					case 't':
						fs = FeatureStruct.NewMutable(PhoneticFeatSys)
							.Symbol("son-")
							.Symbol("syl-")
							.Symbol("cons+")
							.Symbol("high-")
							.Symbol("ant+")
							.Symbol("cor+")
							.Symbol("voice-")
							.Symbol("cont-")
							.Symbol("nas-")
							.Symbol("str-").Value;
						break;
					case 'q':
					case 'c':
					case 'k':
					case 'x':
						fs = FeatureStruct.NewMutable(PhoneticFeatSys)
							.Symbol("son-")
							.Symbol("syl-")
							.Symbol("cons+")
							.Symbol("high+")
							.Symbol("ant-")
							.Symbol("cor-")
							.Symbol("voice-")
							.Symbol("cont-")
							.Symbol("nas-")
							.Symbol("str-").Value;
						break;
					case 'j':
						fs = FeatureStruct.NewMutable(PhoneticFeatSys)
							.Symbol("son-")
							.Symbol("syl-")
							.Symbol("cons+")
							.Symbol("high+")
							.Symbol("ant-")
							.Symbol("cor+")
							.Symbol("voice+")
							.Symbol("cont-")
							.Symbol("nas-")
							.Symbol("str+").Value;
						break;
					case 's':
						fs = FeatureStruct.NewMutable(PhoneticFeatSys)
							.Symbol("son-")
							.Symbol("syl-")
							.Symbol("cons+")
							.Symbol("high-")
							.Symbol("ant+")
							.Symbol("cor+")
							.Symbol("voice-")
							.Symbol("cont+")
							.Symbol("nas-")
							.Symbol("str+").Value;
						break;
					case 'z':
						fs = FeatureStruct.NewMutable(PhoneticFeatSys)
							.Symbol("son-")
							.Symbol("syl-")
							.Symbol("cons+")
							.Symbol("high-")
							.Symbol("ant+")
							.Symbol("cor+")
							.Symbol("voice+")
							.Symbol("cont+")
							.Symbol("nas-")
							.Symbol("str+").Value;
						break;
					case 'f':
						fs = FeatureStruct.NewMutable(PhoneticFeatSys)
							.Symbol("son-")
							.Symbol("syl-")
							.Symbol("cons+")
							.Symbol("high-")
							.Symbol("ant+")
							.Symbol("cor-")
							.Symbol("voice-")
							.Symbol("cont+")
							.Symbol("nas-")
							.Symbol("str+").Value;
						break;
					case 'v':
						fs = FeatureStruct.NewMutable(PhoneticFeatSys)
							.Symbol("son-")
							.Symbol("syl-")
							.Symbol("cons+")
							.Symbol("high-")
							.Symbol("ant+")
							.Symbol("cor-")
							.Symbol("voice+")
							.Symbol("cont+")
							.Symbol("nas-")
							.Symbol("str+").Value;
						break;
					case 'w':
						fs = FeatureStruct.NewMutable(PhoneticFeatSys)
							.Symbol("son+")
							.Symbol("syl-")
							.Symbol("cons-")
							.Symbol("high+")
							.Symbol("back+")
							.Symbol("front-")
							.Symbol("low-")
							.Symbol("rnd+")
							.Symbol("ant-")
							.Symbol("cor-").Value;
						break;
					case 'y':
						fs = FeatureStruct.NewMutable(PhoneticFeatSys)
							.Symbol("son+")
							.Symbol("syl-")
							.Symbol("cons-")
							.Symbol("high+")
							.Symbol("back-")
							.Symbol("front+")
							.Symbol("low-")
							.Symbol("rnd-")
							.Symbol("ant-")
							.Symbol("cor-").Value;
						break;
					case 'h':
						fs = FeatureStruct.NewMutable(PhoneticFeatSys)
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
							.Symbol("str-").Value;
						break;
					case 'r':
						fs = FeatureStruct.NewMutable(PhoneticFeatSys)
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
							.Symbol("str-").Value;
						break;
					case 'l':
						fs = FeatureStruct.NewMutable(PhoneticFeatSys)
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
							.Symbol("str-").Value;
						break;
					case 'm':
						fs = FeatureStruct.NewMutable(PhoneticFeatSys)
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
							.Symbol("str-").Value;
						break;
					case 'n':
						fs = FeatureStruct.NewMutable(PhoneticFeatSys)
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
							.Symbol("str-").Value;
						break;
					case 'a':
						fs = FeatureStruct.NewMutable(PhoneticFeatSys)
							.Symbol("son+")
							.Symbol("syl+")
							.Symbol("cons-")
							.Symbol("high-")
							.Symbol("back-")
							.Symbol("front+")
							.Symbol("low+")
							.Symbol("rnd-").Value;
						break;
					case 'e':
						fs = FeatureStruct.NewMutable(PhoneticFeatSys)
							.Symbol("son+")
							.Symbol("syl+")
							.Symbol("cons-")
							.Symbol("high-")
							.Symbol("back-")
							.Symbol("front+")
							.Symbol("low-")
							.Symbol("rnd-").Value;
						break;
					case 'i':
						fs = FeatureStruct.NewMutable(PhoneticFeatSys)
							.Symbol("son+")
							.Symbol("syl+")
							.Symbol("cons-")
							.Symbol("high+")
							.Symbol("back-")
							.Symbol("front+")
							.Symbol("low-")
							.Symbol("rnd-").Value;
						break;
					case 'o':
						fs = FeatureStruct.NewMutable(PhoneticFeatSys)
							.Symbol("son+")
							.Symbol("syl+")
							.Symbol("cons-")
							.Symbol("high-")
							.Symbol("back+")
							.Symbol("front-")
							.Symbol("low+")
							.Symbol("rnd-").Value;
						break;
					case 'u':
						fs = FeatureStruct.NewMutable(PhoneticFeatSys)
							.Symbol("son+")
							.Symbol("syl+")
							.Symbol("cons-")
							.Symbol("high+")
							.Symbol("back+")
							.Symbol("front-")
							.Symbol("low-")
							.Symbol("rnd+").Value;
						break;
					case '+':
					case ',':
					case ' ':
					case '.':
						type = Bdry;
						fs = FeatureStruct.NewMutable(PhoneticFeatSys)
							.Feature("strRep").EqualTo(str[i].ToString(CultureInfo.InvariantCulture)).Value;
						break;
					default:
						fs = FeatureStruct.NewMutable(PhoneticFeatSys)
							.Feature("strRep").EqualTo(str[i].ToString(CultureInfo.InvariantCulture)).Value;
						break;
				}

				fs.AddValue(Type, type);
				stringData.Annotations.Add(i, i + 1, fs);
			}
			return stringData;
		}
	}
}
