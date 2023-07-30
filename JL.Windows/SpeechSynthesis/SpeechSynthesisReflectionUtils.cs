using System.Globalization;
using System.Reflection;
using System.Speech.Synthesis;
using JL.Core.Utilities;

namespace JL.Windows.SpeechSynthesis;

internal static class SpeechSynthesisReflectionUtils
{
    private const string VoiceSynthesizerProperty = "VoiceSynthesizer";
    private const string InstalledVoicesField = "_installedVoices";

    private const string OneCoreVoicesRegistry = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech_OneCore\Voices";

    private static readonly Type s_objectTokenCategoryType = typeof(SpeechSynthesizer).Assembly
        .GetType("System.Speech.Internal.ObjectTokens.ObjectTokenCategory")!;

    private static readonly Type s_voiceInfoType = typeof(SpeechSynthesizer).Assembly
        .GetType("System.Speech.Synthesis.VoiceInfo")!;

    private static readonly Type s_installedVoiceType = typeof(SpeechSynthesizer).Assembly
        .GetType("System.Speech.Synthesis.InstalledVoice")!;

    private static object? GetProperty(object target, string propName)
    {
        return target.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);
    }

    private static object? GetField(object target, string propName)
    {
        return target.GetType().GetField(propName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);
    }

    public static void InjectOneCoreVoices(this SpeechSynthesizer synthesizer)
    {
        try
        {
            object voiceSynthesizer = GetProperty(synthesizer, VoiceSynthesizerProperty) ?? throw new NotSupportedException(string.Create(CultureInfo.InvariantCulture, $"Property not found: {VoiceSynthesizerProperty}"));

            List<InstalledVoice> installedVoices = GetField(voiceSynthesizer, InstalledVoicesField) as List<InstalledVoice> ?? throw new NotSupportedException($"Field not found or null: {InstalledVoicesField}");

            using IDisposable objectTokenCategory = s_objectTokenCategoryType
                    .GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)?
                    .Invoke(null, new object?[] { OneCoreVoicesRegistry }) as IDisposable
                    ?? throw new NotSupportedException(string.Create(CultureInfo.InvariantCulture, $"Failed to call Create on {s_objectTokenCategoryType} instance"));

            IEnumerable<object?> tokens = s_objectTokenCategoryType
                .GetMethod("FindMatchingTokens", BindingFlags.Instance | BindingFlags.NonPublic)?
                .Invoke(objectTokenCategory, new object?[] { null, null }) as IEnumerable<object?> ?? throw new NotSupportedException("Failed to list matching tokens");

            foreach (object? token in tokens)
            {
                if (token is null || GetProperty(token, "Attributes") is null)
                {
                    continue;
                }

                object voiceInfo = typeof(SpeechSynthesizer).Assembly
                    .CreateInstance(s_voiceInfoType.FullName!, true, BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { token }, null, null)
                    ?? throw new NotSupportedException($"Failed to instantiate {s_voiceInfoType}");

                var installedVoice = (InstalledVoice)typeof(SpeechSynthesizer).Assembly
                    .CreateInstance(s_installedVoiceType.FullName!, true, BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { voiceSynthesizer, voiceInfo }, null, null)!;

                installedVoices.Add(installedVoice);
            }
        }

        catch (Exception ex)
        {
            Utils.Logger.Error(ex, "Injecting One Core voices failed");
            // Utils.Frontend.Alert(AlertLevel.Error, "Injecting One Core voices failed");
        }
    }
}
