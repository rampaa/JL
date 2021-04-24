using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

// ReSharper disable IdentifierTypo

// ReSharper disable InconsistentNaming

namespace JapaneseLookup
{
    public class Deconjugation
    {
        private static string file = File.ReadAllText("../net5.0-windows/Resources/deconjugator_edited.json");

        private static Rule[] rules = JsonSerializer.Deserialize<Rule[]>(file);

        //  private static dynamic rules = JsonConvert.DeserializeObject(file);

        public class Form
        {
            public Form(string text, string original_text, List<string> tags, HashSet<string> seentext,
                List<string> process)
            {
                this.text = text;
                this.original_text = original_text;
                this.tags = tags;
                this.seentext = seentext;
                this.process = process;
            }

            public string text { get; set; }
            public string original_text { get; set; }
            public List<string> tags { get; set; }
            public HashSet<string> seentext { get; set; }
            public List<string> process { get; set; }
        }

        public class Rule
        {
            public Rule(string type, object dec_end, object con_end, object dec_tag, object con_tag, string detail)
            {
                this.type = type;
                this.dec_end = dec_end;
                this.con_end = con_end;
                this.dec_tag = dec_tag;
                this.con_tag = con_tag;
                this.detail = detail;
            }

            public string type { get; set; }
            public object dec_end { get; set; }
            public object con_end { get; set; }
            public object dec_tag { get; set; }
            public object con_tag { get; set; }
            public string detail { get; set; }
        }

        public static void Test()
        {
            // foreach (var rule in rules)
            // {
            //     Debug.WriteLine(JsonSerializer.Serialize(rule));
            // }

            var result = deconjugate("主");
            Debug.WriteLine(result);
        }

        private static Form stdrule_deconjugate_inner(Form my_form,
            Rule my_rule)
        {
            // ending doesn't match
            if (!my_form.text.EndsWith((string) my_rule.con_end)) return null;
            // tag doesn't match
            if (my_form.tags.Count > 0 &&
                my_form.tags[my_form.tags.Count - 1] != (string) my_rule.con_tag)
                return null;

            var newtext =
                my_form.text.Substring
                (
                    0, my_form.text.Length - ((string) my_rule.con_end).Length
                )
                + ((string) my_rule.dec_end);

            // I hate javascript reeeeeeeeeeeeeee
            var newform = new Form(
                newtext,
                my_form.original_text,
                my_form.tags,
                my_form.seentext,
                my_form.process
            ); //new Object();

            newform.process.Add(my_rule.detail);

            if (newform.tags.Count == 0)
                newform.tags.Add((string) my_rule.con_tag);
            newform.tags.Add((string) my_rule.dec_tag);

            if (newform.seentext.Count == 0)
                newform.seentext.Add(my_form.text);
            newform.seentext.Add(newtext);

            return newform;
        }


        private static string index_or_value(object variable, int index)
        {
            return variable is List<string> list ? list[index] : (string) variable;
        }

        private static HashSet<Form> stdrule_deconjugate(Form my_form,
            Rule my_rule)
        {
            // can't deconjugate nothingness
            if (my_form.text == "")
                return null;
            // deconjugated form too much longer than conjugated form
            if (my_form.text.Length > my_form.original_text.Length + 10)
                return null;
            // impossibly information-dense
            if (my_form.tags.Count > my_form.original_text.Length + 6)
                return null;
            // blank detail mean it can't be the last (first applied, but rightmost) rule
            if (my_rule.detail == "" && my_form.tags.Count == 0)
                return null;

            var array = new List<string>();
            // pick the first one that is an array
            // FIXME: use minimum length for safety reasons? assert all arrays equal length?
            if (my_rule.dec_end is List<string>)
                array = new List<string> {(string) my_rule.dec_end};
            else if (my_rule.con_end is List<string>)
                array = new List<string> {(string) my_rule.con_end};
            else if (my_rule.dec_tag is List<string>)
                array = new List<string> {(string) my_rule.dec_tag};
            else if (my_rule.con_tag is List<string>)
                array = new List<string> {(string) my_rule.con_tag};

            if (array.Count < 1)
            {
                var result = stdrule_deconjugate_inner(my_form, my_rule);
                return result == null ? null : new HashSet<Form> {result};
            }
            else
            {
                var collection = new HashSet<Form>();
                for (var i = 0; i < array.Count; i++)
                {
                    var virtual_rule = new Rule
                    (
                        my_rule.type,
                        index_or_value(my_rule.dec_end, i),
                        index_or_value(my_rule.con_end, i),
                        index_or_value(my_rule.dec_tag, i),
                        index_or_value(my_rule.con_tag, i),
                        my_rule.detail
                    );

                    var ret = stdrule_deconjugate_inner(my_form, virtual_rule);
                    if (ret != null) collection.Add(ret);
                }

                return collection;
            }
        }

        private static HashSet<Form> rewriterule_deconjugate(Form my_form,
            Rule my_rule)
        {
            if (my_form.text != (string) my_rule.con_end)
                return null;
            return stdrule_deconjugate(my_form, my_rule);
        }

        private static HashSet<Form> onlyfinalrule_deconjugate(Form my_form,
            Rule my_rule)
        {
            if (my_form.tags.Count != 0)
                return null;
            return stdrule_deconjugate(my_form, my_rule);
        }

