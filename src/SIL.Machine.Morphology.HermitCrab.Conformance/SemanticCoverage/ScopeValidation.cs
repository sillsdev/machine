#nullable enable
namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

internal static class ScopeValidation
{
    public static bool HasPattern(string scope)
    {
        if (scope.Contains('*') || scope.Contains('?'))
            return true;

        bool inArrayRank = false;
        foreach (char character in scope)
        {
            if (character == '[')
            {
                if (inArrayRank)
                    return true;
                inArrayRank = true;
            }
            else if (character == ']')
            {
                if (!inArrayRank)
                    return true;
                inArrayRank = false;
            }
            else if (inArrayRank && character != ',')
            {
                return true;
            }
        }

        return inArrayRank;
    }
}
