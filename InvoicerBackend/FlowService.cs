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
            lock (data) { data[key] = value; }
        }

        public static string Get(string flowId, string key)
        {
            if (_store.TryGetValue(flowId, out var data))
            {
                lock (data)
                {
                    if (data.TryGetValue(key, out var value)) return value;
                }
            }
            return null;
        }

        public static Dictionary<string, Dictionary<string, string>> GetAllFlows()
        {
            // Return a deep copy to avoid thread issues
            var copy = new Dictionary<string, Dictionary<string, string>>();
            foreach (var kvp in _store)
            {
                lock (kvp.Value)
                {
                    copy[kvp.Key] = new Dictionary<string, string>(kvp.Value);
                }
            }
            return copy;
        }
    }

    public static class FlowRequestEndpoints
    {
        public static WebApplication AddFlowEndpoints(this WebApplication app)
        {
            app.AddAsyncEndpointWithBearerAuth<FlowRequest, bool>(
            "SetFlowData",
            async (DataIn, LoginInfo) =>
            {
                var req = (FlowRequest)DataIn;
                FlowService.Set(req.FlowId, req.Key, req.Value);
                return true;
            },
                "Refresh"
            );

            app.AddAsyncEndpointWithBearerAuth<FlowRequest, object>(
                "GetFlowData",
                async (DataIn, LoginInfo) =>
                {
                    var req = (FlowRequest)DataIn;
                    var val = FlowService.Get(req.FlowId, req.Key);
                    return new { Value = val };
                },
                "Refresh"
            );
            return app;
        }
    }
}