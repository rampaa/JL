using Caching;
using JL.Core.Utilities;

namespace JL.Core.Deconjugation;

// translated from https://github.com/wareya/nazeka/blob/master/background-script.js
internal static class Deconjugator
{
    private static readonly LRUCache<string, HashSet<Form>> s_cache = new(Utils.CacheSize, Utils.CacheSize / 5);
    public static Rule[] Rules { get; set; } = Array.Empty<Rule>();

    private static Form? StdruleDeconjugateInner(Form myForm, VirtualRule myRule)
    {
        // tag doesn't match
        if (myForm.Tags.Count > 0 && myForm.Tags[^1] != myRule.ConTag)
        {
            return null;
        }

        // ending doesn't match
        if (!myForm.Text.EndsWith(myRule.ConEnd, StringComparison.Ordinal))
        {
            return null;
        }

        string newText =
            myForm.Text[..^myRule.ConEnd.Length]
            +
            myRule.DecEnd;

        Form newForm = new(
            newText,
            myForm.OriginalText,
            myForm.Tags.ToList(),
            myForm.SeenText.ToHashSet(),
            myForm.Process.ToList()
        );

        newForm.Process.Add(myRule.Detail);

        if (newForm.Tags.Count is 0)
        {
            newForm.Tags.Add(myRule.ConTag);
        }

        newForm.Tags.Add(myRule.DecTag);

        if (newForm.SeenText.Count is 0)
        {
            _ = newForm.SeenText.Add(myForm.Text);
        }

        _ = newForm.SeenText.Add(newText);

        return newForm;
    }

    private static HashSet<Form>? StdruleDeconjugate(Form myForm, Rule myRule)
    {
        // can't deconjugate nothingness
        if (myForm.Text is "")
        {
            return null;
        }

        // deconjugated form too much longer than conjugated form
        if (myForm.Text.Length > (myForm.OriginalText.Length + 10))
        {
            return null;
        }

        // impossibly information-dense
        if (myForm.Tags.Count > (myForm.OriginalText.Length + 6))
        {
            return null;
        }

        // blank detail mean it can't be the last (first applied, but rightmost) rule
        if (myRule.Detail is "" && myForm.Tags.Count is 0)
        {
            return null;
        }

        HashSet<Form> collection = new();

        string[] array = myRule.DecEnd;
        if (array.Length is 1)
        {
            VirtualRule virtualRule = new
            (
                myRule.DecEnd[0],
                myRule.ConEnd[0],
                myRule.DecTag![0],
                myRule.ConTag![0],
                myRule.Detail
            );
            Form? result = StdruleDeconjugateInner(myForm, virtualRule);
            if (result is not null)
            {
                _ = collection.Add(result);
            }
        }
        else if (array.Length > 1)
        {
            string maybeDecEnd = myRule.DecEnd[0];
            string maybeConEnd = myRule.ConEnd[0];
            string maybeDecTag = myRule.DecTag![0];
            string maybeConTag = myRule.ConTag![0];

            for (int i = 0; i < array.Length; i++)
            {
                maybeDecEnd = myRule.DecEnd.ElementAtOrDefault(i) ?? maybeDecEnd;
                maybeConEnd = myRule.ConEnd.ElementAtOrDefault(i) ?? maybeConEnd;
                maybeDecTag = myRule.DecTag.ElementAtOrDefault(i) ?? maybeDecTag;
                maybeConTag = myRule.ConTag.ElementAtOrDefault(i) ?? maybeConTag;

                VirtualRule virtualRule = new
                (
                    maybeDecEnd,
                    maybeConEnd,
                    maybeDecTag,
                    maybeConTag,
                    myRule.Detail
                );
                Form? ret = StdruleDeconjugateInner(myForm, virtualRule);
                if (ret is not null)
                {
                    _ = collection.Add(ret);
                }
            }
        }

        return collection;
    }

    private static HashSet<Form>? RewriteruleDeconjugate(Form myForm, Rule myRule)
    {
        return myForm.Text != myRule.ConEnd[0]
            ? null
            : StdruleDeconjugate(myForm, myRule);
    }

    private static HashSet<Form>? OnlyfinalruleDeconjugate(Form myForm, Rule myRule)
    {
        return myForm.Tags.Count is not 0
            ? null
            : StdruleDeconjugate(myForm, myRule);
    }

    private static HashSet<Form>? NeverfinalruleDeconjugate(Form myForm, Rule myRule)
    {
        return myForm.Tags.Count is 0
            ? null
            : StdruleDeconjugate(myForm, myRule);
    }

