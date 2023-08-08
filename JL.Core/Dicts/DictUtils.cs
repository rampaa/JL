using System.Globalization;
using System.Text.Json;
using JL.Core.Dicts.CustomNameDict;
using JL.Core.Dicts.CustomWordDict;
using JL.Core.Dicts.EDICT.JMdict;
using JL.Core.Dicts.EDICT.JMnedict;
using JL.Core.Dicts.EDICT.KANJIDIC;
using JL.Core.Dicts.EPWING.EpwingNazeka;
using JL.Core.Dicts.EPWING.EpwingYomichan;
using JL.Core.Dicts.Options;
using JL.Core.Dicts.YomichanKanji;
using JL.Core.PitchAccent;
using JL.Core.Utilities;
using JL.Core.WordClass;


namespace JL.Core.Dicts;

public static class DictUtils
{
    public static bool DictsReady { get; private set; } = false;
    public static bool CustomWordDictReady { get; private set; } = false;
    public static bool CustomNameDictReady { get; private set; } = false;
    public static bool UpdatingJmdict { get; internal set; } = false;
    public static bool UpdatingJmnedict { get; internal set; } = false;
    public static bool UpdatingKanjidic { get; internal set; } = false;
    public static readonly Dictionary<string, Dict> Dicts = new();
    internal static Dictionary<string, IList<JmdictWordClass>> WordClassDictionary { get; set; } = new(55000); // 2022/10/29: 48909, 2023/04/22: 49503, 2023/07/28: 49272
    internal static readonly Dictionary<string, string> s_kanjiCompositionDict = new(86934);
    internal static readonly Uri s_jmdictUrl = new("https://www.edrdg.org/pub/Nihongo/JMdict_e.gz");
    internal static readonly Uri s_jmnedictUrl = new("https://www.edrdg.org/pub/Nihongo/JMnedict.xml.gz");
    internal static readonly Uri s_kanjidicUrl = new("https://www.edrdg.org/kanjidic/kanjidic2.xml.gz");

    public static readonly Dictionary<string, Dict> BuiltInDicts = new()
    {
        {
            "CustomWordDictionary", new Dict(DictType.CustomWordDictionary,
                "Custom Word Dictionary",
                Path.Join(Utils.ResourcesPath, "custom_words.txt"),
                true, 1, 128,
                new DictOptions(new NewlineBetweenDefinitionsOption(true)))
        },
        {
            "CustomNameDictionary", new Dict(DictType.CustomNameDictionary,
                "Custom Name Dictionary",
                Path.Join(Utils.ResourcesPath, "custom_names.txt"),
                true, 2, 128,
                new DictOptions())
        },
        {
            "JMdict", new Dict(DictType.JMdict, "JMdict",
                Path.Join(Utils.ResourcesPath, "JMdict.xml"),
                true, 3, 500000,
                new DictOptions(
                    new NewlineBetweenDefinitionsOption(true),
                    wordClassInfo: new WordClassInfoOption(true),
                    dialectInfo: new DialectInfoOption(true),
                    pOrthographyInfo: new POrthographyInfoOption(true),
                    pOrthographyInfoColor: new POrthographyInfoColorOption("#FFD2691E"),
                    pOrthographyInfoFontSize: new POrthographyInfoFontSizeOption(15),
                    aOrthographyInfo: new AOrthographyInfoOption(true),
                    rOrthographyInfo: new ROrthographyInfoOption(true),
                    wordTypeInfo: new WordTypeInfoOption(true),
                    miscInfo: new MiscInfoOption(true),
                    loanwordEtymology: new LoanwordEtymologyOption(true),
                    relatedTerm: new RelatedTermOption(false),
                    antonym: new AntonymOption(false)
                ))
        },
        {
            "Kanjidic", new Dict(DictType.Kanjidic, "Kanjidic",
                Path.Join(Utils.ResourcesPath, "kanjidic2.xml"),
                true, 4, 13108,
                new DictOptions(noAll: new NoAllOption(false)))
        },
        {
            "JMnedict", new Dict(DictType.JMnedict, "JMnedict",
                Path.Join(Utils.ResourcesPath, "JMnedict.xml"),
                true, 5, 700000,
                new DictOptions(new NewlineBetweenDefinitionsOption(true)))
        }
    };