        private static HashSet<Form> neverfinalrule_deconjugate(Form my_form,
            Rule my_rule)
        {
            if (my_form.tags.Count == 0)
                return null;
            return stdrule_deconjugate(my_form, my_rule);
        }

        private static HashSet<Form> contextrule_deconjugate(Form my_form,
            Rule my_rule)
        {
            var result = my_rule.detail switch
            {
                "v1inftrap" => v1inftrap_check(my_form, my_rule),
                "saspecial" => saspecial_check(my_form, my_rule),
                _ => false
            };
            if (!result)
                return null;
            return stdrule_deconjugate(my_form, my_rule);
        }

        private static Form substitution_inner(Form my_form,
            Rule my_rule)
        {
            if (!my_form.text.Contains((string) my_rule.con_end))
                return null;
            var newtext = new Regex((string) my_rule.con_end)
                .Replace(my_form.text, (string) my_rule.dec_end);

            // I hate javascript reeeeeeeeeeeeeee
            var newform = new Form
            (
                newtext,
                my_form.original_text,
                my_form.tags,
                my_form.seentext,
                my_form.process
            ); //new Object();

            newform.process.Add(my_rule.detail);

            if (newform.seentext.Count == 0)
                newform.seentext.Add(my_form.text);
            newform.seentext.Add(newtext);

            return newform;
        }

        private static HashSet<Form> substitution_deconjugate(Form my_form,
            Rule my_rule)
        {
            if (my_form.process.Count != 0)
                return null;

            // can't deconjugate nothingness
            if (my_form.text == "")
                return null;

            var array = new List<string>();
            // pick the first one that is an array
            // FIXME: use minimum length for safety reasons? assert all arrays equal length?
            if (my_rule.dec_end is List<string>)
                array = new List<string> {(string) my_rule.dec_end};
            else if (my_rule.con_end is List<string>)
                array = new List<string> {(string) my_rule.con_end};

            if (array.Count < 1)
            {
                var result = substitution_inner(my_form, my_rule);
                return result == null ? null : new HashSet<Form> {result};
            }
            else
            {
                var collection = new HashSet<Form>();
                for (var i = 0; i < array.Count; i++)
                {
                    var virtual_rule = new Rule
                    (
                        my_rule.type,
                        index_or_value(my_rule.dec_end, i),
                        index_or_value(my_rule.con_end, i),
                        null,
                        null,
                        my_rule.detail
                    );

                    var ret = substitution_inner(my_form, virtual_rule);
                    if (ret != null) collection.Add(ret);
                }

                return collection;
            }
        }

        private static bool v1inftrap_check(Form my_form,
            Rule my_rule)
        {
            if (my_form.tags.Count != 1) return true;
            var my_tag = my_form.tags[0];
            if (my_tag == "stem-ren")
                return false;
            return true;
        }

        private static bool saspecial_check(Form my_form,
            Rule my_rule)
        {
            if (my_form.text == "") return false;
            if (!my_form.text.EndsWith((string) my_rule.con_end)) return false;
            var base_text = my_form.text.Substring(0, my_form.text.Length - ((string) my_rule.con_end).Length);
            if (base_text.EndsWith("さ"))
                return false;
            return true;
        }

        public static HashSet<Form> deconjugate(string mytext)
        {
            var processed = new HashSet<Form>();
            var novel = new HashSet<Form>();

            var start_form =
                new Form(mytext,
                    mytext,
                    new List<string>(),
                    new HashSet<string>(),
                    new List<string>()
                );
            novel.Add(start_form);

            var myrules = rules;

            while (novel.Count > 0)
            {
                Console.WriteLine(novel);
                var new_novel = new HashSet<Form>();
                foreach (Form form in novel)
                {
                    foreach (Rule rule in myrules)
                    {
                        // var newform = rule_functions[rule.type](form, rule);
                        // HashSet<Form> newform = stdrule_deconjugate(form, rule);
                        HashSet<Form> newform = null;
                        switch (rule.type)
                        {
                            case "stdrule":
                                newform = stdrule_deconjugate(form, rule);
                                break;
                            case "rewriterule":
                                newform = rewriterule_deconjugate(form, rule);
                                break;
                            case "onlyfinalrule":
                                newform = onlyfinalrule_deconjugate(form, rule);
                                break;
                            case "neverfinalrule":
                                newform = neverfinalrule_deconjugate(form, rule);
                                break;
                            case "contextrule":
                                newform = contextrule_deconjugate(form, rule);
                                break;
                            case "substitution":
                                newform = substitution_deconjugate(form, rule);
                                break;
                        }

                        if (newform != null && newform.Count > 1)
                        {
                            foreach (var myform in newform)
                            {
                                if (myform != null &&
                                    !processed.Contains(myform) &&
                                    !novel.Contains(myform) &&
                                    !new_novel.Contains(myform))
                                    new_novel.Add(myform);
                            }
                        }
                        else if (newform != null &&
                                 !processed.Contains(newform.First()) &&
                                 !novel.Contains(newform.First()) &&
                                 !new_novel.Contains(newform.First()))
                            new_novel.Add(newform.First());
                    }
                }

                processed = union(processed, novel);
                novel = new_novel;
            }

            return processed;
        }

        private static HashSet<Form> union(HashSet<Form> setA, HashSet<Form> setB)
        {
            foreach (var elem in setB)
                setA.Add(elem);
            return setA;
        }
    }
}