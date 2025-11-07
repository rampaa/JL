using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json;
using JL.Core.Config;
using JL.Core.Dicts.CustomNameDict;
using JL.Core.Dicts.CustomWordDict;
using JL.Core.Dicts.EPWING.Nazeka;
using JL.Core.Dicts.EPWING.Yomichan;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.JMdict;
using JL.Core.Dicts.JMnedict;
using JL.Core.Dicts.KANJIDIC;
using JL.Core.Dicts.KanjiDict;
using JL.Core.Dicts.Options;
using JL.Core.Dicts.PitchAccent;
using JL.Core.Frontend;
using JL.Core.Utilities;
using JL.Core.Utilities.Bool;
using JL.Core.Utilities.Database;
using JL.Core.WordClass;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts;

public static class DictUtils
{
    public static bool DictsReady { get; private set; } // = false;
    public static readonly Dictionary<string, Dict> Dicts = new(StringComparer.OrdinalIgnoreCase);
    internal static IDictionary<string, IList<JmdictWordClass>> WordClassDictionary { get; set; } = new Dictionary<string, IList<JmdictWordClass>>(55000, StringComparer.Ordinal); // 2022/10/29: 48909, 2023/04/22: 49503, 2023/07/28: 49272
    private static readonly Uri s_jmdictUrl = new("https://www.edrdg.org/pub/Nihongo/JMdict_e.gz");
    private static readonly Uri s_jmnedictUrl = new("https://www.edrdg.org/pub/Nihongo/JMnedict.xml.gz");
    private static readonly Uri s_kanjidicUrl = new("https://www.edrdg.org/kanjidic/kanjidic2.xml.gz");

    internal static bool DBIsUsedForAtLeastOneDict { get; private set; }
    internal static bool DBIsUsedForAtLeastOneYomichanDict { get; private set; }
    internal static bool DBIsUsedForAtLeastOneNazekaDict { get; private set; }
    internal static bool DBIsUsedForJmdict { get; private set; }
    internal static bool JmdictIsActive { get; private set; }
    internal static bool AnyCustomWordDictIsActive { get; private set; }
    internal static bool DBIsUsedForAtLeastOneWordDict { get; private set; }
    internal static bool AtLeastOneKanjiDictIsActive { get; private set; }
    internal static bool DBIsUsedForAtLeastOneYomichanOrNazekaWordDict { get; private set; }

    public static CancellationTokenSource? ProfileCustomWordsCancellationTokenSource { get; private set; }
    public static CancellationTokenSource? ProfileCustomNamesCancellationTokenSource { get; private set; }

