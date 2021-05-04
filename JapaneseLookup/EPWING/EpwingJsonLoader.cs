using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using System.Diagnostics;

namespace JapaneseLookup.EPWING
{
    class EpwingJsonLoader
    {
        public async static void Loader(string path)
        {
            List<EpwingEntry> epwingEntryList = new();

            string[] jsonFiles = Directory.GetFiles(path, "*_bank_*.json");

            foreach (string jsonFile in jsonFiles)
            {
                await using FileStream openStream = File.OpenRead(jsonFile);
                var jsonObject = await JsonSerializer.DeserializeAsync<List<List<JsonElement>>>(openStream);

                foreach(var obj in jsonObject)
                {
                    epwingEntryList.Add(new EpwingEntry(obj));
                }
            }

            DictionaryBuilder(epwingEntryList);
        }

        public static void DictionaryBuilder(List<EpwingEntry> epwing)
        {
            //Debug.WriteLine(epwing[20000].Expression);
            //Debug.WriteLine(epwing[20000].Reading);
            //foreach (var gloss in epwing[20000].Glosssary)
            //{
             //Debug.WriteLine(gloss);
            //}
            

            foreach(var entry in epwing)
            {
                //Rules = POS, Reading, Expression
                //TermTags, DefinitionTags
                //dammar;damar
                if ("\"\"" != entry.TermTags)
                Debug.WriteLine(entry.TermTags);
                //foreach(var gloss in entry.Glosssary)
                //{
                //    Debug.WriteLine(gloss);
                //}
            }
        }
    }
}