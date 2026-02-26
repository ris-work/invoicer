namespace InvoicerBackend
{
    public static class DiagnosticEndpoints
    {
        public static WebApplication AddDiagnosticEndpoints(this WebApplication app)
        {
            // --- Diagnostic Endpoints ---

            // 1. Get All Flow Sessions
            app.AddAsyncEndpointWithBearerAuth<object, Dictionary<string, Dictionary<string, string>>>(
                "GetAllFlows",
                async (DataIn, LoginInfo) =>
                {
                    // We need to expose the internal store of FlowService for diagnostics
                    // Assuming FlowService has a method to dump all or we access it here.
                    // For now, we assume FlowService._store is accessible or we add a method to it.
                    // Adding a method to FlowService is cleaner.

                    // We will return a dictionary of FlowId -> Data
                    return FlowService.GetAllFlows();
                },
                "Refresh"
            );

            // 2. Get System Info
            app.AddAsyncEndpointWithBearerAuth<object, Dictionary<string, object>>(
                "GetSystemInfo",
                async (DataIn, LoginInfo) =>
                {
                    var info = new Dictionary<string, object>
                    {
                        { "Timestamp", DateTime.UtcNow },
                        { "OsVersion", Environment.OSVersion.ToString() },
                        { "MachineName", Environment.MachineName },
                        { "UserName", Environment.UserName },
                        { "DotNetVersion", Environment.Version.ToString() },
                        { "FrameworkDescription", System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription },
                        { "ProcessArchitecture", System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString() },
                        { "WorkingSet", GC.GetTotalMemory(false) / 1024 / 1024 + " MB" },
                        { "ProcessorCount", Environment.ProcessorCount },
                        { "CurrentDirectory", Environment.CurrentDirectory }
                    };
                    return info;
                },
                "Refresh"
            );
            return app;
        }
    }
}
