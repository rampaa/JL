using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MeCab;

namespace JapaneseLookup.Parsers
{
    class Mecab
    {
        public static (int longestMorphLength, HashSet<string>) Parse(string text)
        {
            var parameter = new MeCabParam
            {
                DicDir = "../net5.0-windows/Resources/Unidic",
                AllMorphs = true
            };
            var tagger = MeCabTagger.Create(parameter);
            var nodes = tagger.ParseToNodes(text).ToArray();

            string longestMorph = nodes[1]?.Surface;

            tagger.AllMorphs = false;
            string bestTypeGuess = tagger.ParseToNodes(text).ToArray()[1].Feature?.Split(",")[0];

            HashSet<string> words = new();

            foreach (var node in nodes.Skip(1))
            {
                if (node.Surface != longestMorph)
                    break;

                string[] featureFields = node.Feature?.Split(",");

                if (featureFields is null || featureFields.Length < 11)
                    break;

                if (featureFields[0] == "名詞" || featureFields[0] != bestTypeGuess)
                    continue;

                //Debug.WriteLine("START");
                //for(int i = 0; i < 29; i++)
                //{
                //    Debug.WriteLine(featureFields[i]);
                //}
                //Debug.WriteLine("END");

                //words.Add(featureFields[7]);
                //words.Add(featureFields[8]);
                words.Add(featureFields[10]);
            }
            return (longestMorph.Length, words);
        }
    }
}