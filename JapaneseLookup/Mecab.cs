using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeCab;

namespace JapaneseLookup
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
