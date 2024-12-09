using Eto.Drawing;
using Eto.Forms;
using System;
using System.Configuration;
using System.Linq;
using Tomlyn;
using System.Collections.Generic;
using RV.InvNew.Common;
using Microsoft.EntityFrameworkCore;

public static partial class ColorRandomizer
{
    static Random x2 = new Random();
    public static Eto.Drawing.Color bg()
    {
        return Color.FromArgb(205 + Math.Abs(x2.Next() % 50), 205 + Math.Abs(x2.Next() % 50), 205 + Math.Abs(x2.Next() % 50), 255);
    }
    public static Eto.Drawing.Color fg()
    {
        return Color.FromArgb(50 + Math.Abs(x2.Next() % 50), 50 + Math.Abs(x2.Next() % 50), 50 + Math.Abs(x2.Next() % 50), 255);
    }
}

namespace RV.InvNew.AuthManager
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            Eto.Style.Add<TextBoxCell>("AlternatingColumn",tbc =>
                { }//tbc.BackgroundColor = Color.FromArgb(240, 240, 255, 255); }
            );
            var UserList = new GridView();
            UserList.Enabled = true;
            UserList.Size = new Size(-1, 300);
            GridColumn A;
            Random x2 = new Random();
            UserList.CellFormatting += (e, a) => { 
                a.BackgroundColor = Color.FromArgb(205 + Math.Abs(x2.Next()%50), 205 + Math.Abs(x2.Next()%50), 205 + Math.Abs(x2.Next()%50), 255);
                a.ForegroundColor = Color.FromArgb(50 + Math.Abs(x2.Next() % 50), 50 + Math.Abs(x2.Next() % 50), 50 + Math.Abs(x2.Next() % 50), 255);
                if (UserList.Columns.IndexOf(a.Column) == 3 || UserList.Columns.IndexOf(a.Column) == 2)
                {
                    a.Font = Fonts.Monospace(10, FontStyle.None, FontDecoration.None);
                    
                }
            };
            
            UserList.Columns.Add( A = new GridColumn() { HeaderText = "UserID", DataCell = new TextBoxCell(0) { TextAlignment = TextAlignment.Right },   });
            ((Control)UserList).BackgroundColor = Color.FromArgb(255, 255, 0, 255);
            UserList.Columns.Add(new GridColumn { HeaderText = "Username", DataCell = new TextBoxCell(1)  });
            UserList.Columns.Add(new GridColumn { HeaderText = "Modified", DataCell = new TextBoxCell(2) {TextAlignment=TextAlignment.Right} });
            UserList.Columns.Add(new GridColumn { HeaderText = "Created", DataCell = new TextBoxCell(3) { TextAlignment = TextAlignment.Right } });
            UserList.Columns.Add(new GridColumn { HeaderText = "Active", DataCell = new CheckBoxCell(4) });
            UserList.Columns.Add(new GridColumn { HeaderText = "Active tokens", DataCell = new TextBoxCell(5) { TextAlignment = TextAlignment.Right } });
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
                

            
            Title = "User Manager";
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
                            new Button((e, a) => {new NewUserForm(null).ShowModal(); UserList.DataStore = this.GetAllUsersGrid(); }){ Text = "New User", BackgroundColor = Color.FromArgb(0xaa, 0xff, 0xaa, 0xff)  },
                            new Button((e, a) => {
                                var Selected = (GridItem)UserList.SelectedItem;
                                if (Selected!= null) new TestPasswordDialog((long)Selected.GetValue(0)).ShowModal();
                                else MessageBox.Show("Please select ONE user", MessageBoxType.Error);
                                UserList.DataStore = this.GetAllUsersGrid();
                            }){ Text = "Test Password" },
                            new Button((e, a) => {
                                var Selected = (GridItem)UserList.SelectedItem;
                                if (Selected!= null) using (var ctx = new NewinvContext()){
                                        ctx.Credentials.Where(e => e.Userid==(long)Selected.GetValue(0)).First().Active=false;
                                        ctx.SaveChanges();
                                }
                                else MessageBox.Show("Please select ONE user", MessageBoxType.Error);
                                UserList.DataStore = this.GetAllUsersGrid(); 
                            }){ Text = "Deactivate User", BackgroundColor = Color.FromArgb(0xff, 0xaa, 0xaa, 0xff) },
                            new Button((e, a) => {
                                var Selected = (GridItem)UserList.SelectedItem;
                                if (Selected!= null) using (var ctx = new NewinvContext()){
                                        ctx.Tokens.Where(e => e.Userid==(long)Selected.GetValue(0)).ExecuteUpdate((s) => s.SetProperty(e=>e.Active, e => false));
                                        ctx.SaveChanges();
                                }
                                else MessageBox.Show("Please select ONE user", MessageBoxType.Error);
                                UserList.DataStore = this.GetAllUsersGrid();
                            }){ Text = "Deactivate all tokens", BackgroundColor = Color.FromArgb(0xff, 0x66, 0xaa, 0xff) },
                            new Button((e, a) => {
                                var Selected = (GridItem)UserList.SelectedItem;
                                if (Selected!= null) using (var ctx = new NewinvContext()){
                                        ctx.Credentials.Where(e => e.Userid==(long)Selected.GetValue(0)).First().Active=true;
                                        ctx.SaveChanges();
                                }
                                else MessageBox.Show("Please select ONE user", MessageBoxType.Error);
                                UserList.DataStore = this.GetAllUsersGrid();
                            }){ Text = "Activate User", BackgroundColor = Color.FromArgb(0xff, 0xff, 0xaa, 0xff) },
                            new Button((e, a) => {
                                var Selected = (GridItem)UserList.SelectedItem;
                                if (Selected!= null) new ManagePermissions((long)Selected.GetValue(0)).ShowModal();
                                else MessageBox.Show("Please select ONE user", MessageBoxType.Error);
                                UserList.DataStore = this.GetAllUsersGrid();
                            }){ Text = "Edit/Set Permissions", BackgroundColor = Color.FromArgb(0xff, 0xff, 0xaa, 0xff)  },
                        },
                       Orientation = Orientation.Horizontal,
                       Spacing = 4,
                       Padding = new Padding(0, 5),
                       BackgroundColor = Color.FromArgb(220,220,220,255),

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
                    var GR = new GridItem(item.Userid, item.Username, item.Modified.ToString("s"), item.CreatedTime.ToString("s"), item.Active, ctx.Tokens.Count(e => e.Userid == item.Userid && e.Active == true));

                    list.Add(GR);
                }
            }
            return list;
        }
    }
}
