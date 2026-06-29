namespace SIL.Machine.Corpora;

using NUnit.Framework;

[TestFixture]
public class SegmentBoundaryAdjusterTests
{
    [Test]
    public void ProhibitedSentenceStartingCharacters()
    {
        var adjuster = new SegmentBoundaryAdjuster();

        // Second segment starts with space
        var (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary("In the beginning God created", " the heavens");
        Assert.That(segment, Is.EqualTo("In the beginning God created "));
        Assert.That(nextSegment, Is.EqualTo("the heavens"));

        // Second segment starts with comma
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary("He took the bread", ", blessed it");
        Assert.That(segment, Is.EqualTo("He took the bread, "));
        Assert.That(nextSegment, Is.EqualTo("blessed it"));

        // Second segment starts with semicolon
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary("first, apostles", "; second prophets");
        Assert.That(segment, Is.EqualTo("first, apostles; "));
        Assert.That(nextSegment, Is.EqualTo("second prophets"));

        // Second segment starts with colon
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary("He taught them, saying", ": Blessed are the");
        Assert.That(segment, Is.EqualTo("He taught them, saying: "));
        Assert.That(nextSegment, Is.EqualTo("Blessed are the"));

        // Second segment starts with period
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary("what belongs to God", ". They will only");
        Assert.That(segment, Is.EqualTo("what belongs to God. "));
        Assert.That(nextSegment, Is.EqualTo("They will only"));

        // Second segment starts with exclamation mark
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "You hypocrite",
            "! First take the beam out of your own eye"
        );
        Assert.That(segment, Is.EqualTo("You hypocrite! "));
        Assert.That(nextSegment, Is.EqualTo("First take the beam out of your own eye"));

        // Second segment starts with question mark
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "Why do you worry about clothes",
            "? See how the flowers"
        );
        Assert.That(segment, Is.EqualTo("Why do you worry about clothes? "));
        Assert.That(nextSegment, Is.EqualTo("See how the flowers"));

