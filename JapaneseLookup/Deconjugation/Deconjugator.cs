using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JapaneseLookup.Deconjugation
{
    // translated from https://github.com/wareya/nazeka/blob/master/background-script.js
    public static class Deconjugator
    {
        private static readonly string File =
            System.IO.File.ReadAllText("../net5.0-windows/Resources/deconjugator_edited_arrays.json");

        private static readonly Rule[] Rules = JsonSerializer.Deserialize<Rule[]>(File);

        private static Form StdruleDeconjugateInner(Form myForm,
            Rule myRule)
        {
            // tag doesn't match
            if (myForm.Tags.Count > 0 &&
                myForm.Tags[^1] != myRule.ConTag.First())
            {
                return null;
            }

            // ending doesn't match
            if (!myForm.Text.EndsWith(myRule.ConEnd.First()))
                return null;

            var newText =
                myForm.Text.Substring(0, myForm.Text.Length - myRule.ConEnd.First().Length)
                +
                myRule.DecEnd.First();

            var clone = JsonSerializer.Deserialize<Form>(JsonSerializer.Serialize(myForm));
            var newForm = new Form(
                newText,
                myForm.OriginalText,
                clone?.Tags,
                clone?.Seentext,
                clone?.Process
            );

            newForm.Process.Add(myRule.Detail);

            if (newForm.Tags.Count == 0)
                newForm.Tags.Add(myRule.ConTag.First());
            newForm.Tags.Add(myRule.DecTag.First());

            if (newForm.Seentext.Count == 0)
                newForm.Seentext.Add(myForm.Text);
            newForm.Seentext.Add(newText);

            return newForm;
        }

        private static HashSet<Form> StdruleDeconjugate(Form myForm,
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

            var array = myRule.DecEnd;

            if (array.Count == 1)
            {
                var result = StdruleDeconjugateInner(myForm, myRule);
                return result == null ? null : new HashSet<Form> {result};
            }
            else if (array.Count > 1)
            {
                var collection = new HashSet<Form>();

                var maybeDecEnd = myRule.DecEnd[0];
                var maybeConEnd = myRule.ConEnd[0];
                var maybeDecTag = myRule.DecTag[0];
                var maybeConTag = myRule.ConTag[0];

                var length = array.Count;
                for (var i = 0; i < length; i++)
                {
                    maybeDecEnd = myRule.DecEnd.ElementAtOrDefault(i) ?? maybeDecEnd;
                    maybeConEnd = myRule.ConEnd.ElementAtOrDefault(i) ?? maybeConEnd;
                    maybeDecTag = myRule.DecTag.ElementAtOrDefault(i) ?? maybeDecTag;
                    maybeConTag = myRule.ConTag.ElementAtOrDefault(i) ?? maybeConTag;

                    var virtualRule = new Rule
                    (
                        myRule.Type,
                        new List<string> {maybeDecEnd},
                        new List<string> {maybeConEnd},
                        new List<string> {maybeDecTag},
                        new List<string> {maybeConTag},
                        myRule.Detail
                    );
                    var ret = StdruleDeconjugateInner(myForm, virtualRule);
                    if (ret != null) collection.Add(ret);
                }

                return collection;
            }

            return null;
        }

        private static HashSet<Form> RewriteruleDeconjugate(Form myForm,
            Rule myRule)
        {
            if (myForm.Text != myRule.ConEnd.First())
                return null;
            return StdruleDeconjugate(myForm, myRule);
        }

        private static HashSet<Form> OnlyfinalruleDeconjugate(Form myForm,
            Rule myRule)
        {
            if (myForm.Tags.Count != 0)
                return null;
            return StdruleDeconjugate(myForm, myRule);
        }

        private static HashSet<Form> NeverfinalruleDeconjugate(Form myForm,
            Rule myRule)
        {
            if (myForm.Tags.Count == 0)
                return null;
            return StdruleDeconjugate(myForm, myRule);
        }

        private static HashSet<Form> ContextruleDeconjugate(Form myForm,
            Rule myRule)
        {
            var result = myRule.Detail switch
            {
                "v1inftrap" => V1InftrapCheck(myForm),
                "saspecial" => SaspecialCheck(myForm, myRule),
                _ => false
            };
            if (!result)
                return null;
            return StdruleDeconjugate(myForm, myRule);
        }

        private static Form SubstitutionInner(Form myForm,
            Rule myRule)
        {
            if (!myForm.Text.Contains(myRule.ConEnd.First()))
                return null;
            var newText = new Regex(myRule.ConEnd.First())
                .Replace(myForm.Text, myRule.DecEnd.First());

            var clone = JsonSerializer.Deserialize<Form>(JsonSerializer.Serialize(myForm));
            var newForm = new Form(
                newText,
                myForm.OriginalText,
                clone?.Tags,
                clone?.Seentext,
                clone?.Process
            );

            newForm.Process.Add(myRule.Detail);

            if (newForm.Seentext.Count == 0)
                newForm.Seentext.Add(myForm.Text);
            newForm.Seentext.Add(newText);

            return newForm;
        }

        private static HashSet<Form> SubstitutionDeconjugate(Form myForm,
            Rule myRule)
        {
            if (myForm.Process.Count != 0)
                return null;

            // can't deconjugate nothingness
            if (myForm.Text == "")
                return null;

            var array = myRule.DecEnd;

            if (array.Count == 1)
            {
                var result = SubstitutionInner(myForm, myRule);
                return result == null ? null : new HashSet<Form> {result};
            }
            else if (array.Count > 1)
            {
                var collection = new HashSet<Form>();

                var maybeDecEnd = myRule.DecEnd[0];
                var maybeConEnd = myRule.ConEnd[0];

                var length = array.Count;
                for (var i = 0; i < length; i++)
                {
                    maybeDecEnd = myRule.DecEnd.ElementAtOrDefault(i) ?? maybeDecEnd;
                    maybeConEnd = myRule.ConEnd.ElementAtOrDefault(i) ?? maybeConEnd;

                    var virtualRule = new Rule
                    (
                        myRule.Type,
                        new List<string> {maybeDecEnd},
                        new List<string> {maybeConEnd},
                        null,
                        null,
                        myRule.Detail
                    );

                    var ret = SubstitutionInner(myForm, virtualRule);
                    if (ret != null) collection.Add(ret);
                }

                return collection;
            }

            return null;
        }

        private static bool V1InftrapCheck(Form myForm)
        {
            if (myForm.Tags.Count != 1) return true;
            var myTag = myForm.Tags[0];
            if (myTag == "stem-ren")
                return false;
            return true;
        }

        private static bool SaspecialCheck(Form myForm,
            Rule myRule)
        {
            if (myForm.Text == "") return false;
            if (!myForm.Text.EndsWith(myRule.ConEnd.First())) return false;
            var baseText = myForm.Text.Substring(0, myForm.Text.Length - myRule.ConEnd.First().Length);
            if (baseText.EndsWith("さ"))
                return false;
            return true;
        }

        internal static HashSet<Form> Deconjugate(string myText)
        {
            var processed = new HashSet<Form>();
            var novel = new HashSet<Form>();

            var startForm =
                new Form(myText,
                    myText,
                    new List<string>(),
                    new HashSet<string>(),
                    new List<string>()
                );
            novel.Add(startForm);

            while (novel.Count > 0)
            {
                var newNovel = new HashSet<Form>();
                foreach (Form form in novel)
                {
                    foreach (Rule rule in Rules)
                    {
                        HashSet<Form> newForm = null;

                        switch (rule.Type)
                        {
                            case "stdrule":
                                newForm = StdruleDeconjugate(form, rule);
                                break;
                            case "rewriterule":
                                newForm = RewriteruleDeconjugate(form, rule);
                                break;
                            case "onlyfinalrule":
                                newForm = OnlyfinalruleDeconjugate(form, rule);
                                break;
                            case "neverfinalrule":
                                newForm = NeverfinalruleDeconjugate(form, rule);
                                break;
                            case "contextrule":
                                newForm = ContextruleDeconjugate(form, rule);
                                break;
                            case "substitution":
                                newForm = SubstitutionDeconjugate(form, rule);
                                break;
                        }

                        if (newForm == null || newForm.Count == 0) continue;

                        foreach (var myForm in newForm)
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

                processed = Union(processed, novel);
                novel = newNovel;
            }

            return processed;
        }

        private static HashSet<Form> Union(HashSet<Form> setA, HashSet<Form> setB)
        {
            foreach (var elem in setB)
                setA.Add(elem);
            return setA;
        }
    }
}