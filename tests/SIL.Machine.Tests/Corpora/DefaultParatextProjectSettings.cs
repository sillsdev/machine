using System.Text;
using SIL.Scripture;

namespace SIL.Machine.Corpora;

public class DefaultParatextProjectSettings(
    string id = "Id",
    string name = "Test",
    string fullName = "TestProject",
    Encoding? encoding = null,
    ScrVers? versification = null,
    UsfmStylesheet? stylesheet = null,
    string fileNamePrefix = "",
    string fileNameForm = "41MAT",
    string fileNameSuffix = "Test.SFM",
    string biblicalTermsListType = "Project",
    string biblicalTermsProjectName = "Test",
    string biblicalTermsFileName = "ProjectBiblicalTerms.xml",
    string languageCode = "en",
    string translationType = "Standard",
    string normalizationForm = "Undefined",
    string language = "",
    string? parentGuid = null,
    string? parentName = null
)
    : ParatextProjectSettings(
        id,
        name,
        fullName,
        encoding ?? Encoding.UTF8,
        versification ?? ScrVers.English,
        stylesheet ?? new UsfmStylesheet("usfm.sty"),
        fileNamePrefix,
        fileNameForm,
        fileNameSuffix,
        biblicalTermsListType,
        biblicalTermsProjectName,
        biblicalTermsFileName,
        languageCode,
        translationType,
        normalizationForm,
        language,
        parentGuid,
        parentName
    );
