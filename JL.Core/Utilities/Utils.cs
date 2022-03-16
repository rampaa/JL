using System.Diagnostics;
using System.IO.Compression;
using System.Configuration;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using JL.Core.Dicts;
using Serilog;
using Serilog.Core;

namespace JL.Core.Utilities
{
    public static class Utils
    {
        public static readonly Logger Logger = new LoggerConfiguration().WriteTo.File("Logs/log.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileTimeLimit: TimeSpan.FromDays(90),
                shared: true)
            .CreateLogger();

        public static void CreateDefaultDictsConfig()
        {
            var jso = new JsonSerializerOptions
            {
                WriteIndented = true, Converters = { new JsonStringEnumConverter(), }
            };

            try
            {
                Directory.CreateDirectory(Path.Join(Storage.ApplicationPath, "Config"));
                File.WriteAllText(Path.Join(Storage.ApplicationPath, "Config/dicts.json"),
                    JsonSerializer.Serialize(Storage.BuiltInDicts, jso));
            }
            catch (Exception e)
            {
                Storage.Frontend.Alert(AlertLevel.Error, "Couldn't write default Dicts config");
                Logger.Error(e, "Couldn't write default Dicts config");
            }
        }

        public static void SerializeDicts()
        {
            try
            {
                var jso = new JsonSerializerOptions
                {
                    WriteIndented = true, Converters = { new JsonStringEnumConverter(), }
                };

                File.WriteAllTextAsync(Path.Join(Storage.ApplicationPath, "Config/dicts.json"),
                    JsonSerializer.Serialize(Storage.Dicts, jso));
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "SerializeDicts failed");
                throw;
            }
        }

        public static async Task DeserializeDicts()
        {
            try
            {
                var jso = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter(), } };

                Dictionary<DictType, Dict> deserializedDicts = await JsonSerializer
                    .DeserializeAsync<Dictionary<DictType, Dict>>(
                        new StreamReader(Path.Join(Storage.ApplicationPath, "Config/dicts.json")).BaseStream, jso)
                    .ConfigureAwait(false);

                if (deserializedDicts != null)
                {
                    foreach (Dict dict in deserializedDicts.Values)
                    {
                        if (!Storage.Dicts.ContainsKey(dict.Type))
                        {
                            dict.Contents = new Dictionary<string, List<IResult>>();
                            Storage.Dicts.Add(dict.Type, dict);
                        }
                    }
                }
                else
                {
                    Storage.Frontend.Alert(AlertLevel.Error, "Couldn't load Config/dicts.json");
                    Utils.Logger.Error("Couldn't load Config/dicts.json");
                }
            }
            catch (Exception e)
            {
                Utils.Logger.Fatal(e, "DeserializeDicts failed");
                throw;
            }
        }

        public static string GetMd5String(byte[] bytes)
        {
            byte[] hash = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5"))!.ComputeHash(bytes);
            string encoded = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();

            return encoded;
        }
    }
}
