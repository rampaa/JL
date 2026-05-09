using System.Diagnostics;
using JL.Core.Utilities;

namespace JL.Core.Deconjugation;

// Modified from https://github.com/wareya/nazeka/blob/master/background-script.js
internal static class Deconjugator
{
    public static Rule[] Rules { get; set; } = [];

    private static Form? StandardRuleDeconjugateInner(Form form, in VirtualRule virtualRule)
    {
        // tag doesn't match
        if (form.Process.Count > 0 && form.LastTag != virtualRule.ConTag)
        {
            return null;
        }

        // ending doesn't match
        if (!form.Text.AsSpan().EndsWith(virtualRule.ConEnd, StringComparison.Ordinal))
        {
            return null;
        }

        if (form.Text.Length == virtualRule.ConEnd.Length && virtualRule.DecEnd.Length is 0)
        {
            return null;
        }

        string newText = string.Concat(form.Text.AsSpan(0, form.Text.Length - virtualRule.ConEnd.Length), virtualRule.DecEnd);
        return new Form(newText, form.OriginalText, virtualRule.DecTag, [.. form.Process, virtualRule.Detail]);
    }

    private static List<Form>? StandardRuleDeconjugate(Form form, in Rule rule)
    {
        // can't deconjugate nothingness
        if (form.Text.Length is 0)
        {
            return null;
        }

        // deconjugated form too much longer than conjugated form
        if (form.Text.Length > form.OriginalText.Length + 10)
        {
            return null;
        }

        // impossibly information-dense
        if (form.Process.Count > form.OriginalText.Length + 5)
        {
            return null;
        }

        // blank detail mean it can't be the last (first applied, but rightmost) rule
        if (rule.Detail.Length is 0 && form.LastTag.Length is 0)
        {
            return null;
        }

        Debug.Assert(rule.ConTags is not null);
        Debug.Assert(rule.DecTags is not null);
        string[] decEnds = rule.DecEnds;
        if (decEnds.Length is 1)
        {
            VirtualRule virtualRule = new
            (
                rule.DecEnds[0],
                rule.ConEnds[0],
                rule.DecTags[0],
                rule.ConTags[0],
                rule.Detail
            );

            Form? result = StandardRuleDeconjugateInner(form, virtualRule);
            return result is not null
                ? [result]
                : null;
        }

        List<Form> forms = new(decEnds.Length);
        bool multiDecTag = rule.DecTags.Length > 1;
        string? singleDecTag = multiDecTag ? null : rule.DecTags[0];
        bool multiConTag = rule.ConTags.Length > 1;
        string? singleConTag = multiConTag ? null : rule.ConTags[0];

        for (int i = 0; i < decEnds.Length; i++)
        {
            VirtualRule virtualRule = new
            (
                rule.DecEnds[i],
                rule.ConEnds[i],
                multiDecTag
                    ? rule.DecTags[i]
                    // ReSharper disable once NullableWarningSuppressionIsUsed
                    : singleDecTag!,
                multiConTag
                    ? rule.ConTags[i]
                    // ReSharper disable once NullableWarningSuppressionIsUsed
                    : singleConTag!,
                rule.Detail
            );
            Form? newForm = StandardRuleDeconjugateInner(form, virtualRule);
            if (newForm is not null)
            {
                forms.Add(newForm);
            }
        }

        return forms.Count > 0
            ? forms
            : null;
    }

    private static List<Form>? RewriteRuleDeconjugate(Form form, in Rule rule)
    {
        return form.Text != rule.ConEnds[0]
            ? null
            : StandardRuleDeconjugate(form, rule);
    }

    private static List<Form>? OnlyFinalRuleDeconjugate(Form form, in Rule rule)
    {
        return form.Process.Count is not 0
            ? null
            : StandardRuleDeconjugate(form, rule);
    }

    private static List<Form>? NeverFinalRuleDeconjugate(Form form, in Rule rule)
    {
        return form.Process.Count is 0
            ? null
            : StandardRuleDeconjugate(form, rule);
    }

    public static List<Form> Deconjugate(string text)
    {
        List<Form> processedForms = [];
        List<Form> formsToProcess = [new(text, text, "", [])];

        Rule[] rules = Rules;
        bool addFormToProcess = false;
        while (formsToProcess.Count > 0)
        {
            List<Form> newFormsToProcess = [];
            foreach (ref readonly Form form in formsToProcess.AsReadOnlySpan())
            {
                // ReSharper disable once ForCanBeConvertedToForeach
                for (int j = 0; j < rules.Length; j++)
                {
                    ref readonly Rule rule = ref rules[j];
                    List<Form>? newForms = rule.Type switch
                    {
                        RuleType.Standard => StandardRuleDeconjugate(form, rule),
                        RuleType.Rewrite => RewriteRuleDeconjugate(form, rule),
                        RuleType.OnlyFinal => OnlyFinalRuleDeconjugate(form, rule),
                        RuleType.NeverFinal => NeverFinalRuleDeconjugate(form, rule),
                        _ => null
                    };

                    if (newForms is null)
                    {
                        continue;
                    }

                    foreach (ref readonly Form newForm in newForms.AsReadOnlySpan())
                    {
                        if (!newFormsToProcess.AsReadOnlySpan().Contains(newForm))
                        {
                            newFormsToProcess.Add(newForm);
                        }
                    }
                }

                if (addFormToProcess)
                {
                    bool add = true;
                    int formProcessCount = -1;
                    string formTag = form.LastTag;

                    for (int i = processedForms.Count - 1; i >= 0; i--)
                    {
                        Form existingForm = processedForms[i];
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

            formsToProcess = newFormsToProcess;
        }

        return processedForms;
    }
}