        // Second segment starts with closing parenthesis
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "Simon (who is called Peter",
            ") and his brother Andrew"
        );
        Assert.That(segment, Is.EqualTo("Simon (who is called Peter) "));
        Assert.That(nextSegment, Is.EqualTo("and his brother Andrew"));

        // Second segment starts with closing square bracket
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "manuscripts do not include John 7:53-8:11.",
            "] Then they all went"
        );
        Assert.That(segment, Is.EqualTo("manuscripts do not include John 7:53-8:11.] "));
        Assert.That(nextSegment, Is.EqualTo("Then they all went"));

        // Second segment starts with closing double quote
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary("\u201cLord,", "\u201d he said");
        Assert.That(segment, Is.EqualTo("\u201cLord,\u201d "));
        Assert.That(nextSegment, Is.EqualTo("he said"));

        // Second segment starts with closing single quote
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary("Your sins are forgiven,", "\u2019 or to say");
        Assert.That(segment, Is.EqualTo("Your sins are forgiven,\u2019 "));
        Assert.That(nextSegment, Is.EqualTo("or to say"));

        // Second segment starts with multiple prohibited characters in a row
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary("or to say", ", \u2018Get up and walk");
        Assert.That(segment, Is.EqualTo("or to say, "));
        Assert.That(nextSegment, Is.EqualTo("\u2018Get up and walk"));
    }

    [Test]
    public void ProhibitedSentenceEndingCharacters()
    {
        var adjuster = new SegmentBoundaryAdjuster();

        // First segment ends with opening parenthesis
        var (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "They said, \u201cRabbi\u2019 (",
            "which means \u201cTeacher\u201d)"
        );
        Assert.That(segment, Is.EqualTo("They said, \u201cRabbi\u2019 "));
        Assert.That(nextSegment, Is.EqualTo("(which means \u201cTeacher\u201d)"));

        // First segment ends with opening square bracket
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "from Galilee!\u201d [",
            "The most ancient Greek manuscripts"
        );
        Assert.That(segment, Is.EqualTo("from Galilee!\u201d "));
        Assert.That(nextSegment, Is.EqualTo("[The most ancient Greek manuscripts"));

        // First segment ends with double guillemet
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary("Il dit \u00ab", "Venez \u00e0 moi");
        Assert.That(segment, Is.EqualTo("Il dit "));
        Assert.That(nextSegment, Is.EqualTo("\u00abVenez \u00e0 moi"));

        // First segment ends with single guillemet
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "J\u00e9sus s'\u00e9cria \u2039",
            "P\u00e8re pourquoi"
        );
        Assert.That(segment, Is.EqualTo("J\u00e9sus s'\u00e9cria "));
        Assert.That(nextSegment, Is.EqualTo("\u2039P\u00e8re pourquoi"));

        // First segment ends with opening double quote
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "Knowing their thoughts, Jesus said, \u201c",
            "Why do you"
        );
        Assert.That(segment, Is.EqualTo("Knowing their thoughts, Jesus said, "));
        Assert.That(nextSegment, Is.EqualTo("\u201cWhy do you"));

        // First segment ends with opening single quote
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary("or to say, \u2018", "Get up and walk\u2019?");
        Assert.That(segment, Is.EqualTo("or to say, "));
        Assert.That(nextSegment, Is.EqualTo("\u2018Get up and walk\u2019?"));

        // First segment ends with multiple prohibited characters in a row
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "This is what the Lord says: \u201c\u2018",
            "I remember the devotion of your youth"
        );
        Assert.That(segment, Is.EqualTo("This is what the Lord says: "));
        Assert.That(nextSegment, Is.EqualTo("\u201c\u2018I remember the devotion of your youth"));
    }

    [Test]
    public void LateSentenceStarts()
    {
        var adjuster = new SegmentBoundaryAdjuster();

        // Sentence starts one word late
        var (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "In the beginning God created the heavens and the earth. And ",
            "the earth was without form"
        );
        Assert.That(segment, Is.EqualTo("In the beginning God created the heavens and the earth. "));
        Assert.That(nextSegment, Is.EqualTo("And the earth was without form"));

        // Sentence starts two words late
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "In the beginning God created the heavens and the earth. And the ",
            "earth was without form"
        );
        Assert.That(segment, Is.EqualTo("In the beginning God created the heavens and the earth. "));
        Assert.That(nextSegment, Is.EqualTo("And the earth was without form"));

        // Sentence starts three words late
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "In the beginning God created the heavens and the earth. And the earth",
            "was without form"
        );
        Assert.That(segment, Is.EqualTo("In the beginning God created the heavens and the earth. "));
        Assert.That(nextSegment, Is.EqualTo("And the earth was without form"));

        // Two-word capitalized phrase
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "are possible with God.\u201d Then Peter answered ",
            "and said to him,"
        );
        Assert.That(segment, Is.EqualTo("are possible with God.\u201d "));
        Assert.That(nextSegment, Is.EqualTo("Then Peter answered and said to him,"));

        // Doesn't apply to uncapitalized word
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "In the beginning God created the heavens and the earth. and the earth",
            "was without form"
        );
        Assert.That(segment, Is.EqualTo("In the beginning God created the heavens and the earth. and the earth"));
        Assert.That(nextSegment, Is.EqualTo("was without form"));
    }

    [Test]
    public void EarlySentenceEnds()
    {
        var adjuster = new SegmentBoundaryAdjuster();

        // Sentence ends one word early
        var (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "And the earth was without form and ",
            "void. And darkness"
        );
        Assert.That(segment, Is.EqualTo("And the earth was without form and void. "));
        Assert.That(nextSegment, Is.EqualTo("And darkness"));

        // Sentence ends two words early
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "And the earth was without form ",
            "and void. And darkness"
        );
        Assert.That(segment, Is.EqualTo("And the earth was without form and void. "));
        Assert.That(nextSegment, Is.EqualTo("And darkness"));

        // Sentence ends three words early
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "And the earth was without ",
            "form and void. And darkness"
        );
        Assert.That(segment, Is.EqualTo("And the earth was without form and void. "));
        Assert.That(nextSegment, Is.EqualTo("And darkness"));

        // Early sentence end with comma
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "They have ",
            "forsaken me, the source of living water."
        );
        Assert.That(segment, Is.EqualTo("They have forsaken me, "));
        Assert.That(nextSegment, Is.EqualTo("the source of living water."));

        // Early sentence end with semicolon
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "Your wickedness will ",
            "punish you; your backsliding will rebuke you"
        );
        Assert.That(segment, Is.EqualTo("Your wickedness will punish you; "));
        Assert.That(nextSegment, Is.EqualTo("your backsliding will rebuke you"));

        // Early sentence end with exclamation
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "look at ",
            "the fields! They are ripe for harvest."
        );
        Assert.That(segment, Is.EqualTo("look at the fields! "));
        Assert.That(nextSegment, Is.EqualTo("They are ripe for harvest."));

        // Early sentence end with question mark
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "Where can you get this ",
            "living water? Are you greater than our father Jacob?"
        );
        Assert.That(segment, Is.EqualTo("Where can you get this living water? "));
        Assert.That(nextSegment, Is.EqualTo("Are you greater than our father Jacob?"));

        // Early sentence end with closing parenthesis
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "This was before John was put ",
            "in prison) An argument developed"
        );
        Assert.That(segment, Is.EqualTo("This was before John was put in prison) "));
        Assert.That(nextSegment, Is.EqualTo("An argument developed"));

        // Early sentence end with closing square bracket
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "manuscripts do not include ",
            "this passage] Then they all went"
        );
        Assert.That(segment, Is.EqualTo("manuscripts do not include this passage] "));
        Assert.That(nextSegment, Is.EqualTo("Then they all went"));

        // Early sentence end with closing double quote
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "your testimony is ",
            "not valid\u201d Jesus answered,"
        );
        Assert.That(segment, Is.EqualTo("your testimony is not valid\u201d "));
        Assert.That(nextSegment, Is.EqualTo("Jesus answered,"));

        // Early sentence end with closing single quote
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "bread from heaven ",
            "to eat.\u2019 Jesus said to them,"
        );
        Assert.That(segment, Is.EqualTo("bread from heaven to eat.\u2019 "));
        Assert.That(nextSegment, Is.EqualTo("Jesus said to them,"));

        // Early sentence end with multiple closing quotes
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "\u2018Make straight the way for ",
            "the Lord.\u2019\u201d Now the Pharisees"
        );
        Assert.That(segment, Is.EqualTo("\u2018Make straight the way for the Lord.\u2019\u201d "));
        Assert.That(nextSegment, Is.EqualTo("Now the Pharisees"));
    }

    [Test]
    public void MultipleAdjustments()
    {
        var adjuster = new SegmentBoundaryAdjuster();

        // Second segment starts with a comma and the sentence ends late
        var (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "He took the bread",
            ", and blessed it. Then he gave it to them"
        );
        Assert.That(segment, Is.EqualTo("He took the bread, and blessed it. "));
        Assert.That(nextSegment, Is.EqualTo("Then he gave it to them"));

        // Second segment starts with a comma and the sentence ends early
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "When you are persecuted in one place",
            ", flee to another. Truly I tell you"
        );
        Assert.That(segment, Is.EqualTo("When you are persecuted in one place, flee to another. "));
        Assert.That(nextSegment, Is.EqualTo("Truly I tell you"));

        // First segment ends with an opening double quote and the sentence starts late
        (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "Jesus knew their thoughts. He said \u201c",
            "Why do you entertain evil thoughts in your hearts?"
        );
        Assert.That(segment, Is.EqualTo("Jesus knew their thoughts. "));
        Assert.That(nextSegment, Is.EqualTo("He said \u201cWhy do you entertain evil thoughts in your hearts?"));
    }

    [Test]
    public void MultipleSegments()
    {
        var adjuster = new SegmentBoundaryAdjuster();

        var verses = new List<string>
        {
            "Jesus said, \u201c",
            "Come unto me all who are weary",
            "; I will give you rest",
        };
        var adjustedVerses = adjuster.AdjustSegmentBoundaries(verses);

        Assert.That(
            adjustedVerses,
            Is.EqualTo(
                new List<string> { "Jesus said, ", "\u201cCome unto me all who are weary; ", "I will give you rest" }
            )
        );
    }

    [Test]
    public void TokenizedSegments()
    {
        var adjuster = new SegmentBoundaryAdjuster();

        // Second segment starts with comma
        int adjustedBoundary = adjuster.AdjustTokenizedSegmentPairBoundaries(
            4,
            new[] { "He", "took", "the", "bread", ",", "blessed", "it" }
        );
        Assert.That(adjustedBoundary, Is.EqualTo(5));

        // Second segment starts with multiple prohibited characters in a row
        adjustedBoundary = adjuster.AdjustTokenizedSegmentPairBoundaries(
            4,
            new[] { "I", "have", "no", "husband", ",", "\u201d", "she", "replied" }
        );
        Assert.That(adjustedBoundary, Is.EqualTo(6));

        // First segment ends with opening double quote
        adjustedBoundary = adjuster.AdjustTokenizedSegmentPairBoundaries(
            8,
            new[] { "Knowing", "their", "thoughts", ",", "Jesus", "said", ",", "\u201c", "Why", "do", "you" }
        );
        Assert.That(adjustedBoundary, Is.EqualTo(7));

        // First segment ends with multiple prohibited characters in a row
        adjustedBoundary = adjuster.AdjustTokenizedSegmentPairBoundaries(
            9,
            new[]
            {
                "This",
                "is",
                "what",
                "the",
                "Lord",
                "says",
                ":",
                "\u201c",
                "\u2018",
                "I",
                "remember",
                "the",
                "devotion",
                "of",
                "your",
                "youth",
            }
        );
        Assert.That(adjustedBoundary, Is.EqualTo(7));

        // Sentence starts three words late
        adjustedBoundary = adjuster.AdjustTokenizedSegmentPairBoundaries(
            14,
            new[]
            {
                "In",
                "the",
                "beginning",
                "God",
                "created",
                "the",
                "heavens",
                "and",
                "the",
                "earth",
                ".",
                "And",
                "the",
                "earth",
                "was",
                "without",
                "form",
            }
        );
        Assert.That(adjustedBoundary, Is.EqualTo(11));

        // Early sentence end with question mark
        adjustedBoundary = adjuster.AdjustTokenizedSegmentPairBoundaries(
            6,
            new[]
            {
                "Where",
                "can",
                "you",
                "get",
                "this",
                ",",
                "living",
                "water",
                "?",
                "Are",
                "you",
                "greater",
                "than",
                "our",
                "father",
                "Jacob",
                "?",
            }
        );
        Assert.That(adjustedBoundary, Is.EqualTo(9));

        // Early sentence end with multiple closing quotes
        adjustedBoundary = adjuster.AdjustTokenizedSegmentPairBoundaries(
            6,
            new[]
            {
                "\u2018",
                "Make",
                "straight",
                "the",
                "way",
                "for",
                "the",
                "Lord",
                ".",
                "\u2019",
                "\u201d",
                "Now",
                "the",
                "Pharisees",
            }
        );
        Assert.That(adjustedBoundary, Is.EqualTo(11));
    }

    [Test]
    public void NoAdjustmentForCorrectBoundaries()
    {
        var adjuster = new SegmentBoundaryAdjuster();

        var (segment, nextSegment) = adjuster.AdjustSegmentPairBoundary(
            "In the beginning God created the heavens and the earth.",
            "And the earth was without form"
        );
        Assert.That(segment, Is.EqualTo("In the beginning God created the heavens and the earth."));
        Assert.That(nextSegment, Is.EqualTo("And the earth was without form"));

        var adjustedSegments = adjuster.AdjustSegmentBoundaries(
            new List<string>
            {
                "\u201cWhy do you entertain evil thoughts in your hearts?",
                "Which is easier: to say, ",
                "\u2018Your sins are forgiven,\u2019",
                "or to say",
                "\u2018Get up and walk\u2019?",
            }
        );
        Assert.That(
            adjustedSegments,
            Is.EqualTo(
                new List<string>
                {
                    "\u201cWhy do you entertain evil thoughts in your hearts?",
                    "Which is easier: to say, ",
                    "\u2018Your sins are forgiven,\u2019",
                    "or to say",
                    "\u2018Get up and walk\u2019?",
                }
            )
        );

        int adjustedBoundary = adjuster.AdjustTokenizedSegmentPairBoundaries(
            8,
            new[]
            {
                "But",
                "go",
                "and",
                "learn",
                "what",
                "this",
                "means",
                ":",
                "\u2018",
                "I",
                "desire",
                "mercy",
                "not",
                "sacrifice",
                "\u2019",
            }
        );
        Assert.That(adjustedBoundary, Is.EqualTo(8));
    }

    [Test]
    public void JoinTokens()
    {
        Assert.That(
            TokenRejoiner.JoinTokens(new[] { "He", "took", "the", "bread", ",", "blessed", "it" }),
            Is.EqualTo("He took the bread, blessed it ")
        );

        Assert.That(
            TokenRejoiner.JoinTokens(
                new[] { "Knowing", "their", "thoughts", ",", "Jesus", "said", ",", "\u201c", "Why", "do", "you" }
            ),
            Is.EqualTo("Knowing their thoughts, Jesus said, \u201cWhy do you ")
        );

        Assert.That(
            TokenRejoiner.JoinTokens(new[] { "Freely", "you", "have", "received", ";", "freely", "give", "." }),
            Is.EqualTo("Freely you have received; freely give. ")
        );

        Assert.That(
            TokenRejoiner.JoinTokens(new[] { "Il", "dit", "\u00ab", "Venez", "\u00bb", ".", "Maintenant" }),
            Is.EqualTo("Il dit \u00abVenez\u00bb. Maintenant ")
        );

        Assert.That(
            TokenRejoiner.JoinTokens(new[] { "Il", "dit", "<<", "Venez", ">>", ".", "Maintenant" }),
            Is.EqualTo("Il dit <<Venez>>. Maintenant ")
        );

        Assert.That(
            TokenRejoiner.JoinTokens(
                new[] { "J\u00e9sus", "s'\u00e9cria", "\u2039", "P\u00e8re", "!", "\u203a", ",", "puis", "parti t" }
            ),
            Is.EqualTo("J\u00e9sus s'\u00e9cria \u2039P\u00e8re!\u203a, puis parti t ")
        );
    }

    [Test]
    public void AddTokenToJoinedText()
    {
        var rejoiner = new TokenRejoiner();

        Assert.That(rejoiner.AddTokenToJoinedText("Knowing"), Is.EqualTo("Knowing"));
        Assert.That(rejoiner.AddTokenToJoinedText("their"), Is.EqualTo("Knowing their"));
        Assert.That(rejoiner.AddTokenToJoinedText("thoughts"), Is.EqualTo("Knowing their thoughts"));
        Assert.That(rejoiner.AddTokenToJoinedText(","), Is.EqualTo("Knowing their thoughts,"));
        Assert.That(rejoiner.AddTokenToJoinedText("Jesus"), Is.EqualTo("Knowing their thoughts, Jesus"));
        Assert.That(rejoiner.AddTokenToJoinedText("said"), Is.EqualTo("Knowing their thoughts, Jesus said"));
        Assert.That(rejoiner.AddTokenToJoinedText(","), Is.EqualTo("Knowing their thoughts, Jesus said,"));
        Assert.That(rejoiner.AddTokenToJoinedText("\u201c"), Is.EqualTo("Knowing their thoughts, Jesus said, \u201c"));
        Assert.That(rejoiner.AddTokenToJoinedText("Why"), Is.EqualTo("Knowing their thoughts, Jesus said, \u201cWhy"));
        Assert.That(
            rejoiner.AddTokenToJoinedText("do"),
            Is.EqualTo("Knowing their thoughts, Jesus said, \u201cWhy do")
        );
        Assert.That(
            rejoiner.AddTokenToJoinedText("you"),
            Is.EqualTo("Knowing their thoughts, Jesus said, \u201cWhy do you")
        );
    }
}
