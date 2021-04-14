namespace JapaneseLookup.Anki
{
    public class Response
    {
        // camelCase property names because AnkiConnect
        #pragma warning disable IDE1006

        // result can be: 
        //   a number
        //   an array of strings
        //   an array of (JSON) objects
        //   an array of booleans
        // /shrug
        public object result { get; set; }

        public object error { get; set; }
    }
}