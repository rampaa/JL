using JL.Core.Dicts.Interfaces;

namespace JL.Core.Dicts.EPWING;

internal static class EpwingUtils
{
    public static void AddRecordToDictionary(string normalizedKey, IDictRecord record, IDictionary<string, IList<IDictRecord>> dictionary)
    {
        if (dictionary.TryGetValue(normalizedKey, out IList<IDictRecord>? records))
        {
            if (!records.Contains(record))
            {
                records.Add(record);
            }
        }
        else
        {
            dictionary[normalizedKey] = [record];
        }
    }
}
