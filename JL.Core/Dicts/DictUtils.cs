using System.Collections.Frozen;
using System.Runtime.Serialization;
using System.Text.Json;
using JL.Core.Config;
using JL.Core.Dicts.CustomNameDict;
using JL.Core.Dicts.CustomWordDict;
using JL.Core.Dicts.EPWING.Nazeka;
using JL.Core.Dicts.EPWING.Yomichan;
using JL.Core.Dicts.JMdict;
using JL.Core.Dicts.JMnedict;
using JL.Core.Dicts.KANJIDIC;
using JL.Core.Dicts.KanjiDict;
using JL.Core.Dicts.Options;
using JL.Core.Dicts.PitchAccent;
using JL.Core.Utilities;
using JL.Core.WordClass;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts;

public static class DictUtils
{
    public static bool DictsReady { get; private set; } // = false;
    public static bool UpdatingJmdict { get; internal set; } // = false;
    public static bool UpdatingJmnedict { get; internal set; } // = false;
    public static bool UpdatingKanjidic { get; internal set; } // = false;
    public static readonly Dictionary<string, DictBase> Dicts = new(StringComparer.OrdinalIgnoreCase);
    internal static IDictionary<string, IList<JmdictWordClass>> WordClassDictionary { get; set; } = new Dictionary<string, IList<JmdictWordClass>>(55000, StringComparer.Ordinal); // 2022/10/29: 48909, 2023/04/22: 49503, 2023/07/28: 49272
    internal static readonly Uri s_jmdictUrl = new("https://www.edrdg.org/pub/Nihongo/JMdict_e.gz");
    internal static readonly Uri s_jmnedictUrl = new("https://www.edrdg.org/pub/Nihongo/JMnedict.xml.gz");
    internal static readonly Uri s_kanjidicUrl = new("https://www.edrdg.org/kanjidic/kanjidic2.xml.gz");

    internal static bool DBIsUsedForAtLeastOneDict { get; private set; }
    internal static bool DBIsUsedForAtLeastOneYomichanDict { get; private set; }
    internal static bool DBIsUsedForAtLeastOneNazekaDict { get; private set; }
    internal static bool DBIsUsedForJmdict { get; private set; }
    internal static bool DBIsUsedForAtLeastOneWordDict { get; private set; }
    internal static bool AtLeastOneKanjiDictIsActive { get; private set; }
    internal static bool DBIsUsedAtLeastOneYomichanOrNazekaWordDict { get; private set; }

    public static CancellationTokenSource? ProfileCustomWordsCancellationTokenSource { get; private set; }
    public static CancellationTokenSource? ProfileCustomNamesCancellationTokenSource { get; private set; }

    public static readonly Dictionary<string, DictBase> BuiltInDicts = new(7, StringComparer.OrdinalIgnoreCase)
    {
        {
            nameof(DictType.ProfileCustomWordDictionary), new Dict<CustomWordRecord>(DictType.ProfileCustomWordDictionary,
                "Custom Word Dictionary (Profile)",
                Path.Join(ProfileUtils.ProfileFolderPath, "Default_Custom_Words.txt"),
                true, -1, 128,
                new DictOptions(
                    new UseDBOption(false),
                    new NoAllOption(false),
                    new NewlineBetweenDefinitionsOption(true)))
        },
        {
            nameof(DictType.ProfileCustomNameDictionary), new Dict<CustomNameRecord>(DictType.ProfileCustomNameDictionary,
                "Custom Name Dictionary (Profile)",
                Path.Join(ProfileUtils.ProfileFolderPath, "Default_Custom_Names.txt"),
                true, 0, 128,
                new DictOptions(
                    new UseDBOption(false),
                    new NoAllOption(false)))
        },
        {
            nameof(DictType.CustomWordDictionary), new Dict<CustomWordRecord>(DictType.CustomWordDictionary,
                "Custom Word Dictionary",
                Path.Join(Utils.ResourcesPath, "custom_words.txt"),
                true, 1, 128,
                new DictOptions(
                    new UseDBOption(false),
                    new NoAllOption(false),
                    new NewlineBetweenDefinitionsOption(true)))
        },
        {
            nameof(DictType.CustomNameDictionary), new Dict<CustomNameRecord>(DictType.CustomNameDictionary,
                "Custom Name Dictionary",
                Path.Join(Utils.ResourcesPath, "custom_names.txt"),
                true, 2, 128,
                new DictOptions(
                    new UseDBOption(false),
                    new NoAllOption(false)))
        },
        {
            nameof(DictType.JMdict), new Dict<JmdictRecord>(DictType.JMdict, nameof(DictType.JMdict),
                Path.Join(Utils.ResourcesPath, $"{nameof(DictType.JMdict)}.xml"),
                true, 3, 500000,
                new DictOptions(
                    new UseDBOption(true),
                    new NoAllOption(false),
                    new NewlineBetweenDefinitionsOption(true),
                    wordClassInfo: new WordClassInfoOption(true),
                    dialectInfo: new DialectInfoOption(true),
                    pOrthographyInfo: new POrthographyInfoOption(true),
                    pOrthographyInfoColor: new POrthographyInfoColorOption("#FFD2691E"),
                    pOrthographyInfoFontSize: new POrthographyInfoFontSizeOption(15),
                    aOrthographyInfo: new AOrthographyInfoOption(true),
                    rOrthographyInfo: new ROrthographyInfoOption(true),
                    wordTypeInfo: new WordTypeInfoOption(true),
                    extraDefinitionInfo: new ExtraDefinitionInfoOption(true),
                    spellingRestrictionInfo: new SpellingRestrictionInfoOption(true),
                    miscInfo: new MiscInfoOption(true),
                    loanwordEtymology: new LoanwordEtymologyOption(true),
                    relatedTerm: new RelatedTermOption(true),
                    antonym: new AntonymOption(true),
                    autoUpdateAfterNDays: new AutoUpdateAfterNDaysOption(0)
                ))
        },
        {
            nameof(DictType.Kanjidic), new Dict<KanjidicRecord>(DictType.Kanjidic, nameof(DictType.Kanjidic),
                Path.Join(Utils.ResourcesPath, "kanjidic2.xml"),
                true, 4, 13108,
                new DictOptions(
                    new UseDBOption(true),
                    new NoAllOption(false),
                    autoUpdateAfterNDays: new AutoUpdateAfterNDaysOption(0)))
        },
        {
            nameof(DictType.JMnedict), new Dict<JmnedictRecord>(DictType.JMnedict, nameof(DictType.JMnedict),
                Path.Join(Utils.ResourcesPath, $"{nameof(DictType.JMnedict)}.xml"),
                true, 5, 700000,
                new DictOptions(
                    new UseDBOption(true),
                    new NoAllOption(false),
                    new NewlineBetweenDefinitionsOption(true),
                    autoUpdateAfterNDays: new AutoUpdateAfterNDaysOption(0)))
        }
    };

    public static readonly Dictionary<DictType, DictBase> SingleDictTypeDicts = new(8);

