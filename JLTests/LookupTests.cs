using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using JL;
using JL.Dicts;
using JL.Dicts.EDICT;
using JL.Dicts.EDICT.JMdict;
using JL.Lookup;
using NUnit.Framework;

namespace JLTests
{
    [TestFixture]
    public class LookupTests
    {
        private static readonly JsonSerializerOptions Jso = new()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        [OneTimeSetUp]
        public void ClassInit()
        {
            string jmdictPath = Storage.BuiltInDicts["JMdict"].Path;

            Storage.Dicts.Add(DictType.JMdict, new Dict(DictType.JMdict, jmdictPath, true, 0));
            Storage.Dicts[DictType.JMdict].Contents = new Dictionary<string, List<IResult>>();

            if (!File.Exists(Path.Join(ConfigManager.ApplicationPath, jmdictPath)))
            {
                ResourceUpdater.UpdateResource(Storage.Dicts[DictType.JMdict].Path,
                    new Uri("http://ftp.edrdg.org/pub/Nihongo/JMdict_e.gz"),
                    DictType.JMdict.ToString(), false, true).Wait();
            }

            JMdictLoader.Load(Storage.Dicts[DictType.JMdict].Path).Wait();
        }

        [Test]
        public void LookupText_始まる()
        {
            // Arrange
            string expected =
                "[{\"foundSpelling\":[\"始まる\"],\"readings\":[\"はじまる\"],\"definitions\":[\"(v5r, vi) (1) to begin; to start; to commence (v5r, vi) (2) to happen (again); to begin (anew) (v5r, vi) (3) to date (from); to originate (in) \"],\"foundForm\":[\"始まる\"],\"EdictID\":[\"1307500\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"2147483647\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\"],\"aOrthographyInfoList\":[],\"DictType\":[\"JMdict\"]}]";

            string text = "始まる";

            // Act
            var result = Lookup.LookupText(
                text);
            string actual = JsonSerializer.Serialize(result, Jso);

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, actual);
        }
    }
}
