﻿using Search.Modules.Helpers;
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
                return -1;
            }

            return firstScore == secondScore ? 0 : 1;
        }
    }

    public class DistanceComparer : IComparer<ProductModel>
    {
        string _input;

        public DistanceComparer(string input) => _input = input;

        public int Compare(ProductModel first, ProductModel second)
        {
            var firstWords = first.Name.Split(' ');
            var secondWords = second.Name.Split(' ');
            var inputWords = _input.Split(' ');

            //if (first.Name == second.Name)
            //{
            //    return 0;
            //}

            //firstWords = firstWords.Except(secondWords).ToArray();
            //secondWords = secondWords.Except(firstWords).ToArray();

            //if (first.Name.Contains("s530-001") && second.Name.Contains("s530-002") ||
            //    first.Name.Contains("s530-002") && second.Name.Contains("s530-001"))
            //{ }

            int minFirstDistance = _input.Split(' ').ToList().Min(iw =>
               firstWords.ToList().Min(fw =>
                   CalcLevenshteinDistance(fw, iw)));

            int minSecondDistance = _input.Split(' ').ToList().Min(iw =>
                    secondWords.ToList().Min(sw =>
                        CalcLevenshteinDistance(sw, iw)));

            if (minFirstDistance < minSecondDistance) return -1;
            return minFirstDistance == minSecondDistance ? 0 : 1;
        }

        static int CalcLevenshteinDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0;

            int lengthA = a.Length;
            int lengthB = b.Length;
            var distances = new int[lengthA + 1, lengthB + 1];
            for (int i = 0; i <= lengthA; distances[i, 0] = i++) ;
            for (int j = 0; j <= lengthB; distances[0, j] = j++) ;

            for (int i = 1; i <= lengthA; i++)
                for (int j = 1; j <= lengthB; j++)
                {
                    int cost = b[j - 1] == a[i - 1] ? 0 : 1;
                    distances[i, j] = Math.Min(
                        Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                        distances[i - 1, j - 1] + cost
                    );
                }
            return distances[lengthA, lengthB];
        }
    }
}