    private static HashSet<Form>? ContextruleDeconjugate(Form myForm, Rule myRule)
    {
        bool result = myRule.ContextRule switch
        {
            "v1inftrap" => V1InftrapCheck(myForm),
            "saspecial" => SaspecialCheck(myForm, myRule),
            _ => false
        };

        return result
            ? StdruleDeconjugate(myForm, myRule)
            : null;
    }

    private static Form? SubstitutionInner(Form myForm, Rule myRule)
    {
        if (!myForm.Text.Contains(myRule.ConEnd[0], StringComparison.Ordinal))
        {
            return null;
        }

        string newText = myForm.Text.Replace(myRule.ConEnd[0], myRule.DecEnd[0], StringComparison.Ordinal);

        Form newForm = new(
            newText,
            myForm.OriginalText,
            myForm.Tags.ToList(),
            myForm.SeenText.ToHashSet(),
            myForm.Process.ToList()
        );

        newForm.Process.Add(myRule.Detail);

        if (newForm.SeenText.Count is 0)
        {
            _ = newForm.SeenText.Add(myForm.Text);
        }

        _ = newForm.SeenText.Add(newText);

        return newForm;
    }

    private static HashSet<Form>? SubstitutionDeconjugate(Form myForm, Rule myRule)
    {
        if (myForm.Process.Count is not 0)
        {
            return null;
        }

        // can't deconjugate nothingness
        if (myForm.Text is "")
        {
            return null;
        }

        HashSet<Form> collection = new();

        string[] array = myRule.DecEnd;
        if (array.Length is 1)
        {
            Form? result = SubstitutionInner(myForm, myRule);
            if (result is not null)
            {
                _ = collection.Add(result);
            }
        }
        else if (array.Length > 1)
        {
            string maybeDecEnd = myRule.DecEnd[0];
            string maybeConEnd = myRule.ConEnd[0];

            for (int i = 0; i < array.Length; i++)
            {
                maybeDecEnd = myRule.DecEnd.ElementAtOrDefault(i) ?? maybeDecEnd;
                maybeConEnd = myRule.ConEnd.ElementAtOrDefault(i) ?? maybeConEnd;

                Rule virtualRule = new
                (
                    myRule.Type,
                    null,
                    new[] { maybeDecEnd },
                    new[] { maybeConEnd },
                    null,
                    null,
                    myRule.Detail
                );
                Form? ret = SubstitutionInner(myForm, virtualRule);
                if (ret is not null)
                {
                    _ = collection.Add(ret);
                }
            }
        }

        return collection;
    }

    private static bool V1InftrapCheck(Form myForm)
    {
        if (myForm.Tags.Count is not 1)
        {
            return true;
        }

        string myTag = myForm.Tags[0];
        return myTag is not "stem-ren";
    }

    private static bool SaspecialCheck(Form myForm, Rule myRule)
    {
        if (myForm.Text is "")
        {
            return false;
        }

        if (!myForm.Text.EndsWith(myRule.ConEnd[0], StringComparison.Ordinal))
        {
            return false;
        }

        string baseText = myForm.Text[..^myRule.ConEnd[0].Length];
        return !baseText.EndsWith('さ');
    }

    public static HashSet<Form> Deconjugate(string text)
    {
        if (s_cache.TryGet(text, out HashSet<Form> data))
        {
            return data;
        }

        HashSet<Form> processed = new();
        HashSet<Form> novel = new();

        Form startForm = new
        (
            text,
            text,
            new List<string>(),
            new HashSet<string>(),
            new List<string>()
        );
        _ = novel.Add(startForm);

        while (novel.Count > 0)
        {
            HashSet<Form> newNovel = new();
            foreach (Form form in novel)
            {
                foreach (Rule rule in Rules)
                {
                    HashSet<Form>? newForm = rule.Type switch
                    {
                        "stdrule" => StdruleDeconjugate(form, rule),
                        "rewriterule" => RewriteruleDeconjugate(form, rule),
                        "onlyfinalrule" => OnlyfinalruleDeconjugate(form, rule),
                        "neverfinalrule" => NeverfinalruleDeconjugate(form, rule),
                        "contextrule" => ContextruleDeconjugate(form, rule),
                        "substitution" => SubstitutionDeconjugate(form, rule),
                        _ => null
                    };

                    if (newForm is null || newForm.Count is 0)
                    {
                        continue;
                    }

                    foreach (Form myForm in newForm)
                    {
                        if (!processed.Contains(myForm) &&
                            !novel.Contains(myForm) &&
                            !newNovel.Contains(myForm))
                        {
                            _ = newNovel.Add(myForm);
                        }
                    }
                }
            }

            processed.UnionWith(novel);
            novel = newNovel;
        }

        s_cache.AddReplace(text, processed);

        return processed;
    }
}
