using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RV.InvNew.Common;

namespace RV.InvNew.AuthManager
{
    internal class TestPasswordDialog : Dialog
    {
        public TestPasswordDialog(long UserID)
        {
            Padding = 20;
            PasswordBox Password = new PasswordBox();
            Button Test = new Button((e, a) =>
            {
                using (var ctx = new NewinvContext())
                {
                    Credential target = ctx.Credentials.Single(x => x.Userid == UserID);
                    string hashed = Utils.DoPBKDF2(Password.Text);
                    if (target.PasswordPbkdf2 == hashed)
                    {
                        MessageBox.Show("Password matched!", MessageBoxType.Information);
                    }
                    else
                    {
                        MessageBox.Show("Password did not match", MessageBoxType.Error);
                    }
                };
            })
            { Text = "Test" };

            Button Cancel = new Button((e, a) => this.Close()) { Text = "Cancel" };
            Content = new TableLayout([
                null,
                new TableRow([null, new Label() { Text = "Password: "}, Password, null]),
                new TableRow([null, Test, Cancel, null]),
                null
            ])
            { Spacing = new Eto.Drawing.Size(20, 20) };
        }
    }
}
