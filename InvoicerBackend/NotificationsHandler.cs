using RV.InvNew.Common;
using System;
using System.IO.Compression;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Transactions;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using RV.InvNew.Common;

namespace InvoicerBackend
{
    public static class NotificationsHandler
    {
        public static WebApplication AddNotificationsHandler(this WebApplication app)
        {
            app.MapPost("/SendNotification", (AuthenticatedRequest<NotificationTransfer> AN) => {
                if (AN.Get() != null)
                {
                    var N = AN.Get();
                    using (var ctx = new NewinvContext())
                    {
                        ctx.Notifications.Add(new Notification() { TimeTai = DateTime.UtcNow, TimeExpiresTai = DateTime.UtcNow.AddDays(3), NotifPriority = N.NotifPriority, NotifTarget = N.NotifTarget, NotifContents = N.NotifContents, NotifFrom = AN.Principal, NotifSource = "API", NotifOtherStatus = "Queued" });
                        ctx.SaveChanges();
                    }
                }
                else
                    throw new UnauthorizedAccessException();
            }).WithName("SendNotification").WithOpenApi();

            app.MapPost("/GetNotifications", (AuthenticatedRequest<string> ANR) => {
                if (ANR.Get() != null)
                {
                    var N = ANR.Get();
                    List<NotificationTransfer> LNT = new();
                    using (var ctx = new NewinvContext())
                    {
                        LNT = ctx.Notifications.Where(e => e.NotifTarget == ANR.Principal || e.NotifTarget.ToLower() == "Everyone".ToLowerInvariant()).Take(100).Select(e => new NotificationTransfer() { NotifContents = e.NotifContents, NotifId = e.NotifId, NotifPriority = e.NotifPriority, TimeExpiresTai = e.TimeExpiresTai, NotifTarget = e.NotifTarget, TimeTai = e.TimeTai }).ToList();
                    }
                    return LNT;
                }
                else
                    throw new UnauthorizedAccessException();
            }).WithName("GetNotifications").WithOpenApi();

            app.MapPost("/MarkNotificationsAsDone", (AuthenticatedRequest<List<NotificationTransfer>> ALNT) => {
                if (ALNT.Get() != null)
                {
                    var N = ALNT.Get();
                    System.Console.WriteLine(JsonSerializer.Serialize(N));
                    //TODO: Use transactions
                    using (var ctx = new NewinvContext())
                    {
                        foreach (var notif in N)
                        {

                            try
                            {
                                ctx.Notifications.Where(e => e.NotifTarget == ALNT.Principal && e.NotifId == notif.NotifId).First().NotifIsDone = true;
                                ctx.SaveChanges();
                            }
                            catch (Exception E) { }
                        }
                    }
                }
                else
                    throw new UnauthorizedAccessException();
            }).WithName("MarkNotificationsAsRead").WithOpenApi();
            return app;

        }
    }
}
