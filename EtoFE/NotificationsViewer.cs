using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;
using RV.InvNew.Common;

namespace EtoFE
{
    class NotificationsViewer : Eto.Forms.Dialog
    {
        public NotificationsViewer(List<NotificationTransfer> LN)
        {
            GridView NotificationsGridView = new GridView();
            NotificationsGridView.Columns.Add(
                new GridColumn() { HeaderText = "Notification ID", DataCell = new TextBoxCell(0) }
            );
            NotificationsGridView.Columns.Add(
                new GridColumn()
                {
                    HeaderText = "Notification Priority",
                    DataCell = new TextBoxCell(1),
                }
            );
            NotificationsGridView.Columns.Add(
                new GridColumn()
                {
                    HeaderText = "Notification Target",
                    DataCell = new TextBoxCell(2),
                }
            );
            NotificationsGridView.Columns.Add(
                new GridColumn() { HeaderText = "Notification Time", DataCell = new TextBoxCell(3) }
            );
            NotificationsGridView.Columns.Add(
                new GridColumn()
                {
                    HeaderText = "Notification Contents",
                    DataCell = new TextBoxCell(4),
                }
            );
            long count = 0;
            count = LN.Count;
            List<GridItem> NGI = new();
            foreach (var notification in LN)
            {
                NGI.Add(
                    new GridItem(
                        notification.NotifId,
                        notification.NotifPriority,
                        notification.NotifTarget,
                        notification.TimeTai,
                        notification.NotifContents
                    )
                    { }
                );
            }
            NotificationsGridView.DataStore = NGI;

            MessageBox.Show(count.ToString(), "Count", MessageBoxType.Information);
            Title = "Notifications";
            Button MarkAllAsRead = new Button()
            {
                BackgroundColor = Eto.Drawing.Colors.DarkRed,
                TextColor = Eto.Drawing.Colors.White,
                Text = "Mark as read",
            };
            Button Exit = new Button() { Text = "Exit" };
            StackLayout SLB = new StackLayout(null, MarkAllAsRead, Exit)
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Padding = 5,
            };
            Content = new StackLayout(NotificationsGridView, SLB)
            {
                Orientation = Orientation.Vertical,
                Padding = 5,
                HorizontalContentAlignment = HorizontalAlignment.Right,
            };
            Exit.Click += (_, _) =>
            {
                this.Close();
            };
            MarkAllAsRead.Click += (_, _) =>
            {
                var NotificationsToMark = LN.Select(e => new NotificationTransfer()
                    {
                        NotifId = e.NotifId,
                    })
                    .ToList();
                //MessageBox.Show(NotificationsToMark.Count.ToString());
                var NotificationsMarkAR = new AuthenticatedRequest<List<NotificationTransfer>>(
                    NotificationsToMark,
                    LoginTokens.token
                );
                var NotificationsMark = Program
                    .client.PostAsJsonAsync<object>("/MarkNotificationsAsDone", NotificationsMarkAR)
                    .GetAwaiter()
                    .GetResult();
            };
            Width = 400;
            KeyDown += (e, a) => {
                if (a.Key == Keys.Escape) this.Close();
            };
        }
    }
}