    public static readonly Dictionary<string, string> JmdictEntities = new()
    {
        { "bra", "\"Brazilian\"" },
        { "hob", "\"Hokkaido-ben\"" },
        { "ksb", "\"Kansai-ben\"" },
        { "ktb", "\"Kantou-ben\"" },
        { "kyb", "\"Kyoto-ben\"" },
        { "kyu", "\"Kyuushuu-ben\"" },
        { "nab", "\"Nagano-ben\"" },
        { "osb", "\"Osaka-ben\"" },
        { "rkb", "\"Ryuukyuu-ben\"" },
        { "thb", "\"Touhoku-ben\"" },
        { "tsb", "\"Tosa-ben\"" },
        { "tsug", "\"Tsugaru-ben\"" },

        { "agric", "\"agriculture\"" },
        { "anat", "\"anatomy\"" },
        { "archeol", "\"archeology\"" },
        { "archit", "\"architecture\"" },
        { "art", "\"art, aesthetics\"" },
        { "astron", "\"astronomy\"" },
        { "audvid", "\"audiovisual\"" },
        { "aviat", "\"aviation\"" },
        { "baseb", "\"baseball\"" },
        { "biochem", "\"biochemistry\"" },
        { "biol", "\"biology\"" },
        { "bot", "\"botany\"" },
        { "Buddh", "\"Buddhism\"" },
        { "bus", "\"business\"" },
        { "cards", "\"card games\"" },
        { "chem", "\"chemistry\"" },
        { "Christn", "\"Christianity\"" },
        { "cloth", "\"clothing\"" },
        { "comp", "\"computing\"" },
        { "cryst", "\"crystallography\"" },
        { "dent", "\"dentistry\"" },
        { "ecol", "\"ecology\"" },
        { "econ", "\"economics\"" },
        { "elec", "\"electricity, elec. eng.\"" },
        { "electr", "\"electronics\"" },
        { "embryo", "\"embryology\"" },
        { "engr", "\"engineering\"" },
        { "ent", "\"entomology\"" },
        { "film", "\"film\"" },
        { "finc", "\"finance\"" },
        { "fish", "\"fishing\"" },
        { "food", "\"food, cooking\"" },
        { "gardn", "\"gardening, horticulture\"" },
        { "genet", "\"genetics\"" },
        { "geogr", "\"geography\"" },
        { "geol", "\"geology\"" },
        { "geom", "\"geometry\"" },
        { "go", "\"go (game)\"" },
        { "golf", "\"golf\"" },
        { "gramm", "\"grammar\"" },
        { "grmyth", "\"Greek mythology\"" },
        { "hanaf", "\"hanafuda\"" },
        { "horse", "\"horse racing\"" },
        { "kabuki", "\"kabuki\"" },
        { "law", "\"law\"" },
        { "ling", "\"linguistics\"" },
        { "logic", "\"logic\"" },
        { "MA", "\"martial arts\"" },
        { "mahj", "\"mahjong\"" },
        { "manga", "\"manga\"" },
        { "math", "\"mathematics\"" },
        { "mech", "\"mechanical engineering\"" },
        { "med", "\"medicine\"" },
        { "met", "\"meteorology\"" },
        { "mil", "\"military\"" },
        { "mining", "\"mining\"" },
        { "music", "\"music\"" },
        { "noh", "\"noh\"" },
        { "ornith", "\"ornithology\"" },
        { "paleo", "\"paleontology\"" },
        { "pathol", "\"pathology\"" },
        { "pharm", "\"pharmacology\"" },
        { "phil", "\"philosophy\"" },
        { "photo", "\"photography\"" },
        { "physics", "\"physics\"" },
        { "physiol", "\"physiology\"" },
        { "politics", "\"politics\"" },
        { "print", "\"printing\"" },
        { "psy", "\"psychiatry\"" },
        { "psyanal", "\"psychoanalysis\"" },
        { "psych", "\"psychology\"" },
        { "rail", "\"railway\"" },
        { "rommyth", "\"Roman mythology\"" },
        { "Shinto", "\"Shinto\"" },
        { "shogi", "\"shogi\"" },
        { "ski", "\"skiing\"" },
        { "sports", "\"sports\"" },
        { "stat", "\"statistics\"" },
        { "stockm", "\"stock market\"" },
        { "sumo", "\"sumo\"" },
        { "telec", "\"telecommunications\"" },
        { "tradem", "\"trademark\"" },
        { "tv", "\"television\"" },
        { "vidg", "\"video games\"" },
        { "zool", "\"zoology\"" },

        { "ateji", "\"ateji (phonetic) reading\"" },
        { "ik", "\"word containing irregular kana usage\"" },
        { "iK", "\"word containing irregular kanji usage\"" },
        { "io", "\"irregular okurigana usage\"" },
        { "oK", "\"word containing out-dated kanji or kanji usage\"" },
        { "rK", "\"rarely-used kanji form\"" },
        { "sK", "\"search-only kanji form\"" },

        { "abbr", "\"abbreviation\"" },
        { "arch", "\"archaic\"" },
        { "char", "\"character\"" },
        { "chn", "\"children's language\"" },
        { "col", "\"colloquial\"" },
        { "company", "\"company name\"" },
        { "creat", "\"creature\"" },
        { "dated", "\"dated term\"" },
        { "dei", "\"deity\"" },
        { "derog", "\"derogatory\"" },
        { "doc", "\"document\"" },
        { "euph", "\"euphemistic\"" },
        { "ev", "\"event\"" },
        { "fam", "\"familiar language\"" },
        { "fem", "\"female term or language\"" },
        { "fict", "\"fiction\"" },
        { "form", "\"formal or literary term\"" },
        { "given", "\"given name or forename, gender not specified\"" },
        { "group", "\"group\"" },
        { "hist", "\"historical term\"" },
        { "hon", "\"honorific or respectful (sonkeigo) language\"" },
        { "hum", "\"humble (kenjougo) language\"" },
        { "id", "\"idiomatic expression\"" },
        { "joc", "\"jocular, humorous term\"" },
        { "leg", "\"legend\"" },
        { "m-sl", "\"manga slang\"" },
        { "male", "\"male term or language\"" },
        { "myth", "\"mythology\"" },
        { "net-sl", "\"Internet slang\"" },
        { "obj", "\"object\"" },
        { "obs", "\"obsolete term\"" },
        { "on-mim", "\"onomatopoeic or mimetic word\"" },
        { "organization", "\"organization name\"" },
        { "oth", "\"other\"" },
        { "person", "\"full name of a particular person\"" },
        { "place", "\"place name\"" },
        { "poet", "\"poetical term\"" },
        { "pol", "\"polite (teineigo) language\"" },
        { "product", "\"product name\"" },
        { "proverb", "\"proverb\"" },
        { "quote", "\"quotation\"" },
        { "rare", "\"rare term\"" },
        { "relig", "\"religion\"" },
        { "sens", "\"sensitive\"" },
        { "serv", "\"service\"" },
        { "ship", "\"ship name\"" },
        { "sl", "\"slang\"" },
        { "station", "\"railway station\"" },
        { "surname", "\"family or surname\"" },
        { "uk", "\"word usually written using kana alone\"" },
        { "unclass", "\"unclassified name\"" },
        { "vulg", "\"vulgar expression or word\"" },
        { "work", "\"work of art, literature, music, etc. name\"" },
        { "X", "\"rude or X-rated term (not displayed in educational software)\"" },
        { "yoji", "\"yojijukugo\"" },

        { "adj-f", "\"noun or verb acting prenominally\"" },
        { "adj-i", "\"adjective (keiyoushi)\"" },
        { "adj-ix", "\"adjective (keiyoushi) - yoi/ii class\"" },
        { "adj-kari", "\"'kari' adjective (archaic)\"" },
        { "adj-ku", "\"'ku' adjective (archaic)\"" },
        { "adj-na", "\"adjectival nouns or quasi-adjectives (keiyodoshi)\"" },
        { "adj-nari", "\"archaic/formal form of na-adjective\"" },
        { "adj-no", "\"nouns which may take the genitive case particle 'no'\"" },
        { "adj-pn", "\"pre-noun adjectival (rentaishi)\"" },
        { "adj-shiku", "\"'shiku' adjective (archaic)\"" },
        { "adj-t", "\"'taru' adjective\"" },
        { "adv", "\"adverb (fukushi)\"" },
        { "adv-to", "\"adverb taking the 'to' particle\"" },
        { "aux", "\"auxiliary\"" },
        { "aux-adj", "\"auxiliary adjective\"" },
        { "aux-v", "\"auxiliary verb\"" },
        { "conj", "\"conjunction\"" },
        { "cop", "\"copula\"" },
        { "ctr", "\"counter\"" },
        { "exp", "\"expressions (phrases, clauses, etc.)\"" },
        { "int", "\"interjection (kandoushi)\"" },
        { "n", "\"noun (common) (futsuumeishi)\"" },
        { "n-adv", "\"adverbial noun (fukushitekimeishi)\"" },
        { "n-pr", "\"proper noun\"" },
        { "n-pref", "\"noun, used as a prefix\"" },
        { "n-suf", "\"noun, used as a suffix\"" },
        { "n-t", "\"noun (temporal) (jisoumeishi)\"" },
        { "num", "\"numeric\"" },
        { "pn", "\"pronoun\"" },
        { "pref", "\"prefix\"" },
        { "prt", "\"particle\"" },
        { "suf", "\"suffix\"" },
        { "unc", "\"unclassified\"" },
        { "v-unspec", "\"verb unspecified\"" },
        { "v1", "\"Ichidan verb\"" },
        { "v1-s", "\"Ichidan verb - kureru special class\"" },
        { "v2a-s", "\"Nidan verb with 'u' ending (archaic)\"" },
        { "v2b-k", "\"Nidan verb (upper class) with 'bu' ending (archaic)\"" },
        { "v2b-s", "\"Nidan verb (lower class) with 'bu' ending (archaic)\"" },
        { "v2d-k", "\"Nidan verb (upper class) with 'dzu' ending (archaic)\"" },
        { "v2d-s", "\"Nidan verb (lower class) with 'dzu' ending (archaic)\"" },
        { "v2g-k", "\"Nidan verb (upper class) with 'gu' ending (archaic)\"" },
        { "v2g-s", "\"Nidan verb (lower class) with 'gu' ending (archaic)\"" },
        { "v2h-k", "\"Nidan verb (upper class) with 'hu/fu' ending (archaic)\"" },
        { "v2h-s", "\"Nidan verb (lower class) with 'hu/fu' ending (archaic)\"" },
        { "v2k-k", "\"Nidan verb (upper class) with 'ku' ending (archaic)\"" },
        { "v2k-s", "\"Nidan verb (lower class) with 'ku' ending (archaic)\"" },
        { "v2m-k", "\"Nidan verb (upper class) with 'mu' ending (archaic)\"" },
        { "v2m-s", "\"Nidan verb (lower class) with 'mu' ending (archaic)\"" },
        { "v2n-s", "\"Nidan verb (lower class) with 'nu' ending (archaic)\"" },
        { "v2r-k", "\"Nidan verb (upper class) with 'ru' ending (archaic)\"" },
        { "v2r-s", "\"Nidan verb (lower class) with 'ru' ending (archaic)\"" },
        { "v2s-s", "\"Nidan verb (lower class) with 'su' ending (archaic)\"" },
        { "v2t-k", "\"Nidan verb (upper class) with 'tsu' ending (archaic)\"" },
        { "v2t-s", "\"Nidan verb (lower class) with 'tsu' ending (archaic)\"" },
        { "v2w-s", "\"Nidan verb (lower class) with 'u' ending and 'we' conjugation (archaic)\"" },
        { "v2y-k", "\"Nidan verb (upper class) with 'yu' ending (archaic)\"" },
        { "v2y-s", "\"Nidan verb (lower class) with 'yu' ending (archaic)\"" },
        { "v2z-s", "\"Nidan verb (lower class) with 'zu' ending (archaic)\"" },
        { "v4b", "\"Yodan verb with 'bu' ending (archaic)\"" },
        { "v4g", "\"Yodan verb with 'gu' ending (archaic)\"" },
        { "v4h", "\"Yodan verb with 'hu/fu' ending (archaic)\"" },
        { "v4k", "\"Yodan verb with 'ku' ending (archaic)\"" },
        { "v4m", "\"Yodan verb with 'mu' ending (archaic)\"" },
        { "v4n", "\"Yodan verb with 'nu' ending (archaic)\"" },
        { "v4r", "\"Yodan verb with 'ru' ending (archaic)\"" },
        { "v4s", "\"Yodan verb with 'su' ending (archaic)\"" },
        { "v4t", "\"Yodan verb with 'tsu' ending (archaic)\"" },
        { "v5aru", "\"Godan verb - -aru special class\"" },
        { "v5b", "\"Godan verb with 'bu' ending\"" },
        { "v5g", "\"Godan verb with 'gu' ending\"" },
        { "v5k", "\"Godan verb with 'ku' ending\"" },
        { "v5k-s", "\"Godan verb - Iku/Yuku special class\"" },
        { "v5m", "\"Godan verb with 'mu' ending\"" },
        { "v5n", "\"Godan verb with 'nu' ending\"" },
        { "v5r", "\"Godan verb with 'ru' ending\"" },
        { "v5r-i", "\"Godan verb with 'ru' ending (irregular verb)\"" },
        { "v5s", "\"Godan verb with 'su' ending\"" },
        { "v5t", "\"Godan verb with 'tsu' ending\"" },
        { "v5u", "\"Godan verb with 'u' ending\"" },
        { "v5u-s", "\"Godan verb with 'u' ending (special class)\"" },
        { "v5uru", "\"Godan verb - Uru old class verb (old form of Eru)\"" },
        { "vi", "\"intransitive verb\"" },
        { "vk", "\"Kuru verb - special class\"" },
        { "vn", "\"irregular nu verb\"" },
        { "vr", "\"irregular ru verb, plain form ends with -ri\"" },
        { "vs", "\"noun or participle which takes the aux. verb suru\"" },
        { "vs-c", "\"su verb - precursor to the modern suru\"" },
        { "vs-i", "\"suru verb - included\"" },
        { "vs-s", "\"suru verb - special class\"" },
        { "vt", "\"transitive verb\"" },
        { "vz", "\"Ichidan verb - zuru verb (alternative form of -jiru verbs)\"" },

        { "gikun", "\"gikun (meaning as reading) or jukujikun (special kanji reading)\"" },
        { "ok", "\"out-dated or obsolete kana usage\"" },
        { "sk", "\"search-only kana form\"" }
    };

