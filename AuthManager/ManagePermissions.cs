using System;
using Eto.Forms;
using Eto.Drawing;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RV.InvNew.Common;

namespace RV.InvNew.AuthManager
{
	public partial class ManagePermissions : Dialog
	{
		public ManagePermissions(long UserID, bool Elevated = false)
		{
			Title = "Manage Permissions";
			MinimumSize = new Eto.Drawing.Size(200, 200);
			var TableCheckBoxes = new TableLayout() { Padding = 10, Spacing = new Eto.Drawing.Size(10, 10) };
			List<PermissionsList[]> chunked;
			using (var ctx = new NewinvContext()) {
				chunked = ctx.PermissionsLists.ToList().Chunk(6).ToList();
			}
			var ChunkedCheckBoxes = chunked;
			string CurrentPerms = "";
            using (var ctx = new NewinvContext())
            {
                var currentList = ctx.UserAuthorizations.Where((e) => e.Userid == UserID);
                if (currentList.Count() != 0)
                {
					CurrentPerms = Elevated ? currentList.First().UserDefaultCap: currentList.First().UserCap;
                }
            }
			Dictionary<string, bool> CurrentPermsLookup = new();
			foreach (string Perm in CurrentPerms.Split(',')) {
				CurrentPermsLookup.Add(Perm, true);
			}
			var CurrentPermsLookupRO = (IReadOnlyDictionary<string, bool>)CurrentPermsLookup;
            foreach (var chunk in chunked) {
				int w = 0;
				var row = new TableRow() { };
				foreach(var CheckBoxLabel in chunk)
				{
					w++;
					row.Cells.Add(new CheckBox() { Text = CheckBoxLabel.Permission, BackgroundColor = ColorRandomizer.bg(), TextColor = ColorRandomizer.fg(), Checked = CurrentPermsLookupRO.GetValueOrDefault(CheckBoxLabel.Permission, false) }); 
				}
				TableCheckBoxes.Rows.Add(row);
			}
            
            var SaveButton = new Button((e, a) =>
			{
				var rows = TableCheckBoxes.Rows;
				List<string> CurrentPermissions = new List<string>();
				foreach (var row in rows)
				{
					foreach (var cell in row.Cells)
					{
						var cellCheckBox = (CheckBox)cell.Control;
						if (cellCheckBox != null && cellCheckBox.Checked == true) CurrentPermissions.Add(cellCheckBox.Text);
					}
				}
				string PermCS = String.Join(",", CurrentPermissions);
				MessageBox.Show(PermCS, "Selected Permissions", MessageBoxType.Information);
				using (var ctx = new NewinvContext()) {
					var currentList = ctx.UserAuthorizations.Where((e) => e.Userid == UserID);
					if (currentList.Count() != 0)
					{
						if (Elevated)
						{
							currentList.First().UserCap = PermCS;
						}
						else
						{
                            currentList.First().UserDefaultCap = PermCS;
                        }
							ctx.SaveChanges();
					}
					else
					{
						ctx.UserAuthorizations.Add(new UserAuthorization { Userid = UserID, UserCap = PermCS, UserDefaultCap = PermCS });
						ctx.SaveChanges();
					}
				}
			})
			{ Text = "Save"};
			var CancelButton = new Button((e, a) => { this.Close(); }) { Text = "Cancel" };

			Content = new StackLayout
			{
				Padding = 10,
				Items =
				{
					"Hello World!",
					TableCheckBoxes,
					new StackLayout(SaveButton, CancelButton){Orientation = Orientation.Horizontal, Spacing = 10}
					// add more controls here
				}
			};

		}
	}
}