    public static readonly Dictionary<string, Dict> BuiltInDicts = new(7, StringComparer.OrdinalIgnoreCase)
    {
        {
            nameof(DictType.ProfileCustomWordDictionary), new Dict(DictType.ProfileCustomWordDictionary,
                "Custom Word Dictionary (Profile)",
                Path.Join(ProfileUtils.ProfileFolderPath, "Default_Custom_Words.txt"),
                true, -1, 128,
                new DictOptions(
                    new UseDBOption(false),
                    new NoAllOption(false),
                    new NewlineBetweenDefinitionsOption(true)),
                autoUpdatable: false,
                url: null,
                revision: null)
        },
        {
            nameof(DictType.ProfileCustomNameDictionary), new Dict(DictType.ProfileCustomNameDictionary,
                "Custom Name Dictionary (Profile)",
                Path.Join(ProfileUtils.ProfileFolderPath, "Default_Custom_Names.txt"),
                true, 0, 128,
                new DictOptions(
                    new UseDBOption(false),
                    new NoAllOption(false)),
                autoUpdatable: false,
                url: null,
                revision: null)
        },
        {
            nameof(DictType.CustomWordDictionary), new Dict(DictType.CustomWordDictionary,
                "Custom Word Dictionary",
                Path.Join(AppInfo.ResourcesPath, "custom_words.txt"),
                true, 1, 128,
                new DictOptions(
                    new UseDBOption(false),
                    new NoAllOption(false),
                    new NewlineBetweenDefinitionsOption(true)),
                autoUpdatable: false,
                url: null,
                revision: null)
        },
        {
            nameof(DictType.CustomNameDictionary), new Dict(DictType.CustomNameDictionary,
                "Custom Name Dictionary",
                Path.Join(AppInfo.ResourcesPath, "custom_names.txt"),
                true, 2, 128,
                new DictOptions(
                    new UseDBOption(false),
                    new NoAllOption(false)),
                autoUpdatable: false,
                url: null,
                revision: null)
        },
        {
            nameof(DictType.JMdict), new Dict(DictType.JMdict, nameof(DictType.JMdict),
                Path.Join(AppInfo.ResourcesPath, $"{nameof(DictType.JMdict)}.xml"),
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
                ),
                autoUpdatable: true,
                url: s_jmdictUrl,
                revision: null)
        },
        {
            nameof(DictType.Kanjidic), new Dict(DictType.Kanjidic, nameof(DictType.Kanjidic),
                Path.Join(AppInfo.ResourcesPath, "kanjidic2.xml"),
                true, 4, 13108,
                new DictOptions(
                    new UseDBOption(true),
                    new NoAllOption(false),
                    autoUpdateAfterNDays: new AutoUpdateAfterNDaysOption(0)),
                autoUpdatable: true,
                url: s_kanjidicUrl,
                revision: null)
        },
        {
            nameof(DictType.JMnedict), new Dict(DictType.JMnedict, nameof(DictType.JMnedict),
                Path.Join(AppInfo.ResourcesPath, $"{nameof(DictType.JMnedict)}.xml"),
                true, 5, 700000,
                new DictOptions(
                    new UseDBOption(true),
                    new NoAllOption(false),
                    new NewlineBetweenDefinitionsOption(true),
                    autoUpdateAfterNDays: new AutoUpdateAfterNDaysOption(0)),
                autoUpdatable: true,
                url: s_jmnedictUrl,
                revision: null)
        }
    };

    public static readonly Dictionary<DictType, Dict> SingleDictTypeDicts = new(8);

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
        bool rebuildingAnyDB = false;
        ConcurrentBag<Dict> dictsToBeRemoved = [];

        Dictionary<string, string> dictDBPaths = new(StringComparer.Ordinal);

        List<Task> tasks = [];

        Dict[] dicts = Dicts.Values.ToArray();

        CheckSingleDictActiveness();
        CheckDBUsageForDicts(dicts);

        int customDictionaryTaskCount = 0;
        AtomicBool anyCustomDictionaryTaskIsActuallyUsed = new(false);

        foreach (Dict dict in dicts)
        {
            switch (dict.Type)
            {
                case DictType.JMdict:
                    LoadJmdict(dict, tasks, dictDBPaths, ref rebuildingAnyDB, ref dictCleared);
                    break;

                case DictType.JMnedict:
                    LoadJmnedict(dict, tasks, dictDBPaths, ref rebuildingAnyDB, ref dictCleared);
                    break;

                case DictType.Kanjidic:
                    LoadKanjidic(dict, tasks, dictDBPaths, ref rebuildingAnyDB, ref dictCleared);
                    break;

                case DictType.NonspecificWordYomichan:
                case DictType.NonspecificKanjiWithWordSchemaYomichan:
                case DictType.NonspecificNameYomichan:
                case DictType.NonspecificYomichan:
                    LoadYomichanDict(dict, tasks, dictDBPaths, ref rebuildingAnyDB, ref dictCleared);
                    break;

                case DictType.NonspecificKanjiYomichan:
                    LoadYomichanKanjiDict(dict, tasks, dictDBPaths, dictsToBeRemoved, ref rebuildingAnyDB, ref dictCleared);
                    break;

                case DictType.CustomWordDictionary:
                case DictType.ProfileCustomWordDictionary:
                    LoadCustomWordDict(dict, tasks, anyCustomDictionaryTaskIsActuallyUsed, ref customDictionaryTaskCount, ref dictCleared);
                    break;

                case DictType.CustomNameDictionary:
                case DictType.ProfileCustomNameDictionary:
                    LoadCustomNameDict(dict, tasks, anyCustomDictionaryTaskIsActuallyUsed, ref customDictionaryTaskCount, ref dictCleared);
                    break;

                case DictType.NonspecificWordNazeka:
                case DictType.NonspecificKanjiNazeka:
                case DictType.NonspecificNameNazeka:
                case DictType.NonspecificNazeka:
                    LoadNazekaDict(dict, tasks, dictDBPaths, dictsToBeRemoved, ref rebuildingAnyDB, ref dictCleared);
                    break;

                case DictType.PitchAccentYomichan:
                    LoadYomichanPitchAccentDict(dict, tasks, dictDBPaths, dictsToBeRemoved, ref rebuildingAnyDB, ref dictCleared);
                    break;

                default:
                {
                    LoggerManager.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(DictType), nameof(DictUtils), nameof(LoadDictionaries), dict.Type);
                    FrontendManager.Frontend.Alert(AlertLevel.Error, $"Invalid dictionary type: {dict.Type}");
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
                    FrontendManager.Frontend.Alert(AlertLevel.Information, "Rebuilding some databases because their schemas are out of date...");
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);

                if (!dictsToBeRemoved.IsEmpty)
                {
                    foreach (Dict dict in dictsToBeRemoved)
                    {
                        _ = Dicts.Remove(dict.Name);
                        _ = SingleDictTypeDicts.Remove(dict.Type);

                        string dbPath = DBUtils.GetFreqDBPath(dict.Name);
                        if (File.Exists(dbPath))
                        {
                            DBUtils.DeleteDB(dbPath);
                        }
                    }

                    IOrderedEnumerable<Dict> orderedDicts = Dicts.Values.OrderBy(static d => d.Priority);
                    int priority = 1;

                    foreach (Dict dict in orderedDicts)
                    {
                        dict.Priority = priority;
                        ++priority;
                    }
                }
            }

            Dict[] dictsSnapshot = Dicts.Values.ToArray();
            CheckSingleDictActiveness();
            CheckDBUsageForDicts(dictsSnapshot);

            if (dictsSnapshot.All(static d => !d.Updating)
                && (tasks.Count > customDictionaryTaskCount || anyCustomDictionaryTaskIsActuallyUsed))
            {
                FrontendManager.Frontend.Alert(AlertLevel.Success, "Finished loading dictionaries");
            }

            ProfileCustomWordsCancellationTokenSource.Dispose();
            ProfileCustomWordsCancellationTokenSource = null;

            ProfileCustomNamesCancellationTokenSource.Dispose();
            ProfileCustomNamesCancellationTokenSource = null;
        }

        DictsReady = true;
    }

    private static DBState PrepareDictDB(Dict dict, Dictionary<string, string> dictDBPaths, int dbVersion, ref bool rebuildingAnyDB)
    {
        bool useDB = dict.Options.UseDB.Value;
        string dbPath = DBUtils.GetDictDBPath(dict.Name);
        string dbJournalPath = $"{dbPath}-journal";
        bool dbExists = File.Exists(dbPath);
        bool dbExisted = dbExists;
        bool dbJournalExists = File.Exists(dbJournalPath);

        if (!dict.Updating)
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

        dict.Ready = false;

        if (useDB && !DBUtils.DictDBPaths.ContainsKey(dict.Name))
        {
            dictDBPaths.Add(dict.Name, dbPath);
        }

        if (dbExists && DBUtils.CheckIfDBSchemaIsOutOfDate(dbVersion, dbPath))
        {
            DBUtils.DeleteDB(dbPath);
            dbExists = false;
            rebuildingAnyDB = true;
        }

        return new DBState(useDB, dbExists, dbExisted);
    }

    private static void LoadJmdict(Dict dict, List<Task> tasks, Dictionary<string, string> dictDBPaths, ref bool rebuildingAnyDB, ref bool dictCleared)
    {
        if (dict.Updating)
        {
            return;
        }

        DBState dBContext = PrepareDictDB(dict, dictDBPaths, JmdictDBManager.Version, ref rebuildingAnyDB);

        bool useDB = dBContext.UseDB;
        bool dbExists = dBContext.DBExists;
        bool loadFromDB = dbExists && !dBContext.UseDB;

        if (dict is { Active: true, Contents.Count: 0 } && (!useDB || !dbExists))
        {
            bool dbExisted = dBContext.DBExisted;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    // 2022/05/11: 394949, 2022/08/15: 398303, 2023/04/22: 403739, 2023/12/16: 419334, 2024/02/22: 421519
                    dict.Contents = new Dictionary<string, IList<IDictRecord>>(dict.Size > 0 ? dict.Size : 450000, StringComparer.Ordinal);

                    if (loadFromDB)
                    {
                        JmdictDBManager.LoadFromDB(dict);
                        dict.Size = dict.Contents.Count;
                    }
                    else
                    {
                        await JmdictLoader.Load(dict).ConfigureAwait(false);
                        if (dict.Active)
                        {
                            dict.Size = dict.Contents.Count;

                            if (!dbExists && (useDB || dbExisted))
                            {
                                JmdictDBManager.CreateDB(dict.Name);
                                JmdictDBManager.InsertRecordsToDB(dict);

                                if (useDB)
                                {
                                    dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                                }
                            }
                        }
                    }

                    dict.Ready = true;
                }
                catch (Exception ex)
                {
                    string fullDictPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
                    LoggerManager.Logger.Error(ex, "Couldn't import '{DictType}'-'{DictName}' from '{FullDictPath}'", dict.Type.GetDescription(), dict.Name, fullDictPath);
                    FrontendManager.Frontend.Alert(AlertLevel.Error, $"Couldn't import {dict.Name}");
                    File.Delete(fullDictPath);
                    await ResourceUpdater.UpdateJmdict(true, false).ConfigureAwait(false);
                }
            }));
        }

        else if (dict.Contents.Count > 0 && (!dict.Active || dBContext.UseDB))
        {
            if (dBContext.UseDB && !dbExists)
            {
                tasks.Add(Task.Run(() =>
                {
                    JmdictDBManager.CreateDB(dict.Name);
                    JmdictDBManager.InsertRecordsToDB(dict);
                    dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                    dict.Ready = true;
                }));
            }
            else
            {
                dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                dict.Ready = true;
            }

            dictCleared = true;
        }

        else
        {
            dict.Ready = true;
        }
    }

    private static void LoadJmnedict(Dict dict, List<Task> tasks, Dictionary<string, string> dictDBPaths, ref bool rebuildingAnyDB, ref bool dictCleared)
    {
        if (dict.Updating)
        {
            return;
        }

        DBState dBContext = PrepareDictDB(dict, dictDBPaths, JmnedictDBManager.Version, ref rebuildingAnyDB);

        bool useDB = dBContext.UseDB;
        bool dbExists = dBContext.DBExists;
        // loadFromDB = dbExists && !useDB;

        if (dict is { Active: true, Contents.Count: 0 } && (!useDB || !dbExists))
        {
            bool dbExisted = dBContext.DBExisted;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    // 2022/05/11: 608833, 2022/08/15: 609117, 2023/04/22: 609055, 2023/12/16: 609238, 2024/02/22: 609265
                    dict.Contents = new Dictionary<string, IList<IDictRecord>>(dict.Size > 0 ? dict.Size : 620000, StringComparer.Ordinal);

                    // We don't load JMnedict from DB because it is slower and allocates more memory for JMnedict for some reason
                    await JmnedictLoader.Load(dict).ConfigureAwait(false);
                    if (dict.Active)
                    {
                        dict.Size = dict.Contents.Count;

                        if (!dbExists && (useDB || dbExisted))
                        {
                            JmnedictDBManager.CreateDB(dict.Name);
                            JmnedictDBManager.InsertRecordsToDB(dict);

                            if (useDB)
                            {
                                dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                            }
                        }
                    }

                    dict.Ready = true;
                }
                catch (Exception ex)
                {
                    string fullDictPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
                    LoggerManager.Logger.Error(ex, "Couldn't import '{DictType}'-'{DictName}' from '{FullDictPath}'", dict.Type.GetDescription(), dict.Name, fullDictPath);
                    FrontendManager.Frontend.Alert(AlertLevel.Error, $"Couldn't import {dict.Name}");
                    File.Delete(fullDictPath);
                    await ResourceUpdater.UpdateJmnedict(true, false).ConfigureAwait(false);
                }
            }));
        }

        else if (dict.Contents.Count > 0 && (!dict.Active || useDB))
        {
            if (useDB && !dbExists)
            {
                tasks.Add(Task.Run(() =>
                {
                    JmnedictDBManager.CreateDB(dict.Name);
                    JmnedictDBManager.InsertRecordsToDB(dict);
                    dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                    dict.Ready = true;
                }));
            }
            else
            {
                dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                dict.Ready = true;
            }

            dictCleared = true;
        }

        else
        {
            dict.Ready = true;
        }
    }

    private static void LoadKanjidic(Dict dict, List<Task> tasks, Dictionary<string, string> dictDBPaths, ref bool rebuildingAnyDB, ref bool dictCleared)
    {
        if (dict.Updating)
        {
            return;
        }

        DBState dBContext = PrepareDictDB(dict, dictDBPaths, KanjidicDBManager.Version, ref rebuildingAnyDB);

        bool useDB = dBContext.UseDB;
        bool dbExists = dBContext.DBExists;
        bool loadFromDB = dbExists && !dBContext.UseDB;

        if (dict is { Active: true, Contents.Count: 0 } && (!useDB || !dbExists))
        {
            bool dbExisted = dBContext.DBExisted;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    // 2022/05/11: 13108, 2023/12/16: 13108, 2024/02/02 13108
                    dict.Contents = new Dictionary<string, IList<IDictRecord>>(dict.Size > 0 ? dict.Size : 13108, StringComparer.Ordinal);

                    if (loadFromDB)
                    {
                        KanjidicDBManager.LoadFromDB(dict);
                        dict.Size = dict.Contents.Count;
                    }
                    else
                    {
                        await KanjidicLoader.Load(dict).ConfigureAwait(false);
                        if (dict.Active)
                        {
                            dict.Size = dict.Contents.Count;

                            if (!dbExists && (useDB || dbExisted))
                            {
                                KanjidicDBManager.CreateDB(dict.Name);
                                KanjidicDBManager.InsertRecordsToDB(dict);

                                if (useDB)
                                {
                                    dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                                }
                            }
                        }
                    }

                    dict.Ready = true;
                }
                catch (Exception ex)
                {
                    string fullDictPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
                    LoggerManager.Logger.Error(ex, "Couldn't import '{DictType}'-'{DictName}' from '{FullDictPath}'", dict.Type.GetDescription(), dict.Name, fullDictPath);
                    FrontendManager.Frontend.Alert(AlertLevel.Error, $"Couldn't import {dict.Name}");
                    File.Delete(fullDictPath);
                    await ResourceUpdater.UpdateKanjidic(true, false).ConfigureAwait(false);
                }
            }));
        }

        else if (dict.Contents.Count > 0 && (!dict.Active || useDB))
        {
            if (useDB && !dbExists)
            {
                tasks.Add(Task.Run(() =>
                {
                    KanjidicDBManager.CreateDB(dict.Name);
                    KanjidicDBManager.InsertRecordsToDB(dict);
                    dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                    dict.Ready = true;
                }));
            }
            else
            {
                dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                dict.Ready = true;
            }

            dictCleared = true;
        }

        else
        {
            dict.Ready = true;
        }
    }

    private static void LoadYomichanDict(Dict dict, List<Task> tasks, Dictionary<string, string> dictDBPaths, ref bool rebuildingAnyDB, ref bool dictCleared)
    {
        if (dict.Updating)
        {
            return;
        }

        DBState dBContext = PrepareDictDB(dict, dictDBPaths, EpwingYomichanDBManager.Version, ref rebuildingAnyDB);

        bool useDB = dBContext.UseDB;
        bool dbExists = dBContext.DBExists;
        bool loadFromDB = dbExists && !dBContext.UseDB;

        if (dict is { Active: true, Contents.Count: 0 } && (!useDB || !dbExists))
        {
            bool dbExisted = dBContext.DBExisted;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    int dictSize = dict.Size > 0
                        ? dict.Size
                        : 250000;

                    dict.Contents = new Dictionary<string, IList<IDictRecord>>(dictSize, StringComparer.Ordinal);

                    if (loadFromDB)
                    {
                        EpwingYomichanDBManager.LoadFromDB(dict);
                        dict.Size = dict.Contents.Count;
                    }
                    else
                    {
                        await EpwingYomichanLoader.Load(dict).ConfigureAwait(false);
                        dict.Size = dict.Contents.Count;

                        if (!dbExists && (useDB || dbExisted))
                        {
                            EpwingYomichanDBManager.CreateDB(dict.Name);
                            EpwingYomichanDBManager.InsertRecordsToDB(dict);

                            if (useDB)
                            {
                                dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                            }
                        }
                    }

                    dict.Ready = true;
                }

                catch (Exception ex)
                {
                    string fullDictPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
                    LoggerManager.Logger.Error(ex, "Couldn't import '{DictType}'-'{DictName}' from '{FullDictPath}'", dict.Type.GetDescription(), dict.Name, fullDictPath);
                    FrontendManager.Frontend.Alert(AlertLevel.Error, $"Couldn't import {dict.Name}");
                }
            }));
        }

        else if (dict.Contents.Count > 0 && (!dict.Active || useDB))
        {
            if (useDB && !dbExists)
            {
                tasks.Add(Task.Run(() =>
                {
                    EpwingYomichanDBManager.CreateDB(dict.Name);
                    EpwingYomichanDBManager.InsertRecordsToDB(dict);
                    dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                    dict.Ready = true;
                }));
            }
            else
            {
                dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                dict.Ready = true;
            }

            dictCleared = true;
        }

        else
        {
            dict.Ready = true;
        }
    }

    private static void LoadYomichanKanjiDict(Dict dict, List<Task> tasks, Dictionary<string, string> dictDBPaths, ConcurrentBag<Dict> dictsToBeRemoved, ref bool rebuildingAnyDB, ref bool dictCleared)
    {
        if (dict.Updating)
        {
            return;
        }

        DBState dBContext = PrepareDictDB(dict, dictDBPaths, YomichanKanjiDBManager.Version, ref rebuildingAnyDB);

        bool useDB = dBContext.UseDB;
        bool dbExists = dBContext.DBExists;
        bool loadFromDB = dbExists && !dBContext.UseDB;

        if (dict is { Active: true, Contents.Count: 0 } && (!useDB || !dbExists))
        {
            bool dbExisted = dBContext.DBExisted;
            tasks.Add(Task.Run(async () =>
            {
                dict.Contents = new Dictionary<string, IList<IDictRecord>>(dict.Size > 0 ? dict.Size : 250000, StringComparer.Ordinal);

                try
                {
                    if (loadFromDB)
                    {
                        YomichanKanjiDBManager.LoadFromDB(dict);
                        dict.Size = dict.Contents.Count;
                    }
                    else
                    {
                        await YomichanKanjiLoader.Load(dict).ConfigureAwait(false);
                        dict.Size = dict.Contents.Count;

                        if (!dbExists && (useDB || dbExisted))
                        {
                            YomichanKanjiDBManager.CreateDB(dict.Name);
                            YomichanKanjiDBManager.InsertRecordsToDB(dict);

                            if (useDB)
                            {
                                dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                            }
                        }
                    }

                    dict.Ready = true;
                }

                catch (Exception ex)
                {
                    string fullDictPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
                    LoggerManager.Logger.Error(ex, "Couldn't import '{DictType}'-'{DictName}' from '{FullDictPath}'", dict.Type.GetDescription(), dict.Name, fullDictPath);
                    FrontendManager.Frontend.Alert(AlertLevel.Error, $"Couldn't import {dict.Name}");
                    dictsToBeRemoved.Add(dict);
                }
            }));
        }

        else if (dict.Contents.Count > 0 && (!dict.Active || useDB))
        {
            if (useDB && !dbExists)
            {
                tasks.Add(Task.Run(() =>
                {
                    YomichanKanjiDBManager.CreateDB(dict.Name);
                    YomichanKanjiDBManager.InsertRecordsToDB(dict);
                    dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                    dict.Ready = true;
                }));
            }
            else
            {
                dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                dict.Ready = true;
            }

            dictCleared = true;
        }

        else
        {
            dict.Ready = true;
        }
    }

    private static void LoadCustomWordDict(Dict dict, List<Task> tasks, AtomicBool anyCustomDictionaryTaskIsActuallyUsed, ref int customDictionaryTaskCount, ref bool dictCleared)
    {
        if (dict is { Active: true, Contents.Count: 0 })
        {
            ++customDictionaryTaskCount;

            tasks.Add(Task.Run(() =>
            {
                int size = dict.Size > 0
                    ? dict.Size
                    : dict.Type is DictType.CustomWordDictionary
                        ? 1024
                        : 256;

                dict.Contents = new Dictionary<string, IList<IDictRecord>>(size, StringComparer.Ordinal);

                Debug.Assert(ProfileCustomWordsCancellationTokenSource is not null);

                CustomWordLoader.Load(dict,
                    dict.Type is DictType.CustomWordDictionary
                        ? CancellationToken.None
                        : ProfileCustomWordsCancellationTokenSource.Token);

                dict.Size = dict.Contents.Count;
                if (dict.Size > 0)
                {
                    anyCustomDictionaryTaskIsActuallyUsed.SetTrue();
                }

                dict.Ready = true;
            }));
        }

        else if (dict is { Active: false, Contents.Count: > 0 })
        {
            dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
            dictCleared = true;
            dict.Ready = true;
        }

        else
        {
            dict.Ready = true;
        }
    }

    private static void LoadCustomNameDict(Dict dict, List<Task> tasks, AtomicBool anyCustomDictionaryTaskIsActuallyUsed, ref int customDictionaryTaskCount, ref bool dictCleared)
    {
        if (dict is { Active: true, Contents.Count: 0 })
        {
            ++customDictionaryTaskCount;
            tasks.Add(Task.Run(() =>
            {
                int size = dict.Size is not 0
                    ? dict.Size
                    : dict.Type is DictType.CustomNameDictionary
                        ? 1024
                        : 256;

                dict.Contents = new Dictionary<string, IList<IDictRecord>>(size, StringComparer.Ordinal);

                Debug.Assert(ProfileCustomNamesCancellationTokenSource is not null);

                CustomNameLoader.Load(dict,
                    dict.Type is DictType.CustomNameDictionary
                        ? CancellationToken.None
                        : ProfileCustomNamesCancellationTokenSource.Token);

                dict.Size = dict.Contents.Count;
                if (dict.Size > 0)
                {
                    anyCustomDictionaryTaskIsActuallyUsed.SetTrue();
                }

                dict.Ready = true;
            }));
        }

        else if (dict is { Active: false, Contents.Count: > 0 })
        {
            dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
            dictCleared = true;
            dict.Ready = true;
        }

        else
        {
            dict.Ready = true;
        }
    }

    private static void LoadNazekaDict(Dict dict, List<Task> tasks, Dictionary<string, string> dictDBPaths, ConcurrentBag<Dict> dictsToBeRemoved, ref bool rebuildingAnyDB, ref bool dictCleared)
    {
        DBState dBContext = PrepareDictDB(dict, dictDBPaths, EpwingNazekaDBManager.Version, ref rebuildingAnyDB);

        bool useDB = dBContext.UseDB;
        bool dbExists = dBContext.DBExists;
        bool loadFromDB = dbExists && !dBContext.UseDB;

        if (dict is { Active: true, Contents.Count: 0 } && (!useDB || !dbExists))
        {
            bool dbExisted = dBContext.DBExisted;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    int size = dict.Size > 0
                        ? dict.Size
                        : 250000;

                    dict.Contents = new Dictionary<string, IList<IDictRecord>>(size, StringComparer.Ordinal);

                    if (loadFromDB)
                    {
                        EpwingNazekaDBManager.LoadFromDB(dict);
                        dict.Size = dict.Contents.Count;
                    }
                    else
                    {
                        await EpwingNazekaLoader.Load(dict).ConfigureAwait(false);
                        dict.Size = dict.Contents.Count;

                        if (!dbExists && (useDB || dbExisted))
                        {
                            EpwingNazekaDBManager.CreateDB(dict.Name);
                            EpwingNazekaDBManager.InsertRecordsToDB(dict);

                            if (useDB)
                            {
                                dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                            }
                        }
                    }

                    dict.Ready = true;
                }

                catch (Exception ex)
                {
                    string fullDictPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
                    LoggerManager.Logger.Error(ex, "Couldn't import '{DictType}'-'{DictName}' from '{FullDictPath}'", dict.Type.GetDescription(), dict.Name, fullDictPath);
                    FrontendManager.Frontend.Alert(AlertLevel.Error, $"Couldn't import {dict.Name}");
                    dictsToBeRemoved.Add(dict);
                }
            }));
        }

        else if (dict.Contents.Count > 0 && (!dict.Active || useDB))
        {
            if (useDB && !dbExists)
            {
                tasks.Add(Task.Run(() =>
                {
                    EpwingNazekaDBManager.CreateDB(dict.Name);
                    EpwingNazekaDBManager.InsertRecordsToDB(dict);
                    dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                    dict.Ready = true;
                }));
            }
            else
            {
                dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                dict.Ready = true;
            }

            dictCleared = true;
        }

        else
        {
            dict.Ready = true;
        }
    }

    private static void LoadYomichanPitchAccentDict(Dict dict, List<Task> tasks, Dictionary<string, string> dictDBPaths, ConcurrentBag<Dict> dictsToBeRemoved, ref bool rebuildingAnyDB, ref bool dictCleared)
    {
        if (dict.Updating)
        {
            return;
        }

        DBState dBContext = PrepareDictDB(dict, dictDBPaths, YomichanPitchAccentDBManager.Version, ref rebuildingAnyDB);

        bool useDB = dBContext.UseDB;
        bool dbExists = dBContext.DBExists;
        bool loadFromDB = dbExists && !dBContext.UseDB;

        if (dict is { Active: true, Contents.Count: 0 } && (!useDB || !dbExists))
        {
            bool dbExisted = dBContext.DBExisted;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    dict.Contents = new Dictionary<string, IList<IDictRecord>>(dict.Size > 0 ? dict.Size : 434991, StringComparer.Ordinal);

                    if (loadFromDB)
                    {
                        YomichanPitchAccentDBManager.LoadFromDB(dict);
                        dict.Size = dict.Contents.Count;
                    }
                    else
                    {
                        await YomichanPitchAccentLoader.Load(dict).ConfigureAwait(false);
                        dict.Size = dict.Contents.Count;

                        if (!dbExists && (useDB || dbExisted))
                        {
                            YomichanPitchAccentDBManager.CreateDB(dict.Name);
                            YomichanPitchAccentDBManager.InsertRecordsToDB(dict);

                            if (useDB)
                            {
                                dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                            }
                        }
                    }

                    dict.Ready = true;
                }

                catch (Exception ex)
                {
                    string fullDictPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
                    LoggerManager.Logger.Error(ex, "Couldn't import '{DictType}'-'{DictName}' from '{FullDictPath}'", dict.Type.GetDescription(), dict.Name, fullDictPath);
                    FrontendManager.Frontend.Alert(AlertLevel.Error, $"Couldn't import {dict.Name}");
                    dictsToBeRemoved.Add(dict);
                }
            }));
        }

        else if (dict.Contents.Count > 0 && (!dict.Active || useDB))
        {
            if (useDB && !dbExists)
            {
                tasks.Add(Task.Run(() =>
                {
                    YomichanPitchAccentDBManager.CreateDB(dict.Name);
                    YomichanPitchAccentDBManager.InsertRecordsToDB(dict);
                    dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                    dict.Ready = true;
                }));
            }
            else
            {
                dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                dict.Ready = true;
            }

            dictCleared = true;
        }

        else
        {
            dict.Ready = true;
        }
    }

    public static async Task CreateDefaultDictsConfig()
    {
        _ = Directory.CreateDirectory(AppInfo.ConfigPath);

        FileStream fileStream = new(Path.Join(AppInfo.ConfigPath, "dicts.json"), FileStreamOptionsPresets.AsyncCreateFso);
        await using (fileStream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(fileStream, BuiltInDicts, JsonOptions.s_jsoIgnoringWhenWritingNullWithEnumConverterAndIndentation).ConfigureAwait(false);
        }
    }

    public static async Task SerializeDicts()
    {
        FileStream fileStream = new(Path.Join(AppInfo.ConfigPath, "dicts.json"), FileStreamOptionsPresets.AsyncCreateFso);
        await using (fileStream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(fileStream, Dicts, JsonOptions.s_jsoIgnoringWhenWritingNullWithEnumConverterAndIndentation).ConfigureAwait(false);
        }
    }

    internal static async Task DeserializeDicts()
    {
        FileStream dictStream = new(Path.Join(AppInfo.ConfigPath, "dicts.json"), FileStreamOptionsPresets.AsyncReadFso);
        await using (dictStream.ConfigureAwait(false))
        {
            Dictionary<string, Dict>? deserializedDicts = await JsonSerializer
                .DeserializeAsync<Dictionary<string, Dict>>(dictStream, JsonOptions.s_jsoWithEnumConverter).ConfigureAwait(false);

            if (deserializedDicts is not null)
            {
                foreach (Dict dict in BuiltInDicts.Values)
                {
                    if (deserializedDicts.Values.All(d => d.Type != dict.Type))
                    {
                        deserializedDicts.Add(dict.Name, dict);
                    }
                }

                IOrderedEnumerable<Dict> orderedDicts = deserializedDicts.Values.OrderBy(static dict => dict.Priority);

                int priority = 1;
                foreach (Dict dict in orderedDicts)
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

                    if (dict.Type is DictType.JMdict or DictType.Kanjidic or DictType.JMnedict)
                    {
                        dict.AutoUpdatable = true;
                        if (dict.Type is DictType.JMdict)
                        {
                            dict.Url = s_jmdictUrl;
                        }
                        else if (dict.Type is DictType.Kanjidic)
                        {
                            dict.Url = s_kanjidicUrl;
                        }
                        else if (dict.Type is DictType.JMnedict)
                        {
                            dict.Url = s_jmnedictUrl;
                        }
                    }

                    if (dict.Revision is null && YomichanDictTypes.Contains(dict.Type))
                    {
                        await EpwingYomichanUtils.UpdateRevisionInfo(dict).ConfigureAwait(false);
                    }

                    InitDictOptions(dict);

                    dict.Path = PathUtils.GetPortablePath(dict.Path);
                    Dicts.Add(dict.Name, dict);
                }
            }
            else
            {
                FrontendManager.Frontend.Alert(AlertLevel.Error, "Couldn't load Config/dicts.json");
                throw new SerializationException("Couldn't load Config/dicts.json");
            }
        }
    }

    private static void InitDictOptions(Dict dict)
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
            if (ShowImagesOption.ValidDictTypes.Contains(dict.Type))
            {
                dict.Options.ShowImages ??= new ShowImagesOption(true);
            }
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
            if (AutoUpdateAfterNDaysOption.ValidDictTypes.Contains(dict.Type))
            {
                dict.Options.AutoUpdateAfterNDays ??= new AutoUpdateAfterNDaysOption(0);
            }
        }
    }

    private static void CheckSingleDictActiveness()
    {
        JmdictIsActive = SingleDictTypeDicts.TryGetValue(DictType.JMdict, out Dict? jmdict) && jmdict.Active;
        AnyCustomWordDictIsActive = (SingleDictTypeDicts.TryGetValue(DictType.CustomWordDictionary, out Dict? customWordDict) && customWordDict.Active)
            || (SingleDictTypeDicts.TryGetValue(DictType.ProfileCustomWordDictionary, out Dict? profileCustomWordDict) && profileCustomWordDict.Active);
    }

    private static void CheckDBUsageForDicts(Dict[] dicts)
    {
        DBIsUsedForAtLeastOneDict = false;
        DBIsUsedForAtLeastOneWordDict = false;
        DBIsUsedForAtLeastOneYomichanDict = false;
        DBIsUsedForAtLeastOneNazekaDict = false;
        DBIsUsedForAtLeastOneYomichanOrNazekaWordDict = false;
        AtLeastOneKanjiDictIsActive = false;
        DBIsUsedForJmdict = false;

        foreach (Dict dict in dicts)
        {
            if (dict.Active)
            {
                if (KanjiDictTypes.Contains(dict.Type))
                {
                    AtLeastOneKanjiDictIsActive = true;
                }

                if (dict.Options.UseDB.Value)
                {
                    DBIsUsedForAtLeastOneDict = true;

                    if (dict.Type is DictType.JMdict)
                    {
                        DBIsUsedForJmdict = true;
                    }

                    if (dict.Type is DictType.JMdict or DictType.NonspecificWordYomichan or DictType.NonspecificWordNazeka)
                    {
                        DBIsUsedForAtLeastOneWordDict = true;
                    }

                    if (s_yomichanWordAndNameDictTypeSet.Contains(dict.Type))
                    {
                        DBIsUsedForAtLeastOneYomichanDict = true;
                    }

                    if (s_nazekaWordAndNameDictTypeSet.Contains(dict.Type))
                    {
                        DBIsUsedForAtLeastOneNazekaDict = true;
                    }

                    if (dict.Type is DictType.NonspecificWordYomichan or DictType.NonspecificYomichan or DictType.NonspecificWordNazeka or DictType.NonspecificNazeka)
                    {
                        DBIsUsedForAtLeastOneYomichanOrNazekaWordDict = true;
                    }
                }

                if (DBIsUsedForAtLeastOneDict
                    && DBIsUsedForAtLeastOneWordDict
                    && DBIsUsedForAtLeastOneYomichanDict
                    && DBIsUsedForAtLeastOneNazekaDict
                    && DBIsUsedForAtLeastOneYomichanOrNazekaWordDict
                    && AtLeastOneKanjiDictIsActive
                    && DBIsUsedForJmdict)
                {
                    return;
                }
            }
        }
    }
}
