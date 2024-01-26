namespace SIL.Machine.AspNetCore.Services;

[TestFixture]
public class BiblicalRangeStringParserTests {

    [Test]
    [TestCaseSource(nameof(GetCases))]
    public void TestParse(string rangeString, Dictionary<string, List<int>> expectedOutput, bool throwsException){
        var parser = new BiblicalRangeStringParser();
        if(!throwsException){
            Assert.That(parser.Parse(rangeString), Is.EquivalentTo(expectedOutput));
        }
        else {
            Assert.Throws<ArgumentException>(() => {
                parser.Parse(rangeString);
            });
        }
    }

    public static IEnumerable<TestCaseData> GetCases(){
        yield return new TestCaseData("MAL", new Dictionary<string, List<int>>{ {"MAL" , new List<int>()}}, false);
        yield return new TestCaseData("GEN,EXO", new Dictionary<string, List<int>>{ {"GEN" , new List<int>()},{"EXO" , new List<int>()} }, false);
        yield return new TestCaseData("1JN,2JN", new Dictionary<string, List<int>>{ {"1JN" , new List<int>()},{"2JN" , new List<int>()} }, false);
        yield return new TestCaseData("OT", Enumerable.Range(1, 39).Select(i => (Canon.BookNumberToId(i), new List<int>())).ToDictionary(), false);
        yield return new TestCaseData("NT", Enumerable.Range(40, 27).Select(i => (Canon.BookNumberToId(i), new List<int>())).ToDictionary(), false);
        yield return new TestCaseData("NT,OT", Enumerable.Range(1, 66).Select(i => (Canon.BookNumberToId(i), new List<int>())).ToDictionary(), false);
        yield return new TestCaseData("MAT;MRK", new Dictionary<string, List<int>>{ {"MAT" , new List<int>()},{"MRK" , new List<int>()} }, false);
        yield return new TestCaseData("MAT; MRK", new Dictionary<string, List<int>>{ {"MAT" , new List<int>()},{"MRK" , new List<int>()} }, false);
        yield return new TestCaseData("MAT1,2,3", new Dictionary<string, List<int>>{ {"MAT" , new List<int>(){1,2,3}} }, false);
        yield return new TestCaseData("MAT1, 2, 3", new Dictionary<string, List<int>>{ {"MAT" , new List<int>(){1,2,3}} }, false);
        yield return new TestCaseData("MAT-LUK", new Dictionary<string, List<int>>{ {"MAT" , new List<int>()},{"MRK" , new List<int>()},{"LUK" , new List<int>()} }, false);
        yield return new TestCaseData("MAT1,2,3;MAT-LUK", new Dictionary<string, List<int>>{ {"MAT" , new List<int>()},{"MRK" , new List<int>()},{"LUK" , new List<int>()} }, false);
        yield return new TestCaseData("2JN-3JN;EXO1,8,3-5;GEN", new Dictionary<string, List<int>>{ {"GEN" , new List<int>()},{"EXO" , new List<int>(){1,3,4,5,8}},{"2JN" , new List<int>()},{"3JN" , new List<int>()} }, false);
        yield return new TestCaseData("1JN 1;1JN 2;1JN 3-5", new Dictionary<string, List<int>>{ {"1JN" , new List<int>()}}, false);
        yield return new TestCaseData("MAT-ROM;-ACT4-28", new Dictionary<string, List<int>>{ {"MAT" , new List<int>()},{"MRK" , new List<int>()},{"LUK" , new List<int>()},{"JHN" , new List<int>()},{"ACT" , new List<int>(){1,2,3}},{"ROM" , new List<int>()} }, false);
        yield return new TestCaseData("2JN;-2JN 1", new Dictionary<string, List<int>>{}, false);
        yield return new TestCaseData("NT;OT;-MRK;-EXO", Enumerable.Range(1, 66).Where(i => i != 2 && i!= 41).Select(i => (Canon.BookNumberToId(i), new List<int>())).ToDictionary(), false);
        yield return new TestCaseData("NT;-MAT3-5,17;-REV21,22", Enumerable.Range(40, 27).Select(i => {
            if (i == 40){
                return (Canon.BookNumberToId(i), Enumerable.Range(1,28).Where(c => !(c == 3 || c == 4 || c == 5 || c== 17)).ToList());
            }
            if (i == 66){
                return (Canon.BookNumberToId(i), Enumerable.Range(1,20).ToList());
            }
            return (Canon.BookNumberToId(i), new List<int>());
            }).ToDictionary(), false);
        yield return new TestCaseData("MAT-JHN;-MAT-LUK", new Dictionary<string, List<int>>{ {"JHN" , new List<int>()} }, false);


        //*Throw exceptions
        yield return new TestCaseData("MAT3-1", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("MRK-MAT", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("MRK;-MRK10-3", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("MAT0-10", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("MAT-FLUM", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("-MAT-FLUM", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("ABC", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("MAT-ABC", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("NT;-ABC-LUK", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("MAT 500", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("MAT 1-500", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("MAT;-MAT 300-500", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("-MRK", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("-MRK 1", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("MRK 2-5;-MRK 1-4", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("MRK 2-5;-MRK 6", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("OT;-MRK-LUK", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("NT;OT;-ABC", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("MAT;-ABC 1", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("NT,OT,-MRK,-EXO", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("OT,MAT1", new Dictionary<string, List<int>>(), true);
        yield return new TestCaseData("OT,MAT-LUK", new Dictionary<string, List<int>>(), true);


    }

}