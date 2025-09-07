using System.Text.RegularExpressions;
using JL.Core.Config;
using JL.Core.Frontend;

namespace JL.Core.Utilities;

public static partial class RegexReplacerUtils
{
    [GeneratedRegex(@"\|REGEX\|(?<regex>.+)\|BECOMES\|(?<replacement>.*)\|MODIFIER\|(?<modifiers>.*)\|END\|", RegexOptions.CultureInvariant)]
    private static partial Regex ReplacementRegex { get; }

    internal static List<KeyValuePair<Regex, string>>? s_regexReplacements;

    private static readonly string s_filePath = Path.Join(ProfileUtils.ProfileFolderPath, "Regex_Replacements.txt");

    public static string GetProfileSpecificFilePath()
    {
        return Path.Join(ProfileUtils.ProfileFolderPath, $"{ProfileUtils.CurrentProfileName}_Regex_Replacements.txt");
    }

    public static void PopulateRegexReplacements()
    {
        s_regexReplacements?.Clear();

        List<string> filePaths = new(2);

        if (File.Exists(s_filePath))
        {
            filePaths.Add(s_filePath);
        }

        string profilePath = GetProfileSpecificFilePath();
        if (File.Exists(profilePath))
        {
            filePaths.Add(profilePath);
        }

        if (filePaths.Count is 0)
        {
            if (s_regexReplacements?.Count is 0)
            {
                s_regexReplacements = null;
            }

            return;
        }

        s_regexReplacements = [];
        foreach (ref readonly string filePath in filePaths.AsReadOnlySpan())
        {
            foreach (string line in File.ReadLines(filePath))
            {
                Match match = ReplacementRegex.Match(line);
                if (match.Success)
                {
                    string regexPattern = match.Groups["regex"].Value;

                    ReadOnlySpan<char> modifiers = match.Groups["modifiers"].Value.AsSpan();
                    RegexOptions regexOptions = RegexOptions.Compiled;
                    foreach (char modifier in modifiers)
                    {
                        regexOptions |= modifier switch
                        {
                            'i' => RegexOptions.IgnoreCase,
                            'm' => RegexOptions.Multiline,
                            's' => RegexOptions.Singleline,
                            'n' => RegexOptions.ExplicitCapture,
                            'x' => RegexOptions.IgnorePatternWhitespace,
                            _ => RegexOptions.None
                        };
                    }

                    try
                    {
                        Regex regex = new(regexPattern, regexOptions);
                        s_regexReplacements.Add(KeyValuePair.Create(regex, match.Groups["replacement"].Value));
                    }
                    catch (ArgumentException e)
                    {
                        LoggerManager.Logger.Error(e, "Invalid RegEx: {RegexPattern}", regexPattern);
                        FrontendManager.Frontend.Alert(AlertLevel.Error, $"Invalid RegEx: {regexPattern}");
                    }
                }
            }
        }

        if (s_regexReplacements.Count is 0)
        {
            s_regexReplacements = null;
        }
    }
}
