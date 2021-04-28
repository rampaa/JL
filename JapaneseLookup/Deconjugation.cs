using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = System.Text.Json.JsonSerializer;

// ReSharper disable CommentTypo

// ReSharper disable StringLiteralTypo

// ReSharper disable IdentifierTypo

// ReSharper disable InconsistentNaming

namespace JapaneseLookup
{
    public static class Deconjugation
    {
        private static readonly string file = File.ReadAllText("../net5.0-windows/Resources/deconjugator_edited.json");

        // private static Rule[] rules = JsonSerializer.Deserialize<Rule[]>(file);

        private static readonly Rule[] rules = JsonConvert.DeserializeObject<Rule[]>(file);

        internal class Form
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

            public string text { get; }
            public string original_text { get; }
            public List<string> tags { get; }
            public HashSet<string> seentext { get; }
            public List<string> process { get; }
        }

        private class Rule
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

            public string type { get; }
            public object dec_end { get; }
            public object con_end { get; }
            public object dec_tag { get; }
            public object con_tag { get; }
            public string detail { get; }
        }

        public static void Test()
        {
            Thread.Sleep(500);
            var result = deconjugate("わからない");

            Debug.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }));
        }

        private static Form stdrule_deconjugate_inner(Form my_form,
            Rule my_rule)
        {
            // ending doesn't match
            if (!my_form.text.EndsWith((string) my_rule.con_end))
                return null;
            // tag doesn't match
            if (my_form.tags.Count > 0 &&
                my_form.tags[^1] != (string) my_rule.con_tag)
            {
                // Debug.WriteLine("TAG DIDN'T MATCH; my_form: " + JsonSerializer.Serialize(
                //     my_form, new JsonSerializerOptions
                //     {
                //         Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                //     }));
                // Debug.WriteLine("TAG DIDN'T MATCH; my_rule: " + JsonSerializer.Serialize(
                //     my_rule, new JsonSerializerOptions
                //     {
                //         Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                //     }));
                return null;
            }

            var newtext =
                my_form.text.Substring(0, my_form.text.Length - ((string) my_rule.con_end).Length)
                +
                (string) my_rule.dec_end;

            // Debug.WriteLine(JsonSerializer.Serialize("newtext: " + newtext, new JsonSerializerOptions
            // {
            //     Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            // }));

            // I hate javascript reeeeeeeeeeeeeee
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            var clone = JsonSerializer.Deserialize<Form>(JsonSerializer.Serialize(my_form));
            stopWatch.Stop();
            double ms = (stopWatch.ElapsedTicks * 1000.0) / Stopwatch.Frequency;
            Debug.WriteLine(string.Concat(ms.ToString(), " ms"));

            var newform = new Form(
                newtext,
                my_form.original_text,
                clone?.tags,
                clone?.seentext,
                clone?.process
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
            return variable is string[] array ? array[index] : (string) variable;
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

            var array = new List<object>();
            // pick the first one that is an array
            // FIXME: use minimum length for safety reasons? assert all arrays equal length?

            var dec_end_array_or_string =
                (my_rule.dec_end as JArray)?.ToObject<string[]>() ?? (object) ((string) my_rule.dec_end);
            var con_end_array_or_string =
                (my_rule.con_end as JArray)?.ToObject<string[]>() ?? (object) ((string) my_rule.con_end);
            var dec_tag_array_or_string =
                (my_rule.dec_tag as JArray)?.ToObject<string[]>() ?? (object) ((string) my_rule.dec_tag);
            var con_tag_array_or_string =
                (my_rule.con_tag as JArray)?.ToObject<string[]>() ?? (object) ((string) my_rule.con_tag);

            if (my_rule.dec_end != null)
                array = new List<object> {dec_end_array_or_string};
            else if (my_rule.con_end != null)
                array = new List<object> {con_end_array_or_string};
            else if (my_rule.dec_tag != null)
                array = new List<object> {dec_tag_array_or_string};
            else if (my_rule.con_tag != null)
                array = new List<object> {con_tag_array_or_string};

            if (array[0] is string)
            {
                var result = stdrule_deconjugate_inner(my_form, my_rule);

                // Debug.WriteLine("result: " + JsonSerializer.Serialize(result, new JsonSerializerOptions
                // {
                //     Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                // }));

                return result == null ? null : new HashSet<Form> {result};
            }
            else
            {
                var collection = new HashSet<Form>();

                var length = ((string[]) array[0]).Length;
                for (var i = 0; i < length; i++)
                {
                    var virtual_rule = new Rule
                    (
                        my_rule.type,
                        index_or_value(dec_end_array_or_string, i),
                        index_or_value(con_end_array_or_string, i),
                        index_or_value(dec_tag_array_or_string, i),
                        index_or_value(con_tag_array_or_string, i),
                        my_rule.detail
                    );
                    // Debug.WriteLine("virtual_rule: " + JsonSerializer.Serialize(virtual_rule, new JsonSerializerOptions
                    // {
                    //     Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    // }));

                    // Debug.WriteLine("sending to inner my_form: " + JsonSerializer.Serialize(my_form,
                    //     new JsonSerializerOptions
                    //     {
                    //         Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    //     }));
                    var ret = stdrule_deconjugate_inner(my_form, virtual_rule);

                    // Debug.WriteLine("ret: " + JsonSerializer.Serialize(ret, new JsonSerializerOptions
                    // {
                    //     Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    // }));

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
                "v1inftrap" => v1inftrap_check(my_form),
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
            var clone = JsonSerializer.Deserialize<Form>(JsonSerializer.Serialize(my_form));
            var newform = new Form(
                newtext,
                my_form.original_text,
                clone?.tags,
                clone?.seentext,
                clone?.process
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

            var array = new List<object>();

            var dec_end_array_or_string =
                (my_rule.dec_end as JArray)?.ToObject<string[]>() ?? (object) ((string) my_rule.dec_end);
            var con_end_array_or_string =
                (my_rule.con_end as JArray)?.ToObject<string[]>() ?? (object) ((string) my_rule.con_end);

            // pick the first one that is an array
            // FIXME: use minimum length for safety reasons? assert all arrays equal length?
            if (my_rule.dec_end != null)
                array = new List<object> {dec_end_array_or_string};
            else if (my_rule.con_end != null)
                array = new List<object> {con_end_array_or_string};

            if (array[0] is string)
            {
                var result = substitution_inner(my_form, my_rule);
                return result == null ? null : new HashSet<Form> {result};
            }
            else
            {
                var collection = new HashSet<Form>();

                var length = ((string[]) array[0]).Length;
                for (var i = 0; i < length; i++)
                {
                    var virtual_rule = new Rule
                    (
                        my_rule.type,
                        index_or_value(dec_end_array_or_string, i),
                        index_or_value(con_end_array_or_string, i),
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

        private static bool v1inftrap_check(Form my_form)
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

        internal static HashSet<Form> deconjugate(string mytext)
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
                // foreach (var n in novel)
                // {
                //     Debug.WriteLine("novel: " + JsonSerializer.Serialize(n, new JsonSerializerOptions
                //     {
                //         Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                //     }));
                // }

                var new_novel = new HashSet<Form>();
                foreach (Form form in novel)
                {
                    foreach (Rule rule in myrules)
                    {
                        HashSet<Form> newform = null;

                        // Debug.WriteLine("rule: " + JsonSerializer.Serialize(rule, new JsonSerializerOptions
                        // {
                        //     Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        // }));

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

                        if (newform == null || newform.Count == 0) continue;

                        foreach (var myform in newform)
                        {
                            if (myform != null &&
                                !processed.Contains(myform) &&
                                !novel.Contains(myform) &&
                                !new_novel.Contains(myform))
                            {
                                new_novel.Add(myform);
                            }
                        }

                        // foreach (var nn in new_novel)
                        // {
                        //     Debug.WriteLine("new_novel:" + JsonSerializer.Serialize(nn,
                        //         new JsonSerializerOptions
                        //         {
                        //             Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        //         }));
                        // }
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