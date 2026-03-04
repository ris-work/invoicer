using static InvoicerBackend.RegisterEndpoint;

namespace InvoicerBackend
{
    public static class PrivilegeUtilities
    {
        public static WebApplication AddPrivilegeUtilities(this WebApplication app)
        {
            // Add this to your WebApplication map logic
            app.AddEndpointWithBearerAuth<object, LoginDetails>(
                "BearerEcho",
                (DataIn, LoginInfo) =>
                {
                    // Simply return the LoginDetails struct populated by the auth middleware
                    return LoginInfo;
                },
                "Refresh"
            );
            app.AddEndpointWithBearerAuth<object, string[]?>(
                "BearerPrivileges",
                (DataIn, LoginInfo) =>
                {
                    // Simply return the LoginDetails struct populated by the auth middleware
                    return LoginInfo.PermittedTo;
                },
                "Refresh"
            );
            return app;
        }
    }
}
