using System.Reflection;
using System.Speech.Synthesis;
using JL.Core.Utilities;

namespace JL.Windows.SpeechSynthesis;

internal static class SpeechSynthesisReflectionUtils
{
    private const string VoiceSynthesizerProperty = "VoiceSynthesizer";
    private const string InstalledVoicesField = "_installedVoices";
    private const string OneCoreVoicesRegistry = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech_OneCore\Voices";

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
            Type objectTokenCategoryType = typeof(SpeechSynthesizer).Assembly
                .GetType("System.Speech.Internal.ObjectTokens.ObjectTokenCategory")!;

            Type voiceInfoType = typeof(SpeechSynthesizer).Assembly
                .GetType("System.Speech.Synthesis.VoiceInfo")!;

            Type installedVoiceType = typeof(SpeechSynthesizer).Assembly
                .GetType("System.Speech.Synthesis.InstalledVoice")!;

            object voiceSynthesizer = GetProperty(synthesizer, VoiceSynthesizerProperty)!;
            List<InstalledVoice> installedVoices = (List<InstalledVoice>?)GetField(voiceSynthesizer, InstalledVoicesField)!;

            using IDisposable objectTokenCategory = (IDisposable?)objectTokenCategoryType
                    .GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)?
                    .Invoke(null, new object[] { OneCoreVoicesRegistry })!;

            IEnumerable<object?> tokens = (IEnumerable<object?>?)objectTokenCategoryType
                .GetMethod("FindMatchingTokens", BindingFlags.Instance | BindingFlags.NonPublic)?
                .Invoke(objectTokenCategory, new object?[] { null, null })!;

            foreach (object? token in tokens)
            {
                if (token is null || GetProperty(token, "Attributes") is null)
                {
                    continue;
                }

                object voiceInfo = typeof(SpeechSynthesizer).Assembly
                    .CreateInstance(voiceInfoType.FullName!, true, BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { token }, null, null)!;

                InstalledVoice installedVoice = (InstalledVoice)typeof(SpeechSynthesizer).Assembly
                    .CreateInstance(installedVoiceType.FullName!, true, BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { voiceSynthesizer, voiceInfo }, null, null)!;

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
