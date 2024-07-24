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
            UserList.Columns.Add(new GridColumn () { HeaderText = "UserID", DataCell = new TextBoxCell(0) });
            UserList.Columns.Add(new GridColumn { HeaderText = "Username", DataCell = new TextBoxCell(1) });
            UserList.Columns.Add(new GridColumn { HeaderText = "Modified", DataCell = new TextBoxCell(2) });
            UserList.Columns.Add(new GridColumn { HeaderText = "Created", DataCell = new TextBoxCell(3) });
            UserList.MouseDoubleClick += (e, a) => { MessageBox.Show(((GridItem)UserList.SelectedItem).GetValue(0).ToString()); };
            
            string x="";
            using (var ctx = new NewinvContext())
            {
                var users = ctx.Credentials.ToList();
                var list = new List<GridItem>();
                
                foreach (var item in users)
                {
                    var GR = new GridItem(item.Userid, item.Username, item.Modified.ToString(), item.CreatedTime.ToString());
                    
                    list.Add(GR);
                }
                UserList.DataStore = list;

            }
            Title = "My Eto Form";
            Size = new Size(-1, -1);
            Resizable = false;
            

            Content = new StackLayout
            {
                Padding = 10,
                Items =
                {
                    "Hello World!",
                    x,
                    new Button((e, a) => (new NewUserForm()).ShowModal()),
                    UserList

                }

            };
            KeyUp += ((e, a) => MessageBox.Show(a.Key.ToString()));

            
        }
    }
}
