using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SIL.Extensions;
using SIL.Scripture;

public class ScriptureRangeParser
{
    private readonly Dictionary<string, int> _bookLengths = new Dictionary<string, int>();
    private static readonly Regex CommaSeparatedBooks = new Regex(
        @"^([A-Z\d]{3}|OT|NT)(, ?([A-Z\d]{3}|OT|NT))*$",
        RegexOptions.Compiled
    );
    private static readonly Regex BookRange = new Regex(@"^-?[A-Z\d]{3}-[A-Z\d]{3}$", RegexOptions.Compiled);
    private static readonly Regex ChapterSelection = new Regex(
        @"^-?[A-Z\d]{3} ?(\d+|\d+-\d+)(, ?(\d+|\d+-\d+))*$",
        RegexOptions.Compiled
    );

    public ScriptureRangeParser(ScrVers versification = null)
    {
        if (versification == null)
            versification = ScrVers.Original;
        foreach ((string bookId, int bookNum) in Canon.AllBookIds.Zip(Canon.AllBookNumbers))
        {
            _bookLengths[bookId] = versification.GetLastChapter(bookNum);
        }
    }

    private Dictionary<string, List<int>> ParseSection(string section)
    {
        section = section.Trim();
        Dictionary<string, List<int>> chaptersPerBook = new Dictionary<string, List<int>>();

        //*Specific chapters from one book*
        if (char.IsDigit(section.Last()))
        {
            string bookName = section.Substring(0, 3);
            if (!_bookLengths.ContainsKey(bookName))
            {
                throw new ArgumentException($"{bookName} is an invalid book ID.");
            }

            HashSet<int> chapters = new HashSet<int>();

            int lastChapter = _bookLengths[bookName];
            string[] chapterRangeStrings = section.Substring(3).Split(',');
            foreach (string chapterRangeString in chapterRangeStrings.Select(s => s.Trim()))
            {
                if (chapterRangeString.Contains('-'))
                {
                    string[] startAndEnd = chapterRangeString.Split('-');
                    int start,
                        end;
                    if (!(int.TryParse(startAndEnd[0], out start) && int.TryParse(startAndEnd[1], out end)))
                    {
                        throw new ArgumentException($"{chapterRangeString} is an invalid chapter range.");
                    }
                    if (start == 0 || end > lastChapter || end <= start)
                    {
                        throw new ArgumentException($"{chapterRangeString} is an invalid chapter range.");
                    }
                    for (int chapterNum = start; chapterNum <= end; chapterNum++)
                    {
                        chapters.Add(chapterNum);
                    }
                }
                else
                {
                    int chapterNum;
                    if (!int.TryParse(chapterRangeString, out chapterNum))
                    {
                        throw new ArgumentException($"{section} is an invalid chapter number.");
                    }
                    if (chapterNum > lastChapter)
                    {
                        throw new ArgumentException($"{section} is an invalid chapter number.");
                    }
                    chapters.Add(chapterNum);
                }
            }
            if (chapters.Count() == lastChapter)
            {
                chaptersPerBook[bookName] = new List<int>();
            }
            else
            {
                chaptersPerBook[bookName] = chapters.ToList();
                chaptersPerBook[bookName].Sort();
            }
        }
        //*Ranges of books to be added*
        else if (section.Contains('-'))
        {
            string[] startAndEnd = section.Split('-');
            if (
                startAndEnd.Length != 2
                || !_bookLengths.ContainsKey(startAndEnd[0])
                || !_bookLengths.ContainsKey(startAndEnd[1])
                || Canon.BookIdToNumber(startAndEnd[1]) <= Canon.BookIdToNumber(startAndEnd[0])
            )
            {
                throw new ArgumentException($"{section} is an invalid book range.");
            }
            for (
                int bookNum = Canon.BookIdToNumber(startAndEnd[0]);
                bookNum <= Canon.BookIdToNumber(startAndEnd[1]);
                bookNum++
            )
            {
                chaptersPerBook[Canon.BookNumberToId(bookNum)] = new List<int>();
            }
        }
        //*OT*
        else if (section == "OT")
        {
            for (int bookNum = 1; bookNum <= 39; bookNum++)
            {
                chaptersPerBook[Canon.BookNumberToId(bookNum)] = new List<int>();
            }
        }
        //*NT*
        else if (section == "NT")
        {
            for (int bookNum = 40; bookNum <= 66; bookNum++)
            {
                chaptersPerBook[Canon.BookNumberToId(bookNum)] = new List<int>();
            }
        }
        //*Whole book*
        else
        {
            if (!_bookLengths.ContainsKey(section))
            {
                throw new ArgumentException($"{section} is an invalid book ID.");
            }
            chaptersPerBook[section] = new List<int>();
        }

        return chaptersPerBook;
    }