    public static readonly Dictionary<string, string> JmnedictEntities = new()
    {
        #pragma warning disable format
        { "char", "character" }, { "company", "company name" }, { "creat", "creature" }, { "dei", "deity" },
        { "doc", "document" }, { "ev", "event" }, { "fem", "female given name or forename" }, { "fict", "fiction" },
        { "given", "given name or forename, gender not specified" },
        { "group", "group" }, { "leg", "legend" }, { "masc", "male given name or forename" }, { "myth", "mythology" },
        { "obj", "object" }, { "organization", "organization name" }, { "oth", "other" }, { "person", "full name of a particular person" },
        { "place", "place name" }, { "product", "product name" }, { "relig", "religion" }, { "serv", "service" },
        { "station", "railway station" }, { "surname", "family or surname" }, { "unclass", "unclassified name" }, { "work", "work of art, literature, music, etc. name" },
        #pragma warning restore format
    };

    public static readonly DictType[] YomichanDictTypes = {
        DictType.Daijirin,
        DictType.Daijisen,
        DictType.Gakken,
        DictType.GakkenYojijukugoYomichan,
        DictType.IwanamiYomichan,
        DictType.JitsuyouYomichan,
        DictType.KanjigenYomichan,
        DictType.Kenkyuusha,
        DictType.KireiCakeYomichan,
        DictType.Kotowaza,
        DictType.Koujien,
        DictType.Meikyou,
        DictType.NikkokuYomichan,
        DictType.OubunshaYomichan,
        DictType.ShinjirinYomichan,
        DictType.ShinmeikaiYomichan,
        DictType.ShinmeikaiYojijukugoYomichan,
        DictType.WeblioKogoYomichan,
        DictType.ZokugoYomichan,
        DictType.PitchAccentYomichan,
        DictType.NonspecificWordYomichan,
        DictType.NonspecificKanjiYomichan,
        DictType.NonspecificNameYomichan,
        DictType.NonspecificYomichan
    };

