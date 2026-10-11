using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using JL.Core.Japanese;
using JL.Core.Utilities;
using JL.Core.Utilities.Array;

namespace JL.Core.WordClass;

internal static class JmdictWordClassBinarySerializer
{
    private const int Version = 4;
    private const int ChecksumLength = 32;

    internal static void Serialize(string path, Dictionary<string, List<JmdictWordClass>> dictionary)
    {
        int maximumArrayLength = 0;
        Dictionary<JmdictWordClass, int> recordIds = [];
        Dictionary<string[], int> wordClassIds = new(ArrayComparer<string>.Instance);
        Dictionary<string, int> stringIds = new(StringComparer.Ordinal);
        foreach (List<JmdictWordClass> bucket in dictionary.Values)
        {
            foreach (JmdictWordClass record in bucket.AsReadOnlySpan())
            {
                if (!recordIds.TryAdd(record, recordIds.Count))
                {
                    continue;
                }

                maximumArrayLength = Math.Max(maximumArrayLength, record.WordClasses.Length);
                _ = wordClassIds.TryAdd(record.WordClasses, wordClassIds.Count);
                _ = stringIds.TryAdd(record.Spelling, stringIds.Count);
                foreach (string wordClass in record.WordClasses)
                {
                    _ = stringIds.TryAdd(wordClass, stringIds.Count);
                }

                if (record.Readings is not null)
                {
                    maximumArrayLength = Math.Max(maximumArrayLength, record.Readings.Length);
                    foreach (string reading in record.Readings)
                    {
                        _ = stringIds.TryAdd(reading, stringIds.Count);
                    }
                }
            }
        }

        // Expand each distinct source record once; reading buckets contain no additional records to process.
        foreach (JmdictWordClass record in recordIds.Keys)
        {
            if (record.Readings is null)
            {
                continue;
            }

            foreach (string reading in record.Readings)
            {
                string key = JapaneseUtils.NormalizeText(reading).GetPooledString();
                ref List<JmdictWordClass>? records = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, key, out bool exists);
                if (exists)
                {
                    Debug.Assert(records is not null);
                    records.Add(record);
                }
                else
                {
                    records = [record];
                }
            }
        }

        HashSet<int> uniqueRecordIds = [];
        foreach (List<JmdictWordClass> records in dictionary.Values)
        {
            int count = records.Count;
            if (count <= 1)
            {
                maximumArrayLength = Math.Max(maximumArrayLength, count);
                continue;
            }

            uniqueRecordIds.Clear();
            Span<JmdictWordClass> recordsSpan = CollectionsMarshal.AsSpan(records);
            int uniqueCount = 0;
            for (int i = 0; i < count; i++)
            {
                JmdictWordClass record = recordsSpan[i];
                if (uniqueRecordIds.Add(recordIds[record]))
                {
                    recordsSpan[uniqueCount++] = record;
                }
            }
            maximumArrayLength = Math.Max(maximumArrayLength, uniqueCount);
            if (uniqueCount < count)
            {
                records.RemoveRange(uniqueCount, count - uniqueCount);
            }
        }

        foreach (string key in dictionary.Keys)
        {
            _ = stringIds.TryAdd(key, stringIds.Count);
        }

