using System.Collections.Frozen;
using System.Diagnostics;
using JL.Core.Lookup;
using JL.Core.Utilities;
using JL.Core.Utilities.ObjectPool;

namespace JL.Core.Deconjugation;

// Modified from https://github.com/wareya/nazeka/blob/master/background-script.js
internal static class Deconjugator
{
    internal static FrozenDictionary<char, RuleBucket> RuleBucketsByLastDecEndChar { get; set; } = FrozenDictionary<char, RuleBucket>.Empty;
    internal static RuleBucket RulesWithEmptyConEnd { get; set; }

    private static void ApplyRuleInternal(in Form form, in VirtualRule rule, PooledList<Form> newFormsToProcess, ReadOnlySpan<char> textSpan)
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

        if (textSpan.Length == rule.ConEnd.Length && rule.DecEnd.Length is 0)
        {
            return;
        }

        // Too many proper deconjugation steps
        if ((form.Process?.ProperStepCount ?? 0) > 7)
        {
            return;
        }

        // ending doesn't match
        if (!textSpan.EndsWith(rule.ConEnd, StringComparison.Ordinal))
        {
            return;
        }

        int stemLength = textSpan.Length - rule.ConEnd.Length;
        ReadOnlySpan<char> stem = textSpan[..stemLength];

        if (DiscoveredFormsContain(newFormsToProcess.AsReadOnlySpan(), stem, rule.DecEnd, rule.DecTag, form.Process, rule.Detail))
        {
            return;
        }

        string newText = string.Concat(stem, rule.DecEnd);
        newFormsToProcess.Add(new Form(newText, form.OriginalText, rule.DecTag, new ProcessNode(rule.Detail, form.Process)));
    }

    private static void ApplyBucket(in Form form, in RuleBucket bucket, PooledList<Form> newFormsToProcess, ReadOnlySpan<char> textSpan)
    {
        if (form.Process is null)
        {
            foreach (ref readonly VirtualRule rule in bucket.AllRules.AsSpan())
            {
                ApplyRuleInternal(form, rule, newFormsToProcess, textSpan);
            }
        }
        else if (bucket.RulesByTag.TryGetValue(form.LastTag, out VirtualRule[]? rules))
        {
            foreach (ref readonly VirtualRule rule in rules.AsSpan())
            {
                ApplyRuleInternal(form, rule, newFormsToProcess, textSpan);
            }
        }
    }

    private static bool DiscoveredFormsContain(ReadOnlySpan<Form> discoveredForms, ReadOnlySpan<char> stem, string decEnd, string tag, ProcessNode? parentProcessNode, string newDetail)
    {
        int targetTextLength = stem.Length + decEnd.Length;
        int targetProperStepCount = parentProcessNode?.ProperStepCount ?? 0;

        foreach (ref readonly Form form in discoveredForms)
        {
            if (form.LastTag == tag && form.Text.Length == targetTextLength)
            {
                ReadOnlySpan<char> formText = form.Text.AsSpan();
                if (formText.StartsWith(stem) && formText[stem.Length..].Equals(decEnd, StringComparison.Ordinal))
                {
                    ProcessNode? currentProcess = form.Process;
                    if (currentProcess is not null && currentProcess.ProperStepCount == targetProperStepCount && currentProcess.Detail == newDetail && ReferenceEquals(currentProcess.Parent, parentProcessNode))
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
        List<Form> processedForms = new(4);
        PooledList<Form> newFormsToProcess = new(64);
        PooledList<Form> formsToProcess = new(64);
        formsToProcess.Add(new Form(text, text, "", null));

        FrozenDictionary<char, RuleBucket> ruleBucketsByLastDecEndChar = RuleBucketsByLastDecEndChar;
        FrozenSet<string> validWordClasses = DeconjugatorUtils.ValidWordClasses;
        while (formsToProcess.Count > 0)
        {
            foreach (ref readonly Form form in formsToProcess.AsReadOnlySpan())
            {
                ReadOnlySpan<char> textSpan = form.Text.AsSpan();
                if (textSpan.Length is not 0)
                {
                    if (ruleBucketsByLastDecEndChar.TryGetValue(textSpan[^1], out RuleBucket bucket))
                    {
                        ApplyBucket(form, bucket, newFormsToProcess, textSpan);
                    }
                }

                ApplyBucket(form, RulesWithEmptyConEnd, newFormsToProcess, textSpan);

                string formTag = form.LastTag;
                if (validWordClasses.Contains(formTag))
                {
                    bool add = true;

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
                                int lastIndex = processedFormsSpan.Length - 1;
                                if (i < lastIndex)
                                {
                                    (processedForms[i], processedForms[lastIndex]) = (processedForms[lastIndex], processedForms[i]);
                                }

                                processedForms.RemoveAt(lastIndex);
                                processedFormsSpan = processedForms.AsReadOnlySpan();
                            }
                        }
                    }

                    if (add)
                    {
                        processedForms.Add(form);
                    }
                }
            }

            formsToProcess.Clear();
            (newFormsToProcess, formsToProcess) = (formsToProcess, newFormsToProcess);
        }

        newFormsToProcess.Dispose();
        formsToProcess.Dispose();

        return processedForms;
    }
}
