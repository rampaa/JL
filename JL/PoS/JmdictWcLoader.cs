using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;
using JL.Dicts;
using JL.Dicts.EDICT.JMdict;

namespace JL.PoS
{
    public static class JmdictWcLoader
    {
        public static async Task Load()
        {
            await using FileStream openStream =
                File.OpenRead(Path.Join(ConfigManager.ApplicationPath, "Resources/PoS.json"));
            Storage.WcDict = await JsonSerializer.DeserializeAsync<Dictionary<string, List<JmdictWc>>>(openStream);
            if (Storage.WcDict == null) throw new InvalidOperationException();

            foreach ((string _, var value) in Storage.WcDict.ToList())
            {
                foreach (JmdictWc jMDictWcEntry in value.ToList())
                {
                    if (jMDictWcEntry.Readings != null)
                    {
                        foreach (string reading in jMDictWcEntry.Readings)
                        {
                            if (Storage.WcDict.TryGetValue(reading, out var result))
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

            foreach ((string _, var values) in Storage.Dicts[DictType.JMdict].Contents.ToList())
            {
                foreach (IResult result in values)
                {
                    var value = (JMdictResult)result;
                    if (!value.WordClasses?.Any() ?? true)
                        continue;

                    var wordClasses = value.WordClasses?.SelectMany(wc => wc).ToHashSet().ToList();

                    foreach (string unusedWc in unusedWcs)
                    {
                        wordClasses.Remove(unusedWc);
                    }

                    if (!wordClasses.Any())
                        continue;

                    if (jmdictWcDict.TryGetValue(value.PrimarySpelling, out var psr))
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
                        foreach (string spelling in value.AlternativeSpellings)
                        {
                            if (jmdictWcDict.TryGetValue(spelling, out var asr))
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

            await File.WriteAllBytesAsync(Path.Join(ConfigManager.ApplicationPath, "Resources/PoS.json"),
                JsonSerializer.SerializeToUtf8Bytes(jmdictWcDict, options));
        }
    }
}
