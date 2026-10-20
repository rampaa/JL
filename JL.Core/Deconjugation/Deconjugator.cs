using System.Collections.Frozen;
using System.Diagnostics;
using JL.Core.Utilities;
using JL.Core.Utilities.ObjectPool;

namespace JL.Core.Deconjugation;

// Modified from https://github.com/wareya/nazeka/blob/master/background-script.js
internal static class Deconjugator
{
    internal static DeconjugationRules? Rules { get; set; }

    private static void ApplyMatchedRule(in Form form, in VirtualRule rule, PooledList<Form> newFormsToProcess, ReadOnlySpan<char> textSpan)
    {
        if (rule.Type is RuleType.Rewrite && textSpan.Length != rule.ConEnd.Length)
        {
            return;
        }

        if (textSpan.Length == rule.ConEnd.Length && rule.DecEnd.Length is 0)
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
        newFormsToProcess.Add(new Form(newText, rule.DecTag, new ProcessNode(rule.Detail, form.Process)));
    }

    private static bool DiscoveredFormsContain(ReadOnlySpan<Form> discoveredForms, ReadOnlySpan<char> stem, string decEnd, string tag, ProcessNode? parentProcessNode, string newDetail)
    {
        int targetTextLength = stem.Length + decEnd.Length;

        foreach (ref readonly Form form in discoveredForms)
        {
            if (form.LastTag == tag && form.Text.Length == targetTextLength)
            {
                ReadOnlySpan<char> formText = form.Text.AsSpan();
                if (formText.StartsWith(stem) && formText[stem.Length..].Equals(decEnd, StringComparison.Ordinal))
                {
                    ProcessNode? currentProcess = form.Process;
                    if (currentProcess is not null && ReferenceEquals(currentProcess.Parent, parentProcessNode) && currentProcess.Detail == newDetail)
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
        formsToProcess.Add(new Form(text, "", null));

        DeconjugationRules? rules = Rules;
        Debug.Assert(rules is not null);

        FrozenSet<string> validWordClasses = DeconjugatorUtils.ValidWordClasses;
        while (formsToProcess.Count > 0)
        {
            foreach (ref readonly Form form in formsToProcess.AsReadOnlySpan())
            {
                // Too many proper deconjugation steps
                if ((form.Process?.ProperStepCount ?? 0) <= 7)
                {
                    ReadOnlySpan<char> textSpan = form.Text.AsSpan();
                    RuleSuffixMatcher? suffixMatcher = rules.GetSuffixMatcher(form);
                    if (suffixMatcher is not null)
                    {
                        foreach (ref readonly VirtualRule rule in suffixMatcher.GetMatchingRules(textSpan))
                        {
                            ApplyMatchedRule(form, rule, newFormsToProcess, textSpan);
                        }

                        foreach (ref readonly VirtualRule rule in suffixMatcher.EmptyEndingRules.AsSpan())
                        {
                            ApplyMatchedRule(form, rule, newFormsToProcess, textSpan);
                        }
                    }
                }

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
