using System.Text.Json;
using System.Text.RegularExpressions;
using Caching;

namespace JL.Core.Deconjugation
{
    // translated from https://github.com/wareya/nazeka/blob/master/background-script.js
    public static class Deconjugator
    {
        private static readonly Rule[] s_rules = JsonSerializer.Deserialize<Rule[]>(File.ReadAllText($"{Storage.ResourcesPath}/deconjugator_edited_arrays.json"))!;

        private static readonly LRUCache<string, HashSet<Form>> s_cache = new(777, 88);

        private static Form? StdruleDeconjugateInner(Form myForm, VirtualRule myRule)
        {
            // tag doesn't match
            if (myForm.Tags.Count > 0 && myForm.Tags[^1] != myRule.ConTag)
                return null;
            // ending doesn't match
            if (!myForm.Text.EndsWith(myRule.ConEnd))
                return null;

            string newText =
                myForm.Text[..^myRule.ConEnd.Length]
                +
                myRule.DecEnd;

            Form newForm = new(
                newText,
                myForm.OriginalText,
                myForm.Tags.ToList(),
                myForm.Seentext.ToHashSet(),
                myForm.Process.ToList()
            );

            newForm.Process.Add(myRule.Detail);

            if (newForm.Tags.Count == 0)
                newForm.Tags.Add(myRule.ConTag);

            newForm.Tags.Add(myRule.DecTag);

            if (newForm.Seentext.Count == 0)
                newForm.Seentext.Add(myForm.Text);

            newForm.Seentext.Add(newText);

            return newForm;
        }

        private static HashSet<Form>? StdruleDeconjugate(Form myForm,
            Rule myRule)
        {
            // can't deconjugate nothingness
            if (myForm.Text == "")
                return null;
            // deconjugated form too much longer than conjugated form
            if (myForm.Text.Length > myForm.OriginalText.Length + 10)
                return null;
            // impossibly information-dense
            if (myForm.Tags.Count > myForm.OriginalText.Length + 6)
                return null;
            // blank detail mean it can't be the last (first applied, but rightmost) rule
            if (myRule.Detail == "" && myForm.Tags.Count == 0)
                return null;

            HashSet<Form> collection = new();

            List<string> array = myRule.DecEnd;
            if (array.Count == 1)
            {
                VirtualRule virtualRule = new
                (
                    myRule.DecEnd.First(),
                    myRule.ConEnd.First(),
                    myRule.DecTag!.First(),
                    myRule.ConTag!.First(),
                    myRule.Detail
                );
                Form? result = StdruleDeconjugateInner(myForm, virtualRule);
                if (result != null) collection.Add(result);
            }
            else if (array.Count > 1)
            {
                string maybeDecEnd = myRule.DecEnd[0];
                string maybeConEnd = myRule.ConEnd[0];
                string maybeDecTag = myRule.DecTag![0];
                string maybeConTag = myRule.ConTag![0];

                for (int i = 0; i < array.Count; i++)
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
                    if (ret != null) collection.Add(ret);
                }
            }

            return collection;
        }

        private static HashSet<Form>? RewriteruleDeconjugate(Form myForm, Rule myRule)
        {
            if (myForm.Text != myRule.ConEnd.First())
                return null;

            return StdruleDeconjugate(myForm, myRule);
        }

        private static HashSet<Form>? OnlyfinalruleDeconjugate(Form myForm, Rule myRule)
        {
            if (myForm.Tags.Count != 0)
                return null;

            return StdruleDeconjugate(myForm, myRule);
        }

        private static HashSet<Form>? NeverfinalruleDeconjugate(Form myForm, Rule myRule)
        {
            if (myForm.Tags.Count == 0)
                return null;

            return StdruleDeconjugate(myForm, myRule);
        }

