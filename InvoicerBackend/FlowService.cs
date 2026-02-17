using System.Collections.Concurrent;

namespace InvoicerBackend
{
    // Simple in-memory store for flow states
    public static class FlowService
    {
        private static ConcurrentDictionary<string, Dictionary<string, string>> _store = new();

        public static void Set(string flowId, string key, string value)
        {
            var data = _store.GetOrAdd(flowId, _ => new Dictionary<string, string>());
            data[key] = value;
        }

        public static string Get(string flowId, string key)
        {
            if (_store.TryGetValue(flowId, out var data))
            {
                if (data.TryGetValue(key, out var value))
                {
                    return value;
                }
            }
            return null;
        }

        public static void Dump(string flowId)
        {
            if (_store.TryGetValue(flowId, out var data))
            {
                Console.WriteLine($"Dumping Flow {flowId}:");
                foreach (var kvp in data) Console.WriteLine($"- {kvp.Key}: {kvp.Value}");
            }
        }
    }
}