﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Search
{
    static class Extensions
    {
        static string Vowel = "аеёиоуыэюя";                     // Гласные буквы
        static string Deaf = "кпстф";                           // Глухие согласные
        static string Voiced = "бвгджзлмнрхцчшщ";               // Звонкие и шипящие согласные
        static string Consonants = "бвгджзйклмнпрстфхцчшщ";     // Все согласные
        static string Brief = "й";                              // 'й'
        static string Others = "ьъ";                            // Другие

        static List<string> SyllablesList;
        static char[] splitSymbols = new char[] { ' ', '-' };

        public static List<string> GetSeparatedString(this string input)
        {
            SyllablesList = new List<string>();
            input = input.ToLower();

            var currentSymbol = string.Empty;  // Текущий символ
            var currentSyllable = string.Empty;  // Текущий слог

            for (var i = 0; i < input.Length; i++)
            {
                currentSymbol = input.Substring(i, 1);
                currentSyllable += currentSymbol;

                // Проверка на признаки конца слогов

                // если буква равна 'й' и она не первая и не последняя и это не последний слог
                if (i != 0 && i != input.Length - 1 &&
                  Brief.Contains(currentSymbol) &&
                  IsNotLastSyllable(input.Substring(i + 1))) // , input.Length - i + 1
                {
                    currentSyllable = AddSyllable(currentSyllable);
                    //Console.WriteLine("если буква равна \'й\' и она не первая и не последняя и это не последний слог");
                    continue;
                }

                // если текущая гласная и следующая тоже гласная
                if (i < input.Length - 1 &&
                   Vowel.Contains(currentSymbol) &&
                   Vowel.Contains(input.Substring(i + 1, 1)))
                {
                    currentSyllable = AddSyllable(currentSyllable);
                    //Console.WriteLine("если текущая гласная и следующая тоже гласная");
                    continue;
                }


                // если текущая гласная, следующая согласная, а после неё гласная
                if (i < input.Length - 2 &&
                   Vowel.Contains(currentSymbol) &&
                   Consonants.Contains(input.Substring(i + 1, 1)) &&
                   Vowel.Contains(input.Substring(i + 2, 1)))
                {
                    currentSyllable = AddSyllable(currentSyllable);
                    //Console.WriteLine("если текущая гласная, следующая согласная, а после неё гласная");
                    continue;
                }


                // если текущая гласная, следующая глухая согласная, а после согласная и это не последний слог
                if (i < input.Length - 2 &&
                   Vowel.Contains(currentSymbol) &&
                   Deaf.Contains(input.Substring(i + 1, 1)) &&
                   Consonants.Contains(input.Substring(i + 2, 1)) &&
                   IsNotLastSyllable(input.Substring(i + 1))) // , input.Length - i + 1
                {
                    currentSyllable = AddSyllable(currentSyllable);
                    //Console.WriteLine("если текущая гласная, следующая глухая согласная, а после согласная и это не последний слог");
                    continue;
                }


                // если текущая звонкая или шипящая согласная, перед ней гласная, следующая не гласная и не другая, и это не последний слог
                if (i > 0 &&
                   i < input.Length - 1 &&
                   Voiced.Contains(currentSymbol) &&
                   Vowel.Contains(input.Substring(i - 1, 1)) &&
                   !Vowel.Contains(input.Substring(i + 1, 1)) &&
                   !Others.Contains(input.Substring(i + 1, 1)) &&
                   IsNotLastSyllable(input.Substring(i + 1))) // input.Length - i + 1
                {
                    currentSyllable = AddSyllable(currentSyllable);
                    //Console.WriteLine("если текущая звонкая или шипящая согласная, перед ней гласная, следующая не гласная и не другая, и это не последний слог");
                    continue;
                }


                // если текущая другая, а следующая не гласная, если это первый слог
                if (i < input.Length - 1 &&
                   Others.Contains(currentSymbol) &&
                   !Vowel.Contains(input.Substring(i + 1, 1)) &&
                   IsNotLastSyllable(input.Substring(0, i)))
                {
                    currentSyllable = AddSyllable(currentSyllable);
                    //Console.WriteLine("если текущая другая, а следующая не гласная, если это первый слог");
                    continue;
                }

                // если текущая гласная, следующая согласная, а после другая, и это не последний слог
                if (i < input.Length - 2 &&
                   Vowel.Contains(currentSymbol) &&
                   Consonants.Contains(input.Substring(i + 1, 1)) &&
                   Others.Contains(input.Substring(i + 2, 1)) &&
                   IsNotLastSyllable(input.Substring(i + 1)))
                {
                    currentSyllable = AddSyllable(currentSyllable);
                    //Console.WriteLine("если текущая гласная, следующая глухая согласная, а после согласная и это не последний слог");
                    continue;
                }
            }

            SyllablesList.Add(currentSyllable);
            return string.Join("-", SyllablesList).Split(splitSymbols, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        //public static IEnumerable<string> GetSeparatedStringEng(this string input)
        //{
        //    var glas = "eyuioa".ToCharArray();
        //    var sb = new StringBuilder();
        //    int i = 0;
        //    for (; input.Skip(i).Count(glas.Contains) > 1; i++)
        //    {
        //        sb.Append(input[i]);
        //        if (glas.Contains(input[i]))
        //        {
        //            yield return sb.ToString();
        //            sb.Clear();
        //        }
        //    }
        //    yield return input.Substring(i);
        //}

        public static List<string> GetSeparatedStringEng(this string input)
        {
            input = input.Trim();
            var result = new List<string>();

            var vowels = "eyuioa".ToCharArray();
            var syllable = new StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] != ' ')
                {
                    syllable.Append(input[i]);
                }

                if (vowels.Contains(input[i]) && !string.IsNullOrEmpty(syllable.ToString()))
                {
                    if (i != input.Length - 1)
                    {
                        var nextLetters = input.Substring(i + 1);
                        var spaceIndex = nextLetters.IndexOf(' ');

                        var nextIndex = spaceIndex == -1 ? input.Length - i - 1 : spaceIndex;
                        if (input.Substring(i + 1, nextIndex).Intersect(vowels).Count() == 0)
                        {
                            syllable.Append(input.Substring(i + 1, nextIndex));
                            i++;
                        }
                    }

                    result.Add(syllable.ToString());
                    syllable.Clear();
                }
            }

            return result;
        }

        /// <summary> Добавляем слог в массив и начинаем новый слог </summary>
        static string AddSyllable(string syllable)
        {
            SyllablesList.Add(syllable);
            return "";
        }

        static bool IsNotLastSyllable(string input)
        {
            // Есть ли в строке гласные?

            for (var i = 0; i < input.Length; i++)
            {
                if (Vowel.Contains(input.Substring(i, 1)))
                {
                    return true;
                }
            }
            return false;
        }

        public static void Shuffle<T>(this List<T> list)
        {
            var random = new Random(DateTime.Now.Millisecond & 0x0000FFFF);
            for (int i = list.Count - 1; i >= 1; i--)
            {
                int j = random.Next(i + 1);
                var temp = list[j];

                list[j] = list[i];
                list[i] = temp;
            }
        }

        public static string DeleteEndings(this string input)
        {
            if (input.Contains(' '))
            {
                var words = new List<string>();
                Array.ForEach(input.Split(' '), w => words.Add(w.DeleteEndings()));

                return string.Join(" ", words);
            }
            else
            {
                var syllables = input.GetSeparatedString();

                var endings = new List<string>
                {
                    "ой", "ий", "ай", "ая", "ою", "ые", "ых", "ам", "ие",
                    "ов", "ое", "ия", "ей", "ин", "ии", "ом", "ям",
                    "а", "ы", "и", "у", "ю", "о", "ь"
                };

                foreach (var ending in endings)
                {
                    if (input.EndsWith(ending))
                    {
                        int vowelsInEnding = ending.Intersect(Vowel).Count();
                        if (syllables.Count > vowelsInEnding)
                        {
                            input = input.Substring(0, input.Length - ending.Length);
                        }
                        break;
                    }
                }

                return input;
            }
        }
    }
}