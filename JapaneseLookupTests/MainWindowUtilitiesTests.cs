using JapaneseLookup;
using NUnit.Framework;
using System.Text.Json;
using JapaneseLookup.EDICT;

namespace JapaneseLookupTests
{
    [TestFixture]
    public class MainWindowUtilitiesTests
    {
        private static readonly JsonSerializerOptions Jso = new()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        [OneTimeSetUp]
        public void ClassInit()
        {
            JMdictLoader.Load();
        }

        [Test]
        public void Lookup_始まる()
        {
            // Arrange
            var expected =
                "[{\"foundSpelling\":[\"始まる\"],\"kanaSpellings\":[],\"readings\":[\"はじまる\"],\"definitions\":[\"(v5r, vi) (1) to begin; to start; to commence (v5r, vi) (2) to happen (again); to begin (anew) (v5r, vi) (3) to date (from); to originate (in) \"],\"foundForm\":[\"始まる\"],\"EdictID\":[\"1307500\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\"],\"aOrthographyInfoList\":[]}]";

            string text = "始まる";

            // Act
            var result = MainWindowUtilities.Lookup(
                text);
            var actual = JsonSerializer.Serialize(result, Jso);

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, actual);
        }

        [Test]
        public void Lookup_ニューモノウルトラマイクロスコーピックシリコヴォルケーノコニオシス()
        {
            // Arrange
            var expected =
                "[{\"foundSpelling\":[\"ニューモノウルトラマイクロスコーピックシリコヴォルケーノコニオシス\"],\"kanaSpellings\":[\"ニューモノウルトラマイクロスコーピックシリコヴォルケーノコニオシス\"],\"readings\":[],\"definitions\":[\"(n) (1) pneumonoultramicroscopicsilicovolcanoconiosis; pneumonoultramicroscopicsilicovolcanokoniosis \"],\"foundForm\":[\"ニューモノウルトラマイクロスコーピックシリコヴォルケーノコニオシス\"],\"EdictID\":[\"2443650\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[],\"aOrthographyInfoList\":[]},{\"foundSpelling\":[\"乳母\"],\"kanaSpellings\":[\"にゅうも\"],\"readings\":[\"うば\",\"めのと\",\"おんば\",\"にゅうぼ\",\"ちうば\",\"ちおも\",\"ちも\",\"にゅうも\",\"まま\"],\"definitions\":[\"(n) (1) wet nurse; nursing mother \"],\"foundForm\":[\"ニューモ\"],\"EdictID\":[\"1582750\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\",\"\",\"\",\"\",\"\",\"\",\"\",\"\",\"\"],\"aOrthographyInfoList\":[]},{\"foundSpelling\":[\"ニュー\"],\"kanaSpellings\":[\"ニュー\"],\"readings\":[],\"definitions\":[\"(adj-f) (1) new \"],\"foundForm\":[\"ニュー\"],\"EdictID\":[\"1091340\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[],\"aOrthographyInfoList\":[]},{\"foundSpelling\":[\"Ν\"],\"kanaSpellings\":[\"ニュー\"],\"readings\":[\"ニュー\"],\"definitions\":[\"(n) (1) nu \"],\"foundForm\":[\"ニュー\"],\"EdictID\":[\"2173750\"],\"alternativeSpellings\":[\"ν\"],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\"],\"aOrthographyInfoList\":[\"\"]},{\"foundSpelling\":[\"荷\"],\"kanaSpellings\":[\"に\"],\"readings\":[\"に\"],\"definitions\":[\"(n) (1) load; baggage; cargo; freight; goods (n) (2) burden; responsibility \"],\"foundForm\":[\"ニ\"],\"EdictID\":[\"1195250\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\"],\"aOrthographyInfoList\":[]},{\"foundSpelling\":[\"似\"],\"kanaSpellings\":[\"に\"],\"readings\":[\"に\"],\"definitions\":[\"(suf) (1) looking like (someone); taking after (either of one\'s parents) \"],\"foundForm\":[\"ニ\"],\"EdictID\":[\"1314550\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\"],\"aOrthographyInfoList\":[]},{\"foundSpelling\":[\"丹\"],\"kanaSpellings\":[\"に\"],\"readings\":[\"に\"],\"definitions\":[\"(n) (1) red earth (i.e. containing cinnabar or minium); vermilion \"],\"foundForm\":[\"ニ\"],\"EdictID\":[\"1416890\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\"],\"aOrthographyInfoList\":[]},{\"foundSpelling\":[\"二\"],\"kanaSpellings\":[\"に\"],\"readings\":[\"に\",\"ふた\",\"ふ\",\"ふう\"],\"definitions\":[\"(num) (1) (ふ and ふう used mainly when counting aloud; 弐, 貳 and 貮 are used in legal documents.) two \"],\"foundForm\":[\"ニ\"],\"EdictID\":[\"1461140\"],\"alternativeSpellings\":[\"２\",\"弐\",\"弍\",\"貳\",\"貮\"],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\",\"\",\"\",\"\"],\"aOrthographyInfoList\":[\"\",\"\",\"\",\"oK\",\"oK\"]},{\"foundSpelling\":[\"に\"],\"kanaSpellings\":[\"に\"],\"readings\":[],\"definitions\":[\"(prt) (1) at (place, time); in; on; during (prt) (2) to (direction, state); toward; into (prt) (3) for (purpose) (prt) (4) because of (reason); for; with (prt) (5) by; from (prt) (6) as (i.e. in the role of) (prt) (7) per; in; for; a (e.g. \\\"once a month\\\") (prt) (8) and; in addition to (prt) (9) (arch) if; although \"],\"foundForm\":[\"ニ\"],\"EdictID\":[\"2028990\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[],\"aOrthographyInfoList\":[]},{\"foundSpelling\":[\"ニ\"],\"kanaSpellings\":[\"ニ\"],\"readings\":[],\"definitions\":[\"(n) (1) 4th in a sequence denoted by the iroha system; 4th note in the diatonic scale (used in key names, etc.) \"],\"foundForm\":[\"ニ\"],\"EdictID\":[\"2029750\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[],\"aOrthographyInfoList\":[]},{\"foundSpelling\":[\"尼\"],\"kanaSpellings\":[\"に\"],\"readings\":[\"に\"],\"definitions\":[\"(n, n-suf) (1) (abbr) bhikkhuni (fully ordained Buddhist nun) (n) (2) (abbr) Indonesia \"],\"foundForm\":[\"ニ\"],\"EdictID\":[\"2233520\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\"],\"aOrthographyInfoList\":[]},{\"foundSpelling\":[\"土\"],\"kanaSpellings\":[\"に\"],\"readings\":[\"に\"],\"definitions\":[\"(n) (1) (arch) soil (esp. reddish soil) \"],\"foundForm\":[\"ニ\"],\"EdictID\":[\"2423330\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\"],\"aOrthographyInfoList\":[]},{\"foundSpelling\":[\"煮\"],\"kanaSpellings\":[\"に\"],\"readings\":[\"に\"],\"definitions\":[\"(suf) (1) simmered with; cooked with (n) (2) boiling; boiled dish \"],\"foundForm\":[\"ニ\"],\"EdictID\":[\"2773810\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\"],\"aOrthographyInfoList\":[]},{\"foundSpelling\":[\"似る\"],\"kanaSpellings\":[\"にる\"],\"readings\":[\"にる\"],\"definitions\":[\"(v1, vi) (1) to resemble; to look like; to be like; to be alike; to be similar; to take after \"],\"foundForm\":[\"ニ\"],\"EdictID\":[\"1314600\"],\"alternativeSpellings\":[],\"process\":[\"(infinitive)\"],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\"],\"aOrthographyInfoList\":[]},{\"foundSpelling\":[\"煮る\"],\"kanaSpellings\":[\"にる\"],\"readings\":[\"にる\"],\"definitions\":[\"(v1, vt) (1) to boil; to simmer; to stew; to seethe \"],\"foundForm\":[\"ニ\"],\"EdictID\":[\"1322540\"],\"alternativeSpellings\":[],\"process\":[\"(infinitive)\"],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\"],\"aOrthographyInfoList\":[]}]";

            string text = "ニューモノウルトラマイクロスコーピックシリコヴォルケーノコニオシス";

            // Act
            var result = MainWindowUtilities.Lookup(
                text);
            var actual = JsonSerializer.Serialize(result, Jso);

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, actual);
        }
    }
}