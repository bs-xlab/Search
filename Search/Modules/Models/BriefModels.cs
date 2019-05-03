using Search.Modules.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Search.Modules.Models
{
    public abstract class BriefModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }

    public class ProductModel : BriefModel
    {
        public int BrandID { get; set; }
        public int GroupID { get; set; }
        public List<string> Syllables { get; set; }
        public List<List<int>> IndexesList { get; set; }
        public string FullIdentity { get; set; }
    }

    public class BrandModel : BriefModel
    {
        public bool HasRussianSymbols { get; set; }
    }

    public class GroupModel : BriefModel { }

    public class NodesSorter : IComparer<Graph.Node>
    {
        Graph.Node _pattern;

        public NodesSorter(Graph.Node pattern) => _pattern = pattern;

        public int Compare(Graph.Node first, Graph.Node second)
        {
            int index = 0;

            var countFirstPositions = 0;
            Array.ForEach(first.HashSum, h =>
            {
                if (h ^ _pattern.HashSum[index]) countFirstPositions++;
                index++;
            });

            index = 0;

            var countSecondPositions = 0;
            Array.ForEach(second.HashSum, h =>
            {
                if (h ^ _pattern.HashSum[index]) countSecondPositions++;
                index++;
            });

            if (countFirstPositions > countSecondPositions)
            {
                return 1;
            }

            return countFirstPositions == countSecondPositions ? 0 : -1;
        }
    }

    public class TitlesComparer : IComparer<string>
    {
        List<string> _patternWords;

        public TitlesComparer(string examplar)
        {
            _patternWords = new List<string>();
            var words = examplar.Split(' ');

            Array.ForEach(words, w =>
            {
                var syllables = Regex.IsMatch(w, "^[а-я0-9 ]+$")
                   ? w.GetSyllablesWithoutEndings()
                   : w.GetSeparatedStringEng();
                _patternWords.Add(string.Join("", syllables));
            });
        }

        public int Compare(string first, string second)
        {
            var firstScore = _patternWords.Count(w => first.Contains(w));
            var secondScore = _patternWords.Count(w => second.Contains(w));

            if (firstScore > secondScore)
            {
                return 1;
            }

            return firstScore == secondScore ? 0 : -1;
        }
    }
}