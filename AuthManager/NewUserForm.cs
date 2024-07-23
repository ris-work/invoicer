using System;
using System.Net.Mime;
using System.Windows.Documents;
using Eto.Forms;

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
			var Save = new Button((e, a) => { }) { Text = "Save" };
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