    public static readonly Dictionary<string, string> JmdictEntities = new(254, StringComparer.Ordinal)
    {
        // ReSharper disable BadExpressionBracesLineBreaks
        { "bra", "Brazilian" },
        { "hob", "Hokkaido-ben" },
        { "ksb", "Kansai-ben" },
        { "ktb", "Kantou-ben" },
        { "kyb", "Kyoto-ben" },
        { "kyu", "Kyuushuu-ben" },
        { "nab", "Nagano-ben" },
        { "osb", "Osaka-ben" },
        { "rkb", "Ryuukyuu-ben" },
        { "thb", "Touhoku-ben" },
        { "tsb", "Tosa-ben" },
        { "tsug", "Tsugaru-ben" },

        { "agric", "agriculture" },
        { "anat", "anatomy" },
        { "archeol", "archeology" },
        { "archit", "architecture" },
        { "art", "art, aesthetics" },
        { "astron", "astronomy" },
        { "audvid", "audiovisual" },
        { "aviat", "aviation" },
        { "baseb", "baseball" },
        { "biochem", "biochemistry" },
        { "biol", "biology" },
        { "bot", "botany" },
        { "boxing", "boxing" },
        { "Buddh", "Buddhism" },
        { "bus", "business" },
        { "cards", "card games" },
        { "chem", "chemistry" },
        { "chmyth", "Chinese mythology" },
        { "Christn", "Christianity" },
        { "civeng", "civil engineering" },
        { "cloth", "clothing" },
        { "comp", "computing" },
        { "cryst", "crystallography" },
        { "dent", "dentistry" },
        { "ecol", "ecology" },
        { "econ", "economics" },
        { "elec", "electricity, elec. eng." },
        { "electr", "electronics" },
        { "embryo", "embryology" },
        { "engr", "engineering" },
        { "ent", "entomology" },
        { "figskt", "figure skating" },
        { "film", "film" },
        { "finc", "finance" },
        { "fish", "fishing" },
        { "food", "food, cooking" },
        { "gardn", "gardening, horticulture" },
        { "genet", "genetics" },
        { "geogr", "geography" },
        { "geol", "geology" },
        { "geom", "geometry" },
        { "go", "go (game)" },
        { "golf", "golf" },
        { "gramm", "grammar" },
        { "grmyth", "Greek mythology" },
        { "hanaf", "hanafuda" },
        { "horse", "horse racing" },
        { "internet", "networking, WWW" },
        { "jpmyth", "Japanese mythology" },
        { "kabuki", "kabuki" },
        { "law", "law" },
        { "ling", "linguistics" },
        { "logic", "logic" },
        { "MA", "martial arts" },
        { "mahj", "mahjong" },
        { "manga", "manga" },
        { "math", "mathematics" },
        { "mech", "mechanical engineering" },
        { "med", "medicine" },
        { "met", "meteorology" },
        { "mil", "military" },
        { "min", "mineralogy" },
        { "mining", "mining" },
        { "motor", "motorsport" },
        { "music", "music" },
        { "noh", "noh" },
        { "ornith", "ornithology" },
        { "paleo", "paleontology" },
        { "pathol", "pathology" },
        { "pharm", "pharmacology" },
        { "phil", "philosophy" },
        { "photo", "photography" },
        { "physics", "physics" },
        { "physiol", "physiology" },
        { "politics", "politics" },
        { "print", "printing" },
        { "prowres", "professional wrestling" },
        { "psy", "psychiatry" },
        { "psyanal", "psychoanalysis" },
        { "psych", "psychology" },
        { "rail", "railway" },
        { "rommyth", "Roman mythology" },
        { "Shinto", "Shinto" },
        { "shogi", "shogi" },
        { "ski", "skiing" },
        { "sports", "sports" },
        { "stat", "statistics" },
        { "stockm", "stock market" },
        { "sumo", "sumo" },
        { "surg", "surgery" },
        { "telec", "telecommunications" },
        { "tradem", "trademark" },
        { "tv", "television" },
        { "vet", "veterinary terms" },
        { "vidg", "video games" },
        { "zool", "zoology" },

        { "ateji", "ateji (phonetic) reading" },
        { "ik", "word containing irregular kana usage" },
        { "iK", "word containing irregular kanji usage" },
        { "io", "irregular okurigana usage" },
        { "oK", "word containing out-dated kanji or kanji usage" },
        { "rK", "rarely-used kanji form" },
        { "sK", "search-only kanji form" },

        { "abbr", "abbreviation" },
        { "arch", "archaic" },
        { "char", "character" },
        { "chn", "children's language" },
        { "col", "colloquial" },
        { "company", "company name" },
        { "creat", "creature" },
        { "dated", "dated term" },
        { "dei", "deity" },
        { "derog", "derogatory" },
        { "doc", "document" },
        { "euph", "euphemistic" },
        { "ev", "event" },
        { "fam", "familiar language" },
        { "fem", "female term or language" },
        { "fict", "fiction" },
        { "form", "formal or literary term" },
        { "given", "given name or forename, gender not specified" },
        { "group", "group" },
        { "hist", "historical term" },
        { "hon", "honorific or respectful (sonkeigo) language" },
        { "hum", "humble (kenjougo) language" },
        { "id", "idiomatic expression" },
        { "joc", "jocular, humorous term" },
        { "leg", "legend" },
        { "m-sl", "manga slang" },
        { "male", "male term or language" },
        { "myth", "mythology" },
        { "net-sl", "Internet slang" },
        { "obj", "object" },
        { "obs", "obsolete term" },
        { "on-mim", "onomatopoeic or mimetic word" },
        { "organization", "organization name" },
        { "oth", "other" },
        { "person", "full name of a particular person" },
        { "place", "place name" },
        { "poet", "poetical term" },
        { "pol", "polite (teineigo) language" },
        { "product", "product name" },
        { "proverb", "proverb" },
        { "quote", "quotation" },
        { "rare", "rare term" },
        { "relig", "religion" },
        { "sens", "sensitive" },
        { "serv", "service" },
        { "ship", "ship name" },
        { "sl", "slang" },
        { "station", "railway station" },
        { "surname", "family or surname" },
        { "uk", "word usually written using kana alone" },
        { "unclass", "unclassified name" },
        { "vulg", "vulgar expression or word" },
        { "work", "work of art, literature, music, etc. name" },
        { "X", "rude or X-rated term (not displayed in educational software)" },
        { "yoji", "yojijukugo" },

        { "adj-f", "noun or verb acting prenominally" },
        { "adj-i", "adjective (keiyoushi)" },
        { "adj-ix", "adjective (keiyoushi) - yoi/ii class" },
        { "adj-kari", "'kari' adjective (archaic)" },
        { "adj-ku", "'ku' adjective (archaic)" },
        { "adj-na", "adjectival nouns or quasi-adjectives (keiyodoshi)" },
        { "adj-nari", "archaic/formal form of na-adjective" },
        { "adj-no", "nouns which may take the genitive case particle 'no'" },
        { "adj-pn", "pre-noun adjectival (rentaishi)" },
        { "adj-shiku", "'shiku' adjective (archaic)" },
        { "adj-t", "'taru' adjective" },
        { "adv", "adverb (fukushi)" },
        { "adv-to", "adverb taking the 'to' particle" },
        { "aux", "auxiliary" },
        { "aux-adj", "auxiliary adjective" },
        { "aux-v", "auxiliary verb" },
        { "conj", "conjunction" },
        { "cop", "copula" },
        { "ctr", "counter" },
        { "exp", "expressions (phrases, clauses, etc.)" },
        { "int", "interjection (kandoushi)" },
        { "n", "noun (common) (futsuumeishi)" },
        { "n-adv", "adverbial noun (fukushitekimeishi)" },
        { "n-pr", "proper noun" },
        { "n-pref", "noun, used as a prefix" },
        { "n-suf", "noun, used as a suffix" },
        { "n-t", "noun (temporal) (jisoumeishi)" },
        { "num", "numeric" },
        { "pn", "pronoun" },
        { "pref", "prefix" },
        { "prt", "particle" },
        { "suf", "suffix" },
        { "unc", "unclassified" },
        { "v-unspec", "verb unspecified" },
        { "v1", "Ichidan verb" },
        { "v1-s", "Ichidan verb - kureru special class" },
        { "v2a-s", "Nidan verb with 'u' ending (archaic)" },
        { "v2b-k", "Nidan verb (upper class) with 'bu' ending (archaic)" },
        { "v2b-s", "Nidan verb (lower class) with 'bu' ending (archaic)" },
        { "v2d-k", "Nidan verb (upper class) with 'dzu' ending (archaic)" },
        { "v2d-s", "Nidan verb (lower class) with 'dzu' ending (archaic)" },
        { "v2g-k", "Nidan verb (upper class) with 'gu' ending (archaic)" },
        { "v2g-s", "Nidan verb (lower class) with 'gu' ending (archaic)" },
        { "v2h-k", "Nidan verb (upper class) with 'hu/fu' ending (archaic)" },
        { "v2h-s", "Nidan verb (lower class) with 'hu/fu' ending (archaic)" },
        { "v2k-k", "Nidan verb (upper class) with 'ku' ending (archaic)" },
        { "v2k-s", "Nidan verb (lower class) with 'ku' ending (archaic)" },
        { "v2m-k", "Nidan verb (upper class) with 'mu' ending (archaic)" },
        { "v2m-s", "Nidan verb (lower class) with 'mu' ending (archaic)" },
        { "v2n-s", "Nidan verb (lower class) with 'nu' ending (archaic)" },
        { "v2r-k", "Nidan verb (upper class) with 'ru' ending (archaic)" },
        { "v2r-s", "Nidan verb (lower class) with 'ru' ending (archaic)" },
        { "v2s-s", "Nidan verb (lower class) with 'su' ending (archaic)" },
        { "v2t-k", "Nidan verb (upper class) with 'tsu' ending (archaic)" },
        { "v2t-s", "Nidan verb (lower class) with 'tsu' ending (archaic)" },
        { "v2w-s", "Nidan verb (lower class) with 'u' ending and 'we' conjugation (archaic)" },
        { "v2y-k", "Nidan verb (upper class) with 'yu' ending (archaic)" },
        { "v2y-s", "Nidan verb (lower class) with 'yu' ending (archaic)" },
        { "v2z-s", "Nidan verb (lower class) with 'zu' ending (archaic)" },
        { "v4b", "Yodan verb with 'bu' ending (archaic)" },
        { "v4g", "Yodan verb with 'gu' ending (archaic)" },
        { "v4h", "Yodan verb with 'hu/fu' ending (archaic)" },
        { "v4k", "Yodan verb with 'ku' ending (archaic)" },
        { "v4m", "Yodan verb with 'mu' ending (archaic)" },
        { "v4n", "Yodan verb with 'nu' ending (archaic)" },
        { "v4r", "Yodan verb with 'ru' ending (archaic)" },
        { "v4s", "Yodan verb with 'su' ending (archaic)" },
        { "v4t", "Yodan verb with 'tsu' ending (archaic)" },
        { "v5aru", "Godan verb - -aru special class" },
        { "v5b", "Godan verb with 'bu' ending" },
        { "v5g", "Godan verb with 'gu' ending" },
        { "v5k", "Godan verb with 'ku' ending" },
        { "v5k-s", "Godan verb - Iku/Yuku special class" },
        { "v5m", "Godan verb with 'mu' ending" },
        { "v5n", "Godan verb with 'nu' ending" },
        { "v5r", "Godan verb with 'ru' ending" },
        { "v5r-i", "Godan verb with 'ru' ending (irregular verb)" },
        { "v5s", "Godan verb with 'su' ending" },
        { "v5t", "Godan verb with 'tsu' ending" },
        { "v5u", "Godan verb with 'u' ending" },
        { "v5u-s", "Godan verb with 'u' ending (special class)" },
        { "v5uru", "Godan verb - Uru old class verb (old form of Eru)" },
        { "vi", "intransitive verb" },
        { "vk", "Kuru verb - special class" },
        { "vn", "irregular nu verb" },
        { "vr", "irregular ru verb, plain form ends with -ri" },
        { "vs", "noun or participle which takes the aux. verb suru" },
        { "vs-c", "su verb - precursor to the modern suru" },
        { "vs-i", "suru verb - included" },
        { "vs-s", "suru verb - special class" },
        { "vt", "transitive verb" },
        { "vz", "Ichidan verb - zuru verb (alternative form of -jiru verbs)" },

        { "gikun", "gikun (meaning as reading) or jukujikun (special kanji reading)" },
        { "ok", "out-dated or obsolete kana usage" },
        { "rk", "rarely used kana form" },
        { "sk", "search-only kana form" }
        // ReSharper restore BadExpressionBracesLineBreaks
    };

