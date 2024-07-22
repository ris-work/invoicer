using Eto.Drawing;
using Eto.Forms;
using System;
using System.Configuration;
using System.Linq;
using Tomlyn;
using System.Collections.Generic;

namespace AuthManager
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            var UserList = new GridView();
            UserList.Columns.Add(new GridColumn { HeaderText = "UserID", DataCell = new TextBoxCell(0) });
            UserList.Columns.Add(new GridColumn { HeaderText = "Username", DataCell = new TextBoxCell(1) });
            UserList.Columns.Add(new GridColumn { HeaderText = "Modified", DataCell = new TextBoxCell(2) });
            UserList.Columns.Add(new GridColumn { HeaderText = "Created", DataCell = new TextBoxCell(3) });
            string x="";
            using (var ctx = new NewinvContext())
            {
                var users = ctx.Credentials.ToList();
                var list = new List<GridItem>();
                
                foreach (var item in users)
                {
                    list.Add(new GridItem(item.Userid, item.Username, item.Modified, item.Created.ToString()));
                }
                UserList.DataStore = list;

            }
            Title = "My Eto Form";
            MinimumSize = new Size(200, 200);

            Content = new StackLayout
            {
                Padding = 10,
                Items =
                {
                    "Hello World!",
                    x,
                    new Button((e, a) => MessageBox.Show("Hello")){  },
                    UserList

                }

            };
            KeyUp += ((e, a) => MessageBox.Show(a.Key.ToString()));

            
        }
    }
}
