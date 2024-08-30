using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

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
        foreach (T enumItem in Enum.GetValues<T>())
        {
            if (enumItem.GetDescription() == description)
            {
                return enumItem;
            }
        }

        throw new ArgumentException("Enum not found", nameof(description));
        // return default;
    }

    internal static List<string> ListUnicodeCharacters(this ReadOnlySpan<char> text)
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

        return textBlocks;
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

        T[] array = new T[list.Count - 1];

        int arrayIndex = 0;
        int listCount = list.Count;
        for (int i = 0; i < listCount; i++)
        {
            if (i != index)
            {
                array[arrayIndex] = list[i];
                ++arrayIndex;
            }
        }

        return array;
    }

    internal static T?[]? RemoveAtToArrayNullable<T>(this List<T?> list, int index) where T : class
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, list.Count);

        if (list.Count is 1 || list.All(static l => l is null))
        {
            return null;
        }

        T?[] array = new T?[list.Count - 1];

        bool hasNonNullElement = false;
        int arrayIndex = 0;
        int listCount = list.Count;
        for (int i = 0; i < listCount; i++)
        {
            if (i != index)
            {
                T? element = list[i];
                array[arrayIndex] = element;
                ++arrayIndex;

                if (element is not null)
                {
                    hasNonNullElement = true;
                }
            }
        }

        return hasNonNullElement ? array : null;
    }

    internal static T[]? TrimListToArray<T>(this List<T> list)
    {
        return list.Count is 0
            ? null
            : list.ToArray();
    }

    internal static T[]?[]? TrimListOfNullableArraysToArrayOfArrays<T>(this List<T[]?> list)
    {
        return list.Count is 0 || list.All(static array => array is null)
            ? null
            : list.ToArray();
    }

    internal static T?[]? TrimListWithNullableElementsToArray<T>(this List<T?> list) where T : class
    {
        return list.Count is 0 || list.All(static element => element is null)
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

    public static bool Contains(this string[] source, string item)
    {
        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] == item)
            {
                return true;
            }
        }

        return false;
    }
}
