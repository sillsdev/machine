using System;
using System.Collections.Generic;
using System.Linq;
using ManyConsole;

namespace SIL.Machine.Morphology.HermitCrab
{
    internal class TestCommand : ConsoleCommand
    {
        private readonly HCContext _context;
        private readonly List<string> _expectedParses;

        public TestCommand(HCContext context)
        {
            _context = context;
            _expectedParses = new List<string>();

            IsCommand("test", "Test if a word parse is correct");
            SkipsCommandSummaryBeforeRunning();
            HasOption(
                "p|parse=",
                "expected parse in the format <form1>:<gloss1>|<form2>:<gloss2>|...",
                p => _expectedParses.Add(p)
            );
            HasAdditionalArguments(1, "<word>");
        }

        public override int Run(string[] remainingArguments)
        {
            string word = remainingArguments[0];
            List<MorphInfo[]> expectedParses = _expectedParses.Select(p => ReadParseString(p).ToArray()).ToList();
            try
            {
                _context.TestCount++;
                _context.Out.WriteLine("Testing \"{0}\"", word);
                var actualParses = new List<IList<MorphInfo>>();
                foreach (Word parse in _context.Morpher.ParseWord(word))
                {
                    IList<MorphInfo> morphInfos = parse.GetMorphInfos();
                    bool found = false;
                    for (int i = 0; i < expectedParses.Count; i++)
                    {
                        if (expectedParses[i].SequenceEqual(morphInfos))
                        {
                            expectedParses.RemoveAt(i);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        actualParses.Add(morphInfos);
                }

                if (expectedParses.Count > 0 || actualParses.Count > 0)
                {
                    _context.FailedTestCount++;
                    _context.Out.WriteLine("Test failed.");
                    _context.Out.WriteLine("Expected parses:");
                    if (expectedParses.Count == 0)
                    {
                        _context.Out.WriteLine("None");
                    }
                    else
                    {
                        foreach (MorphInfo[] expectedParse in expectedParses)
                            _context.Out.WriteParse(expectedParse);
                    }

                    _context.Out.WriteLine("Actual parses:");
                    if (actualParses.Count == 0)
                    {
                        _context.Out.WriteLine("None");
                    }
                    else
                    {
                        foreach (IList<MorphInfo> actualParse in actualParses)
                            _context.Out.WriteParse(actualParse);
                    }
                }
                else
                {
                    _context.PassedTestCount++;
                    _context.Out.WriteLine("Test passed.");
                }
                _context.Out.WriteLine();
                return 0;
            }
            catch (InvalidShapeException ise)
            {
                _context.ErrorTestCount++;
                _context.Out.WriteLine("The word contains an invalid segment at position {0}.", ise.Position + 1);
                _context.Out.WriteLine();
                return 1;
            }
            finally
            {
                _expectedParses.Clear();
            }
        }

        private static IEnumerable<MorphInfo> ReadParseString(string parseStr)
        {
            foreach (string morph in Split(parseStr, "|"))
            {
                string[] parts = Split(morph.Trim(), ":").ToArray();
                yield return new MorphInfo(parts[0].Trim(), parts[1].Trim());
            }
        }

        private static IEnumerable<string> Split(string parseStr, string delimiter)
        {
            int start = 0;
            do
            {
                int end = parseStr.IndexOf(delimiter, start, StringComparison.Ordinal);
                if (end == -1)
                    yield return parseStr.Substring(start);
                else if (end == 0)
                    yield return string.Empty;
                else if (parseStr[end - 1] != '\\')
                    yield return parseStr.Substring(start, end - start);
                else
                    continue;
                start = end + 1;
            } while (start > 0);
        }
    }
}
