using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.InteropServices;
using JL.Core.Lookup;
using JL.Core.Utilities;

namespace JL.Core.Deconjugation;

// Modified from https://github.com/wareya/nazeka/blob/master/background-script.js
internal static class Deconjugator
{
    internal static FrozenDictionary<char, RuleBucket> RuleBucketsByLastDecEndChar { get; set; } = FrozenDictionary<char, RuleBucket>.Empty;
    internal static RuleBucket RulesWithEmptyConEnd { get; set; }

    private static void ApplyRuleInternal(in Form form, in VirtualRule rule, List<Form> discoveredForms, ReadOnlySpan<char> textSpan)
    {
        if (rule.Type is RuleType.OnlyFinal)
        {
            if (form.LastTag.Length is not 0)
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
            if (form.LastTag.Length is 0)
            {
                return;
            }
        }

        // deconjugated form too much longer than conjugated form
        if (textSpan.Length > form.OriginalText.Length + 10)
        {
            return;
        }

        if (textSpan.Length == rule.ConEnd.Length && rule.DecEnd.Length is 0)
        {
            return;
        }

        // impossibly information-dense
        if ((form.Process?.TotalStepCount ?? 0) > form.OriginalText.Length + 6)
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
        discoveredForms.Add(new Form(newText, form.OriginalText, rule.DecTag, new ProcessNode(rule.Detail, form.Process)));
    }

    private static void ApplyBucket(in Form form, in RuleBucket bucket, List<Form> discoveredForms, ReadOnlySpan<char> textSpan)
    {
        if (form.Process is null)
        {
            ReadOnlySpan<VirtualRule> rules = bucket.AllRules.AsSpan();
            for (int i = 0; i < rules.Length; i++)
            {
                ApplyRuleInternal(form, rules[i], discoveredForms, textSpan);
            }
        }
        else if (bucket.RulesByTag.TryGetValue(form.LastTag, out VirtualRule[]? rules))
        {
            ReadOnlySpan<VirtualRule> rulesSpan = rules.AsSpan();
            for (int i = 0; i < rulesSpan.Length; i++)
            {
                ApplyRuleInternal(form, rulesSpan[i], discoveredForms, textSpan);
            }
        }
    }

    private static bool DiscoveredFormsContain(ReadOnlySpan<Form> discoveredForms, ReadOnlySpan<char> stem, string decEnd, string tag, ProcessNode? parentProcessNode, string newDetail)
    {
        int targetTextLength = stem.Length + decEnd.Length;
        int targetTotalCount = (parentProcessNode?.TotalStepCount ?? 0) + 1;

        for (int i = 0; i < discoveredForms.Length; i++)
        {
            ref readonly Form form = ref discoveredForms[i];
            if (form.LastTag == tag && form.Text.Length == targetTextLength)
            {
                ReadOnlySpan<char> formText = form.Text.AsSpan();
                if (formText.StartsWith(stem) && formText[stem.Length..].Equals(decEnd, StringComparison.Ordinal))
                {
                    ProcessNode? currentProcess = form.Process;
                    if (currentProcess is not null && currentProcess.TotalStepCount == targetTotalCount && currentProcess.Detail == newDetail && ReferenceEquals(currentProcess.Parent, parentProcessNode))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public static List<Form> Deconjugate(string text)
    {
        List<Form> processedForms = [];
        List<Form> formsToProcess = [new(text, text, "", null)];
        List<Form> newFormsToProcess = [];
        FrozenDictionary<char, RuleBucket> ruleBucketsByLastDecEndChar = RuleBucketsByLastDecEndChar;

        bool addFormToProcess = false;
        while (formsToProcess.Count > 0)
        {
            newFormsToProcess.Clear();
            ReadOnlySpan<Form> formsToProcessSpan = formsToProcess.AsReadOnlySpan();

            for (int k = 0; k < formsToProcessSpan.Length; k++)
            {
                ref readonly Form form = ref formsToProcessSpan[k];
                ReadOnlySpan<char> textSpan = form.Text.AsSpan();

                if (textSpan.Length is not 0)
                {
                    if (ruleBucketsByLastDecEndChar.TryGetValue(textSpan[^1], out RuleBucket bucket))
                    {
                        ApplyBucket(form, bucket, newFormsToProcess, textSpan);
                    }
                }

                ApplyBucket(form, RulesWithEmptyConEnd, newFormsToProcess, textSpan);

                if (addFormToProcess)
                {
                    bool add = true;
                    string formTag = form.LastTag;

                    Debug.Assert(form.Process is not null);
                    int newFormProperStepCount = form.Process.ProperStepCount;

                    ReadOnlySpan<Form> processedFormsSpan = processedForms.AsReadOnlySpan();
                    for (int i = processedFormsSpan.Length - 1; i >= 0; i--)
                    {
                        ref readonly Form existingForm = ref processedFormsSpan[i];
                        if (existingForm.Text == form.Text && existingForm.LastTag == formTag)
                        {
                            Debug.Assert(existingForm.Process is not null);
                            int existingFormProperStepCount = existingForm.Process.ProperStepCount;

                            if (existingFormProperStepCount < newFormProperStepCount)
                            {
                                add = false;
                                break;
                            }

                            if (existingFormProperStepCount > newFormProperStepCount)
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
