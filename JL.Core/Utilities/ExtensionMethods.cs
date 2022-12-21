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
        if (name != null)
        {
            FieldInfo? field = type.GetField(name);
            if (field != null)
            {
                if (Attribute.GetCustomAttribute(field,
                        typeof(DescriptionAttribute)) is DescriptionAttribute attr)
                {
                    return attr.Description;
                }
                else
                {
                    return name;
                }
            }
        }

        return null;
    }

    public static T GetEnum<T>(this string description) where T : Enum
    {
        foreach (T enumItem in Enum.GetValues(typeof(T)))
        {
            if (enumItem.GetDescription() == description)
            {
                return enumItem;
            }
        }

        throw new ArgumentException("Not found.", nameof(description));
        // return default;
    }

    public static IEnumerable<string> EnumerateUnicodeCharacters(this string s)
    {
        for (int i = 0; i < s.Length; ++i)
        {
            if (char.IsHighSurrogate(s, i)
                && s.Length > i + 1
                && char.IsLowSurrogate(s, i + 1))
            {
                yield return char.ConvertFromUtf32(char.ConvertToUtf32(s, i));
                ++i;
            }
            else
            {
                yield return s[i].ToString();
            }
        }
    }
}