        private static HashSet<Form>? ContextruleDeconjugate(Form myForm, Rule myRule)
        {
            bool result = myRule.Contextrule switch
            {
                "v1inftrap" => V1InftrapCheck(myForm),
                "saspecial" => SaspecialCheck(myForm, myRule),
                _ => false
            };
            if (!result)
                return null;

            return StdruleDeconjugate(myForm, myRule);
        }

        private static Form? SubstitutionInner(Form myForm, Rule myRule)
        {
            if (!myForm.Text.Contains(myRule.ConEnd.First()))
                return null;

            string newText = new Regex(myRule.ConEnd.First())
                .Replace(myForm.Text, myRule.DecEnd.First());

            Form newForm = new(
                newText,
                myForm.OriginalText,
                myForm.Tags.ToList(),
                myForm.Seentext.ToHashSet(),
                myForm.Process.ToList()
            );

            newForm.Process.Add(myRule.Detail);

            if (newForm.Seentext.Count == 0)
                newForm.Seentext.Add(myForm.Text);

            newForm.Seentext.Add(newText);

            return newForm;
        }

        private static HashSet<Form>? SubstitutionDeconjugate(Form myForm, Rule myRule)
        {
            if (myForm.Process.Count != 0)
                return null;

            // can't deconjugate nothingness
            if (myForm.Text == "")
                return null;

            HashSet<Form> collection = new();

            List<string> array = myRule.DecEnd;
            if (array.Count == 1)
            {
                Form? result = SubstitutionInner(myForm, myRule);
                if (result != null) collection.Add(result);
            }
            else if (array.Count > 1)
            {
                string maybeDecEnd = myRule.DecEnd[0];
                string maybeConEnd = myRule.ConEnd[0];

                for (int i = 0; i < array.Count; i++)
                {
                    maybeDecEnd = myRule.DecEnd.ElementAtOrDefault(i) ?? maybeDecEnd;
                    maybeConEnd = myRule.ConEnd.ElementAtOrDefault(i) ?? maybeConEnd;

                    Rule virtualRule = new
                    (
                        myRule.Type,
                        null,
                        new List<string> { maybeDecEnd },
                        new List<string> { maybeConEnd },
                        null,
                        null,
                        myRule.Detail
                    );

                    Form? ret = SubstitutionInner(myForm, virtualRule);
                    if (ret != null) collection.Add(ret);
                }
            }

            return collection;
        }

        private static bool V1InftrapCheck(Form myForm)
        {
            if (myForm.Tags.Count != 1) return true;
            string myTag = myForm.Tags[0];
            if (myTag == "stem-ren")
                return false;

            return true;
        }

        private static bool SaspecialCheck(Form myForm,
            Rule myRule)
        {
            if (myForm.Text == "") return false;
            if (!myForm.Text.EndsWith(myRule.ConEnd.First())) return false;

            string baseText = myForm.Text[..^myRule.ConEnd.First().Length];
            return !baseText.EndsWith("さ");
        }

        public static HashSet<Form> Deconjugate(string myText, bool useCache = true)
        {
            if (useCache)
                if (s_cache.TryGet(myText, out HashSet<Form> data))
                    return data;

            HashSet<Form> processed = new();
            HashSet<Form> novel = new();

            Form startForm = new
                (
                    myText,
                    myText,
                    new List<string>(),
                    new HashSet<string>(),
                    new List<string>()
                );
            novel.Add(startForm);

            while (novel.Count > 0)
            {
                HashSet<Form> newNovel = new();
                foreach (Form form in novel)
                {
                    foreach (Rule rule in s_rules)
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

                        if (newForm == null || newForm.Count == 0) continue;

                        foreach (Form myForm in newForm)
                        {
                            if (myForm != null &&
                                !processed.Contains(myForm) &&
                                !novel.Contains(myForm) &&
                                !newNovel.Contains(myForm))
                            {
                                newNovel.Add(myForm);
                            }
                        }
                    }
                }

                processed.UnionWith(novel);
                novel = newNovel;
            }

            if (useCache)
                s_cache.AddReplace(myText, processed);

            return processed;
        }
    }
}
