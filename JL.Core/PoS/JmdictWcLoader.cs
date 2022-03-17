using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using JL.Core.Dicts;
using JL.Core.Dicts.EDICT.JMdict;

namespace JL.Core.PoS
{
    public static class JmdictWcLoader
    {
        public static async Task Load()
        {
            await using FileStream openStream =
                File.OpenRead($"Resources/PoS.json");
            Storage.WcDict = await JsonSerializer.DeserializeAsync<Dictionary<string, List<JmdictWc>>>(openStream);
            if (Storage.WcDict == null) throw new InvalidOperationException();

            foreach (List<JmdictWc> jmDictWcEntryList in Storage.WcDict.Values.ToList())
            {
                int jmDictWcEntryListCount = jmDictWcEntryList.Count;
                for (int i = 0; i < jmDictWcEntryListCount; i++)
                {
                    JmdictWc jMDictWcEntry = jmDictWcEntryList[i];

                    if (jMDictWcEntry.Readings != null)
                    {
                        int readingCount = jMDictWcEntry.Readings.Count;
                        for (int j = 0; j < readingCount; j++)
                        {
                            string reading = jMDictWcEntry.Readings[j];

                            if (Storage.WcDict.TryGetValue(reading, out List<JmdictWc> result))
                            {
                                result.Add(jMDictWcEntry);
                            }

                            else
                            {
                                Storage.WcDict.Add(reading, new List<JmdictWc> { jMDictWcEntry });
                            }
                        }
                    }
                }
            }

            Storage.WcDict.TrimExcess();
        }

        public static async Task JmdictWordClassSerializer()
        {
            Dictionary<string, List<JmdictWc>> jmdictWcDict = new();

            string[] unusedWcs =
            {
                "adj-f", "adj-ix", "adj-kari", "adj-ku", "adj-nari", "adj-no", "adj-pn", "adj-shiku", "adj-t",
                "adv", "adv-to", "aux", "aux-adj", "aux-v", "conj", "cop", "ctr", "int", "n", "n-adv", "n-pr",
                "n-pref", "n-suf", "n-t", "num", "pn", "pref", "prt", "suf", "unc", "v-unspec", "v1-s", "vi",
                "v2a-s", "v2b-k", "v2b-s", "v2d-k", "v2d-s", "v2g-k", "v2g-s", "v2h-k", "v2h-s", "v2k-k", "v2k-s",
                "v2m-k", "v2m-s", "v2n-s", "v2r-k", "v2r-s", "v2s-s", "v2t-k", "v2t-s", "v2w-s", "v2y-k", "v2y-s",
                "v2z-s", "v4b", "v4g", "v4h", "v4k", "v4m", "v4n", "v4r", "v4s", "v4t", "v5uru", "vn", "vr", "vs-i",
                "vs-s", "vt", "exp", "vs", "vz"
            };

            foreach (List<IResult> jMdictResultList in Storage.Dicts[DictType.JMdict].Contents.Values.ToList())
            {
                int jMdictResultListCount = jMdictResultList.Count;
                for (int i = 0; i < jMdictResultListCount; i++)
                {
                    var value = (JMdictResult)jMdictResultList[i];

                    if (!value.WordClasses?.Any() ?? true)
                        continue;

                    List<string> wordClasses = value.WordClasses?.SelectMany(wc => wc).ToHashSet().Except(unusedWcs).ToList();

                    if (!wordClasses.Any())
                        continue;

                    if (jmdictWcDict.TryGetValue(value.PrimarySpelling, out List<JmdictWc> psr))
                    {
                        if (!psr.Any(r =>
                            r.Readings?.SequenceEqual(value.Readings ?? new List<string>()) ??
                            value.Readings == null && r.Spelling == value.PrimarySpelling))
                            psr.Add(new JmdictWc(value.PrimarySpelling, value.Readings, wordClasses));
                    }

                    else
                    {
                        jmdictWcDict.Add(value.PrimarySpelling,
                            new List<JmdictWc> { new(value.PrimarySpelling, value.Readings, wordClasses) });
                    }

                    if (value.AlternativeSpellings != null)
                    {
                        int alternativeSpellingCount = value.AlternativeSpellings.Count;
                        for (int j = 0; j < alternativeSpellingCount; j++)
                        {
                            string spelling = value.AlternativeSpellings[j];

                            if (jmdictWcDict.TryGetValue(spelling, out List<JmdictWc> asr))
                            {
                                if (!asr.Any(r =>
                                    r.Readings?.SequenceEqual(value.Readings ?? new List<string>()) ??
                                    value.Readings == null && r.Spelling == spelling))
                                    asr.Add(new JmdictWc(spelling, value.Readings, wordClasses));
                            }

                            else
                            {
                                jmdictWcDict.Add(spelling,
                                    new List<JmdictWc> { new(spelling, value.Readings, wordClasses) });
                            }
                        }
                    }
                }
            }

            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };

            await File.WriteAllBytesAsync($"Resources/PoS.json",
                JsonSerializer.SerializeToUtf8Bytes(jmdictWcDict, options));
        }
    }
}