        // Class arrays, readings and buckets share a width; table counts remain 32-bit.
        byte arrayLengthWidth = GetLengthWidth(maximumArrayLength);
        byte stringIdWidth = GetIdWidth(stringIds.Count);
        byte wordClassIdWidth = GetIdWidth(wordClassIds.Count);
        byte recordIdWidth = GetIdWidth(recordIds.Count);
        string temporaryPath = PathUtils.GetTempPath(path);
        using (FileStream fileStream = new(temporaryPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 64 * 1024))
        {
            using BinaryWriter writer = new(fileStream, TextUtils.Utf8NoBom, true);
            writer.Write(Version);
            writer.Write(stringIdWidth);
            writer.Write(wordClassIdWidth);
            writer.Write(recordIdWidth);
            writer.Write(arrayLengthWidth);

            writer.Write(stringIds.Count);
            foreach (string value in stringIds.Keys)
            {
                writer.Write(value);
            }

            writer.Write(wordClassIds.Count);
            foreach (string[] wordClasses in wordClassIds.Keys)
            {
                WriteStrings(writer, wordClasses, stringIds, stringIdWidth, arrayLengthWidth);
            }

            writer.Write(recordIds.Count);
            foreach (JmdictWordClass record in recordIds.Keys)
            {
                WriteNumber(writer, stringIds[record.Spelling], stringIdWidth);
                WriteNumber(writer, wordClassIds[record.WordClasses], wordClassIdWidth);
                writer.Write(record.Readings is not null);
                if (record.Readings is not null)
                {
                    WriteStrings(writer, record.Readings, stringIds, stringIdWidth, arrayLengthWidth);
                }
            }

            // Write insertion order rather than FrozenDictionary enumeration: it affects hash-bucket order when loading.
            writer.Write(dictionary.Count);
            foreach ((string key, List<JmdictWordClass> records) in dictionary)
            {
                WriteNumber(writer, stringIds[key], stringIdWidth);
                WriteNumber(writer, records.Count, arrayLengthWidth);
                foreach (JmdictWordClass record in records.AsReadOnlySpan())
                {
                    WriteNumber(writer, recordIds[record], recordIdWidth);
                }
            }

            writer.Flush();
            fileStream.Position = 0;
            Span<byte> checksum = stackalloc byte[ChecksumLength];
            _ = SHA256.HashData(fileStream, checksum);
            writer.Write(checksum);
        }

