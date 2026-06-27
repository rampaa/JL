using JL.Core.Dicts.Interfaces;
using JL.Core.Frontend;
using JL.Core.Utilities.Japanese;

namespace JL.Core.Dicts.CustomNameDict;

public static class CustomNameLoader
{
    internal static void Load(Dict dict, CancellationToken cancellationToken)
    {
        string fullPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        if (!File.Exists(fullPath))
        {
            return;
        }

        IDictionary<string, IList<IDictRecord>> customNameDictionary = dict.Contents;

        Span<Range> tabRanges = stackalloc Range[5];
        foreach (string line in File.ReadLines(fullPath))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                customNameDictionary.Clear();
                break;
            }

            ReadOnlySpan<char> lineSpan = line.AsSpan();
            int tabCount = lineSpan.Split(tabRanges, '\t', StringSplitOptions.TrimEntries);

            if (tabCount >= 3)
            {
                string? extraInfo = null;
                ImageInfo? imageInfo = null;
                if (tabCount >= 4)
                {
                    ReadOnlySpan<char> extraInfoSpan = lineSpan[tabRanges[3]];
                    extraInfo = extraInfoSpan.Length is 0
                        ? null
                        : extraInfoSpan.ToString().Replace("\\n", "\n", StringComparison.Ordinal);

                    if (tabCount is 5)
                    {
                        ReadOnlySpan<char> imagePathSpan = lineSpan[tabRanges[4]];
                        if (imagePathSpan.Length > 0)
                        {
                            imageInfo = FrontendManager.Frontend.GetImageInfo(imagePathSpan.ToString());
                        }
                    }
                }

                string nameType = lineSpan[tabRanges[2]].ToString();
                string? reading = lineSpan[tabRanges[1]].ToString();
                string spelling = lineSpan[tabRanges[0]].ToString();

                if (reading.Length is 0 || reading == spelling)
                {
                    reading = null;
                }

                AddToDictionary(spelling, reading, nameType, extraInfo, imageInfo, customNameDictionary);
            }
        }
    }

    public static void AddToDictionary(string spelling, string? reading, string nameType, string? extraInfo, ImageInfo? imageInfo, IDictionary<string, IList<IDictRecord>> customNameDictionary)
    {
        CustomNameRecord record = new(spelling, reading, nameType, extraInfo, imageInfo);
        _ = DictUtils.AddRecordToDictionary(JapaneseUtils.NormalizeText(spelling), record, customNameDictionary);
    }
}
