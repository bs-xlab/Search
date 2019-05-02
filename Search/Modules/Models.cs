using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Search
{
    abstract class BriefModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }

    class ProductModel : BriefModel
    {
        public int BrandID { get; set; }
        public int GroupID { get; set; }
        public List<string> Syllables { get; set; }
        public List<List<int>> IndexesList { get; set; }
        public string FullIdentity { get; set; }
    }

    class BrandModel : BriefModel
    {
        public bool OnlyRussianWords { get; set; }
    }

    class GroupModel : BriefModel { }

    class Graph
    {
        static Graph Instance;
        public Node Root { get; set; }

        public static Graph GetInstance()
        {
            if (Instance is null)
            {
                Instance = new Graph();
            }

            return Instance;
        }

        Graph()
        {
            Root = new Node(new bool[Form1.Syllables.Count]);
        }

        public List<Node> FindString(string input)
        {
            var result = new List<Node>();
            input = input.Trim();

            var inputWords = input.Split(' '); 
            var lastWord = inputWords.LastOrDefault();

            if (!(lastWord is null))
            {
                //var allSyllables = new List<string>();

                //if (inputWords.Count() > 1)
                //{
                //    var otherWords = inputWords.Take(inputWords.Count() - 1).ToList();

                //    var mainSyllables = otherWords.SelectMany(b =>
                //            Regex.IsMatch(b, "^[а-я0-9 ]+$")
                //                ? b.GetSyllablesWithoutEndings()
                //                : b.GetSeparatedStringEng())
                //            .Distinct()
                //            .ToList();

                //    allSyllables.AddRange(mainSyllables);
                //}

                var offset = 0;
                while (lastWord.Substring(0, lastWord.Length - offset) != string.Empty && result.Count == 0)
                {
                    var targetString = input.Substring(0, input.Length - offset);

                    var syllables = (Regex.IsMatch(targetString, "^[а-я0-9 ]+$")
                                ? targetString.GetSyllablesWithoutEndings()
                                : targetString.GetSeparatedStringEng())
                            .Distinct()
                            .ToList();

                    //allSyllables.AddRange(syllables);

                    bool found = false;
                    var hash = new bool[Form1.Syllables.Count];

                    syllables.ForEach(s =>
                    {
                        var index = Form1.Syllables.IndexOf(s);
                        if (index != -1)
                        {
                            hash[index] = true;
                            found = true;
                        }
                    });

                    if (found)
                    {
                        var node = Root.FindHash(hash);

                        if (!(node is null))
                        {
                            node.GetChildren(result);
                        }
                    }
                    offset++;

                    //allSyllables.Take(allSyllables.Count - syllables.Count).ToList();
                }
            }

            return result;
        }

        public void AddNode(List<List<int>> indexesList)
        {
            indexesList.ForEach(l => Root.AddNote(l, false));

            //for (int offset = 0; offset < indexesList.Count; offset++)
            //{
            //    var list = indexesList.Skip(offset).Take(indexesList.Count - offset).ToList();
            //    list.AddRange(indexesList.Take(offset));

            //    var indexes = list.SelectMany(l => l).ToList();
            //    Root.AddNote(indexes, offset == 0);
            //}
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

            public override bool Equals(object obj) => true; // ((Node)obj).HashSum == this;

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
