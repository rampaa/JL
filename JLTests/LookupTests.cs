using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using JL.Windows;
using JL.Core;
using JL.Core.Dicts;
using JL.Core.Dicts.EDICT;
using JL.Core.Dicts.EDICT.JMdict;
using JL.Core.Lookup;
using NUnit.Framework;

namespace JLTests
{
    [TestFixture]
    public class LookupTests
    {
        [OneTimeSetUp]
        public void ClassInit()
        {
            Storage.Frontend = new DummyFrontend();

            string jmdictPath = Storage.BuiltInDicts["JMdict"].Path;

            Storage.Dicts.Add(DictType.JMdict, new Dict(DictType.JMdict, jmdictPath, true, 0));
            Storage.Dicts[DictType.JMdict].Contents = new Dictionary<string, List<IResult>>();

            if (!File.Exists(jmdictPath))
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
            List<Dictionary<LookupResult, List<string>>> result = Lookup.LookupText(
                text);
            string actual = JsonSerializer.Serialize(result, Storage.JsoUnsafeEscaping);

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, actual);
        }
    }
}
