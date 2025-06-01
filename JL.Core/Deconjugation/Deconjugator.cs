using System.Diagnostics;
using JL.Core.Utilities;

namespace JL.Core.Deconjugation;

// translated from https://github.com/wareya/nazeka/blob/master/background-script.js
internal static class Deconjugator
{
    public static Rule[] Rules { get; set; } = [];

    private static Form? StandardRuleDeconjugateInner(Form myForm, in VirtualRule myRule)
    {
        // tag doesn't match
        if (myForm.Tags.Count > 0 && myForm.Tags[^1] != myRule.ConTag)
        {
            return null;
        }

        // ending doesn't match
        if (!myForm.Text.AsSpan().EndsWith(myRule.ConEnd, StringComparison.Ordinal))
        {
            return null;
        }

        if (myForm.Text.Length == myRule.ConEnd.Length && myRule.DecEnd.Length is 0)
        {
            return null;
        }

        string newText = string.Concat(myForm.Text.AsSpan(0, myForm.Text.Length - myRule.ConEnd.Length), myRule.DecEnd);
        return new Form(newText, myForm.OriginalText,
            myForm.Tags.Count is 0
                ? [myRule.ConTag, myRule.DecTag]
                : [.. myForm.Tags, myRule.DecTag],
            [.. myForm.Process, myRule.Detail]);
    }

    private static List<Form>? StandardRuleDeconjugate(Form myForm, in Rule myRule)
    {
        // can't deconjugate nothingness
        if (myForm.Text.Length is 0)
        {
            return null;
        }

        // deconjugated form too much longer than conjugated form
        if (myForm.Text.Length > myForm.OriginalText.Length + 10)
        {
            return null;
        }

        // impossibly information-dense
        if (myForm.Tags.Count > myForm.OriginalText.Length + 6)
        {
            return null;
        }

        // blank detail mean it can't be the last (first applied, but rightmost) rule
        if (myRule.Detail.Length is 0 && myForm.Tags.Count is 0)
        {
            return null;
        }

        Debug.Assert(myRule.ConTags is not null);
        Debug.Assert(myRule.DecTags is not null);
        string[] array = myRule.DecEnds;
        if (array.Length is 1)
        {
            VirtualRule virtualRule = new
            (
                myRule.DecEnds[0],
                myRule.ConEnds[0],
                myRule.DecTags[0],
                myRule.ConTags[0],
                myRule.Detail
            );

            Form? result = StandardRuleDeconjugateInner(myForm, virtualRule);
            return result is not null
                ? [result]
                : null;
        }

        List<Form> collection = new(array.Length);
        bool multiDecTag = myRule.DecTags.Length > 1;
        string? singleDecTag = multiDecTag ? null : myRule.DecTags[0];
        bool multiConTag = myRule.ConTags.Length > 1;
        string? singleConTag = multiConTag ? null : myRule.ConTags[0];

        for (int i = 0; i < array.Length; i++)
        {
            VirtualRule virtualRule = new
            (
                myRule.DecEnds[i],
                myRule.ConEnds[i],
                multiDecTag ? myRule.DecTags[i] : singleDecTag!,
                multiConTag ? myRule.ConTags[i] : singleConTag!,
                myRule.Detail
            );
            Form? ret = StandardRuleDeconjugateInner(myForm, virtualRule);
            if (ret is not null)
            {
                collection.Add(ret);
            }
        }

        return collection.Count > 0
            ? collection
            : null;
    }

    private static List<Form>? RewriteRuleDeconjugate(Form myForm, in Rule myRule)
    {
        return myForm.Text != myRule.ConEnds[0]
            ? null
            : StandardRuleDeconjugate(myForm, myRule);
    }

    private static List<Form>? OnlyFinalRuleDeconjugate(Form myForm, in Rule myRule)
    {
        return myForm.Tags.Count is not 0
            ? null
            : StandardRuleDeconjugate(myForm, myRule);
    }

    private static List<Form>? NeverFinalRuleDeconjugate(Form myForm, in Rule myRule)
    {
        return myForm.Tags.Count is 0
            ? null
            : StandardRuleDeconjugate(myForm, myRule);
    }

    private static List<Form>? ContextRuleDeconjugate(Form myForm, in Rule myRule)
    {
        bool result = myRule.ContextRule switch
        {
            "v1inftrap" => V1InfTrapCheck(myForm),
            "saspecial" => SaSpecialCheck(myForm, myRule),
            _ => false
        };

        return result
            ? StandardRuleDeconjugate(myForm, myRule)
            : null;
    }

    private static bool V1InfTrapCheck(Form myForm)
    {
        return myForm.Tags.Count is not 1 || myForm.Tags[0] is not "stem-ren";
    }

    private static bool SaSpecialCheck(Form myForm, in Rule myRule)
    {
        if (myForm.Text.Length is 0)
        {
            return false;
        }

        string conEnd = myRule.ConEnds[0];
        ReadOnlySpan<char> textSpan = myForm.Text.AsSpan();
        return textSpan.EndsWith(conEnd, StringComparison.Ordinal)
            && !textSpan[..^conEnd.Length].EndsWith('さ');
    }

    public static List<Form> Deconjugate(string text)
    {
        List<Form> processed = [];
        List<Form> novel = [new(text, text, [], [])];

        Rule[] rules = Rules;
        bool addFormToProcess = false;
        while (novel.Count > 0)
        {
            List<Form> newNovel = [];
            foreach (ref readonly Form form in novel.AsReadOnlySpan())
            {
                for (int j = 0; j < rules.Length; j++)
                {
                    ref readonly Rule rule = ref rules[j];
                    List<Form>? newForm = rule.Type switch
                    {
                        "stdrule" => StandardRuleDeconjugate(form, rule),
                        "rewriterule" => RewriteRuleDeconjugate(form, rule),
                        "onlyfinalrule" => OnlyFinalRuleDeconjugate(form, rule),
                        "neverfinalrule" => NeverFinalRuleDeconjugate(form, rule),
                        "contextrule" => ContextRuleDeconjugate(form, rule),
                        _ => null
                    };

                    if (newForm is null)
                    {
                        continue;
                    }

                    foreach (ref readonly Form myForm in newForm.AsReadOnlySpan())
                    {
                        if (!newNovel.AsReadOnlySpan().Contains(myForm))
                        {
                            newNovel.Add(myForm);
                        }
                    }
                }

                if (addFormToProcess)
                {
                    processed.Add(form);
                }
                else
                {
                    addFormToProcess = true;
                }
            }

            novel = newNovel;
        }

        return processed;
    }
}