    public Dictionary<string, List<int>> GetChapters(string chapterSelections)
    {
        Dictionary<string, List<int>> chaptersPerBook = new Dictionary<string, List<int>>();
        chapterSelections = chapterSelections.Trim();

        char delimiter = ';';
        if (chapterSelections.Contains(';'))
        {
            delimiter = ';';
        }
        else if (CommaSeparatedBooks.IsMatch(chapterSelections))
        {
            delimiter = ',';
        }
        else if (!BookRange.IsMatch(chapterSelections) && !ChapterSelection.IsMatch(chapterSelections))
        {
            throw new ArgumentException(
                "Invalid syntax. If you are providing multiple selections, e.g. a range of books followed by a selection of chapters from a book, separate each selection with a semicolon."
            );
        }
        string[] selections = chapterSelections.Split(delimiter);
        foreach (string section in selections.Select(s => s.Trim()))
        {
            //*Subtraction*
            if (section.StartsWith("-"))
            {
                Dictionary<string, List<int>> sectionChapters = ParseSection(section.Substring(1));
                foreach (string bookName in sectionChapters.Keys)
                {
                    if (!chaptersPerBook.ContainsKey(bookName))
                    {
                        throw new ArgumentException(
                            $"{bookName} cannot be removed as it is not in the existing book selection."
                        );
                    }

                    if (sectionChapters[bookName].Count() == 0)
                    {
                        sectionChapters[bookName] = Enumerable.Range(1, _bookLengths[bookName]).ToList();
                    }

                    if (chaptersPerBook[bookName].Count() == 0)
                    {
                        chaptersPerBook[bookName] = Enumerable.Range(1, _bookLengths[bookName]).ToList();
                    }

                    foreach (int chapterNumber in sectionChapters[bookName])
                    {
                        if (!chaptersPerBook[bookName].Remove(chapterNumber))
                        {
                            throw new ArgumentException(
                                $"{chapterNumber} cannot be removed as it is not in the existing chapter selection."
                            );
                        }
                    }

                    if (chaptersPerBook[bookName].Count() == 0)
                    {
                        chaptersPerBook.Remove(bookName);
                    }
                }
            }
            //*Addition*
            else
            {
                Dictionary<string, List<int>> sectionChapters = ParseSection(section);
                foreach (string bookName in sectionChapters.Keys)
                {
                    if (chaptersPerBook.ContainsKey(bookName))
                    {
                        if (chaptersPerBook[bookName].Count() == 0 || sectionChapters[bookName].Count() == 0)
                        {
                            chaptersPerBook[bookName] = new List<int>();
                            continue;
                        }
                        chaptersPerBook[bookName] = chaptersPerBook[bookName]
                            .Concat(sectionChapters[bookName])
                            .Distinct()
                            .ToList();
                        chaptersPerBook[bookName].Sort();
                        if (chaptersPerBook[bookName].Count() == _bookLengths[bookName])
                        {
                            chaptersPerBook[bookName] = new List<int>();
                        }
                    }
                    else
                    {
                        chaptersPerBook[bookName] = sectionChapters[bookName];
                    }
                }
            }
        }
        return chaptersPerBook;
    }
}
