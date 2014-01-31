using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.Collections;
using SIL.Machine.Morphology;

namespace SIL.Machine.Test.Morphology
{
	[TestFixture]
	public class PoorMansAffixIdentifierTest
	{
		[Test]
		public void IdentifySuffixes()
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
					"suck",
				};

			var affixIdentifier = new PoorMansAffixIdentifier<string, char>(w => w);
			IEnumerable<Affix<char>> suffixes = affixIdentifier.IdentifyAffixes(words, AffixType.Suffix);
			Assert.That(suffixes.Select(a => a.Ngram.ToString()), Is.EquivalentTo(new[] {"ed", "es", "s"}));
		}

		[Test]
		public void IdentifyPrefixes()
		{
			var words = new[]
				{
					"unamazing",
					"unbias",
					"uncertain",
					"unapt",
					"uneloquent",
					"unfair",
					"ungenial",
					"unangry",

					"decent",
					"expensive",
					"wealthy",
					"united",
					"archaic",
					"silly",
					"remote",
					"asleep",
					"crazy",
					"stupid"
				};

			var affixIdentifier = new PoorMansAffixIdentifier<string, char>(w => w);
			IEnumerable<Affix<char>> prefixes = affixIdentifier.IdentifyAffixes(words, AffixType.Prefix);
			Assert.That(prefixes.Select(a => a.Ngram.ToString()), Is.EquivalentTo(new[] {"un"}));
		}
	}
}
