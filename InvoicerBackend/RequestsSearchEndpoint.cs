using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RV.InvNew.Common;

namespace InvoicerBackend
{


    public static class RequestsEndpoints
    {
        public static WebApplication AddRequestsEndpoints(this WebApplication app)
        {
            app.AddEndpointWithBearerAuth<RequestSearchCriteria>(
                "RequestsSearchEndpoint",
                (AS, LoginInfo) =>
                {
                    var criteria = (RequestSearchCriteria)AS;
                    var results = new List<object>();

                    using (var ctx = new NewinvContext())
                    {
                        // Search in the main requests table
                        var requestsQuery = ctx.Set<Request>().AsQueryable();

                        if (criteria.From.HasValue)
                            requestsQuery = requestsQuery.Where(r => r.DatetimeTai >= criteria.From.Value.ToUniversalTime());

                        if (criteria.To.HasValue)
                            requestsQuery = requestsQuery.Where(r => r.DatetimeTai <= criteria.To.Value.ToUniversalTime());

                        if (criteria.Principal.HasValue)
                            requestsQuery = requestsQuery.Where(r => r.Principal == criteria.Principal.Value);

                        if (!string.IsNullOrEmpty(criteria.Token))
                            requestsQuery = requestsQuery.Where(r => r.Token.Contains(criteria.Token));

                        if (!string.IsNullOrEmpty(criteria.Type))
                            requestsQuery = requestsQuery.Where(r => r.Type == criteria.Type);

                        if (!string.IsNullOrEmpty(criteria.RequestedAction))
                            requestsQuery = requestsQuery.Where(r => r.RequestedAction == criteria.RequestedAction);

                        if (!string.IsNullOrEmpty(criteria.RequestedPrivilegeLevel))
                            requestsQuery = requestsQuery.Where(r => r.RequestedPrivilegeLevel == criteria.RequestedPrivilegeLevel);

                        if (!string.IsNullOrEmpty(criteria.Endpoint))
                            requestsQuery = requestsQuery.Where(r => r.Endpoint == criteria.Endpoint);

                        // Apply limit if specified
                        if (criteria.Limit.HasValue)
                            requestsQuery = requestsQuery.Take(criteria.Limit.Value);
                        requestsQuery = requestsQuery.OrderByDescending(r => r.DatetimeTai);

                        var requests = requestsQuery.ToList();
                        results.AddRange(requests);

                        // Search in the bad requests table if requested
                        if (criteria.IncludeBadRequests)
                        {
                            var badRequestsQuery = ctx.Set<RequestsBad>().AsQueryable();

                            if (criteria.From.HasValue)
                                badRequestsQuery = badRequestsQuery.Where(r => r.TimeTai >= criteria.From.Value.ToUniversalTime());

                            if (criteria.To.HasValue)
                                badRequestsQuery = badRequestsQuery.Where(r => r.TimeTai <= criteria.To.Value.ToUniversalTime());

                            if (criteria.Principal.HasValue)
                                badRequestsQuery = badRequestsQuery.Where(r => r.Principal == criteria.Principal.Value);

                            if (!string.IsNullOrEmpty(criteria.Token))
                                badRequestsQuery = badRequestsQuery.Where(r => r.Token.Contains(criteria.Token));

                            if (!string.IsNullOrEmpty(criteria.Type))
                                badRequestsQuery = badRequestsQuery.Where(r => r.Type == criteria.Type);

                            if (!string.IsNullOrEmpty(criteria.RequestedAction))
                                badRequestsQuery = badRequestsQuery.Where(r => r.RequestedAction == criteria.RequestedAction);

                            if (!string.IsNullOrEmpty(criteria.RequestedPrivilegeLevel))
                                badRequestsQuery = badRequestsQuery.Where(r => r.RequestedPrivilegeLevel == criteria.RequestedPrivilegeLevel);

                            if (!string.IsNullOrEmpty(criteria.Endpoint))
                                badRequestsQuery = badRequestsQuery.Where(r => r.Endpoint == criteria.Endpoint);

                            // Apply limit if specified
                            if (criteria.Limit.HasValue)
                                badRequestsQuery = badRequestsQuery.Take(criteria.Limit.Value);
                            badRequestsQuery = badRequestsQuery.OrderByDescending(r => r.TimeTai);

                            var badRequests = badRequestsQuery.ToList();
                            results.AddRange(badRequests);
                        }
                    }

                    return results;
                },
                "Refresh"
            );

            return app;
        }
    }
}