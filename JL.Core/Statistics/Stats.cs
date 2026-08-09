using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace JL.Core.Statistics;

public sealed class Stats
{
    public ulong Characters { get; set; }
    public ulong Lines { get; set; }
    public TimeSpan Time { get; set; }
    public ulong CardsMined { get; set; }
    public ulong TimesPlayedAudio { get; set; }
    public ulong NumberOfLookups { get; set; }
    public ulong Imoutos { get; set; }

    [JsonIgnore] public Dictionary<string, int> TermLookupCountDict { get; } = new(StringComparer.Ordinal);

    internal void IncrementLookupStat(string deconjugatedMatchedText)
    {
        ref int count = ref CollectionsMarshal.GetValueRefOrAddDefault(TermLookupCountDict, deconjugatedMatchedText, out _);
        ++count;
    }

    internal void ResetStats()
    {
        Characters = 0;
        Lines = 0;
        Time = TimeSpan.Zero;
        CardsMined = 0;
        TimesPlayedAudio = 0;
        Imoutos = 0;
        NumberOfLookups = 0;
        TermLookupCountDict.Clear();
    }

    public override string ToString()
    {
        return
            $"""
            Characters: {Characters.ToString("N0", CultureInfo.InvariantCulture)}
            Lines: {Lines.ToString("N0", CultureInfo.InvariantCulture)}
            Time: {Time.ToString(@"d\.hh\:mm\:ss", CultureInfo.InvariantCulture)}
            Characters per minute: {(Time.TotalMinutes > 0
                ? Math.Round(Characters / Time.TotalMinutes).ToString("N0", CultureInfo.InvariantCulture)
                : Characters is 0
                    ? "0"
                    : "∞")}
            Cards Mined: {CardsMined.ToString("N0", CultureInfo.InvariantCulture)}
            Times Played Audio: {TimesPlayedAudio.ToString("N0", CultureInfo.InvariantCulture)}
            Number of Lookups: {NumberOfLookups.ToString("N0", CultureInfo.InvariantCulture)}
            Imoutos: {Imoutos.ToString("N0", CultureInfo.InvariantCulture)}
            """;
    }
}
