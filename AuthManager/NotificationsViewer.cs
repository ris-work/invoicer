using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;
using RV.InvNew.Common;

namespace AuthManager
{
    class NotificationsViewer : Eto.Forms.Dialog
    {
        public NotificationsViewer()
        {
            List<RV.InvNew.Common.Notification> N = new List<RV.InvNew.Common.Notification>();
            GridView NotificationsGridView = new GridView();
            NotificationsGridView.Columns.Add(new GridColumn()
            {
                HeaderText = "Notification ID",
                DataCell = new TextBoxCell(0)
            });
            NotificationsGridView.Columns.Add(new GridColumn()
            {
                HeaderText = "Notification Type",
                DataCell = new TextBoxCell(1)
            });
            NotificationsGridView.Columns.Add(new GridColumn()
            {
                HeaderText = "Notification Priority",
                DataCell = new TextBoxCell(2)
            });
            NotificationsGridView.Columns.Add(new GridColumn()
            {
                HeaderText = "Notification Target",
                DataCell = new TextBoxCell(3)
            });
            NotificationsGridView.Columns.Add(new GridColumn()
            {
                HeaderText = "Notification IsDone",
                DataCell = new TextBoxCell(4)
            });
            NotificationsGridView.Columns.Add(new GridColumn()
            {
                HeaderText = "Notification Time",
                DataCell = new TextBoxCell(5)
            });
            NotificationsGridView.Columns.Add(new GridColumn()
            {
                HeaderText = "Notification Contents",
                DataCell = new TextBoxCell(6)
            });
            long count = 0;
            using (var ctx = new NewinvContext())
            {
                count = ctx.Notifications.Count();
                N = ctx.Notifications.OrderByDescending(e => e.TimeTai).Take(1000).ToList();
            }
            List<GridItem> NGI = new();
            foreach(var notification in N)
            {
                NGI.Add(new GridItem(notification.NotifId, notification.NotifType, notification.NotifPriority, notification.NotifTarget, notification.NotifIsDone, notification.TimeTai, notification.NotifContents) { });
            }
            NotificationsGridView.DataStore = NGI;
            Content = NotificationsGridView;
            MessageBox.Show(count.ToString(), "Count", MessageBoxType.Information);
            Title = "Notifications";
        }
    }
}
