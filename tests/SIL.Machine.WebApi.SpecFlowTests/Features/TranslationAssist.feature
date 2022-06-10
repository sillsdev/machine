Feature: TranslationAssist
		
	@mytag
	Scenario: Get Translation Suggestion
		Given a new SMT engine for John from es to eng
		And a new text corpora named C1 for John
		And 1JN.txt, 2JN.txt, 3JN.txt are added to corpora C1 in es and en
		When the engine is built for John
		And a translation for John is added with "hello world" for "hola mundo"
		Then the translation for John for "hola mundo" should be "hello world" 