    public static readonly Dictionary<string, string> JmnedictEntities = new(25, StringComparer.Ordinal)
    {
        #pragma warning disable format
        // ReSharper disable BadExpressionBracesLineBreaks
        // ReSharper disable BadListLineBreaks
        { "char", "character" }, { "company", "company name" }, { "creat", "creature" }, { "dei", "deity" },
        { "doc", "document" }, { "ev", "event" }, { "fem", "female given name or forename" }, { "fict", "fiction" },
        { "given", "given name or forename, gender not specified" },
        { "group", "group" }, { "leg", "legend" }, { "masc", "male given name or forename" }, { "myth", "mythology" },
        { "obj", "object" }, { "organization", "organization name" }, { "oth", "other" }, { "person", "full name of a particular person" },
        { "place", "place name" }, { "product", "product name" }, { "relig", "religion" }, { "serv", "service" }, { "ship", "ship name" },
        { "station", "railway station" }, { "surname", "family or surname" },{ "unclass", "unclassified name" }, { "work", "work of art, literature, music, etc. name" }
        // ReSharper restore BadExpressionBracesLineBreaks
        // ReSharper restore BadListLineBreaks
        #pragma warning restore format
    };

    public static readonly DictType[] YomichanDictTypes =
    [
        DictType.NonspecificWordYomichan,
        DictType.NonspecificKanjiYomichan,
        DictType.NonspecificKanjiWithWordSchemaYomichan,
        DictType.NonspecificNameYomichan,
        DictType.NonspecificYomichan,
        DictType.PitchAccentYomichan
    ];

    public static readonly DictType[] NazekaDictTypes =
    [
        DictType.NonspecificWordNazeka,
        DictType.NonspecificKanjiNazeka,
        DictType.NonspecificNameNazeka,
        DictType.NonspecificNazeka
    ];

    public static readonly DictType[] NonspecificDictTypes =
    [
        DictType.NonspecificWordYomichan,
        DictType.NonspecificKanjiYomichan,
        DictType.NonspecificKanjiWithWordSchemaYomichan,
        DictType.NonspecificNameYomichan,
        DictType.NonspecificYomichan,
        DictType.NonspecificWordNazeka,
        DictType.NonspecificKanjiNazeka,
        DictType.NonspecificNameNazeka,
        DictType.NonspecificNazeka
    ];

    public static readonly DictType[] KanjiDictTypes =
    [
        DictType.Kanjidic,
        DictType.NonspecificKanjiYomichan,
        DictType.NonspecificKanjiWithWordSchemaYomichan,
        DictType.NonspecificKanjiNazeka
    ];

    internal static readonly DictType[] s_nameDictTypes =
    [
        DictType.CustomNameDictionary,
        DictType.ProfileCustomNameDictionary,
        DictType.JMnedict,
        DictType.NonspecificNameYomichan,
        DictType.NonspecificNameNazeka
    ];

    internal static readonly DictType[] s_wordDictTypes =
    [
        DictType.CustomWordDictionary,
        DictType.ProfileCustomWordDictionary,
        DictType.JMdict,
        DictType.NonspecificWordYomichan,
        DictType.NonspecificWordNazeka
    ];

    internal static readonly DictType[] s_otherDictTypes =
    [
        DictType.NonspecificYomichan,
        DictType.NonspecificNazeka
    ];

    private static readonly FrozenSet<DictType> s_yomichanWordAndNameDictTypeSet = YomichanDictTypes
        .Where(static dictType => dictType is not DictType.PitchAccentYomichan and not DictType.NonspecificKanjiYomichan and not DictType.NonspecificKanjiWithWordSchemaYomichan)
        .ToFrozenSet();

    private static readonly FrozenSet<DictType> s_nazekaWordAndNameDictTypeSet = NazekaDictTypes.Where(static d => d is not DictType.NonspecificKanjiNazeka).ToFrozenSet();

