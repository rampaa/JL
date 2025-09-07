using System.Diagnostics;
using System.Reflection;
using System.Runtime.Remoting;
using System.Speech.Synthesis;
using JL.Core.Utilities;

namespace JL.Windows.SpeechSynthesis;

// TODO: Get rid of this when migrating to .NET 10, see https://github.com/dotnet/runtime/pull/110123
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
            Assembly? speechSynthesizerAssembly = Assembly.GetAssembly(typeof(SpeechSynthesizer));
            Debug.Assert(speechSynthesizerAssembly is not null);

            Type? objectTokenCategoryType = speechSynthesizerAssembly.GetType("System.Speech.Internal.ObjectTokens.ObjectTokenCategory");
            Debug.Assert(objectTokenCategoryType is not null);

            Type? voiceInfoType = speechSynthesizerAssembly.GetType("System.Speech.Synthesis.VoiceInfo");
            Debug.Assert(voiceInfoType is not null);

            Type? installedVoiceType = speechSynthesizerAssembly.GetType("System.Speech.Synthesis.InstalledVoice");
            Debug.Assert(installedVoiceType is not null);

            object? voiceSynthesizer = GetProperty(synthesizer, VoiceSynthesizerProperty);
            Debug.Assert(voiceSynthesizer is not null);

            List<InstalledVoice>? installedVoices = (List<InstalledVoice>?)GetField(voiceSynthesizer, InstalledVoicesField);
            Debug.Assert(installedVoices is not null);

            using IDisposable? objectTokenCategory = (IDisposable?)objectTokenCategoryType
                .GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)?
                .Invoke(null, [OneCoreVoicesRegistry]);

            Debug.Assert(objectTokenCategory is not null);

            IEnumerable<object?>? tokens = (IEnumerable<object?>?)objectTokenCategoryType
                .GetMethod("FindMatchingTokens", BindingFlags.Instance | BindingFlags.NonPublic)?
                .Invoke(objectTokenCategory, [null, null]);

            Debug.Assert(tokens is not null);

            foreach (object? token in tokens)
            {
                if (token is null || GetProperty(token, "Attributes") is null)
                {
                    continue;
                }

                Debug.Assert(voiceInfoType.Assembly.FullName is not null);
                Debug.Assert(voiceInfoType.FullName is not null);

                ObjectHandle? voiceInfoObjectHandle = Activator.CreateInstance(voiceInfoType.Assembly.FullName, voiceInfoType.FullName, true, BindingFlags.Instance | BindingFlags.NonPublic, null, [token], null, null);
                Debug.Assert(voiceInfoObjectHandle is not null);

                VoiceInfo? voiceInfo = (VoiceInfo?)voiceInfoObjectHandle.Unwrap();
                Debug.Assert(voiceInfo is not null);

                Debug.Assert(installedVoiceType.Assembly.FullName is not null);
                Debug.Assert(installedVoiceType.FullName is not null);

                ObjectHandle? installedVoiceObjectHandle = Activator.CreateInstance(installedVoiceType.Assembly.FullName, installedVoiceType.FullName, true, BindingFlags.Instance | BindingFlags.NonPublic, null, [voiceSynthesizer, voiceInfo], null, null);
                Debug.Assert(installedVoiceObjectHandle is not null);

                InstalledVoice? installedVoice = (InstalledVoice?)installedVoiceObjectHandle.Unwrap();
                Debug.Assert(installedVoice is not null);

                installedVoices.Add(installedVoice);
            }
        }

        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Injecting One Core voices failed");
            // FrontendManager.Frontend.Alert(AlertLevel.Error, "Injecting One Core voices failed");
        }
    }
}
