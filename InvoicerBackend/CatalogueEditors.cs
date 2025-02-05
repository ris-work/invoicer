using RV.InvNew.Common;

namespace InvoicerBackend
{
    public static class CatalogueEditors
    {
        public static WebApplication AddCatalogueEditorHandlers(this WebApplication app)
        {
            app.MapPost("/CreateCatalogueItem", (AuthenticatedRequest<object> o) => { })
                .WithName("CreateCatalogueItem")
                .WithOpenApi();
            return app;
        }
    }
}