    public static readonly DictType[] NazekaDictTypes = {
        DictType.DaijirinNazeka,
        DictType.KenkyuushaNazeka,
        DictType.ShinmeikaiNazeka,
        DictType.NonspecificWordNazeka,
        DictType.NonspecificKanjiNazeka,
        DictType.NonspecificNameNazeka,
        DictType.NonspecificNazeka
    };

    public static readonly DictType[] NonspecificDictTypes = {
        DictType.NonspecificWordYomichan,
        DictType.NonspecificKanjiYomichan,
        DictType.NonspecificNameYomichan,
        DictType.NonspecificYomichan,
        DictType.NonspecificWordNazeka,
        DictType.NonspecificKanjiNazeka,
        DictType.NonspecificNameNazeka,
        DictType.NonspecificNazeka
    };

    internal static readonly DictType[] s_kanjiDictTypes = {
        DictType.Kanjidic,
        DictType.KanjigenYomichan,
        DictType.NonspecificKanjiYomichan,
        DictType.NonspecificKanjiNazeka
    };

    internal static readonly DictType[] s_nameDictTypes = {
        DictType.CustomNameDictionary,
        DictType.JMnedict,
        DictType.NonspecificNameYomichan,
        DictType.NonspecificNameNazeka
    };

    internal static readonly DictType[] s_wordDictTypes = {
        DictType.CustomWordDictionary,
        DictType.JMdict,
        DictType.Daijirin,
        DictType.Daijisen,
        DictType.Gakken,
        DictType.GakkenYojijukugoYomichan,
        DictType.IwanamiYomichan,
        DictType.JitsuyouYomichan,
        DictType.Kenkyuusha,
        DictType.KireiCakeYomichan,
        DictType.Kotowaza,
        DictType.Koujien,
        DictType.Meikyou,
        DictType.NikkokuYomichan,
        DictType.OubunshaYomichan,
        DictType.ShinjirinYomichan,
        DictType.ShinmeikaiYomichan,
        DictType.ShinmeikaiYojijukugoYomichan,
        DictType.WeblioKogoYomichan,
        DictType.ZokugoYomichan,
        DictType.NonspecificWordYomichan,
        DictType.DaijirinNazeka,
        DictType.KenkyuushaNazeka,
        DictType.ShinmeikaiNazeka,
        DictType.NonspecificWordNazeka
    };

