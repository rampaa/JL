using System.ComponentModel;
using System.Reflection;

namespace JL.Core.Utilities;

public static class ExtensionMethods
{
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
            }
        }

        return null;
    }

    public static T GetEnum<T>(this string description) where T : Enum
    {
        foreach (FieldInfo field in typeof(T).GetFields())
        {
            if (Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
            {
                if (attribute.Description == description)
                    return (T)field.GetValue(null)!;
            }
            else
            {
                if (field.Name == description)
                    return (T)field.GetValue(null)!;
            }
        }

        throw new ArgumentException("Not found.", nameof(description));
        // return default;
    }

    public static IEnumerable<string> UnicodeIterator(this string s)
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