    public static async Task LoadDictionaries()
    {
        DictsReady = false;

        ProfileCustomWordsCancellationTokenSource?.Dispose();
        ProfileCustomWordsCancellationTokenSource = new CancellationTokenSource();

        ProfileCustomNamesCancellationTokenSource?.Dispose();
        ProfileCustomNamesCancellationTokenSource = new CancellationTokenSource();

        bool dictCleared = false;
        bool dictRemoved = false;
        bool rebuildingAnyDB = false;

        Dictionary<string, string> dictDBPaths = new(StringComparer.Ordinal);

        List<Task> tasks = [];

        DictBase[] dicts = Dicts.Values.ToArray();

        CheckDBUsageForDicts(dicts);

        int customDictionaryTaskCount = 0;
        bool anyCustomDictionaryTaskIsActuallyUsed = false;

        foreach (DictBase dict in dicts)
        {
            bool useDB = dict.Options.UseDB.Value;
            string dbPath = DBUtils.GetDictDBPath(dict.Name);
            string dbJournalPath = dbPath + "-journal";
            bool dbExists = File.Exists(dbPath);
            bool dbExisted = dbExists;
            bool dbJournalExists = File.Exists(dbJournalPath);

            if ((dict.Type is not DictType.JMdict || !UpdatingJmdict)
                && (dict.Type is not DictType.JMnedict || !UpdatingJmnedict)
                && (dict.Type is not DictType.Kanjidic || !UpdatingKanjidic))
            {
                if (dbJournalExists)
                {
                    if (dbExists)
                    {
                        DBUtils.DeleteDB(dbPath);
                        dbExists = false;
                    }

                    File.Delete(dbJournalPath);
                }
                else if (dbExists && !DBUtils.RecordExists(dbPath))
                {
                    DBUtils.DeleteDB(dbPath);
                    dbExists = false;
                }
            }

            bool loadFromDB;
            dict.Ready = false;

            if (useDB && !DBUtils.DictDBPaths.ContainsKey(dict.Name))
            {
                dictDBPaths.Add(dict.Name, dbPath);
            }

            switch (dict.Type)
            {
                case DictType.JMdict:
                    if (UpdatingJmdict)
                    {
                        break;
                    }

                    if (dbExists && DBUtils.CheckIfDBSchemaIsOutOfDate(JmdictDBManager.Version, dbPath))
                    {
                        DBUtils.DeleteDB(dbPath);
                        dbExists = false;
                        rebuildingAnyDB = true;
                    }
                    loadFromDB = dbExists && !useDB;

                    Dict<JmdictRecord> jmdict = (Dict<JmdictRecord>)dict;
                    if (jmdict is { Active: true, Contents.Count: 0 } && (!useDB || !dbExists))
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                // 2022/05/11: 394949, 2022/08/15: 398303, 2023/04/22: 403739, 2023/12/16: 419334, 2024/02/22: 421519
                                jmdict.Contents = new Dictionary<string, IList<JmdictRecord>>(jmdict.Size > 0 ? jmdict.Size : 450000, StringComparer.Ordinal);

                                if (loadFromDB)
                                {
                                    JmdictDBManager.LoadFromDB(jmdict);
                                    jmdict.Size = jmdict.Contents.Count;
                                }
                                else
                                {
                                    await JmdictLoader.Load(jmdict).ConfigureAwait(false);
                                    if (jmdict.Active)
                                    {
                                        jmdict.Size = jmdict.Contents.Count;

                                        if (!dbExists && (useDB || dbExisted))
                                        {
                                            JmdictDBManager.CreateDB(jmdict.Name);
                                            JmdictDBManager.InsertRecordsToDB(jmdict);

                                            if (useDB)
                                            {
                                                jmdict.Contents = FrozenDictionary<string, IList<JmdictRecord>>.Empty;
                                            }
                                        }
                                    }
                                }

                                jmdict.Ready = true;
                            }
                            catch (Exception ex)
                            {
                                string fullDictPath = Path.GetFullPath(jmdict.Path, Utils.ApplicationPath);
                                Utils.Logger.Error(ex, "Couldn't import '{DictType}'-'{DictName}' from '{FullDictPath}'", jmdict.Type.GetDescription(), jmdict.Name, fullDictPath);
                                Utils.Frontend.Alert(AlertLevel.Error, $"Couldn't import {jmdict.Name}");
                                File.Delete(fullDictPath);
                                await DictUpdater.UpdateJmdict(true, false).ConfigureAwait(false);
                            }
                        }));
                    }

                    else if (jmdict.Contents.Count > 0 && (!jmdict.Active || useDB))
                    {
                        if (useDB && !dbExists)
                        {
                            tasks.Add(Task.Run(() =>
                            {
                                JmdictDBManager.CreateDB(jmdict.Name);
                                JmdictDBManager.InsertRecordsToDB(jmdict);
                                jmdict.Contents = FrozenDictionary<string, IList<JmdictRecord>>.Empty;
                                jmdict.Ready = true;
                            }));
                        }
                        else
                        {
                            jmdict.Contents = FrozenDictionary<string, IList<JmdictRecord>>.Empty;
                            jmdict.Ready = true;
                        }

                        dictCleared = true;
                    }

                    else
                    {
                        jmdict.Ready = true;
                    }

                    break;

                case DictType.JMnedict:
                    if (UpdatingJmnedict)
                    {
                        break;
                    }

                    if (dbExists && DBUtils.CheckIfDBSchemaIsOutOfDate(JmnedictDBManager.Version, dbPath))
                    {
                        DBUtils.DeleteDB(dbPath);
                        dbExists = false;
                        rebuildingAnyDB = true;
                    }
                    // loadFromDB = dbExists && !useDB;

                    Dict<JmnedictRecord> jmnedict = (Dict<JmnedictRecord>)dict;
                    if (jmnedict is { Active: true, Contents.Count: 0 } && (!useDB || !dbExists))
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                // 2022/05/11: 608833, 2022/08/15: 609117, 2023/04/22: 609055, 2023/12/16: 609238, 2024/02/22: 609265
                                jmnedict.Contents = new Dictionary<string, IList<JmnedictRecord>>(jmnedict.Size > 0 ? jmnedict.Size : 620000, StringComparer.Ordinal);

                                // We don't load JMnedict from DB because it is slower and allocates more memory for JMnedict for some reason
                                await JmnedictLoader.Load(jmnedict).ConfigureAwait(false);
                                if (jmnedict.Active)
                                {
                                    jmnedict.Size = jmnedict.Contents.Count;

                                    if (!dbExists && (useDB || dbExisted))
                                    {
                                        JmnedictDBManager.CreateDB(jmnedict.Name);
                                        JmnedictDBManager.InsertRecordsToDB(jmnedict);

                                        if (useDB)
                                        {
                                            jmnedict.Contents = FrozenDictionary<string, IList<JmnedictRecord>>.Empty;
                                        }
                                    }
                                }

                                jmnedict.Ready = true;
                            }
                            catch (Exception ex)
                            {
                                string fullDictPath = Path.GetFullPath(jmnedict.Path, Utils.ApplicationPath);
                                Utils.Logger.Error(ex, "Couldn't import '{DictType}'-'{DictName}' from '{FullDictPath}'", jmnedict.Type.GetDescription(), jmnedict.Name, fullDictPath);
                                Utils.Frontend.Alert(AlertLevel.Error, $"Couldn't import {jmnedict.Name}");
                                File.Delete(fullDictPath);
                                await DictUpdater.UpdateJmnedict(true, false).ConfigureAwait(false);
                            }
                        }));
                    }

                    else if (jmnedict.Contents.Count > 0 && (!jmnedict.Active || useDB))
                    {
                        if (useDB && !dbExists)
                        {
                            tasks.Add(Task.Run(() =>
                            {
                                JmnedictDBManager.CreateDB(jmnedict.Name);
                                JmnedictDBManager.InsertRecordsToDB(jmnedict);
                                jmnedict.Contents = FrozenDictionary<string, IList<JmnedictRecord>>.Empty;
                                jmnedict.Ready = true;
                            }));
                        }
                        else
                        {
                            jmnedict.Contents = FrozenDictionary<string, IList<JmnedictRecord>>.Empty;
                            jmnedict.Ready = true;
                        }

                        dictCleared = true;
                    }

                    else
                    {
                        jmnedict.Ready = true;
                    }

                    break;

                case DictType.Kanjidic:
                    if (UpdatingKanjidic)
                    {
                        break;
                    }

                    if (dbExists && DBUtils.CheckIfDBSchemaIsOutOfDate(KanjidicDBManager.Version, dbPath))
                    {
                        DBUtils.DeleteDB(dbPath);
                        dbExists = false;
                        rebuildingAnyDB = true;
                    }
                    loadFromDB = dbExists && !useDB;

                    Dict<KanjidicRecord> kanjidic = (Dict<KanjidicRecord>)dict;
                    if (kanjidic is { Active: true, Contents.Count: 0 } && (!useDB || !dbExists))
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                // 2022/05/11: 13108, 2023/12/16: 13108, 2024/02/02 13108
                                kanjidic.Contents = new Dictionary<string, IList<KanjidicRecord>>(kanjidic.Size > 0 ? kanjidic.Size : 13108, StringComparer.Ordinal);

                                if (loadFromDB)
                                {
                                    KanjidicDBManager.LoadFromDB(kanjidic);
                                    kanjidic.Size = kanjidic.Contents.Count;
                                }
                                else
                                {
                                    await KanjidicLoader.Load(kanjidic).ConfigureAwait(false);
                                    if (kanjidic.Active)
                                    {
                                        kanjidic.Size = kanjidic.Contents.Count;

                                        if (!dbExists && (useDB || dbExisted))
                                        {
                                            KanjidicDBManager.CreateDB(kanjidic.Name);
                                            KanjidicDBManager.InsertRecordsToDB(kanjidic);

                                            if (useDB)
                                            {
                                                kanjidic.Contents = FrozenDictionary<string, IList<KanjidicRecord>>.Empty;
                                            }
                                        }
                                    }
                                }

                                kanjidic.Ready = true;
                            }
                            catch (Exception ex)
                            {
                                string fullDictPath = Path.GetFullPath(kanjidic.Path, Utils.ApplicationPath);
                                Utils.Logger.Error(ex, "Couldn't import '{DictType}'-'{DictName}' from '{FullDictPath}'", kanjidic.Type.GetDescription(), kanjidic.Name, fullDictPath);
                                Utils.Frontend.Alert(AlertLevel.Error, $"Couldn't import {kanjidic.Name}");
                                File.Delete(fullDictPath);
                                await DictUpdater.UpdateKanjidic(true, false).ConfigureAwait(false);
                            }
                        }));
                    }

                    else if (kanjidic.Contents.Count > 0 && (!kanjidic.Active || useDB))
                    {
                        if (useDB && !dbExists)
                        {
                            tasks.Add(Task.Run(() =>
                            {
                                KanjidicDBManager.CreateDB(kanjidic.Name);
                                KanjidicDBManager.InsertRecordsToDB(kanjidic);
                                kanjidic.Contents = FrozenDictionary<string, IList<KanjidicRecord>>.Empty;
                                kanjidic.Ready = true;
                            }));
                        }
                        else
                        {
                            kanjidic.Contents = FrozenDictionary<string, IList<KanjidicRecord>>.Empty;
                            kanjidic.Ready = true;
                        }
                    }

                    else
                    {
                        kanjidic.Ready = true;
                    }

                    break;

                case DictType.NonspecificWordYomichan:
                case DictType.NonspecificKanjiWithWordSchemaYomichan:
                case DictType.NonspecificNameYomichan:
                case DictType.NonspecificYomichan:
                {
                    if (dbExists && DBUtils.CheckIfDBSchemaIsOutOfDate(EpwingYomichanDBManager.Version, dbPath))
                    {
                        DBUtils.DeleteDB(dbPath);
                        dbExists = false;
                        rebuildingAnyDB = true;
                    }
                    loadFromDB = dbExists && !useDB;

                    Dict<EpwingYomichanRecord> yomichanDict = (Dict<EpwingYomichanRecord>)dict;
                    if (yomichanDict is { Active: true, Contents.Count: 0 } && (!useDB || !dbExists))
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                int dictSize = yomichanDict.Size > 0
                                    ? yomichanDict.Size
                                    : 250000;

                                yomichanDict.Contents = new Dictionary<string, IList<EpwingYomichanRecord>>(dictSize, StringComparer.Ordinal);

                                if (loadFromDB)
                                {
                                    EpwingYomichanDBManager.LoadFromDB(yomichanDict);
                                    yomichanDict.Size = yomichanDict.Contents.Count;
                                }
                                else
                                {
                                    await EpwingYomichanLoader.Load(yomichanDict).ConfigureAwait(false);
                                    yomichanDict.Size = yomichanDict.Contents.Count;

                                    if (!dbExists && (useDB || dbExisted))
                                    {
                                        EpwingYomichanDBManager.CreateDB(yomichanDict.Name);
                                        EpwingYomichanDBManager.InsertRecordsToDB(yomichanDict);

                                        if (useDB)
                                        {
                                            yomichanDict.Contents = FrozenDictionary<string, IList<EpwingYomichanRecord>>.Empty;
                                        }
                                    }
                                }

                                yomichanDict.Ready = true;
                            }

                            catch (Exception ex)
                            {
                                string fullDictPath = Path.GetFullPath(yomichanDict.Path, Utils.ApplicationPath);
                                Utils.Logger.Error(ex, "Couldn't import '{DictType}'-'{DictName}' from '{FullDictPath}'", yomichanDict.Type.GetDescription(), yomichanDict.Name, fullDictPath);
                                Utils.Frontend.Alert(AlertLevel.Error, $"Couldn't import {yomichanDict.Name}");
                                _ = Dicts.Remove(yomichanDict.Name);
                                dictRemoved = true;

                                if (File.Exists(dbPath))
                                {
                                    DBUtils.DeleteDB(dbPath);
                                }
                            }
                        }));
                    }

                    else if (yomichanDict.Contents.Count > 0 && (!yomichanDict.Active || useDB))
                    {
                        if (useDB && !dbExists)
                        {
                            tasks.Add(Task.Run(() =>
                            {
                                EpwingYomichanDBManager.CreateDB(yomichanDict.Name);
                                EpwingYomichanDBManager.InsertRecordsToDB(yomichanDict);
                                yomichanDict.Contents = FrozenDictionary<string, IList<EpwingYomichanRecord>>.Empty;
                                yomichanDict.Ready = true;
                            }));
                        }
                        else
                        {
                            yomichanDict.Contents = FrozenDictionary<string, IList<EpwingYomichanRecord>>.Empty;
                            yomichanDict.Ready = true;
                        }

                        dictCleared = true;
                    }

                    else
                    {
                        yomichanDict.Ready = true;
                    }

                    break;
                }

                case DictType.NonspecificKanjiYomichan:
                {
                    if (dbExists && DBUtils.CheckIfDBSchemaIsOutOfDate(YomichanKanjiDBManager.Version, dbPath))
                    {
                        DBUtils.DeleteDB(dbPath);
                        dbExists = false;
                        rebuildingAnyDB = true;
                    }
                    loadFromDB = dbExists && !useDB;

                    Dict<YomichanKanjiRecord> yomichanKanjiDict = (Dict<YomichanKanjiRecord>)dict;
                    if (yomichanKanjiDict is { Active: true, Contents.Count: 0 } && (!useDB || !dbExists))
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            yomichanKanjiDict.Contents = new Dictionary<string, IList<YomichanKanjiRecord>>(yomichanKanjiDict.Size > 0 ? yomichanKanjiDict.Size : 250000, StringComparer.Ordinal);
                            try
                            {
                                if (loadFromDB)
                                {
                                    YomichanKanjiDBManager.LoadFromDB(yomichanKanjiDict);
                                    yomichanKanjiDict.Size = yomichanKanjiDict.Contents.Count;
                                }
                                else
                                {
                                    await YomichanKanjiLoader.Load(yomichanKanjiDict).ConfigureAwait(false);
                                    yomichanKanjiDict.Size = yomichanKanjiDict.Contents.Count;

                                    if (!dbExists && (useDB || dbExisted))
                                    {
                                        YomichanKanjiDBManager.CreateDB(yomichanKanjiDict.Name);
                                        YomichanKanjiDBManager.InsertRecordsToDB(yomichanKanjiDict);

                                        if (useDB)
                                        {
                                            yomichanKanjiDict.Contents = FrozenDictionary<string, IList<YomichanKanjiRecord>>.Empty;
                                        }
                                    }
                                }

                                yomichanKanjiDict.Ready = true;
                            }

                            catch (Exception ex)
                            {
                                string fullDictPath = Path.GetFullPath(yomichanKanjiDict.Path, Utils.ApplicationPath);
                                Utils.Logger.Error(ex, "Couldn't import '{DictType}'-'{DictName}' from '{FullDictPath}'", yomichanKanjiDict.Type.GetDescription(), yomichanKanjiDict.Name, fullDictPath);
                                Utils.Frontend.Alert(AlertLevel.Error, $"Couldn't import {yomichanKanjiDict.Name}");
                                _ = Dicts.Remove(yomichanKanjiDict.Name);
                                dictRemoved = true;

                                if (File.Exists(dbPath))
                                {
                                    DBUtils.DeleteDB(dbPath);
                                }
                            }
                        }));
                    }

                    else if (yomichanKanjiDict.Contents.Count > 0 && (!yomichanKanjiDict.Active || useDB))
                    {
                        if (useDB && !dbExists)
                        {
                            tasks.Add(Task.Run(() =>
                            {
                                YomichanKanjiDBManager.CreateDB(yomichanKanjiDict.Name);
                                YomichanKanjiDBManager.InsertRecordsToDB(yomichanKanjiDict);
                                yomichanKanjiDict.Contents = FrozenDictionary<string, IList<YomichanKanjiRecord>>.Empty;
                                yomichanKanjiDict.Ready = true;
                            }));
                        }
                        else
                        {
                            yomichanKanjiDict.Contents = FrozenDictionary<string, IList<YomichanKanjiRecord>>.Empty;
                            yomichanKanjiDict.Ready = true;
                        }

                        dictCleared = true;
                    }

                    else
                    {
                        yomichanKanjiDict.Ready = true;
                    }

                    break;
                }

                case DictType.CustomWordDictionary:
                case DictType.ProfileCustomWordDictionary:
                {
                    Dict<CustomWordRecord> customWordDict = (Dict<CustomWordRecord>)dict;
                    if (customWordDict is { Active: true, Contents.Count: 0 })
                    {
                        tasks.Add(Task.Run(() =>
                        {
                            ++customDictionaryTaskCount;

                            int size = customWordDict.Size > 0
                                ? customWordDict.Size
                                : customWordDict.Type is DictType.CustomWordDictionary
                                    ? 1024
                                    : 256;

                            customWordDict.Contents = new Dictionary<string, IList<CustomWordRecord>>(size, StringComparer.Ordinal);

                            CustomWordLoader.Load(customWordDict,
                                customWordDict.Type is DictType.CustomWordDictionary
                                    ? CancellationToken.None
                                    : ProfileCustomWordsCancellationTokenSource.Token);

                            customWordDict.Size = customWordDict.Contents.Count;
                            anyCustomDictionaryTaskIsActuallyUsed = customWordDict.Size > 0 || anyCustomDictionaryTaskIsActuallyUsed;
                            customWordDict.Ready = true;
                        }));
                    }

                    else if (customWordDict is { Active: false, Contents.Count: > 0 })
                    {
                        customWordDict.Contents = FrozenDictionary<string, IList<CustomWordRecord>>.Empty;
                        dictCleared = true;
                        customWordDict.Ready = true;
                    }

                    else
                    {
                        customWordDict.Ready = true;
                    }

                    break;
                }

                case DictType.CustomNameDictionary:
                case DictType.ProfileCustomNameDictionary:
                {
                    Dict<CustomNameRecord> customNameDict = (Dict<CustomNameRecord>)dict;
                    if (customNameDict is { Active: true, Contents.Count: 0 })
                    {
                        tasks.Add(Task.Run(() =>
                        {
                            ++customDictionaryTaskCount;

                            int size = customNameDict.Size is not 0
                                ? customNameDict.Size
                                : customNameDict.Type is DictType.CustomNameDictionary
                                    ? 1024
                                    : 256;

                            customNameDict.Contents = new Dictionary<string, IList<CustomNameRecord>>(size, StringComparer.Ordinal);

                            CustomNameLoader.Load(customNameDict,
                                customNameDict.Type is DictType.CustomNameDictionary
                                    ? CancellationToken.None
                                    : ProfileCustomNamesCancellationTokenSource.Token);

                            customNameDict.Size = customNameDict.Contents.Count;
                            anyCustomDictionaryTaskIsActuallyUsed = customNameDict.Size > 0 || anyCustomDictionaryTaskIsActuallyUsed;
                            customNameDict.Ready = true;
                        }));
                    }

                    else if (customNameDict is { Active: false, Contents.Count: > 0 })
                    {
                        customNameDict.Contents = FrozenDictionary<string, IList<CustomNameRecord>>.Empty;
                        dictCleared = true;
                        customNameDict.Ready = true;
                    }

                    else
                    {
                        customNameDict.Ready = true;
                    }

                    break;
                }

                case DictType.NonspecificWordNazeka:
                case DictType.NonspecificKanjiNazeka:
                case DictType.NonspecificNameNazeka:
                case DictType.NonspecificNazeka:
                {
                    if (dbExists && DBUtils.CheckIfDBSchemaIsOutOfDate(EpwingNazekaDBManager.Version, dbPath))
                    {
                        DBUtils.DeleteDB(dbPath);
                        dbExists = false;
                        rebuildingAnyDB = true;
                    }
                    loadFromDB = dbExists && !useDB;

                    Dict<EpwingNazekaRecord> nazekaDict = (Dict<EpwingNazekaRecord>)dict;
                    if (nazekaDict is { Active: true, Contents.Count: 0 } && (!useDB || !dbExists))
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                int size = nazekaDict.Size > 0
                                    ? nazekaDict.Size
                                    : 250000;

                                nazekaDict.Contents = new Dictionary<string, IList<EpwingNazekaRecord>>(size, StringComparer.Ordinal);

                                if (loadFromDB)
                                {
                                    EpwingNazekaDBManager.LoadFromDB(nazekaDict);
                                    nazekaDict.Size = nazekaDict.Contents.Count;
                                }
                                else
                                {
                                    await EpwingNazekaLoader.Load(nazekaDict).ConfigureAwait(false);
                                    nazekaDict.Size = nazekaDict.Contents.Count;

                                    if (!dbExists && (useDB || dbExisted))
                                    {
                                        EpwingNazekaDBManager.CreateDB(nazekaDict.Name);
                                        EpwingNazekaDBManager.InsertRecordsToDB(nazekaDict);

                                        if (useDB)
                                        {
                                            nazekaDict.Contents = FrozenDictionary<string, IList<EpwingNazekaRecord>>.Empty;
                                        }
                                    }
                                }

                                nazekaDict.Ready = true;
                            }

                            catch (Exception ex)
                            {
                                string fullDictPath = Path.GetFullPath(nazekaDict.Path, Utils.ApplicationPath);
                                Utils.Logger.Error(ex, "Couldn't import '{DictType}'-'{DictName}' from '{FullDictPath}'", nazekaDict.Type.GetDescription(), nazekaDict.Name, fullDictPath);
                                Utils.Frontend.Alert(AlertLevel.Error, $"Couldn't import {nazekaDict.Name}");
                                _ = Dicts.Remove(nazekaDict.Name);
                                dictRemoved = true;

                                if (File.Exists(dbPath))
                                {
                                    DBUtils.DeleteDB(dbPath);
                                }
                            }
                        }));
                    }

                    else if (nazekaDict.Contents.Count > 0 && (!nazekaDict.Active || useDB))
                    {
                        if (useDB && !dbExists)
                        {
                            tasks.Add(Task.Run(() =>
                            {
                                EpwingNazekaDBManager.CreateDB(nazekaDict.Name);
                                EpwingNazekaDBManager.InsertRecordsToDB(nazekaDict);
                                nazekaDict.Contents = FrozenDictionary<string, IList<EpwingNazekaRecord>>.Empty;
                                nazekaDict.Ready = true;
                            }));
                        }
                        else
                        {
                            nazekaDict.Contents = FrozenDictionary<string, IList<EpwingNazekaRecord>>.Empty;
                            nazekaDict.Ready = true;
                        }

                        dictCleared = true;
                    }

                    else
                    {
                        nazekaDict.Ready = true;
                    }

                    break;
                }

                case DictType.PitchAccentYomichan:
                {
                    if (dbExists && DBUtils.CheckIfDBSchemaIsOutOfDate(YomichanPitchAccentDBManager.Version, dbPath))
                    {
                        DBUtils.DeleteDB(dbPath);
                        dbExists = false;
                        rebuildingAnyDB = true;
                    }
                    loadFromDB = dbExists && !useDB;

                    Dict<PitchAccentRecord> pitchAccentDict = (Dict<PitchAccentRecord>)dict;
                    if (pitchAccentDict is { Active: true, Contents.Count: 0 } && (!useDB || !dbExists))
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                pitchAccentDict.Contents = new Dictionary<string, IList<PitchAccentRecord>>(pitchAccentDict.Size > 0 ? pitchAccentDict.Size : 434991, StringComparer.Ordinal);

                                if (loadFromDB)
                                {
                                    YomichanPitchAccentDBManager.LoadFromDB(pitchAccentDict);
                                    pitchAccentDict.Size = pitchAccentDict.Contents.Count;
                                }
                                else
                                {
                                    await YomichanPitchAccentLoader.Load(pitchAccentDict).ConfigureAwait(false);
                                    pitchAccentDict.Size = pitchAccentDict.Contents.Count;

                                    if (!dbExists && (useDB || dbExisted))
                                    {
                                        YomichanPitchAccentDBManager.CreateDB(pitchAccentDict.Name);
                                        YomichanPitchAccentDBManager.InsertRecordsToDB(pitchAccentDict);

                                        if (useDB)
                                        {
                                            pitchAccentDict.Contents = FrozenDictionary<string, IList<PitchAccentRecord>>.Empty;
                                        }
                                    }
                                }

                                pitchAccentDict.Ready = true;
                            }

                            catch (Exception ex)
                            {
                                string fullDictPath = Path.GetFullPath(pitchAccentDict.Path, Utils.ApplicationPath);
                                Utils.Logger.Error(ex, "Couldn't import '{DictType}'-'{DictName}' from '{FullDictPath}'", pitchAccentDict.Type.GetDescription(), pitchAccentDict.Name, fullDictPath);
                                Utils.Frontend.Alert(AlertLevel.Error, $"Couldn't import {pitchAccentDict.Name}");
                                _ = Dicts.Remove(pitchAccentDict.Name);
                                _ = SingleDictTypeDicts.Remove(DictType.PitchAccentYomichan);
                                dictRemoved = true;

                                if (File.Exists(dbPath))
                                {
                                    DBUtils.DeleteDB(dbPath);
                                }
                            }
                        }));
                    }

                    else if (pitchAccentDict.Contents.Count > 0 && (!pitchAccentDict.Active || useDB))
                    {
                        if (useDB && !dbExists)
                        {
                            tasks.Add(Task.Run(() =>
                            {
                                YomichanPitchAccentDBManager.CreateDB(pitchAccentDict.Name);
                                YomichanPitchAccentDBManager.InsertRecordsToDB(pitchAccentDict);
                                pitchAccentDict.Contents = FrozenDictionary<string, IList<PitchAccentRecord>>.Empty;
                                pitchAccentDict.Ready = true;
                            }));
                        }
                        else
                        {
                            pitchAccentDict.Contents = FrozenDictionary<string, IList<PitchAccentRecord>>.Empty;
                            pitchAccentDict.Ready = true;
                        }

                        dictCleared = true;
                    }

                    else
                    {
                        pitchAccentDict.Ready = true;
                    }

                    break;
                }

                default:
                {
                    Utils.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(DictType), nameof(DictUtils), nameof(LoadDictionaries), dict.Type);
                    Utils.Frontend.Alert(AlertLevel.Error, $"Invalid dictionary type: {dict.Type}");
                    break;
                }
            }
        }

        if (dictDBPaths.Count > 0)
        {
            KeyValuePair<string, string>[] tempDictDBPathKeyValuePairs = new KeyValuePair<string, string>[DBUtils.DictDBPaths.Count + dictDBPaths.Count];
            int index = 0;
            foreach ((string key, string value) in DBUtils.DictDBPaths)
            {
                tempDictDBPathKeyValuePairs[index] = KeyValuePair.Create(key, value);
                ++index;
            }

            foreach ((string key, string value) in dictDBPaths)
            {
                tempDictDBPathKeyValuePairs[index] = KeyValuePair.Create(key, value);
                ++index;
            }

            DBUtils.DictDBPaths = tempDictDBPathKeyValuePairs.ToFrozenDictionary(StringComparer.Ordinal);
        }

        if (tasks.Count > 0 || dictCleared)
        {
            SqliteConnection.ClearAllPools();

            if (tasks.Count > 0)
            {
                if (rebuildingAnyDB)
                {
                    Utils.Frontend.Alert(AlertLevel.Information, "Rebuilding some databases because their schemas are out of date...");
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);

                if (dictRemoved)
                {
                    IOrderedEnumerable<DictBase> orderedDicts = Dicts.Values.OrderBy(static d => d.Priority);
                    int priority = 1;

                    foreach (DictBase dict in orderedDicts)
                    {
                        dict.Priority = priority;
                        ++priority;
                    }
                }
            }

            CheckDBUsageForDicts(Dicts.Values.ToArray());

            if (!UpdatingJmdict
                && !UpdatingJmnedict
                && !UpdatingKanjidic
                && (tasks.Count > customDictionaryTaskCount || anyCustomDictionaryTaskIsActuallyUsed))
            {
                Utils.Frontend.Alert(AlertLevel.Success, "Finished loading dictionaries");
            }
        }

        DictsReady = true;
    }

    public static Task CreateDefaultDictsConfig()
    {
        _ = Directory.CreateDirectory(Utils.ConfigPath);
        return File.WriteAllTextAsync(Path.Join(Utils.ConfigPath, "dicts.json"),
            JsonSerializer.Serialize(BuiltInDicts, Utils.s_jsoIgnoringWhenWritingNullWithEnumConverterAndIndentation));
    }

    public static Task SerializeDicts()
    {
        return File.WriteAllTextAsync(Path.Join(Utils.ConfigPath, "dicts.json"),
            JsonSerializer.Serialize(Dicts, Utils.s_jsoIgnoringWhenWritingNullWithEnumConverterAndIndentation));
    }

    internal static async Task DeserializeDicts()
    {
        FileStream dictStream = File.OpenRead(Path.Join(Utils.ConfigPath, "dicts.json"));
        await using (dictStream.ConfigureAwait(false))
        {
            Dictionary<string, DictBase>? deserializedDicts = await JsonSerializer
                .DeserializeAsync<Dictionary<string, DictBase>>(dictStream, Utils.s_jsoWithDictEnumConverter).ConfigureAwait(false);

            if (deserializedDicts is not null)
            {
                foreach (DictBase dict in BuiltInDicts.Values)
                {
                    if (deserializedDicts.Values.All(d => d.Type != dict.Type))
                    {
                        deserializedDicts.Add(dict.Name, dict);
                    }
                }

                IOrderedEnumerable<DictBase> orderedDicts = deserializedDicts.Values.OrderBy(static dict => dict.Priority);

                int priority = 1;
                foreach (DictBase dict in orderedDicts)
                {
                    dict.Priority = priority;
                    ++priority;

                    if (dict.Type is DictType.ProfileCustomNameDictionary)
                    {
                        dict.Path = ProfileUtils.GetProfileCustomNameDictPath(ProfileUtils.CurrentProfileName);
                        SingleDictTypeDicts[dict.Type] = dict;
                    }
                    else if (dict.Type is DictType.ProfileCustomWordDictionary)
                    {
                        dict.Path = ProfileUtils.GetProfileCustomWordDictPath(ProfileUtils.CurrentProfileName);
                        SingleDictTypeDicts[dict.Type] = dict;
                    }
                    else if (dict.Type is DictType.CustomNameDictionary
                             or DictType.CustomWordDictionary
                             or DictType.JMdict
                             or DictType.Kanjidic
                             or DictType.JMnedict
                             or DictType.PitchAccentYomichan)
                    {
                        SingleDictTypeDicts[dict.Type] = dict;
                    }

                    InitDictOptions(dict);

                    dict.Path = Utils.GetPath(dict.Path);
                    Dicts.Add(dict.Name, dict);
                }
            }
            else
            {
                Utils.Frontend.Alert(AlertLevel.Error, "Couldn't load Config/dicts.json");
                throw new SerializationException("Couldn't load Config/dicts.json");
            }
        }
    }

    private static void InitDictOptions(DictBase dict)
    {
        if (dict.Type is DictType.JMdict)
        {
            DictOptions builtInJmdictOptions = BuiltInDicts[nameof(DictType.JMdict)].Options;

            dict.Options.NewlineBetweenDefinitions ??= builtInJmdictOptions.NewlineBetweenDefinitions;
            dict.Options.WordClassInfo ??= builtInJmdictOptions.WordClassInfo;
            dict.Options.DialectInfo ??= builtInJmdictOptions.DialectInfo;
            dict.Options.POrthographyInfo ??= builtInJmdictOptions.POrthographyInfo;
            dict.Options.POrthographyInfoColor ??= builtInJmdictOptions.POrthographyInfoColor;
            dict.Options.POrthographyInfoFontSize ??= builtInJmdictOptions.POrthographyInfoFontSize;
            dict.Options.AOrthographyInfo ??= builtInJmdictOptions.AOrthographyInfo;
            dict.Options.ROrthographyInfo ??= builtInJmdictOptions.ROrthographyInfo;
            dict.Options.WordTypeInfo ??= builtInJmdictOptions.WordTypeInfo;
            dict.Options.ExtraDefinitionInfo ??= builtInJmdictOptions.ExtraDefinitionInfo;
            dict.Options.SpellingRestrictionInfo ??= builtInJmdictOptions.SpellingRestrictionInfo;
            dict.Options.MiscInfo ??= builtInJmdictOptions.MiscInfo;
            dict.Options.LoanwordEtymology ??= builtInJmdictOptions.LoanwordEtymology;
            dict.Options.RelatedTerm ??= builtInJmdictOptions.RelatedTerm;
            dict.Options.Antonym ??= builtInJmdictOptions.Antonym;
            dict.Options.AutoUpdateAfterNDays ??= builtInJmdictOptions.AutoUpdateAfterNDays;
        }
        else if (dict.Type is DictType.Kanjidic)
        {
            DictOptions builtInKanjidicOptions = BuiltInDicts[nameof(DictType.Kanjidic)].Options;
            dict.Options.AutoUpdateAfterNDays ??= builtInKanjidicOptions.AutoUpdateAfterNDays;
        }
        else if (dict.Type is DictType.JMnedict)
        {
            DictOptions builtInJmnedictOptions = BuiltInDicts[nameof(DictType.JMnedict)].Options;

            dict.Options.NewlineBetweenDefinitions ??= builtInJmnedictOptions.NewlineBetweenDefinitions;
            dict.Options.AutoUpdateAfterNDays ??= builtInJmnedictOptions.AutoUpdateAfterNDays;
        }
        else if (dict.Type is DictType.CustomWordDictionary or DictType.ProfileCustomWordDictionary)
        {
            DictOptions builtInCustomWordOptions = BuiltInDicts[nameof(DictType.CustomWordDictionary)].Options;
            dict.Options.NewlineBetweenDefinitions ??= builtInCustomWordOptions.NewlineBetweenDefinitions;
        }
        else
        {
            if (NewlineBetweenDefinitionsOption.ValidDictTypes.Contains(dict.Type))
            {
                dict.Options.NewlineBetweenDefinitions ??= new NewlineBetweenDefinitionsOption(true);
            }
            if (PitchAccentMarkerColorOption.ValidDictTypes.Contains(dict.Type))
            {
                dict.Options.PitchAccentMarkerColor ??= new PitchAccentMarkerColorOption("#FF00BFFF");
            }
            if (ShowPitchAccentWithDottedLinesOption.ValidDictTypes.Contains(dict.Type))
            {
                dict.Options.ShowPitchAccentWithDottedLines ??= new ShowPitchAccentWithDottedLinesOption(true);
            }
        }
    }

    private static void CheckDBUsageForDicts(DictBase[] dicts)
    {
        DBIsUsedForAtLeastOneDict = dicts.Any(static dict => dict is { Active: true, Options.UseDB.Value: true });
        DBIsUsedForAtLeastOneWordDict = DBIsUsedForAtLeastOneDict && dicts.Any(static dict => dict is { Type: DictType.JMdict or DictType.NonspecificWordYomichan or DictType.NonspecificWordNazeka, Active: true, Options.UseDB.Value: true });
        DBIsUsedForAtLeastOneYomichanDict = DBIsUsedForAtLeastOneDict && dicts.Any(static dict => dict.Active && s_yomichanWordAndNameDictTypeSet.Contains(dict.Type) && dict.Options.UseDB.Value);
        DBIsUsedForAtLeastOneNazekaDict = DBIsUsedForAtLeastOneDict && dicts.Any(static dict => dict.Active && s_nazekaWordAndNameDictTypeSet.Contains(dict.Type) && dict.Options.UseDB.Value);
        DBIsUsedAtLeastOneYomichanOrNazekaWordDict = (DBIsUsedForAtLeastOneYomichanDict || DBIsUsedForAtLeastOneNazekaDict) && DBIsUsedForAtLeastOneDict && dicts.Any(static dict => dict is { Type: DictType.NonspecificWordYomichan or DictType.NonspecificYomichan or DictType.NonspecificWordNazeka or DictType.NonspecificNazeka, Active: true, Options.UseDB.Value: true });
        AtLeastOneKanjiDictIsActive = DBIsUsedForAtLeastOneDict && dicts.Any(static dict => dict is { Type: DictType.Kanjidic or DictType.NonspecificKanjiYomichan or DictType.NonspecificKanjiNazeka or DictType.NonspecificKanjiWithWordSchemaYomichan, Active: true });
        DBIsUsedForJmdict = SingleDictTypeDicts[DictType.JMdict] is { Active: true, Options.UseDB.Value: true };
    }
}
