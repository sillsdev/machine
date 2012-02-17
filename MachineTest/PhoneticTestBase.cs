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

			PhoneticFeatSys = FeatureSystem.New()
				.SymbolicFeature("son", son => son
					.Symbol("son+", "+")
					.Symbol("son-", "-")
					.Symbol("son?", "?").Default)
				.SymbolicFeature("syl", syl => syl
					.Symbol("syl+", "+")
					.Symbol("syl-", "-")
					.Symbol("syl?", "?").Default)
				.SymbolicFeature("cons", cons => cons
					.Symbol("cons+", "+")
					.Symbol("cons-", "-")
					.Symbol("cons?", "?").Default)
				.SymbolicFeature("high", high => high
					.Symbol("high+", "+")
					.Symbol("high-", "-")
					.Symbol("high?", "?").Default)
				.SymbolicFeature("back", back => back
					.Symbol("back+", "+")
					.Symbol("back-", "-")
					.Symbol("back?", "?").Default)
				.SymbolicFeature("front", front => front
					.Symbol("front+", "+")
					.Symbol("front-", "-")
					.Symbol("front?", "?").Default)
				.SymbolicFeature("low", low => low
					.Symbol("low+", "+")
					.Symbol("low-", "-")
					.Symbol("low?", "?").Default)
				.SymbolicFeature("rnd", rnd => rnd
					.Symbol("rnd+", "+")
					.Symbol("rnd-", "-")
					.Symbol("rnd?", "?").Default)
				.SymbolicFeature("ant", ant => ant
					.Symbol("ant+", "+")
					.Symbol("ant-", "-")
					.Symbol("ant?", "?").Default)
				.SymbolicFeature("cor", cor => cor
					.Symbol("cor+", "+")
					.Symbol("cor-", "-")
					.Symbol("cor?", "?").Default)
				.SymbolicFeature("voice", voice => voice
					.Symbol("voice+", "+")
					.Symbol("voice-", "-")
					.Symbol("voice?", "?").Default)
				.SymbolicFeature("cont", cont => cont
					.Symbol("cont+", "+")
					.Symbol("cont-", "-")
					.Symbol("cont?", "?").Default)
				.SymbolicFeature("nas", nas => nas
					.Symbol("nas+", "+")
					.Symbol("nas-", "-")
					.Symbol("nas?", "?").Default)
				.SymbolicFeature("str", str => str
					.Symbol("str+", "+")
					.Symbol("str-", "-")
					.Symbol("str?", "?").Default)
				.StringFeature("strRep").Value;

			WordFeatSys = FeatureSystem.New()
				.SymbolicFeature("POS", pos => pos
					.Symbol("noun")
					.Symbol("verb")
					.Symbol("adj")
					.Symbol("adv")
					.Symbol("det")).Value;

			Type = new SymbolicFeature("Type");
			Word = new FeatureSymbol("Word");
			Type.AddPossibleSymbol(Word);
			NP = new FeatureSymbol("NP");
			Type.AddPossibleSymbol(NP);
			VP = new FeatureSymbol("VP");
			Type.AddPossibleSymbol(VP);
			Seg = new FeatureSymbol("Seg");
			Type.AddPossibleSymbol(Seg);
			Bdry = new FeatureSymbol("Bdry");
			Type.AddPossibleSymbol(Bdry);

			TypeFeatSys = new FeatureSystem();
			TypeFeatSys.AddFeature(Type);
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
						fs = FeatureStruct.New(PhoneticFeatSys)
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
						fs = FeatureStruct.New(PhoneticFeatSys)
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
						fs = FeatureStruct.New(PhoneticFeatSys)
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
						fs = FeatureStruct.New(PhoneticFeatSys)
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
						fs = FeatureStruct.New(PhoneticFeatSys)
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
						fs = FeatureStruct.New(PhoneticFeatSys)
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
						fs = FeatureStruct.New(PhoneticFeatSys)
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
						fs = FeatureStruct.New(PhoneticFeatSys)
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
						fs = FeatureStruct.New(PhoneticFeatSys)
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
						fs = FeatureStruct.New(PhoneticFeatSys)
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
						fs = FeatureStruct.New(PhoneticFeatSys)
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
						fs = FeatureStruct.New(PhoneticFeatSys)
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
						fs = FeatureStruct.New(PhoneticFeatSys)
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
						fs = FeatureStruct.New(PhoneticFeatSys)
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
						fs = FeatureStruct.New(PhoneticFeatSys)
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
						fs = FeatureStruct.New(PhoneticFeatSys)
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
						fs = FeatureStruct.New(PhoneticFeatSys)
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
						fs = FeatureStruct.New(PhoneticFeatSys)
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
						fs = FeatureStruct.New(PhoneticFeatSys)
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
						fs = FeatureStruct.New(PhoneticFeatSys)
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
						fs = FeatureStruct.New(PhoneticFeatSys)
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
						fs = FeatureStruct.New(PhoneticFeatSys)
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
						fs = FeatureStruct.New(PhoneticFeatSys)
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
						fs = FeatureStruct.New(PhoneticFeatSys)
							.Feature("strRep").EqualTo(str[i].ToString(CultureInfo.InvariantCulture)).Value;
						break;
					default:
						fs = FeatureStruct.New(PhoneticFeatSys)
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
