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
}
