using static InvoicerBackend.RegisterEndpoint;

namespace InvoicerBackend
{
    public class AmIAllowedToRequest
    {
        public string EndpointName { get; set; }
    }

    public class AmIAllowedToResponse
    {
        public bool Exists { get; set; }
        public bool Allowed { get; set; }
        public string? RequiredPrivilege { get; set; }
        public string Message { get; set; }
    }

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
            app.AddEndpointWithBearerAuth<AmIAllowedToRequest, AmIAllowedToResponse>(
    "AmIAllowedTo",
    (DataIn, LoginInfo) =>
    {
        var req = (AmIAllowedToRequest)DataIn;

        if (string.IsNullOrWhiteSpace(req.EndpointName))
            return new AmIAllowedToResponse { Exists = false, Allowed = false, Message = "Endpoint name is required." };

        if (RegisterEndpoint.EndpointsDict.TryGetValue(req.EndpointName, out var def))
        {
            var requiredPriv = def.Privilege;
            bool allowed = LoginInfo.PermittedTo?.Any(p =>
                string.Equals(p, requiredPriv, StringComparison.OrdinalIgnoreCase)
            ) ?? false;

            return new AmIAllowedToResponse
            {
                Exists = true,
                Allowed = allowed,
                RequiredPrivilege = requiredPriv,
                Message = allowed ? "Access Granted" : $"Missing privilege: {requiredPriv}"
            };
        }

        return new AmIAllowedToResponse { Exists = false, Allowed = false, Message = "Endpoint not found in dictionary." };
    },
    "Refresh"
);
            return app;
        }
    }
}
