using NUnit.Framework;

namespace SIL.Machine.Morphology
{
    [TestFixture]
    public class PoorMansStemmerTests
    {
        [Test]
        public void HaveSameStem()
        {
            var words = new[]
            {
                "calls",
                "fixes",
                "coughs",
                "begs",
                "explains",
                "jams",
                "kisses",
                "learns",
                "whips",
                "visits",
                "rushes",
                "traces",
                "attends",
                "detects",
                "extends",
                "explains",
                "forces",
                "frames",
                "cycles",
                "notices",
                "turns",
                "uses",
                "excites",
                "damages",
                "boils",
                "avoids",
                "allows",
                "jokes",
                "murders",
                "sucks",
                "called",
                "fixed",
                "coughed",
                "begged",
                "explained",
                "jammed",
                "kissed",
                "learned",
                "whipped",
                "visited",
                "rushed",
                "traced",
                "attended",
                "detected",
                "extended",
                "explained",
                "forced",
                "framed",
                "cycled",
                "noticed",
                "turned",
                "used",
                "excited",
                "damaged",
                "boiled",
                "avoided",
                "allowed",
                "joked",
                "murdered",
                "sucked",
                "call",
                "fix",
                "cough",
                "beg",
                "explain",
                "jam",
                "kiss",
                "learn",
                "whip",
                "visit",
                "rush",
                "trace",
                "attend",
                "detect",
                "extend",
                "explain",
                "force",
                "frame",
                "cycle",
                "notice",
                "turn",
                "use",
                "excite",
                "damage",
                "boil",
                "avoid",
                "allow",
                "joke",
                "murder",
                "suck"
            };

            var stemmer = new PoorMansStemmer<string, char>(s => s) { NormalizeScores = true, Threshold = 0.05 };
            stemmer.Train(words);

            Assert.That(stemmer.HaveSameStem("locked", "locks"), Is.True);

            Assert.That(stemmer.HaveSameStem("flock", "locked"), Is.False);

            Assert.That(stemmer.HaveSameStem("locked", "locker"), Is.False);

            Assert.That(stemmer.HaveSameStem("lock", "locked"), Is.True);

            Assert.That(stemmer.HaveSameStem("misses", "missed"), Is.True);

            Assert.That(stemmer.HaveSameStem("extend", "intend"), Is.False);
        }

        [Test]
        public void LargeEnglishWordList()
        {
            var words = new List<string>();
            string path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Morphology", "LEX");
            using (var file = new StreamReader(File.OpenRead(path)))
            {
                string? line;
                while ((line = file.ReadLine()) != null)
                {
                    string word = line.Substring(6);
                    if (word.All(char.IsLetter))
                        words.Add(word);
                }
            }

            var stemmer = new PoorMansStemmer<string, char>(w => w)
            {
                NormalizeScores = true,
                Threshold = 0.03,
                MaxAffixLength = 5
            };
            stemmer.Train(words);

            Assert.That(stemmer.HaveSameStem("spied", "spy"), Is.True);
            Assert.That(stemmer.HaveSameStem("station", "sting"), Is.False);
            Assert.That(stemmer.HaveSameStem("called", "calls"), Is.True);
            Assert.That(stemmer.HaveSameStem("jungle", "excited"), Is.False);
            Assert.That(stemmer.HaveSameStem("jammed", "jams"), Is.True);
            Assert.That(stemmer.HaveSameStem("fix", "fixes"), Is.True);
            Assert.That(stemmer.HaveSameStem("sting", "stern"), Is.False);
            Assert.That(stemmer.HaveSameStem("jogged", "jogging"), Is.True);
            Assert.That(stemmer.HaveSameStem("unbelieveable", "believe"), Is.True);
        }
    }
}
