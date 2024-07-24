using System;
using System.Net.Mime;
using System.Windows.Documents;
using Eto.Forms;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Text;
using NpgsqlTypes;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.Mapping;


namespace AuthManager
{

	public class NewUserForm: Dialog
	{
		public NewUserForm()
		{
			Title = "Hello";
			Size = new Eto.Drawing.Size(-1, -1); ;
			
			var LabelUsername = new Label() { Text = "Username" };
			var LabelPassword = new Label() { Text = "Password" };
            var TextUsername = new TextBox() { PlaceholderText = "Username" };
            var TextPassword = new PasswordBox() {  };
			var Save = new Button((e, a) => {
				byte[] salt = Encoding.UTF8.GetBytes(((String)Config.model["Salt"])); // divide by 8 to convert bits to bytes
                Console.WriteLine($"Salt: {Convert.ToBase64String(salt)}");

                // derive a 256-bit subkey (use HMACSHA256 with 100,000 iterations)
                string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                    password: TextPassword.Text,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 100000,
                    numBytesRequested: 256 / 8));

                Console.WriteLine($"Hashed: {hashed}");
                using (var ctx = new NewinvContext())
				{
					var now = DateTime.UtcNow;
					ctx.Credentials.Add(new Credential { Username = TextUsername.Text, PasswordPbkdf2 = hashed, Modified = now, CreatedTime = now, ValidUntil = now});
					ctx.SaveChanges();
				}
			
			}) { Text = "Save" };
			var Cancel = new Button((e, a) => { this.Close(); }) { Text = "Cancel" };
            

			Content =
				new TableLayout([
						null,
						new Eto.Forms.TableRow(

							[null, LabelUsername, TextUsername, null]
						),
						new Eto.Forms.TableRow(

							[null, LabelPassword, TextPassword, null]
						),
						new Eto.Forms.TableRow(

							[null, Save, Cancel, null]
						),
						null

				])
				{ Spacing = new Eto.Drawing.Size(10,10) };
			Topmost = true;
		}
	}
}