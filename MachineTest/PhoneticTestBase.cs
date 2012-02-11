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
				.SymbolicFeature("cons", cons => cons
					.Symbol("cons+", "+")
					.Symbol("cons-", "-"))
				.SymbolicFeature("voice", voice => voice
					.Symbol("voice+", "+")
					.Symbol("voice-", "-"))
				.SymbolicFeature("sib", sib => sib
					.Symbol("sib+", "+")
					.Symbol("sib-", "-"))
				.SymbolicFeature("cor", cor => cor
					.Symbol("cor+", "+")
					.Symbol("cor-", "-"))
				.SymbolicFeature("lab", lab => lab
					.Symbol("lab+", "+")
					.Symbol("lab-", "-"))
				.SymbolicFeature("low", low => low
					.Symbol("low+", "+")
					.Symbol("low-", "-"))
				.SymbolicFeature("mid", mid => mid
					.Symbol("mid+", "+")
					.Symbol("mid-", "-"))
				.StringFeature("str").Value;

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
					case 'f':
						fs = FeatureStruct.New(PhoneticFeatSys)
							.Symbol("cons+")
							.Symbol("voice-")
							.Symbol("sib-")
							.Symbol("cor-")
							.Symbol("lab+")
							.Symbol("low-")
							.Symbol("mid-").Value;
						break;
					case 'k':
						fs = FeatureStruct.New(PhoneticFeatSys)
							.Symbol("cons+")
							.Symbol("voice-")
							.Symbol("sib-")
							.Symbol("cor-")
							.Symbol("lab-")
							.Symbol("low-")
							.Symbol("mid-").Value;
						break;
					case 'z':
						fs = FeatureStruct.New(PhoneticFeatSys)
							.Symbol("cons+")
							.Symbol("voice+")
							.Symbol("sib+")
							.Symbol("cor+")
							.Symbol("lab-")
							.Symbol("low-")
							.Symbol("mid-").Value;
						break;
					case 's':
						fs = FeatureStruct.New(PhoneticFeatSys)
							.Symbol("cons+")
							.Symbol("voice-")
							.Symbol("sib+")
							.Symbol("cor+")
							.Symbol("lab-")
							.Symbol("low-")
							.Symbol("mid-").Value;
						break;
					case 'a':
						fs = FeatureStruct.New(PhoneticFeatSys)
							.Symbol("cons-")
							.Symbol("voice+")
							.Symbol("sib-")
							.Symbol("cor-")
							.Symbol("lab-")
							.Symbol("low+")
							.Symbol("mid-").Value;
						break;
					case 'i':
						fs = FeatureStruct.New(PhoneticFeatSys)
							.Symbol("cons-")
							.Symbol("voice+")
							.Symbol("sib-")
							.Symbol("cor-")
							.Symbol("lab-")
							.Symbol("low-")
							.Symbol("mid-").Value;
						break;
					case 'e':
						fs = FeatureStruct.New(PhoneticFeatSys)
							.Symbol("cons-")
							.Symbol("voice+")
							.Symbol("sib-")
							.Symbol("cor-")
							.Symbol("lab-")
							.Symbol("low-")
							.Symbol("mid+").Value;
						break;
					case '+':
					case ',':
					case ' ':
					case '.':
						type = Bdry;
						fs = FeatureStruct.New(PhoneticFeatSys)
							.Feature("str").EqualTo(str[i].ToString(CultureInfo.InvariantCulture)).Value;
						break;
					default:
						fs = FeatureStruct.New(PhoneticFeatSys)
							.Feature("str").EqualTo(str[i].ToString(CultureInfo.InvariantCulture)).Value;
						break;
				}

				fs.AddValue(Type, type);
				stringData.Annotations.Add(i, i + 1, fs);
			}
			return stringData;
		}
	}
}
