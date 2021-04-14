using System.Linq;
using MeCab;

namespace JapaneseLookup.Parsers
{
    class Mecab : IParser
    {
        public string Parse(string Text)
        {
            var parameter = new MeCabParam();
            var tagger = MeCabTagger.Create(parameter);
            return tagger.ParseToNodes(Text).ToArray()[1].Surface;
        }
    }
}
