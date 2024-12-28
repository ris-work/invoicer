using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Documents;
using Eto.Forms;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.Mapping;
using NpgsqlTypes;
using RV.InvNew.Common;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace RV.InvNew.AuthManager
{
    public class NewUserForm : Dialog
    {
        public NewUserForm(long? UserID)
        {
            Title = "Hello";
            Size = new Eto.Drawing.Size(-1, -1);
            ;
            using (var ctx = new NewinvContext())
            {
                Credential ToUpdate;
                if (UserID != null)
                {
                    ToUpdate = ctx.Credentials.Where(c => c.Userid == UserID).First();
                }
                else
                {
                    ToUpdate = null;
                }
                string ExistingUsername = "";
                if (UserID != null)
                {
                    ExistingUsername = ToUpdate.Username;
                }

                var LabelUsername = new Label() { Text = "Username" };
                var LabelPassword = new Label() { Text = "Password" };
                var LabelPasswordConfirm = new Label() { Text = "Password (Confirm)" };
                var TextUsername = new TextBox()
                {
                    PlaceholderText = "Username",
                    Text = ExistingUsername,
                };
                var TextPassword = new PasswordBox() { };
                var TextPasswordConfirm = new PasswordBox() { };
                var Save = new Button(
                    (e, a) =>
                    {
                        if (TextPassword.Text != TextPasswordConfirm.Text)
                        {
                            MessageBox.Show(
                                "Password confirmation does not match with the entered password!",
                                MessageBoxType.Error
                            );
                            return;
                        }

                        // derive a 256-bit subkey (use HMACSHA256 with 100,000 iterations)
                        string hashed = Utils.DoPBKDF2(TextPassword.Text);

                        Console.WriteLine($"Hashed: {hashed}");

                        var now = DateTime.UtcNow;
                        using (var ctx = new NewinvContext())
                        {
                            if (UserID == null)
                            {
                                ctx.Credentials.Add(
                                    new Credential
                                    {
                                        Username = TextUsername.Text,
                                        PasswordPbkdf2 = hashed,
                                        Modified = now,
                                        CreatedTime = now,
                                        ValidUntil = now,
                                    }
                                );
                            }
                            else
                            {
                                ToUpdate.Username = TextUsername.Text;
                                if (TextPassword.Text != "")
                                {
                                    ToUpdate.PasswordPbkdf2 = hashed;
                                }
                                ToUpdate.Modified = now;
                                ToUpdate.ValidUntil = now;
                                ctx.Attach(ToUpdate);
                                ctx.Update(ToUpdate);
                            }

                            ctx.SaveChanges();
                            this.Close();
                        }
                    }
                )
                {
                    Text = "Save",
                };
                var Cancel = new Button(
                    (e, a) =>
                    {
                        this.Close();
                    }
                )
                {
                    Text = "Cancel",
                };

                Content = new TableLayout(
                    [
                        null,
                        new Eto.Forms.TableRow([null, LabelUsername, TextUsername, null]),
                        new Eto.Forms.TableRow([null, LabelPassword, TextPassword, null]),
                        new Eto.Forms.TableRow(
                            [null, LabelPasswordConfirm, TextPasswordConfirm, null]
                        ),
                        new Eto.Forms.TableRow([null, Save, Cancel, null]),
                        null,
                    ]
                )
                {
                    Spacing = new Eto.Drawing.Size(10, 10),
                };
                Topmost = true;
            }
        }
    }
}
