using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
using JL.Core.Lookup;
using JL.Core.Utilities;
using JL.Core.Utilities.Bool;
using JL.Core.Utilities.Database;
using JL.Core.WordClass;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts;

public static class DictUtils
{
    internal delegate Task Load(Dict dict);

    internal delegate Task ImportFromDisk(Dict dict);
    private delegate void ImportFromMemory(Dict dict);
    private delegate void LoadFromDB(Dict dict);
    private delegate void HandleLeftOvers(string fullPath);
    internal delegate void CreateDB(string dbPath);
    private delegate int GetMaxSearchKeyLength(SqliteConnection connection);

    public static readonly string CustomWordDictPath = Path.Join(AppInfo.ResourcesPath, "custom_words.txt");
    public static readonly string CustomNameDictPath = Path.Join(AppInfo.ResourcesPath, "custom_names.txt");
    private static readonly string s_configFilePath = Path.Join(AppInfo.ConfigPath, "dicts.json");
    public static bool DictsReady { get; private set; } // = false;
    public static readonly Dictionary<string, Dict> Dicts = new(StringComparer.OrdinalIgnoreCase);
    internal static IDictionary<string, IList<JmdictWordClass>> WordClassDictionary { get; set; } = new Dictionary<string, IList<JmdictWordClass>>(55000, StringComparer.Ordinal); // 2022/10/29: 48909, 2023/04/22: 49503, 2023/07/28: 49272
    private static readonly Uri s_jmdictUrl = new("https://www.edrdg.org/pub/Nihongo/JMdict_e_NG.gz");
    private static readonly Uri s_jmnedictUrl = new("https://www.edrdg.org/pub/Nihongo/JMnedict.xml.gz");
    private static readonly Uri s_kanjidicUrl = new("https://www.edrdg.org/kanjidic/kanjidic2.xml.gz");

    private static readonly SemaphoreSlim s_loadDictionariesSemaphoreSlim = new(1, 1);

    internal static readonly SearchValues<char> s_invalidCharactersForPrimarySpellings = SearchValues.Create('�', '\n');

    internal static bool DBIsUsedForAtLeastOneDict { get; private set; } = true;
    internal static bool DBIsUsedForAtLeastOneYomichanDict { get; private set; } = true;
    internal static bool DBIsUsedForAtLeastOneNazekaDict { get; private set; } = true;
    internal static bool DBIsUsedForJmdict { get; private set; } = true;
    internal static bool DBIsUsedForJmnedict { get; private set; } = true;
    internal static bool JmdictIsActive { get; private set; } = true;
    internal static bool AnyCustomWordDictIsActive { get; private set; } = true;
    internal static bool DBIsUsedForAtLeastOneWordDict { get; private set; } = true;
    internal static bool AtLeastOneKanjiDictIsActive { get; private set; } = true;
    internal static bool DBIsUsedForAtLeastOneYomichanOrNazekaWordDict { get; private set; } = true;
    internal static bool DBIsUsedForPitchDict { get; private set; } // false;

    public static int MaxSearchKeyLength { get; internal set; }

    internal static Dict? PitchDict { get; private set; }

    private static Dict[] s_allDicts = [];
    private static Dict[] s_nameDicts = [];
    private static Dict[] s_wordDicts = [];
    private static Dict[] s_kanjiDicts = [];
    private static Dict[] s_otherDicts = [];

    public static CancellationTokenSource? ProfileCustomWordsCancellationTokenSource { get; private set; }
    public static CancellationTokenSource? ProfileCustomNamesCancellationTokenSource { get; private set; }

