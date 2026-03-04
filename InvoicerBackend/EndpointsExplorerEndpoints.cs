using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.Json.Schema;

namespace InvoicerBackend
{
    public static class EndpointsExplorerEndpoints
    {


public static WebApplication AddEndpointsDefinitionEndpoint(this WebApplication app)
    {
            //JsonSchemaExporterOptions ExporterOptions = new()
        app.MapGet("/Endpoints", () =>
        {
            var options = JsonSerializerOptions.Default;
            var definitions = new List<object>();

            foreach (var kvp in RegisterEndpoint.EndpointsDict)
            {
                var def = kvp.Value;

                // Native .NET 9+ JSON Schema extraction
                JsonNode inputSchema = options.GetJsonSchemaAsNode(def.InputType);
                JsonNode outputSchema = options.GetJsonSchemaAsNode(def.OutputType);

                definitions.Add(new
                {
                    Name = def.EndpointName,
                    InputType = def.InputType.Name,
                    InputSchema = inputSchema,
                    OutputType = def.OutputType.Name,
                    OutputSchema = outputSchema,
                    Privilege = def.Privilege
                });
            }

            return Results.Ok(definitions);
        });

        return app;
    }
}
}
