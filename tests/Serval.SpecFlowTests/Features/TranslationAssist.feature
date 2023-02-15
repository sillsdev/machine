@Integration
Feature: TranslationAssist
		
	Scenario: Get Translation Suggestion
		Given a new SMT engine for John from es to en
		When a new text corpora named C1 for John
		And 1JN.txt, 2JN.txt, 3JN.txt are added to corpora C1 in es and en
		And the engine is built for John
		Then the translation for John for "Espíritu" should be "Spirit"

	Scenario: Get Translation Suggestion from whole Bible
		Given a new SMT engine for John from es to en
		When a new text corpora named C1 for John
		And bible.txt are added to corpora C1 in es and en
		And the engine is built for John
		Then the translation for John for "Espíritu" should be "spirit"

	Scenario: Add training segment
		Given a new SMT engine for John from es to en
		When a new text corpora named C1 for John
		And 1JN.txt, 2JN.txt, 3JN.txt are added to corpora C1 in es and en
		And the engine is built for John
		And the translation for John for "ungidos mundo" is "ungidos world"
		And a translation for John is added with "unction world" for "ungidos mundo"
		Then the translation for John for "ungidos mundo" should be "unction world"

	Scenario: Add More Corpus
		Given a new SMT engine for John from es to en
		When a new text corpora named C1 for John
		And 3JN.txt are added to corpora C1 in es and en
		And the engine is built for John
		And the translation for John for "verdad mundo" is "truth mundo"
		And 1JN.txt, 2JN.txt are added to corpora C1 in es and en
		And the engine is built for John
		Then the translation for John for "verdad mundo" should be "truth world"
