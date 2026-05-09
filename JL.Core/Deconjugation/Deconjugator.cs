using System.Collections.Frozen;
using JL.Core.Utilities;

namespace JL.Core.Deconjugation;

// Modified from https://github.com/wareya/nazeka/blob/master/background-script.js
internal static class Deconjugator
{
    internal static FrozenDictionary<char, VirtualRule[]> s_index = FrozenDictionary<char, VirtualRule[]>.Empty;
    internal static VirtualRule[] s_universalRules = [];

    private static void ApplyRule(in Form form, in VirtualRule rule, List<Form> discoveredForms, ReadOnlySpan<char> textSpan)
    {
        if (rule.Type is RuleType.OnlyFinal)
        {
            if (form.Process.Count is not 0)
            {
                return;
            }
        }
        else if (rule.Type is RuleType.Rewrite)
        {
            if (form.Text != rule.ConEnd)
            {
                return;
            }
        }
        else if (rule.Type is RuleType.NeverFinal)
        {
            if (form.Process.Count is 0)
            {
                return;
            }
        }

        // can't deconjugate nothingness
        if (textSpan.Length is 0)
        {
            return;
        }

        // deconjugated form too much longer than conjugated form
        if (textSpan.Length > form.OriginalText.Length + 10)
        {
            return;
        }

        // impossibly information-dense
        if (form.Process.Count > form.OriginalText.Length + 5)
        {
            return;
        }

        if (textSpan.Length == rule.ConEnd.Length && rule.DecEnd.Length is 0)
        {
            return;
        }

        // blank detail mean it can't be the last (first applied, but rightmost) rule
        if (rule.Detail.Length is 0 && form.LastTag.Length is 0)
        {
            return;
        }

        // tag doesn't match
        if (form.Process.Count > 0 && form.LastTag != rule.ConTag)
        {
            return;
        }

        // ending doesn't match
        if (!textSpan.EndsWith(rule.ConEnd.AsSpan(), StringComparison.Ordinal))
        {
            return;
        }

        int stemLength = textSpan.Length - rule.ConEnd.Length;
        ReadOnlySpan<char> stem = textSpan[..stemLength];

        if (DiscoveredFormsContain(discoveredForms.AsReadOnlySpan(), stem, rule.DecEnd, rule.DecTag, form.Process, rule.Detail))
        {
            return;
        }

        string newText = string.Concat(stem, rule.DecEnd);
        discoveredForms.Add(new Form(newText, form.OriginalText, rule.DecTag, [.. form.Process, rule.Detail]));
    }

    private static bool DiscoveredFormsContain(ReadOnlySpan<Form> discoveredForms, ReadOnlySpan<char> stem, string decEnd, string tag, List<string> parentProcess, string newDetail)
    {
        int targetTextLength = stem.Length + decEnd.Length;
        int targetProcessLength = parentProcess.Count + 1;

        for (int i = 0; i < discoveredForms.Length; i++)
        {
            ref readonly Form form = ref discoveredForms[i];
            if (form.LastTag == tag && form.Text.Length == targetTextLength)
            {
                ReadOnlySpan<char> formText = form.Text.AsSpan();
                if (formText.StartsWith(stem) && formText[stem.Length..].Equals(decEnd, StringComparison.Ordinal))
                {
                    if (form.Process.Count == targetProcessLength && form.Process[^1] == newDetail)
                    {
                        if (form.Process.AsReadOnlySpan()[..^1].SequenceEqual(parentProcess.AsReadOnlySpan()))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    public static List<Form> Deconjugate(string text)
    {
        List<Form> processedForms = [];
        List<Form> formsToProcess = [new(text, text, "", [])];
        List<Form> newFormsToProcess = [];

        bool addFormToProcess = false;
        while (formsToProcess.Count > 0)
        {
            newFormsToProcess.Clear();
            ReadOnlySpan<Form> formsToProcessSpan = formsToProcess.AsReadOnlySpan();

            VirtualRule[] universalRules = s_universalRules;
            int universalCount = universalRules.Length;

            for (int k = 0; k < formsToProcessSpan.Length; k++)
            {
                ref readonly Form form = ref formsToProcessSpan[k];
                ReadOnlySpan<char> textSpan = form.Text.AsSpan();

                if (textSpan.Length is not 0)
                {
                    if (s_index.TryGetValue(textSpan[^1], out VirtualRule[]? bucket))
                    {
                        for (int j = 0; j < bucket.Length; j++)
                        {
                            ApplyRule(form, bucket[j], newFormsToProcess, textSpan);
                        }
                    }

                    for (int j = 0; j < universalCount; j++)
                    {
                        ApplyRule(form, universalRules[j], newFormsToProcess, textSpan);
                    }
                }

                if (addFormToProcess)
                {
                    bool add = true;
                    int formProcessCount = -1;
                    string formTag = form.LastTag;

                    ReadOnlySpan<Form> processedFormsSpan = processedForms.AsReadOnlySpan();
                    for (int i = processedForms.Count - 1; i >= 0; i--)
                    {
                        ref readonly Form existingForm = ref processedFormsSpan[i];
                        if (existingForm.Text == form.Text && existingForm.LastTag == formTag)
                        {
                            int existingFormProcessCount = 1;
                            for (int j = existingForm.Process.Count - 1; j > 0; j--)
                            {
                                string existingFormProcess = existingForm.Process[j];
                                if (existingFormProcess.Length > 0 && existingFormProcess[0] is not '(')
                                {
                                    ++existingFormProcessCount;
                                }
                            }

                            if (formProcessCount is -1)
                            {
                                formProcessCount = 1;
                                for (int j = form.Process.Count - 1; j > 0; j--)
                                {
                                    string formProcess = form.Process[j];
                                    if (formProcess.Length > 0 && formProcess[0] is not '(')
                                    {
                                        ++formProcessCount;
                                    }
                                }
                            }

                            if (existingFormProcessCount < formProcessCount)
                            {
                                add = false;
                                break;
                            }

                            if (existingFormProcessCount > formProcessCount)
                            {
                                processedForms.RemoveAt(i);
                                processedFormsSpan = processedForms.AsReadOnlySpan();
                            }
                        }
                    }

                    if (add)
                    {
                        processedForms.Add(form);
                    }
                }
                else
                {
                    addFormToProcess = true;
                }
            }

            formsToProcess.Clear();
            (newFormsToProcess, formsToProcess) = (formsToProcess, newFormsToProcess);
        }

        return processedForms;
    }
}
