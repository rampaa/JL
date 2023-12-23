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

        throw new ArgumentException("Not found.", nameof(description));
        // return default;
    }

    internal static List<string> ListUnicodeCharacters(this string s)
    {
        List<string> textBlocks = new(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            if (char.IsHighSurrogate(s, i)
                && s.Length > i + 1
                && char.IsLowSurrogate(s, i + 1))
            {
                textBlocks.Add(char.ConvertFromUtf32(char.ConvertToUtf32(s, i)));

                ++i;
            }
            else
            {
                textBlocks.Add(s[i].ToString());
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

    internal static T[] RemoveAt<T>(this T[] source, int index)
    {
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
        if (index >= list.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (list.Count is 1 || list.All(static l => l is null))
        {
            return null;
        }

        T[] array = new T[list.Count - 1];

        bool hasNonNullElement = false;
        int arrayIndex = 0;
        for (int i = 0; i < list.Count; i++)
        {
            if (i != index)
            {
                T element = list[i];
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

    internal static string[]? TrimStringListToStringArray(this List<string> list)
    {
        if (list.Count is 0 || list.All(string.IsNullOrEmpty))
        {
            return null;
        }

        return list.ToArray();
    }

    internal static T[]?[]? TrimListOfNullableArraysToArrayOfArrays<T>(this List<T[]?> list)
    {
        if (list.Count is 0 || list.All(static array => array is null))
        {
            return null;
        }

        return list.ToArray();
    }

    internal static T?[]? TrimListWithNullableElementsToArray<T>(this List<T?> list) where T : class
    {
        if (list.Count is 0 || list.All(static element => element is null))
        {
            return null;
        }

        return list.ToArray();
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

    internal static bool Contains(this string[] source, string item)
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
