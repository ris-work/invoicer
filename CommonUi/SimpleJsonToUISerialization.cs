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
            Dictionary<string, (string, object, string?)> Output = new();
            var JsonDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                SimpleJsonNoArrays
            );
            foreach (KeyValuePair<string, JsonElement> kv in JsonDict.AsEnumerable())
            {
                try
                {
                    Output.Add(kv.Key, (kv.Key, kv.Value.GetDouble(), null));
                }
                catch (Exception E)
                {
                    Output.Add(kv.Key, (kv.Key, kv.Value.GetString(), null));
                }
            }
            return Output;
        }
    }
}
