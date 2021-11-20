using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace JapaneseLookup.PoS
{
    public static class JmdictWcLoader
    {
        public static Dictionary<string, List<JmdictWC>> WcDict { get; set; }
        public static async Task Load()
        {
            await using FileStream openStream = File.OpenRead(Path.Join(ConfigManager.ApplicationPath, "Resources/PoS.json"));
            WcDict = await JsonSerializer.DeserializeAsync<Dictionary<string, List<JmdictWC>>>(openStream);

            foreach (var (key, value) in WcDict.ToList())
            {
                foreach (var jMDictWCEntry in value.ToList())
                {
                    foreach (var reading in jMDictWCEntry.Readings)
                    {
                        if (WcDict.TryGetValue(reading, out var result))
                        {
                            result.Add(jMDictWCEntry);
                        }

                        else
                        {
                            WcDict.Add(reading, new List<JmdictWC> { jMDictWCEntry });
                        }
                    }
                }
            }
        }
        public static async Task JmdictWordClassSerializer()
        {
            Dictionary<string, List<JmdictWC>> jmdictWcDict = new();

            string[] unusedWcs = {
                "adj-f", "adj-ix", "adj-kari", "adj-ku","adj-nari",
                "adj-no", "adj-pn", "adj-shiku", "adj-t", "adv", "adv-to", "aux",
                "aux-adj", "aux-v", "conj", "cop","ctr", "int", "n", "n-adv", "n-pr",
                "n-pref", "n-suf", "n-t", "num", "pn", "pref", "prt", "suf", "unc",
                "v-unspec", "v1-s", "vi", "v2a-s", "v2b-k", "v2b-s", "v2d-k", "v2d-s",
                "v2g-k", "v2g-s", "v2h-k", "v2h-s", "v2k-k","v2k-s", "v2m-k", "v2m-s",
                "v2n-s", "v2r-k", "v2r-s", "v2s-s", "v2t-k", "v2t-s", "v2w-s", "v2y-k",
                "v2y-s", "v2z-s", "v4b", "v4g", "v4h", "v4k", "v4m", "v4n", "v4r",
                "v4s","v4t", "v5uru", "vn", "vr", "vs-i", "vs-s", "vt",
                "exp", "vs", "vz"
            };

            foreach (var (key, values) in ConfigManager.Dicts[Dicts.DictType.JMdict].Contents)
            {
                foreach (EDICT.JMdict.JMdictResult value in values)
                {
                    if (!value.WordClasses?.Any() ?? true)
                        continue;

                    var wordClasses = value.WordClasses?.SelectMany(wc => wc).ToHashSet().ToList() ?? null;

                    if (wordClasses == null)
                        continue;

                    foreach (string unusedWc in unusedWcs)
                    {
                        wordClasses.Remove(unusedWc);
                    }

                    if (!wordClasses.Any())
                        continue;

                    if (jmdictWcDict.TryGetValue(value.PrimarySpelling, out var pSR))
                    {
                        if (!pSR.Any(r => r.Readings?.SequenceEqual(value?.Readings ?? new List<string>()) ?? value.Readings == null && r.Spelling == value.PrimarySpelling))
                            pSR.Add(new JmdictWC(value.PrimarySpelling, value.Readings, wordClasses));
                    }

                    else
                    {
                        jmdictWcDict.Add(value.PrimarySpelling, new List<JmdictWC> { new JmdictWC(value.PrimarySpelling, value.Readings, wordClasses) });
                    }

                    if (value.AlternativeSpellings != null)
                    {
                        foreach (var spelling in value.AlternativeSpellings)
                        {
                            if (jmdictWcDict.TryGetValue(spelling, out var aSR))
                            {
                                if (!aSR.Any(r => r.Readings?.SequenceEqual(value?.Readings ?? new List<string>()) ?? value.Readings == null && r.Spelling == spelling))
                                    aSR.Add(new JmdictWC(spelling, value.Readings, wordClasses));
                            }

                            else
                            {
                                jmdictWcDict.Add(spelling, new List<JmdictWC> { new JmdictWC(spelling, value.Readings, wordClasses) });
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

            await File.WriteAllBytesAsync(Path.Join(ConfigManager.ApplicationPath, "Resources/PoS.json"),
                JsonSerializer.SerializeToUtf8Bytes(jmdictWcDict, options));
        }
    }
}
