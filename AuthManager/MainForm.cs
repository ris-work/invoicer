using Eto.Drawing;
using Eto.Forms;
using System;
using System.Configuration;
using System.Linq;
using Tomlyn;
using System.Collections.Generic;
using AuthManager;

namespace AuthManager
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            var UserList = new GridView();
            UserList.Enabled = true;
            UserList.Size = new Size(-1, 300);
            
            UserList.Columns.Add(new GridColumn () { HeaderText = "UserID", DataCell = new TextBoxCell(0) });
            UserList.Columns.Add(new GridColumn { HeaderText = "Username", DataCell = new TextBoxCell(1) });
            UserList.Columns.Add(new GridColumn { HeaderText = "Modified", DataCell = new TextBoxCell(2) });
            UserList.Columns.Add(new GridColumn { HeaderText = "Created", DataCell = new TextBoxCell(3) });
            UserList.MouseDoubleClick += (e, a) => { 
                MessageBox.Show(((GridItem)UserList.SelectedItem).GetValue(0).ToString()); 
                (new NewUserForm((long)((GridItem)UserList.SelectedItem).GetValue(0))).ShowModal();
                using (var ctx = new NewinvContext())
                {
                    
                    UserList.DataStore = this.GetAllUsersGrid();
                    UserList.Invalidate();

                }
                UserList.Invalidate();
            };
            
            string x="";
            
                UserList.DataStore = this.GetAllUsersGrid();
                

            
            Title = "My Eto Form";
            Size = new Size(-1, -1);
            Resizable = false;


            Content = new StackLayout
            {
                Padding = 10,
                Items =
                {
                    "Hello World!",
                    new StackLayout {
                       Items = {
                            null,
                            new Button((e, a) => {new NewUserForm(null).ShowModal(); UserList.DataStore = this.GetAllUsersGrid(); }){ Text = "New User", BackgroundColor = Color.FromArgb(0xaa, 0xff, 0xaa, 0xff)  },
                            new Button((e, a) => {
                                var Selected = (GridItem)UserList.SelectedItem;
                                if (Selected!= null) new TestPasswordDialog((long)Selected.GetValue(0)).ShowModal();
                                else MessageBox.Show("Please select ONE user", MessageBoxType.Error);
                                UserList.DataStore = this.GetAllUsersGrid();
                            }){ Text = "Test Password" },
                            new Button((e, a) => {new NewUserForm(null).ShowModal(); UserList.DataStore = this.GetAllUsersGrid(); }){ Text = "Deactivate User", BackgroundColor = Color.FromArgb(0xff, 0xaa, 0xaa, 0xff) }
                        },
                       Orientation = Orientation.Horizontal,
                       Spacing = 4,
                       Padding = 10,
                       BackgroundColor = Color.FromArgb(200,200,200,255),

                    },
                    null,
                    UserList

                }

            };
            KeyUp += ((e, a) => MessageBox.Show(a.Key.ToString()));

            
        }
        public List<GridItem> GetAllUsersGrid()
        {
            var list = new List<GridItem>();
            using (var ctx = new NewinvContext())
            {
                var users = ctx.Credentials.OrderBy(e => e.Userid).ToList();

                foreach (var item in users)
                {
                    var GR = new GridItem(item.Userid, item.Username, item.Modified.ToString(), item.CreatedTime.ToString());

                    list.Add(GR);
                }
            }
            return list;
        }
    }
}
