using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;

namespace JL.Core.Utilities;

public static class ExtensionMethods
{
    // Falls back to name
    public static string? GetDescription(this Enum value)
    {
        Type type = value.GetType();
        string? name = Enum.GetName(type, value);
        if (name is not null)
        {
            FieldInfo? fieldInfo = type.GetField(name);
            if (fieldInfo is not null)
            {
                return Attribute.GetCustomAttribute(fieldInfo, typeof(DescriptionAttribute)) is DescriptionAttribute attr
                    ? attr.Description
                    : name;
            }
        }

        return null;
    }

    public static T GetEnum<T>(this string description) where T : struct, Enum
    {
        foreach (T enumValue in Enum.GetValues<T>())
        {
            if (enumValue.GetDescription() == description)
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
            if (char.IsHighSurrogate(highSurrogateCandidate)
                && text.Length > i + 1)
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

        return CollectionsMarshal.AsSpan(textBlocks);
    }

    internal static void AddRange<T>(this ConcurrentBag<T> source, IEnumerable<T> items)
    {
        foreach (T item in items)
        {
            source.Add(item);
        }
    }

    internal static T[]? RemoveAt<T>(this T[] source, int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, source.Length);

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
            Array.Copy(source, destination, index);
        }

        if (index < source.Length - 1)
        {
            Array.Copy(source, index + 1, destination, index, source.Length - index - 1);
        }

        return destination;
    }

    internal static T[]? RemoveAtToArray<T>(this List<T> list, int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, list.Count);

        if (list.Count is 1)
        {
            return null;
        }

        ReadOnlySpan<T> listSpan = CollectionsMarshal.AsSpan(list);

        bool allElementsAreNull = true;
        for (int i = 0; i < listSpan.Length; i++)
        {
            if (i != index && listSpan[i] is not null)
            {
                allElementsAreNull = false;
                break;
            }
        }

        if (allElementsAreNull)
        {
            return null;
        }

        T[] array = new T[listSpan.Length - 1];

        int arrayIndex = 0;
        for (int i = 0; i < listSpan.Length; i++)
        {
            if (i != index)
            {
                array[arrayIndex] = listSpan[i];
                ++arrayIndex;
            }
        }

        return array;
    }

    internal static T[]? TrimToArray<T>(this List<T> list) where T : notnull
    {
        return list.Count is 0
            ? null
            : list.ToArray();
    }

    internal static T?[]? TrimListOfNullableElementsToArray<T>(this List<T?> list) where T : class
    {
        if (list.Count is 0)
        {
            return null;
        }

        bool allElementsAreNull = true;
        foreach (T? item in CollectionsMarshal.AsSpan(list))
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

        return CollectionsMarshal.AsSpan(indexes);
    }

    public static bool SequenceEqual<T>(this List<T> source, List<T> target)
    {
        return MemoryExtensions.SequenceEqual(CollectionsMarshal.AsSpan(source), CollectionsMarshal.AsSpan(target));
    }
}
