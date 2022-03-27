namespace JL.Core.Deconjugation
{
    public class Form
    {
        public string Text { get; }
        public string OriginalText { get; }
        public List<string> Tags { get; }
        public HashSet<string> Seentext { get; }
        public List<string> Process { get; }
        public Form(string text, string originalText, List<string> tags, HashSet<string> seentext,
            List<string> process)
        {
            Text = text;
            OriginalText = originalText;
            Tags = tags;
            Seentext = seentext;
            Process = process;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            Form form = obj as Form;

            return Text == form.Text
                   && OriginalText == form.OriginalText
                   && Tags.SequenceEqual(form.Tags)
                   && Seentext.SetEquals(form.Seentext)
                   && Process.SequenceEqual(form.Process);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash = hash * 37 + Text?.GetHashCode() ?? 0;
                hash = hash * 37 + OriginalText?.GetHashCode() ?? 0;

                if (Tags == null)
                    hash *= 37;

                else
                    foreach (string tag in Tags)
                        hash = hash * 37 + tag?.GetHashCode() ?? 0;

                if (Process == null)
                    hash *= 37;

                else
                    foreach (string process in Process)
                        hash = hash * 37 + process?.GetHashCode() ?? 0;

                if (Seentext == null)
                    hash *= 37;

                else
                    foreach (string seenText in Seentext)
                        hash = hash * 37 + seenText?.GetHashCode() ?? 0;

                return hash;
            }
        }

    }
}
