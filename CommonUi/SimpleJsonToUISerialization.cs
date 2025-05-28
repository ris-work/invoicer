using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CommonUi
{
    public static class SimpleJsonToUISerialization
    {
        public static IReadOnlyDictionary<
            string,
            (string, object, string?)
        > ConvertToUISerialization(string SimpleJsonNoArrays)
        {
            Console.WriteLine($"UI generation requested for: ${SimpleJsonNoArrays}");
            Dictionary<string, (string, object, string?)> Output = new();
            var JsonDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                SimpleJsonNoArrays
            );
            foreach (KeyValuePair<string, JsonElement> kv in JsonDict.AsEnumerable())
            {
                try
                {
                    Output.Add(kv.Key, (kv.Key, kv.Value.GetInt64(), null));
                }
                catch (Exception E)
                {
                    try
                    {
                        Output.Add(kv.Key, (kv.Key, kv.Value.GetDouble(), null));
                    }
                    catch (Exception _)
                    {
                        try
                        {
                            Output.Add(kv.Key, (kv.Key, kv.Value.GetString(), null));
                        }
                        catch (Exception)
                        {
                            Output.Add(kv.Key, (kv.Key, kv.Value.GetBoolean(), null));
                        }
                    }
                }
            }
            return Output;
        }
    }
}
