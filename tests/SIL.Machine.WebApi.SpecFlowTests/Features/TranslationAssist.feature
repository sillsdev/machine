Feature: TranslationAssist
		
	@mytag
	Scenario: Get Translation Suggestion
		Given a new SMT engine for John from es to eng
		And KJV-TXT corpora for John in eng
		And VBL-TXT corpora for John in es 
		When the engine is built for John
		And a translation for John is added with "hello world" for "hola mundo"
		Then the translation for John for "hola mundo" should be "hello world" 