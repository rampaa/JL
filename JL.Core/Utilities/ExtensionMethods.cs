using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MessagePack;
using Microsoft.Data.Sqlite;

namespace JL.Core.Utilities;

public static class ExtensionMethods
{
    // Falls back to name
    public static string GetDescription<T>(this T value) where T : struct, Enum
    {
        string? name = Enum.GetName(value);
        Debug.Assert(name is not null);
        Type enumType = typeof(T);
        return enumType.GetField(name)?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? name;
    }

    public static T GetEnum<T>(this string description) where T : struct, Enum
    {
        foreach (T enumValue in Enum.GetValues<T>())
        {
            if (description == Enum.GetName(enumValue) || description == enumValue.GetDescription())
            {
                return enumValue;
            }
        }

        throw new ArgumentException("Enum not found", nameof(description));
        // return default;
    }

    internal static ReadOnlySpan<string> ListUnicodeCharacters(this ReadOnlySpan<char> text)
    {
        List<string> textBlocks = new(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char highSurrogateCandidate = text[i];
            if (char.IsHighSurrogate(highSurrogateCandidate) && i < text.Length - 1)
            {
                char lowSurrogateCandidate = text[i + 1];
                if (char.IsLowSurrogate(lowSurrogateCandidate))
                {
                    textBlocks.Add(char.ConvertFromUtf32(char.ConvertToUtf32(highSurrogateCandidate, lowSurrogateCandidate)));
                    ++i;
                }
                else
                {
                    textBlocks.Add(highSurrogateCandidate.ToString());
                }
            }
            else
            {
                textBlocks.Add(highSurrogateCandidate.ToString());
            }
        }

        return textBlocks.AsReadOnlySpan();
    }

    internal static void AddRange<T>(this ConcurrentBag<T> source, List<T> items) where T : notnull
    {
        foreach (T item in items.AsReadOnlySpan())
        {
            source.Add(item);
        }
    }

    internal static T[]? RemoveAt<T>(this T[] source, int index) where T : notnull
    {
        Debug.Assert(source.Length > index);

        if (source.Length is 1)
        {
            return null;
        }

        T[] destination = new T[source.Length - 1];
        if (index > 0)
        {
            source.AsReadOnlySpan(0, index).CopyTo(destination.AsSpan());
        }

        if (index < source.Length - 1)
        {
            source.AsReadOnlySpan(index + 1, source.Length - index - 1).CopyTo(destination.AsSpan(index));
        }

        return destination;
    }

    internal static T?[]? RemoveAtNullable<T>(this T[] source, int index) where T : class?
    {
        Debug.Assert(source.Length > index);

        if (source.Length is 1)
        {
            return null;
        }

        bool allElementsAreNull = true;
        for (int i = 0; i < source.Length; i++)
        {
            if (i != index && source[i] is not null)
            {
                allElementsAreNull = false;
                break;
            }
        }

        if (allElementsAreNull)
        {
            return null;
        }

        T[] destination = new T[source.Length - 1];
        if (index > 0)
        {
            source.AsReadOnlySpan(0, index).CopyTo(destination.AsSpan());
        }

        if (index < source.Length - 1)
        {
            source.AsReadOnlySpan(index + 1, source.Length - index - 1).CopyTo(destination.AsSpan(index));
        }

        return destination;
    }

    internal static T[]? RemoveAtToArray<T>(this List<T> list, int index) where T : notnull
    {
        Debug.Assert(list.Count > index);
        if (list.Count is 1)
        {
            return null;
        }

        ReadOnlySpan<T> listSpan = list.AsReadOnlySpan();
        T[] array = new T[listSpan.Length - 1];
        if (index > 0)
        {
            listSpan[..index].CopyTo(array.AsSpan());
        }
        if (index < listSpan.Length - 1)
        {
            listSpan[(index + 1)..].CopyTo(array.AsSpan(index));
        }

        return array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T[]? TrimToArray<T>(this List<T> list) where T : notnull
    {
        return list.Count is 0
            ? null
            : list.ToArray();
    }

    internal static T?[]? TrimListOfNullableElementsToArray<T>(this List<T> list) where T : class?
    {
        if (list.Count is 0)
        {
            return null;
        }

        bool allElementsAreNull = true;
        foreach (T? item in list.AsReadOnlySpan())
        {
            if (item is not null)
            {
                allElementsAreNull = false;
                break;
            }
        }

        return allElementsAreNull
            ? null
            : list.ToArray();
    }

    internal static string GetPooledString(this string str)
    {
        return Utils.StringPoolInstance.GetOrAdd(str);
    }

    internal static void DeduplicateStringsInArray(this string[] strings)
    {
        for (int i = 0; i < strings.Length; i++)
        {
            strings[i] = strings[i].GetPooledString();
        }
    }

    internal static int IndexOf(this ReadOnlySpan<char> text, char value, int startIndex)
    {
        int index = text[startIndex..].IndexOf(value);
        return index < 0
            ? -1
            : index + startIndex;
    }

    internal static int IndexOf(this ReadOnlySpan<char> text, ReadOnlySpan<char> value, int startIndex)
    {
        int index = text[startIndex..].IndexOf(value);
        return index < 0
            ? -1
            : index + startIndex;
    }

    internal static int LastIndexOf(this ReadOnlySpan<char> text, char value, int startIndex)
    {
        return text[..startIndex].LastIndexOf(value);
    }

    //public static int LastIndexOf(this ReadOnlySpan<char> text, ReadOnlySpan<char> value, int startIndex)
    //{
    //    return text[..startIndex].LastIndexOf(value);
    //}

    internal static ReadOnlySpan<int> FindAllIndexes(this ReadOnlySpan<char> text, int startIndex, int length, ReadOnlySpan<char> value)
    {
        ReadOnlySpan<char> textToSearch = text.Slice(startIndex, length);

        List<int> indexes = new(textToSearch.Length);
        for (int i = textToSearch.IndexOf(value); i > -1; i = textToSearch.IndexOf(value, i + 1))
        {
            indexes.Add(i + startIndex);
        }

        return indexes.AsReadOnlySpan();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<T> AsReadOnlySpan<T>(this List<T>? list)
    {
        return CollectionsMarshal.AsSpan(list);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<T> AsReadOnlySpan<T>(this T[]? array)
    {
        return new ReadOnlySpan<T>(array);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<T> AsReadOnlySpan<T>(this T[]? array, int start, int length)
    {
        return new ReadOnlySpan<T>(array, start, length);
    }

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public static ReadOnlyMemory<T> AsReadOnlyMemory<T>(this T[]? array)
    //{
    //    return new ReadOnlyMemory<T>(array);
    //}

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public static ReadOnlyMemory<T> AsReadOnlyMemory<T>(this T[]? array, int start, int length)
    //{
    //    return new ReadOnlyMemory<T>(array, start, length);
    //}

    internal static T? GetNullableValueFromBlobStream<T>(this SqliteDataReader dataReader, int index) where T : class?
    {
        if (dataReader.IsDBNull(index))
        {
            return null;
        }

        using Stream stream = dataReader.GetStream(index);
        return MessagePackSerializer.Deserialize<T>(stream);
    }

    internal static T GetValueFromBlobStream<T>(this SqliteDataReader dataReader, int index) where T : notnull
    {
        using Stream stream = dataReader.GetStream(index);
        return MessagePackSerializer.Deserialize<T>(stream);
    }
}