    public static async Task LoadDictionaries()
    {
        DictsReady = false;
        CustomWordDictReady = false;
        CustomNameDictReady = false;

        bool dictRemoved = false;

        List<Task> tasks = new();

        foreach (Dict dict in Dicts.Values.ToList())
        {
            switch (dict.Type)
            {
                case DictType.JMdict:
                    if (!UpdatingJmdict)
                    {
                        if (dict is { Active: true, Contents.Count: 0 })
                        {
                            Task jmdictTask = Task.Run(async () =>
                            {
                                await JmdictLoader.Load(dict).ConfigureAwait(false);
                                dict.Size = dict.Contents.Count;
                            });

                            tasks.Add(jmdictTask);
                        }

                        else if (dict is { Active: false, Contents.Count: > 0 })
                        {
                            dict.Contents.Clear();
                            dict.Contents.TrimExcess();
                        }
                    }

                    break;

                case DictType.JMnedict:
                    if (!UpdatingJmnedict)
                    {
                        if (dict is { Active: true, Contents.Count: 0 })
                        {
                            tasks.Add(Task.Run(async () =>
                            {
                                await JmnedictLoader.Load(dict).ConfigureAwait(false);
                                dict.Size = dict.Contents.Count;
                            }));
                        }

                        else if (dict is { Active: false, Contents.Count: > 0 })
                        {
                            dict.Contents.Clear();
                            dict.Contents.TrimExcess();
                        }
                    }

                    break;
                case DictType.Kanjidic:
                    if (!UpdatingKanjidic)
                    {
                        if (dict is { Active: true, Contents.Count: 0 })
                        {
                            tasks.Add(Task.Run(async () =>
                            {
                                await KanjidicLoader.Load(dict).ConfigureAwait(false);
                                dict.Size = dict.Contents.Count;
                            }));
                        }

                        else if (dict is { Active: false, Contents.Count: > 0 })
                        {
                            dict.Contents.Clear();
                            dict.Contents.TrimExcess();
                        }
                    }

                    break;

                case DictType.Kenkyuusha:
                case DictType.Daijirin:
                case DictType.Daijisen:
                case DictType.Koujien:
                case DictType.Meikyou:
                case DictType.Gakken:
                case DictType.Kotowaza:
                case DictType.IwanamiYomichan:
                case DictType.JitsuyouYomichan:
                case DictType.ShinmeikaiYomichan:
                case DictType.NikkokuYomichan:
                case DictType.ShinjirinYomichan:
                case DictType.OubunshaYomichan:
                case DictType.ZokugoYomichan:
                case DictType.WeblioKogoYomichan:
                case DictType.GakkenYojijukugoYomichan:
                case DictType.ShinmeikaiYojijukugoYomichan:
                case DictType.KanjigenYomichan:
                case DictType.KireiCakeYomichan:
                case DictType.NonspecificWordYomichan:
                case DictType.NonspecificNameYomichan:
                case DictType.NonspecificYomichan:
                    if (dict is { Active: true, Contents.Count: 0 })
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                await EpwingYomichanLoader.Load(dict).ConfigureAwait(false);
                                dict.Size = dict.Contents.Count;
                            }

                            catch (Exception ex)
                            {
                                Utils.Frontend.Alert(AlertLevel.Error, string.Create(CultureInfo.InvariantCulture, $"Couldn't import {dict.Name}"));
                                Utils.Logger.Error(ex, "Couldn't import {DictType}", dict.Type);
                                _ = Dicts.Remove(dict.Name);
                                dictRemoved = true;
                            }
                        }));
                    }

                    else if (dict is { Active: false, Contents.Count: > 0 })
                    {
                        dict.Contents.Clear();
                        dict.Contents.TrimExcess();
                    }

                    break;

                case DictType.NonspecificKanjiYomichan:
                    if (dict is { Active: true, Contents.Count: 0 })
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                await YomichanKanjiLoader.Load(dict).ConfigureAwait(false);
                                dict.Size = dict.Contents.Count;
                            }

                            catch (Exception ex)
                            {
                                Utils.Frontend.Alert(AlertLevel.Error, string.Create(CultureInfo.InvariantCulture, $"Couldn't import {dict.Name}"));
                                Utils.Logger.Error(ex, "Couldn't import {DictType}", dict.Type);
                                _ = Dicts.Remove(dict.Name);
                                dictRemoved = true;
                            }
                        }));
                    }

                    else if (dict is { Active: false, Contents.Count: > 0 })
                    {
                        dict.Contents.Clear();
                        dict.Contents.TrimExcess();
                    }

                    break;

                case DictType.CustomWordDictionary:
                    if (dict is { Active: true, Contents.Count: 0 })
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            await CustomWordLoader.Load(dict).ConfigureAwait(false);
                            dict.Size = dict.Contents.Count;
                            CustomWordDictReady = true;
                        }));
                    }

                    else if (dict is { Active: false, Contents.Count: > 0 })
                    {
                        dict.Contents.Clear();
                        dict.Contents.TrimExcess();
                        CustomWordDictReady = true;
                    }

                    else
                    {
                        CustomWordDictReady = true;
                    }

                    break;

                case DictType.CustomNameDictionary:
                    if (dict is { Active: true, Contents.Count: 0 })
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            await CustomNameLoader.Load(dict).ConfigureAwait(false);
                            dict.Size = dict.Contents.Count;
                            CustomNameDictReady = true;
                        }));
                    }

                    else if (dict is { Active: false, Contents.Count: > 0 })
                    {
                        dict.Contents.Clear();
                        dict.Contents.TrimExcess();
                        CustomNameDictReady = true;
                    }

                    else
                    {
                        CustomNameDictReady = true;
                    }

                    break;

                case DictType.DaijirinNazeka:
                case DictType.KenkyuushaNazeka:
                case DictType.ShinmeikaiNazeka:
                case DictType.NonspecificWordNazeka:
                case DictType.NonspecificKanjiNazeka:
                case DictType.NonspecificNameNazeka:
                case DictType.NonspecificNazeka:
                    if (dict is { Active: true, Contents.Count: 0 })
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                await EpwingNazekaLoader.Load(dict).ConfigureAwait(false);
                                dict.Size = dict.Contents.Count;
                            }

                            catch (Exception ex)
                            {
                                Utils.Frontend.Alert(AlertLevel.Error, string.Create(CultureInfo.InvariantCulture, $"Couldn't import {dict.Name}"));
                                Utils.Logger.Error(ex, "Couldn't import {DictType}", dict.Type);
                                _ = Dicts.Remove(dict.Name);
                                dictRemoved = true;
                            }
                        }));
                    }

                    else if (dict is { Active: false, Contents.Count: > 0 })
                    {
                        dict.Contents.Clear();
                        dict.Contents.TrimExcess();
                    }

                    break;

                case DictType.PitchAccentYomichan:
                    if (dict is { Active: true, Contents.Count: 0 })
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                await PitchAccentLoader.Load(dict).ConfigureAwait(false);
                                dict.Size = dict.Contents.Count;
                            }

                            catch (Exception ex)
                            {
                                Utils.Frontend.Alert(AlertLevel.Error, string.Create(CultureInfo.InvariantCulture, $"Couldn't import {dict.Name}"));
                                Utils.Logger.Error(ex, "Couldn't import {DictType}", dict.Type);
                                _ = Dicts.Remove(dict.Name);
                                dictRemoved = true;
                            }
                        }));
                    }

                    else if (dict is { Active: false, Contents.Count: > 0 })
                    {
                        dict.Contents.Clear();
                        dict.Contents.TrimExcess();
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(null, "Invalid dict type");
            }
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        if (dictRemoved)
        {
            IOrderedEnumerable<Dict> orderedDicts = Dicts.Values.OrderBy(static d => d.Priority);
            int priority = 1;

            foreach (Dict dict in orderedDicts)
            {
                dict.Priority = priority;
                ++priority;
            }
        }

        Utils.Frontend.InvalidateDisplayCache();

        DictsReady = true;
    }

    internal static async Task InitializeKanjiCompositionDict()
    {
        string filePath = Path.Join(Utils.ResourcesPath, "ids.txt");
        if (File.Exists(filePath))
        {
            string[] lines = await File.ReadAllLinesAsync(filePath).ConfigureAwait(false);

            for (int i = 0; i < lines.Length; i++)
            {
                string[] lParts = lines[i].Split("\t", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (lParts.Length is 3)
                {
                    int endIndex = lParts[2].IndexOf('[', StringComparison.Ordinal);

                    s_kanjiCompositionDict.Add(lParts[1].GetPooledString(),
                        endIndex is -1 ? lParts[2] : lParts[2][..endIndex]);
                }

                else if (lParts.Length > 3)
                {
                    for (int j = 2; j < lParts.Length; j++)
                    {
                        if (lParts[j].Contains('J', StringComparison.Ordinal))
                        {
                            int endIndex = lParts[j].IndexOf('[', StringComparison.Ordinal);
                            if (endIndex is not -1)
                            {
                                s_kanjiCompositionDict.Add(lParts[1].GetPooledString(), lParts[j][..endIndex]);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    public static async Task CreateDefaultDictsConfig()
    {
        try
        {
            _ = Directory.CreateDirectory(Utils.ConfigPath);
            await File.WriteAllTextAsync(Path.Join(Utils.ConfigPath, "dicts.json"),
                JsonSerializer.Serialize(BuiltInDicts, Utils.s_jsoWithEnumConverterAndIndentation)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Utils.Frontend.Alert(AlertLevel.Error, "Couldn't write default Dicts config");
            Utils.Logger.Error(ex, "Couldn't write default Dicts config");
        }
    }

    public static async Task SerializeDicts()
    {
        try
        {
            await File.WriteAllTextAsync(Path.Join(Utils.ConfigPath, "dicts.json"),
                JsonSerializer.Serialize(Dicts, Utils.s_jsoWithEnumConverterAndIndentation)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Utils.Logger.Fatal(ex, "SerializeDicts failed");
            throw;
        }
    }

    internal static async Task DeserializeDicts()
    {
        try
        {
            FileStream dictStream = File.OpenRead(Path.Join(Utils.ConfigPath, "dicts.json"));
            await using (dictStream.ConfigureAwait(false))
            {
                Dictionary<string, Dict>? deserializedDicts = await JsonSerializer
                    .DeserializeAsync<Dictionary<string, Dict>>(dictStream, Utils.s_jsoWithEnumConverter).ConfigureAwait(false);

                if (deserializedDicts is not null)
                {
                    IOrderedEnumerable<Dict> orderedDicts = deserializedDicts.Values.OrderBy(static d => d.Priority);
                    int priority = 1;

                    foreach (Dict dict in orderedDicts)
                    {
                        dict.Contents = dict.Size is not 0
                            ? new Dictionary<string, IList<IDictRecord>>(dict.Size)
                            : dict.Type switch
                            {
                                DictType.CustomNameDictionary => new Dictionary<string, IList<IDictRecord>>(1024),
                                DictType.CustomWordDictionary => new Dictionary<string, IList<IDictRecord>>(1024),
                                DictType.JMdict => new Dictionary<string, IList<IDictRecord>>(450000), //2022/05/11: 394949, 2022/08/15: 398303, 2023/04/22: 403739
                                DictType.JMnedict => new Dictionary<string, IList<IDictRecord>>(630000), //2022/05/11: 608833, 2022/08/15: 609117, 2023/04/22: 609055
                                DictType.Kanjidic => new Dictionary<string, IList<IDictRecord>>(13108), //2022/05/11: 13108, 2022/08/15: 13108, 2023/04/22: 13108
                                DictType.Daijirin => new Dictionary<string, IList<IDictRecord>>(420429),
                                DictType.DaijirinNazeka => new Dictionary<string, IList<IDictRecord>>(420429),
                                DictType.Daijisen => new Dictionary<string, IList<IDictRecord>>(679115),
                                DictType.Gakken => new Dictionary<string, IList<IDictRecord>>(254558),
                                DictType.GakkenYojijukugoYomichan => new Dictionary<string, IList<IDictRecord>>(7989),
                                DictType.IwanamiYomichan => new Dictionary<string, IList<IDictRecord>>(101929),
                                DictType.JitsuyouYomichan => new Dictionary<string, IList<IDictRecord>>(69746),
                                DictType.KanjigenYomichan => new Dictionary<string, IList<IDictRecord>>(64730),
                                DictType.Kenkyuusha => new Dictionary<string, IList<IDictRecord>>(303677),
                                DictType.KenkyuushaNazeka => new Dictionary<string, IList<IDictRecord>>(191804),
                                DictType.KireiCakeYomichan => new Dictionary<string, IList<IDictRecord>>(332628),
                                DictType.Kotowaza => new Dictionary<string, IList<IDictRecord>>(30846),
                                DictType.Koujien => new Dictionary<string, IList<IDictRecord>>(402571),
                                DictType.Meikyou => new Dictionary<string, IList<IDictRecord>>(107367),
                                DictType.NikkokuYomichan => new Dictionary<string, IList<IDictRecord>>(451455),
                                DictType.OubunshaYomichan => new Dictionary<string, IList<IDictRecord>>(138935),
                                DictType.PitchAccentYomichan => new Dictionary<string, IList<IDictRecord>>(434991),
                                DictType.ShinjirinYomichan => new Dictionary<string, IList<IDictRecord>>(229758),
                                DictType.ShinmeikaiYomichan => new Dictionary<string, IList<IDictRecord>>(126049),
                                DictType.ShinmeikaiNazeka => new Dictionary<string, IList<IDictRecord>>(126049),
                                DictType.ShinmeikaiYojijukugoYomichan => new Dictionary<string, IList<IDictRecord>>(6088),
                                DictType.WeblioKogoYomichan => new Dictionary<string, IList<IDictRecord>>(30838),
                                DictType.ZokugoYomichan => new Dictionary<string, IList<IDictRecord>>(2392),
                                DictType.NonspecificWordYomichan => new Dictionary<string, IList<IDictRecord>>(250000),
                                DictType.NonspecificKanjiYomichan => new Dictionary<string, IList<IDictRecord>>(250000),
                                DictType.NonspecificNameYomichan => new Dictionary<string, IList<IDictRecord>>(250000),
                                DictType.NonspecificYomichan => new Dictionary<string, IList<IDictRecord>>(250000),
                                DictType.NonspecificWordNazeka => new Dictionary<string, IList<IDictRecord>>(250000),
                                DictType.NonspecificKanjiNazeka => new Dictionary<string, IList<IDictRecord>>(250000),
                                DictType.NonspecificNameNazeka => new Dictionary<string, IList<IDictRecord>>(250000),
                                DictType.NonspecificNazeka => new Dictionary<string, IList<IDictRecord>>(250000),
                                _ => new Dictionary<string, IList<IDictRecord>>(250000)
                            };

                        dict.Priority = priority;
                        ++priority;

                        string fullPath = Path.GetFullPath(dict.Path, Utils.ApplicationPath);
                        string relativePath = Path.GetRelativePath(Utils.ApplicationPath, fullPath);
                        dict.Path = relativePath.StartsWith('.') ? fullPath : relativePath;

                        Dicts.Add(dict.Name, dict);
                    }
                }
                else
                {
                    Utils.Frontend.Alert(AlertLevel.Error, "Couldn't load Config/dicts.json");
                    Utils.Logger.Fatal("Couldn't load Config/dicts.json");
                }
            }
        }
        catch (Exception ex)
        {
            Utils.Logger.Fatal(ex, "DeserializeDicts failed");
            throw;
        }
    }
}