        PathUtils.ReplaceFileAtomicallyOnSameVolume(path, temporaryPath);
    }

    internal static async Task<FrozenDictionary<string, IList<JmdictWordClass>>?> Deserialize(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            byte[] data = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
            return Deserialize(data);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException or ArgumentException or IndexOutOfRangeException)
        {
            LoggerManager.Logger.Error(ex, "Couldn't read word-class data from '{Path}'", path);
            return null;
        }
    }

    private static FrozenDictionary<string, IList<JmdictWordClass>>? Deserialize(byte[] data)
    {
        // Version, three ID widths and array-length width, four table counts, and the checksum.
        const int minimumLength = (sizeof(int) * 5) + 4 + ChecksumLength;
        if (data.Length < minimumLength)
        {
            return null;
        }

        using MemoryStream stream = new(data, 0, data.Length - ChecksumLength, false);
        using BinaryReader reader = new(stream, TextUtils.Utf8NoBom);
        if (reader.ReadInt32() != Version)
        {
            return null;
        }

        byte stringIdWidth = reader.ReadByte();
        byte wordClassIdWidth = reader.ReadByte();
        byte recordIdWidth = reader.ReadByte();
        byte arrayLengthWidth = reader.ReadByte();
        if (stringIdWidth is not (1 or 2 or 4) || wordClassIdWidth is not (1 or 2 or 4) || recordIdWidth is not (1 or 2 or 4) || arrayLengthWidth is not (1 or 2 or 4))
        {
            return null;
        }

        Span<byte> checksum = stackalloc byte[ChecksumLength];
        _ = SHA256.HashData(data.AsSpan(0, data.Length - ChecksumLength), checksum);
        if (!checksum.SequenceEqual(data.AsSpan(data.Length - ChecksumLength)))
        {
            return null;
        }

        int stringCount = ReadCount(reader, 1);
        if (stringCount < 0)
        {
            return null;
        }
        string[] strings = new string[stringCount];
        for (int i = 0; i < strings.Length; i++)
        {
            strings[i] = reader.ReadString().GetPooledString();
        }

        int wordClassCount = ReadCount(reader, arrayLengthWidth);
        if (wordClassCount < 0)
        {
            return null;
        }
        string[][] wordClasses = new string[wordClassCount][];
        for (int i = 0; i < wordClasses.Length; i++)
        {
            string[]? classes = ReadStrings(reader, strings, stringIdWidth, arrayLengthWidth);
            if (classes is null)
            {
                return null;
            }
            wordClasses[i] = classes;
        }

        int recordCount = ReadCount(reader, stringIdWidth + wordClassIdWidth + sizeof(bool));
        if (recordCount < 0)
        {
            return null;
        }
        JmdictWordClass[] records = new JmdictWordClass[recordCount];
        for (int i = 0; i < records.Length; i++)
        {
            string spelling = strings[ReadNumber(reader, stringIdWidth)];
            int wordClassId = ReadNumber(reader, wordClassIdWidth);
            string[]? readings = null;
            if (reader.ReadBoolean())
            {
                readings = ReadStrings(reader, strings, stringIdWidth, arrayLengthWidth);
                if (readings is null)
                {
                    return null;
                }
            }
            records[i] = new JmdictWordClass(spelling, wordClasses[wordClassId], readings);
        }

        int keyCount = ReadCount(reader, stringIdWidth + arrayLengthWidth);
        if (keyCount < 0)
        {
            return null;
        }
        Dictionary<string, IList<JmdictWordClass>> dictionary = new(keyCount, StringComparer.Ordinal);
        for (int i = 0; i < keyCount; i++)
        {
            string key = strings[ReadNumber(reader, stringIdWidth)];
            int count = ReadArrayLength(reader, recordIdWidth, arrayLengthWidth);
            if (count < 0)
            {
                return null;
            }
            JmdictWordClass[] bucket = new JmdictWordClass[count];
            for (int j = 0; j < bucket.Length; j++)
            {
                bucket[j] = records[ReadNumber(reader, recordIdWidth)];
            }
            dictionary.Add(key, bucket);
        }

        return stream.Position == stream.Length
            ? dictionary.ToFrozenDictionary(static entry => entry.Key, static entry => entry.Value, StringComparer.Ordinal)
            : null;
    }

    private static int ReadCount(BinaryReader reader, int minimumEntryLength)
    {
        int count = reader.ReadInt32();
        return count >= 0 && (long)count * minimumEntryLength <= reader.BaseStream.Length - reader.BaseStream.Position ? count : -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ReadArrayLength(BinaryReader reader, int elementWidth, byte lengthWidth)
    {
        int count = ReadNumber(reader, lengthWidth);
        return count >= 0 && (long)count * elementWidth <= reader.BaseStream.Length - reader.BaseStream.Position ? count : -1;
    }

    private static string[]? ReadStrings(BinaryReader reader, string[] strings, byte width, byte lengthWidth)
    {
        int count = ReadArrayLength(reader, width, lengthWidth);
        if (count < 0)
        {
            return null;
        }
        string[] values = new string[count];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = strings[ReadNumber(reader, width)];
        }
        return values;
    }

    private static byte GetLengthWidth(int maximumLength)
    {
        return maximumLength switch
        {
            <= byte.MaxValue => 1,
            <= ushort.MaxValue => 2,
            _ => 4
        };
    }

    private static byte GetIdWidth(int count)
    {
        // IDs start at zero, so the entry count can be one greater than the largest ID.
        return count switch
        {
            <= byte.MaxValue + 1 => 1,
            <= ushort.MaxValue + 1 => 2,
            _ => 4
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ReadNumber(BinaryReader reader, byte width)
    {
        return width switch
        {
            1 => reader.ReadByte(),
            2 => reader.ReadUInt16(),
            _ => reader.ReadInt32()
        };
    }

    private static void WriteStrings(BinaryWriter writer, string[] strings, Dictionary<string, int> stringIds, byte width, byte lengthWidth)
    {
        WriteNumber(writer, strings.Length, lengthWidth);
        foreach (string value in strings)
        {
            WriteNumber(writer, stringIds[value], width);
        }
    }

    private static void WriteNumber(BinaryWriter writer, int value, byte width)
    {
        switch (width)
        {
            case 1:
                writer.Write((byte)value);
                break;
            case 2:
                writer.Write((ushort)value);
                break;
            default:
                writer.Write(value);
                break;
        }
    }
}