    public static readonly Dictionary<string, Dict> BuiltInDicts = new(7, StringComparer.OrdinalIgnoreCase)
    {
        {
            nameof(DictType.ProfileCustomWordDictionary), new Dict(DictType.ProfileCustomWordDictionary,
                "Custom Word Dictionary (Profile)",
                Path.Join(ProfileUtils.ProfileFolderPath, $"Default_{ProfileUtils.CustomWords}.txt"),
                true, -1, 128,
                new DictOptions(
                    new UseDBOption(false),
                    new NoAllOption(false),
                    new NewlineBetweenDefinitionsOption(true),
                    generateMazegakiVariants: new GenerateMazegakiVariantsOption(false),
                    generateFusejiVariants: new GenerateFusejiVariantsOption(false),
                    maxSearchKeyLengthForFusejiGeneration: new MaxSearchKeyLengthForFusejiGenerationOption(9),
                    maxTotalFusejiCount: new MaxTotalFusejiCountOption(1)),
                maxSearchKeyLength: 0,
                autoUpdatable: false,
                url: null,
                revision: null)
        },
        {
            nameof(DictType.ProfileCustomNameDictionary), new Dict(DictType.ProfileCustomNameDictionary,
                "Custom Name Dictionary (Profile)",
                Path.Join(ProfileUtils.ProfileFolderPath, $"Default_{ProfileUtils.CustomNames}.txt"),
                true, 0, 128,
                new DictOptions(
                    new UseDBOption(false),
                    new NoAllOption(false),
                    showImages: new ShowImagesOption(true),
                    showImageAtBottom: new ShowImageAtBottomOption(true),
                    maxImageWidth: new MaxImageWidthOption(0),
                    maxImageHeight: new MaxImageHeightOption(0),
                    generateFusejiVariants: new GenerateFusejiVariantsOption(false),
                    maxSearchKeyLengthForFusejiGeneration: new MaxSearchKeyLengthForFusejiGenerationOption(9),
                    maxTotalFusejiCount: new MaxTotalFusejiCountOption(1)),
                maxSearchKeyLength: 0,
                autoUpdatable: false,
                url: null,
                revision: null)
        },
        {
            nameof(DictType.CustomWordDictionary), new Dict(DictType.CustomWordDictionary,
                "Custom Word Dictionary",
                CustomWordDictPath,
                true, 1, 128,
                new DictOptions(
                    new UseDBOption(false),
                    new NoAllOption(false),
                    new NewlineBetweenDefinitionsOption(true),
                    generateMazegakiVariants: new GenerateMazegakiVariantsOption(false),
                    generateFusejiVariants: new GenerateFusejiVariantsOption(false),
                    maxSearchKeyLengthForFusejiGeneration: new MaxSearchKeyLengthForFusejiGenerationOption(9),
                    maxTotalFusejiCount: new MaxTotalFusejiCountOption(1)),
                maxSearchKeyLength: 0,
                autoUpdatable: false,
                url: null,
                revision: null)
        },
        {
            nameof(DictType.CustomNameDictionary), new Dict(DictType.CustomNameDictionary,
                "Custom Name Dictionary",
                CustomNameDictPath,
                true, 2, 128,
                new DictOptions(
                    new UseDBOption(false),
                    new NoAllOption(false),
                    showImages: new ShowImagesOption(true),
                    showImageAtBottom: new ShowImageAtBottomOption(true),
                    maxImageWidth: new MaxImageWidthOption(0),
                    maxImageHeight: new MaxImageHeightOption(0),
                    generateFusejiVariants: new GenerateFusejiVariantsOption(false),
                    maxSearchKeyLengthForFusejiGeneration: new MaxSearchKeyLengthForFusejiGenerationOption(9),
                    maxTotalFusejiCount: new MaxTotalFusejiCountOption(1)),
                maxSearchKeyLength: 0,
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
                    properNameEntries: new ProperNameEntriesOption(true),
                    wordClassInfo: new WordClassInfoOption(true),
                    dialectInfo: new DialectInfoOption(true),
                    pOrthographyInfo: new POrthographyInfoOption(true),
                    pOrthographyInfoColor: new POrthographyInfoColorOption("#FFD2691E"),
                    pOrthographyInfoFontSize: new POrthographyInfoFontSizeOption(15),
                    aOrthographyInfo: new AOrthographyInfoOption(true),
                    rOrthographyInfo: new ROrthographyInfoOption(true),
                    wordTypeInfo: new WordTypeInfoOption(true),
                    spellingRestrictionInfo: new SpellingRestrictionInfoOption(true),
                    extraDefinitionInfo: new ExtraDefinitionInfoOption(true),
                    miscInfo: new MiscInfoOption(true),
                    loanwordEtymology: new LoanwordEtymologyOption(true),
                    showCrossReferences: new ShowCrossReferencesOption(true),
                    autoUpdateAfterNDays: new AutoUpdateAfterNDaysOption(0),
                    generateMazegakiVariants: new GenerateMazegakiVariantsOption(false),
                    generateFusejiVariants: new GenerateFusejiVariantsOption(false),
                    maxSearchKeyLengthForFusejiGeneration: new MaxSearchKeyLengthForFusejiGenerationOption(9),
                    maxTotalFusejiCount: new MaxTotalFusejiCountOption(1)
                ),
                37,
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
                1,
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
                    autoUpdateAfterNDays: new AutoUpdateAfterNDaysOption(0),
                    generateFusejiVariants: new GenerateFusejiVariantsOption(false),
                    maxSearchKeyLengthForFusejiGeneration: new MaxSearchKeyLengthForFusejiGenerationOption(9),
                    maxTotalFusejiCount: new MaxTotalFusejiCountOption(1)),
                41,
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
        { "org", "organization name" },
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
        { "obj", "object" }, { "org", "organization name" }, { "organization", "organization name" }, { "oth", "other" }, { "person", "full name of a particular person" },
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
        await s_loadDictionariesSemaphoreSlim.WaitAsync().ConfigureAwait(false);

        try
        {
            DictsReady = false;

            CheckSingleDictActiveness();

            ProfileCustomWordsCancellationTokenSource?.Dispose();
            ProfileCustomWordsCancellationTokenSource = new CancellationTokenSource();

            ProfileCustomNamesCancellationTokenSource?.Dispose();
            ProfileCustomNamesCancellationTokenSource = new CancellationTokenSource();

            bool dictCleared = false;
            bool rebuildingAnyDB = false;
            ConcurrentBag<Dict> dictsToBeRemoved = [];

            List<Task> tasks = [];

            Dict[] dicts = Dicts.Values.ToArray();
            CheckDBUsageForDicts(dicts);
            PopulateDictTypeArrays(dicts);
            CalculateMaxSearchKeyLength(dicts);

            int customDictionaryTaskCount = 0;
            AtomicBool anyCustomDictionaryTaskIsActuallyUsed = new(false);

            foreach (Dict dict in dicts)
            {
                switch (dict.Type)
                {
                    case DictType.JMdict:
                        LoadDict(dict, JmdictLoader.Load, JmdictDBManager.Version, ResourceUpdater.HandleLeftOverFiles,
                            JmdictDBManager.CreateDB, JmdictDBManager.ImportFromDisk, JmdictDBManager.ImportFromMemory,
                            JmdictDBManager.LoadFromDB, JmdictDBManager.GetMaxSearchKeyLength, tasks, dictsToBeRemoved, JmdictLoader.Size, true, true, ref rebuildingAnyDB, ref dictCleared);

                        break;

                    case DictType.JMnedict:
                        LoadDict(dict, JmnedictLoader.Load, JmnedictDBManager.Version, ResourceUpdater.HandleLeftOverFiles,
                            JmnedictDBManager.CreateDB, JmnedictDBManager.ImportFromDisk, JmnedictDBManager.ImportFromMemory,
                            null, JmnedictDBManager.GetMaxSearchKeyLength, tasks, dictsToBeRemoved, JmnedictLoader.Size, false, true, ref rebuildingAnyDB, ref dictCleared);

                        break;

                    case DictType.Kanjidic:
                        LoadDict(dict, KanjidicLoader.Load, KanjidicDBManager.Version, ResourceUpdater.HandleLeftOverFiles,
                            KanjidicDBManager.CreateDB, KanjidicDBManager.ImportFromDisk, KanjidicDBManager.ImportFromMemory,
                            KanjidicDBManager.LoadFromDB, null, tasks, dictsToBeRemoved, KanjidicLoader.Size, true, true, ref rebuildingAnyDB, ref dictCleared);

                        break;

                    case DictType.NonspecificWordYomichan:
                    case DictType.NonspecificKanjiWithWordSchemaYomichan:
                    case DictType.NonspecificNameYomichan:
                    case DictType.NonspecificYomichan:
                        LoadDict(dict, EpwingYomichanLoader.Load, EpwingYomichanDBManager.Version, ResourceUpdater.HandleLeftOverFolders,
                            EpwingYomichanDBManager.CreateDB, EpwingYomichanDBManager.ImportFromDisk, EpwingYomichanDBManager.ImportFromMemory,
                            EpwingYomichanDBManager.LoadFromDB, EpwingYomichanDBManager.GetMaxSearchKeyLength, tasks, dictsToBeRemoved, EpwingYomichanLoader.Size, true, false, ref rebuildingAnyDB, ref dictCleared);

                        break;

                    case DictType.NonspecificKanjiYomichan:
                        LoadDict(dict, YomichanKanjiLoader.Load, YomichanKanjiDBManager.Version, ResourceUpdater.HandleLeftOverFolders,
                            YomichanKanjiDBManager.CreateDB, YomichanKanjiDBManager.ImportFromDisk, YomichanKanjiDBManager.ImportFromMemory,
                            YomichanKanjiDBManager.LoadFromDB, null, tasks, dictsToBeRemoved, YomichanKanjiLoader.Size, true, false, ref rebuildingAnyDB, ref dictCleared);
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
                        LoadDict(dict, EpwingNazekaLoader.Load, EpwingNazekaDBManager.Version, ResourceUpdater.HandleLeftOverFiles,
                            EpwingNazekaDBManager.CreateDB, EpwingNazekaDBManager.ImportFromDisk, EpwingNazekaDBManager.ImportFromMemory,
                            EpwingNazekaDBManager.LoadFromDB, EpwingNazekaDBManager.GetMaxSearchKeyLength, tasks, dictsToBeRemoved, EpwingNazekaLoader.Size, true, false, ref rebuildingAnyDB, ref dictCleared);
                        break;

                    case DictType.PitchAccentYomichan:
                        LoadDict(dict, YomichanPitchAccentLoader.Load, YomichanPitchAccentDBManager.Version, ResourceUpdater.HandleLeftOverFolders,
                            YomichanPitchAccentDBManager.CreateDB, YomichanPitchAccentDBManager.ImportFromDisk, YomichanPitchAccentDBManager.ImportFromMemory,
                            YomichanPitchAccentDBManager.LoadFromDB, YomichanPitchAccentDBManager.GetMaxSearchKeyLength, tasks, dictsToBeRemoved, YomichanPitchAccentLoader.Size, true, false, ref rebuildingAnyDB, ref dictCleared);

                        break;

                    default:
                    {
                        LoggerManager.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(DictType), nameof(DictUtils), nameof(LoadDictionaries), dict.Type);
                        break;
                    }
                }
            }

            if (tasks.Count > 0 || dictCleared)
            {
                SqliteConnection.ClearAllPools();

                if (tasks.Count > 0)
                {
                    if (rebuildingAnyDB)
                    {
                        FrontendManager.Frontend.Notify(NotificationLevel.Information, "Rebuilding some databases because their schemas are out of date...");
                    }

                    await Task.WhenAll(tasks).ConfigureAwait(false);
                    if (!dictsToBeRemoved.IsEmpty)
                    {
                        foreach (Dict dict in dictsToBeRemoved)
                        {
                            //_ = Dicts.Remove(dict.Name);
                            //_ = SingleDictTypeDicts.Remove(dict.Type);

                            string dbPath = dict.DBPath;
                            if (File.Exists(dbPath))
                            {
                                DBUtils.DeleteDB(dbPath);
                            }
                        }

                        //IOrderedEnumerable<Dict> orderedDicts = Dicts.Values.OrderBy(static d => d.Priority);
                        //int priority = 1;

                        //foreach (Dict dict in orderedDicts)
                        //{
                        //    dict.Priority = priority;
                        //    ++priority;
                        //}
                    }
                }

                Dict[] dictsSnapshot = Dicts.Values.ToArray();
                CheckSingleDictActiveness();
                CheckDBUsageForDicts(dictsSnapshot);
                PopulateDictTypeArrays(dictsSnapshot);
                CalculateMaxSearchKeyLength(dictsSnapshot);

                if (dictsSnapshot.All(static d => !d.Updating)
                    && (tasks.Count > customDictionaryTaskCount || anyCustomDictionaryTaskIsActuallyUsed.Read()))
                {
                    FrontendManager.Frontend.Notify(NotificationLevel.Success, "Finished loading dictionaries");
                }

                ProfileCustomWordsCancellationTokenSource.Dispose();
                ProfileCustomWordsCancellationTokenSource = null;

                ProfileCustomNamesCancellationTokenSource.Dispose();
                ProfileCustomNamesCancellationTokenSource = null;
            }

            DictsReady = true;
            FrontendManager.Frontend.PopupDictTypeButtonsNeedUpdating();
        }
        finally
        {
            _ = s_loadDictionariesSemaphoreSlim.Release();
        }
    }

    private static DBState PrepareDictDB(Dict dict, int dbVersion, ref bool rebuildingAnyDB)
    {
        dict.Ready = false;

        bool useDB = dict.Options.UseDB.Value;
        string dbPath = dict.DBPath;
        string dbJournalPath = $"{dbPath}-journal";
        string dbWalPath = $"{dbPath}-wal";
        string dbShmPath = $"{dbPath}-shm";
        bool dbExists = File.Exists(dbPath);
        bool dbJournalExists = File.Exists(dbJournalPath);
        bool dbWalExists = File.Exists(dbWalPath);
        bool dbShmExists = File.Exists(dbShmPath);

        if (!dict.Updating)
        {
            if (dbJournalExists || dbWalExists || dbShmExists)
            {
                if (dbExists)
                {
                    DBUtils.DeleteDB(dbPath);
                    dbExists = false;
                }

                if (dbJournalExists)
                {
                    File.Delete(dbJournalPath);
                }

                if (dbWalExists)
                {
                    File.Delete(dbWalPath);
                }

                if (dbShmExists)
                {
                    File.Delete(dbShmPath);
                }
            }
            else if (dbExists && !DBUtils.RecordExists(dict.ReadOnlyConnectionString))
            {
                DBUtils.DeleteDB(dbPath);
                dbExists = false;
            }
        }

        if (dbExists && DBUtils.CheckIfDBSchemaIsOutOfDate(dbVersion, dict.ReadOnlyConnectionString))
        {
            DBUtils.DeleteDB(dbPath);
            dbExists = false;
            rebuildingAnyDB = true;
        }

        return new DBState(useDB, dbExists);
    }

    private static void LoadDict(Dict dict, Load load, int version, HandleLeftOvers handleLeftOvers, CreateDB createDB, ImportFromDisk importFromDisk, ImportFromMemory importFromMemory, LoadFromDB? loadFromDB, GetMaxSearchKeyLength? getMaxSearchKeyLength, List<Task> tasks, ConcurrentBag<Dict> dictsToBeRemoved, int initialDictSize, bool preferLoadingFromDB, bool deleteDictFileOnError, ref bool rebuildingAnyDB, ref bool dictCleared)
    {
        if (dict.Updating)
        {
            return;
        }

        string fullDictPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        handleLeftOvers(fullDictPath);
        DBState dBContext = PrepareDictDB(dict, version, ref rebuildingAnyDB);

        bool useDB = dBContext.UseDB;
        bool dbExists = dBContext.DBExists;
        bool hasContent = dict.Contents.Count > 0;

        if (dict.Active && !useDB && !hasContent)
        {
            tasks.Add(Task.Run(async () =>
            {
                dict.Contents = new Dictionary<string, IList<IDictRecord>>(dict.Size > 0 ? dict.Size : initialDictSize, StringComparer.Ordinal);
                try
                {
                    if (!dbExists || !preferLoadingFromDB)
                    {
                        await load(dict).ConfigureAwait(false);
                    }
                    else
                    {
                        Debug.Assert(loadFromDB is not null);
                        loadFromDB(dict);
                    }

                    dict.Size = dict.Contents.Count;
                    if (dict.Size is 0)
                    {
                        LoggerManager.Logger.Warning("No valid records found for '{DictType}'-'{DictName}' from '{FullDictPath}'. The dict has been deactivated.", dict.Type.GetDescription(), dict.Name, fullDictPath);
                        FrontendManager.Frontend.Notify(NotificationLevel.Warning, $"No valid records found for {dict.Name}. The dict has been deactivated.");

                        dict.Active = false;
                        dictsToBeRemoved.Add(dict);
                        if (deleteDictFileOnError && File.Exists(fullDictPath))
                        {
                            File.Delete(fullDictPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggerManager.Logger.Error(ex, "Couldn't import '{DictType}'-'{DictName}' from '{FullDictPath}'", dict.Type.GetDescription(), dict.Name, fullDictPath);
                    FrontendManager.Frontend.Notify(NotificationLevel.Error, $"Couldn't import {dict.Name}. Check the logs for more details.");

                    dict.Active = false;
                    dictsToBeRemoved.Add(dict);
                    if (deleteDictFileOnError && File.Exists(fullDictPath))
                    {
                        File.Delete(fullDictPath);
                    }
                }
                finally
                {
                    dict.Ready = true;
                }
            }, CancellationToken.None));
        }
        else if (dict.Active && useDB && !dbExists)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    createDB(dict.DBPath);
                    if (!hasContent)
                    {
                        await importFromDisk(dict).ConfigureAwait(false);
                    }
                    else
                    {
                        importFromMemory(dict);
                        dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                    }

                    if (dict.Size is 0)
                    {
                        LoggerManager.Logger.Warning("No valid records found for '{DictType}'-'{DictName}' from '{FullDictPath}'. The dict has been deactivated.", dict.Type.GetDescription(), dict.Name, fullDictPath);
                        FrontendManager.Frontend.Notify(NotificationLevel.Warning, $"No valid records found for {dict.Name}. The dict has been deactivated.");

                        dict.Active = false;
                        dictsToBeRemoved.Add(dict);
                        if (deleteDictFileOnError && File.Exists(fullDictPath))
                        {
                            File.Delete(fullDictPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggerManager.Logger.Error(ex, "Couldn't import '{DictType}'-'{DictName}' from '{FullDictPath}'", dict.Type.GetDescription(), dict.Name, fullDictPath);
                    FrontendManager.Frontend.Notify(NotificationLevel.Error, $"Couldn't import {dict.Name}. Check the logs for more details.");

                    dict.Active = false;
                    dictsToBeRemoved.Add(dict);
                    if (deleteDictFileOnError && File.Exists(fullDictPath))
                    {
                        File.Delete(fullDictPath);
                    }
                }
                finally
                {
                    dict.Ready = true;
                }
            }, CancellationToken.None));
        }
        else if (hasContent && (!dict.Active || useDB))
        {
            dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
            dict.Ready = true;
            dictCleared = true;
        }
        else
        {
            dict.Ready = true;
        }

        if (dict is { MaxSearchKeyLength: 0, Active: true, Options.UseDB.Value: true })
        {
            if (getMaxSearchKeyLength is not null)
            {
                using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(dict.ReadOnlyConnectionString);
                if (connection is not null)
                {
                    dict.MaxSearchKeyLength = getMaxSearchKeyLength(connection);
                }
            }
            else if (KanjiDictTypes.Contains(dict.Type))
            {
                dict.MaxSearchKeyLength = 1;
            }
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
            }, CancellationToken.None));
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
            }, CancellationToken.None));
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

    public static async Task CreateDefaultDictsConfig()
    {
        if (File.Exists(s_configFilePath))
        {
            return;
        }

        _ = Directory.CreateDirectory(AppInfo.ConfigPath);

        FileStream fileStream = new(s_configFilePath, FileStreamOptionsPresets.s_asyncCreateFso);
        await using (fileStream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(fileStream, BuiltInDicts, JsonOptions.s_jsoIgnoringWhenWritingNullWithEnumConverterAndIndentation, CancellationToken.None).ConfigureAwait(false);
        }
    }

    public static async Task SerializeDicts()
    {
        string tempConfigFilePath = PathUtils.GetTempPath(s_configFilePath);

        FileStream fileStream = new(tempConfigFilePath, FileStreamOptionsPresets.s_asyncCreateFso);
        await using (fileStream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(fileStream, Dicts, JsonOptions.s_jsoIgnoringWhenWritingNullWithEnumConverterAndIndentation, CancellationToken.None).ConfigureAwait(false);
        }

        PathUtils.ReplaceFileAtomicallyOnSameVolume(s_configFilePath, tempConfigFilePath);
    }

    internal static async Task DeserializeDicts()
    {
        Dictionary<string, Dict>? deserializedDicts;

        FileStream dictStream = new(s_configFilePath, FileStreamOptionsPresets.s_asyncReadFso);
        await using (dictStream.ConfigureAwait(false))
        {
            deserializedDicts = await JsonSerializer.DeserializeAsync<Dictionary<string, Dict>>(dictStream, JsonOptions.s_jsoWithEnumConverter, CancellationToken.None).ConfigureAwait(false);
        }

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
            FrontendManager.Frontend.Notify(NotificationLevel.Error, "Couldn't load Config/dicts.json");
            throw new SerializationException("Couldn't load Config/dicts.json");
        }
    }

    private static void InitDictOptions(Dict dict)
    {
        if (dict.Type is DictType.JMdict)
        {
            DictOptions builtInJmdictOptions = BuiltInDicts[nameof(DictType.JMdict)].Options;

            dict.Options.NewlineBetweenDefinitions ??= builtInJmdictOptions.NewlineBetweenDefinitions;
            dict.Options.ProperNameEntries ??= builtInJmdictOptions.ProperNameEntries;
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
            dict.Options.ShowCrossReferences ??= builtInJmdictOptions.ShowCrossReferences;
            dict.Options.AutoUpdateAfterNDays ??= builtInJmdictOptions.AutoUpdateAfterNDays;
            dict.Options.GenerateMazegakiVariants ??= builtInJmdictOptions.GenerateMazegakiVariants;
            dict.Options.GenerateFusejiVariants ??= builtInJmdictOptions.GenerateFusejiVariants;
            dict.Options.MaxSearchKeyLengthForFusejiGeneration ??= builtInJmdictOptions.MaxSearchKeyLengthForFusejiGeneration;
            dict.Options.MaxTotalFusejiCount ??= builtInJmdictOptions.MaxTotalFusejiCount;
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
            dict.Options.GenerateFusejiVariants ??= builtInJmnedictOptions.GenerateFusejiVariants;
            dict.Options.MaxSearchKeyLengthForFusejiGeneration ??= builtInJmnedictOptions.MaxSearchKeyLengthForFusejiGeneration;
            dict.Options.MaxTotalFusejiCount ??= builtInJmnedictOptions.MaxTotalFusejiCount;
        }
        else if (dict.Type is DictType.CustomWordDictionary or DictType.ProfileCustomWordDictionary)
        {
            DictOptions builtInCustomWordOptions = BuiltInDicts[nameof(DictType.CustomWordDictionary)].Options;
            dict.Options.NewlineBetweenDefinitions ??= builtInCustomWordOptions.NewlineBetweenDefinitions;
            dict.Options.GenerateMazegakiVariants ??= builtInCustomWordOptions.GenerateMazegakiVariants;
            dict.Options.GenerateFusejiVariants ??= builtInCustomWordOptions.GenerateFusejiVariants;
            dict.Options.MaxSearchKeyLengthForFusejiGeneration ??= builtInCustomWordOptions.MaxSearchKeyLengthForFusejiGeneration;
            dict.Options.MaxTotalFusejiCount ??= builtInCustomWordOptions.MaxTotalFusejiCount;
        }
        else if (dict.Type is DictType.CustomNameDictionary or DictType.ProfileCustomNameDictionary)
        {
            DictOptions builtInCustomNameOptions = BuiltInDicts[nameof(DictType.CustomNameDictionary)].Options;
            dict.Options.ShowImages ??= builtInCustomNameOptions.ShowImages;
            dict.Options.ShowImageAtBottom ??= builtInCustomNameOptions.ShowImageAtBottom;
            dict.Options.MaxImageWidth ??= builtInCustomNameOptions.MaxImageWidth;
            dict.Options.MaxImageHeight ??= builtInCustomNameOptions.MaxImageHeight;
            dict.Options.GenerateMazegakiVariants ??= builtInCustomNameOptions.GenerateMazegakiVariants;
            dict.Options.GenerateFusejiVariants ??= builtInCustomNameOptions.GenerateFusejiVariants;
            dict.Options.MaxSearchKeyLengthForFusejiGeneration ??= builtInCustomNameOptions.MaxSearchKeyLengthForFusejiGeneration;
            dict.Options.MaxTotalFusejiCount ??= builtInCustomNameOptions.MaxTotalFusejiCount;
        }
        else
        {
            if (ShowImagesOption.ValidDictTypes.Contains(dict.Type))
            {
                dict.Options.ShowImages ??= new ShowImagesOption(true);
            }
            if (ShowImageAtBottomOption.ValidDictTypes.Contains(dict.Type))
            {
                dict.Options.ShowImageAtBottom ??= new ShowImageAtBottomOption(true);
            }
            if (MaxImageWidthOption.ValidDictTypes.Contains(dict.Type))
            {
                dict.Options.MaxImageWidth ??= new MaxImageWidthOption(0);
            }
            if (MaxImageHeightOption.ValidDictTypes.Contains(dict.Type))
            {
                dict.Options.MaxImageHeight ??= new MaxImageHeightOption(0);
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
            if (GenerateMazegakiVariantsOption.ValidDictTypes.Contains(dict.Type))
            {
                dict.Options.GenerateMazegakiVariants ??= new GenerateMazegakiVariantsOption(false);
            }
            if (GenerateFusejiVariantsOption.ValidDictTypes.Contains(dict.Type))
            {
                dict.Options.GenerateFusejiVariants ??= new GenerateFusejiVariantsOption(false);
            }
            if (MaxSearchKeyLengthForFusejiGenerationOption.ValidDictTypes.Contains(dict.Type))
            {
                dict.Options.MaxSearchKeyLengthForFusejiGeneration ??= new MaxSearchKeyLengthForFusejiGenerationOption(9);
            }
            if (MaxTotalFusejiCountOption.ValidDictTypes.Contains(dict.Type))
            {
                dict.Options.MaxTotalFusejiCount ??= new MaxTotalFusejiCountOption(1);
            }
        }
    }

    private static void CheckSingleDictActiveness()
    {
        JmdictIsActive = SingleDictTypeDicts.TryGetValue(DictType.JMdict, out Dict? jmdict) && jmdict.Active;
        AnyCustomWordDictIsActive = (SingleDictTypeDicts.TryGetValue(DictType.CustomWordDictionary, out Dict? customWordDict) && customWordDict.Active)
            || (SingleDictTypeDicts.TryGetValue(DictType.ProfileCustomWordDictionary, out Dict? profileCustomWordDict) && profileCustomWordDict.Active);

        DBIsUsedForPitchDict = SingleDictTypeDicts.TryGetValue(DictType.PitchAccentYomichan, out Dict? pitchDict)
            && pitchDict is { Active: true, Options.UseDB.Value: true };
        PitchDict = pitchDict;
    }

    private static void CheckDBUsageForDicts(Dict[] dicts)
    {
        bool dbIsUsedForAtLeastOneDict = false;
        bool dbIsUsedForAtLeastOneWordDict = false;
        bool dbIsUsedForAtLeastOneYomichanDict = false;
        bool dbIsUsedForAtLeastOneNazekaDict = false;
        bool dbIsUsedForAtLeastOneYomichanOrNazekaWordDict = false;
        bool atLeastOneKanjiDictIsActive = false;
        bool dbIsUsedForJmdict = false;
        bool dbIsUsedForJmnedict = false;

        foreach (Dict dict in dicts)
        {
            if (dict.Active)
            {
                if (KanjiDictTypes.Contains(dict.Type))
                {
                    atLeastOneKanjiDictIsActive = true;
                }

                if (dict.Options.UseDB.Value)
                {
                    dbIsUsedForAtLeastOneDict = true;

                    if (dict.Type is DictType.JMdict)
                    {
                        dbIsUsedForJmdict = true;
                    }
                    else if (dict.Type is DictType.JMnedict)
                    {
                        dbIsUsedForJmnedict = true;
                    }

                    if (dict.Type is DictType.JMdict or DictType.NonspecificWordYomichan or DictType.NonspecificWordNazeka)
                    {
                        dbIsUsedForAtLeastOneWordDict = true;
                    }

                    if (s_yomichanWordAndNameDictTypeSet.Contains(dict.Type))
                    {
                        dbIsUsedForAtLeastOneYomichanDict = true;
                    }

                    if (s_nazekaWordAndNameDictTypeSet.Contains(dict.Type))
                    {
                        dbIsUsedForAtLeastOneNazekaDict = true;
                    }

                    if (dict.Type is DictType.NonspecificWordYomichan or DictType.NonspecificYomichan or DictType.NonspecificWordNazeka or DictType.NonspecificNazeka)
                    {
                        dbIsUsedForAtLeastOneYomichanOrNazekaWordDict = true;
                    }
                }

                if (dbIsUsedForAtLeastOneDict
                    && dbIsUsedForAtLeastOneWordDict
                    && dbIsUsedForAtLeastOneYomichanDict
                    && dbIsUsedForAtLeastOneNazekaDict
                    && dbIsUsedForAtLeastOneYomichanOrNazekaWordDict
                    && atLeastOneKanjiDictIsActive
                    && dbIsUsedForJmdict
                    && dbIsUsedForJmnedict)
                {
                    break;
                }
            }
        }

        DBIsUsedForAtLeastOneDict = dbIsUsedForAtLeastOneDict;
        DBIsUsedForAtLeastOneWordDict = dbIsUsedForAtLeastOneWordDict;
        DBIsUsedForAtLeastOneYomichanDict = dbIsUsedForAtLeastOneYomichanDict;
        DBIsUsedForAtLeastOneNazekaDict = dbIsUsedForAtLeastOneNazekaDict;
        DBIsUsedForAtLeastOneYomichanOrNazekaWordDict = dbIsUsedForAtLeastOneYomichanOrNazekaWordDict;
        AtLeastOneKanjiDictIsActive = atLeastOneKanjiDictIsActive;
        DBIsUsedForJmdict = dbIsUsedForJmdict;
    }

    public static bool AddRecordToDictionary(string normalizedKey, IDictRecord record, Dict dict)
    {
        if (normalizedKey.Length > dict.MaxSearchKeyLength)
        {
            dict.MaxSearchKeyLength = normalizedKey.Length;
        }

        Debug.Assert(dict.Contents is Dictionary<string, IList<IDictRecord>>);
        Dictionary<string, IList<IDictRecord>> dictContents = (Dictionary<string, IList<IDictRecord>>)dict.Contents;
        ref IList<IDictRecord>? records = ref CollectionsMarshal.GetValueRefOrAddDefault(dictContents, normalizedKey, out bool exists);
        if (!exists)
        {
            records = [record];
            return true;
        }

        Debug.Assert(records is not null);
        List<IDictRecord> list = (List<IDictRecord>)records;

        if (list.AsReadOnlySpan().Contains(record))
        {
            return false;
        }

        list.Add(record);
        return true;
    }

    internal static void PopulateDictTypeArrays(Dict[] dicts)
    {
        List<Dict> allDicts = new(dicts.Length);
        List<Dict> wordDicts = [];
        List<Dict> nameDicts = [];
        List<Dict> kanjiDicts = [];
        List<Dict> otherDicts = [];

        foreach (Dict dict in dicts)
        {
            if (dict is { Active: true, Type: not DictType.PitchAccentYomichan })
            {
                allDicts.Add(dict);
                if (s_wordDictTypes.Contains(dict.Type))
                {
                    wordDicts.Add(dict);
                }
                else if (KanjiDictTypes.Contains(dict.Type))
                {
                    kanjiDicts.Add(dict);
                }
                else if (s_nameDictTypes.Contains(dict.Type))
                {
                    nameDicts.Add(dict);
                }
                else if (s_otherDictTypes.Contains(dict.Type))
                {
                    otherDicts.Add(dict);
                }
            }
        }

        s_allDicts = allDicts.ToArray();
        s_wordDicts = wordDicts.ToArray();
        s_nameDicts = nameDicts.ToArray();
        s_kanjiDicts = kanjiDicts.ToArray();
        s_otherDicts = otherDicts.ToArray();
    }

    private static void CalculateMaxSearchKeyLength(Dict[] dicts)
    {
        foreach (Dict dict in dicts)
        {
            if (dict.Type is not DictType.PitchAccentYomichan && dict.MaxSearchKeyLength > MaxSearchKeyLength)
            {
                MaxSearchKeyLength = dict.MaxSearchKeyLength;
            }
        }
    }

    internal static Dict[] GetDictForLookupCategoryType(LookupCategory lookupCategory)
    {
        return lookupCategory switch
        {
            LookupCategory.All => s_allDicts,
            LookupCategory.Word => s_wordDicts,
            LookupCategory.Kanji => s_kanjiDicts,
            LookupCategory.Name => s_nameDicts,
            LookupCategory.Other => s_otherDicts,
            _ => s_allDicts
        };
    }
}
