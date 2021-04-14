using System.Collections.Generic;

namespace JapaneseLookup
{
    public class Request
    {
        // camelCase property names because AnkiConnect
        #pragma warning disable IDE1006

        public string action { get; set; }

        public int version { get; set; }

        // maybe Dictionary<string, Dictionary<string,object>>
        public Dictionary<string, object> @params { get; set; }

        public Request(string action, int version, Dictionary<string, object> @params = null)
        {
            this.action = action;
            this.version = version;
            this.@params = @params;
        }
    }
}