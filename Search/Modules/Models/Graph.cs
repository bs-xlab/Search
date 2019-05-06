using Search.Modules.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Search.Modules.Models
{
    public class Graph
    {
        static Lazy<Graph> Instance;
        public Node Root { get; set; }
        int ArrayCapacity;

        public static Graph GetInstance(int arrayCapacity)
        {
            if (Instance is null)
            {
                Instance = new Lazy<Graph>(new Func<Graph>(() => new Graph(arrayCapacity)));
            }

            return Instance.Value;
        }

        Graph(int arrayCapacity)
        {
            ArrayCapacity = arrayCapacity;
            Root = new Node(new bool[arrayCapacity]);
        }

        public (List<Node> Result, Node FoundedNode) FindString(string input, List<string> mainSyllables)
        {
            var result = new List<Node>();
            input = input.Trim().ToLower();

            var inputWords = input.Split(' ');
            var lastWord = inputWords.LastOrDefault();

            var foundedNode = new Node(new bool[mainSyllables.Count]);

            if (!(lastWord is null))
            {
                var offset = 0;
                while (lastWord.Substring(0, lastWord.Length - offset) != string.Empty && result.Count < 10)
                {
                    var targetString = input.Substring(0, input.Length - offset);

                    var syllables = (Regex.IsMatch(targetString, "^[а-я0-9 ]+$")
                        ? targetString.GetSyllablesWithoutEndings()
                        : targetString.GetSeparatedStringEng())
                    .Distinct()
                    .ToList();

                    bool found = false;
                    var hash = new bool[ArrayCapacity];

                    syllables.ForEach(s =>
                    {
                        var index = mainSyllables.IndexOf(s);
                        if (index != -1)
                        {
                            hash[index] = true;
                            found = true;
                        }
                    });

                    if (found)
                    {
                        foundedNode = Root.FindHash(hash);

                        if (!(foundedNode is null))
                        {
                            foundedNode.GetChildren(result);
                        }
                    }
                    offset++;
                }
            }

            return (result, foundedNode);
        }

        public void AddNode(List<List<int>> indexesList)
        {
            indexesList.ForEach(l => Root.AddNote(l, false));
        }

        public class Node
        {
            public bool[] HashSum { get; set; }
            bool EndPoint = false;
            int _hashCode;

            public Node Parent { get; set; }
            public Node LeftNode { get; set; }
            public Node RightNode { get; set; }

            public Node(bool[] hash, Node parent = null)
            {
                HashSum = hash;
                Parent = parent;

                CreateHashCode(hash);
            }

            void CreateHashCode(bool[] hash)
            {
                var count = hash.Length / 64 + 1;
                var codes = new ulong[count];

                for (int i = 0; i < count; i++)
                {
                    var code = new StringBuilder();

                    var partOfNumber = hash.Skip(i * 64).Take(64).ToList();
                    partOfNumber.ForEach(n =>
                    {
                        if (n)
                        {
                            code.Append("1");
                        }
                        else
                        {
                            code.Append("0");
                        }
                    });

                    codes[i] = Convert.ToUInt64(code.ToString(), 2);
                }

                var resultCode = new StringBuilder();
                for (int i = 0; i < codes.Length; i++)
                {
                    var tabs = new string('0', Math.Pow(2, 64).ToString().Length - codes[i].ToString().Length);
                    resultCode.Append(tabs + codes[i].ToString());
                }

                _hashCode = resultCode.ToString().GetHashCode();
            }

            public void AddNote(List<int> indexes, bool endPoint)
            {
                if (indexes.Count > 0)
                {
                    var currentIndex = indexes.FirstOrDefault();
                    var otherIndexes = indexes.Skip(1).ToList();

                    if (RightNode is null)
                    {
                        var array = CloneAndAddIndex(HashSum, currentIndex);
                        RightNode = new Node(array, this);

                        RightNode.AddNote(otherIndexes, endPoint);
                    }
                    else
                    {
                        if (RightNode.HashSum[currentIndex])
                        {
                            RightNode.AddNote(otherIndexes, endPoint);
                        }
                        else
                        {
                            if (LeftNode is null)
                            {
                                LeftNode = new Node(HashSum, this);
                            }

                            LeftNode.AddNote(indexes, endPoint);
                        }
                    }
                }
                else
                {
                    EndPoint = endPoint;
                }
            }

            public override bool Equals(object obj) => ((Node)obj).HashSum == this; // true

            public override int GetHashCode() => _hashCode;

            bool[] CloneAndAddIndex(bool[] hash, int newIndex)
            {
                var result = new bool[hash.Length];

                for (int i = 0; i < hash.Length; i++)
                {
                    result[i] = hash[i];
                }

                result[newIndex] = true;
                return result;
            }

            public Node FindHash(bool[] hash)
            {
                if (hash == this)
                {
                    return this;
                }
                else
                {
                    if (!(RightNode is null))
                    {
                        if (hash & RightNode)
                        {
                            return RightNode.FindHash(hash);
                        }
                    }

                    return LeftNode is null ? null : LeftNode.FindHash(hash);
                }
            }

            public void GetChildren(List<Node> childrens)
            {
                if (childrens.Count > 9) return;

                if (LeftNode is null && RightNode is null)
                {
                    if (!childrens.Contains(this)) childrens.Add(this);
                }
                else
                {
                    if (EndPoint && !childrens.Contains(this))
                    {
                        childrens.Add(this);
                    }

                    if (!(LeftNode is null))
                    {
                        LeftNode.GetChildren(childrens);
                    }

                    RightNode.GetChildren(childrens);
                }
            }

            public static bool operator &(bool[] hash, Node first)
            {
                var resultArray = new bool[hash.Length];

                for (int i = 0; i < resultArray.Length; i++)
                {
                    resultArray[i] = hash[i] & first.HashSum[i];
                }

                bool isEqual = true;

                for (int j = 0; j < resultArray.Length; j++)
                {
                    isEqual &= first.HashSum[j] == resultArray[j];
                }

                return isEqual && resultArray != new Node(new bool[resultArray.Length]);
            }

            public static bool operator ==(bool[] hash, Node first)
            {
                var targetNode = new Node(hash);
                return targetNode.GetHashCode() == first.GetHashCode();

                //bool isEqual = true;

                //for (int j = 0; j < first.HashSum.Length; j++)
                //{
                //    isEqual &= hash[j] == first.HashSum[j];
                //}

                //return isEqual;
            }

            public static bool operator !=(bool[] hash, Node first) => !(hash == first);
        }
    }
}