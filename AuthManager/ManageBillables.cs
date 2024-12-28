using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;
using RV.InvNew.Common;

namespace AuthManager
{
    internal class ManageBillables : Eto.Forms.Dialog
    {
        public ManageBillables(long UserId)
        {
            Title = "Manage Permissions";
            MinimumSize = new Eto.Drawing.Size(200, 200);
            var TableCheckBoxes = new TableLayout()
            {
                Padding = 10,
                Spacing = new Eto.Drawing.Size(10, 10),
            };
            List<PermissionsListCategoriesName> ExistingList;
            Button CalculateButton = new Button() { Text = "Calculate" };
            using (var ctx = new NewinvContext())
            {
                ExistingList = ctx.PermissionsListCategoriesNames.ToList();
            }
            List<CheckBox> CheckBoxes = new();
            for (ushort i = 0; i < 63; i++)
            {
                CheckBoxes.Add(
                    new CheckBox
                    {
                        Text = ExistingList
                            .FirstOrDefault(
                                e => e.Category == (long)1 << i,
                                new PermissionsListCategoriesName
                                {
                                    Category = 1 << i,
                                    CategoryName = "Unspecified",
                                }
                            )
                            .CategoryName,
                    }
                );
            }
            List<CheckBox[]> ChunkedCB = CheckBoxes.Chunk(8).ToList();

            foreach (var chunk in ChunkedCB)
            {
                var row = new TableRow();
                foreach (var checkbox in chunk)
                {
                    row.Cells.Add(checkbox);
                }
                TableCheckBoxes.Rows.Add(row);
            }
            CalculateButton.Click += (e, a) =>
            {
                var CBA = CheckBoxes.ToArray();
                string bitsAsString = "";
                long bitsAsLong = 0;
                for (ushort i = 0; i < CBA.Length; i++)
                {
                    bitsAsString += (CBA[i].Checked ?? false) ? "1" : "0";
                    if ((CBA[i].Checked ?? false) == true)
                    {
                        bitsAsLong |= (long)1 << i;
                    }
                }
                MessageBox.Show(
                    $"Reverse bits (bitmask in reverse): {bitsAsString}{Environment.NewLine}{bitsAsLong}",
                    "Information re: Bits Set",
                    MessageBoxType.Information
                );
            };
            Content = new StackLayout(TableCheckBoxes, CalculateButton) { };
        }
    }
}
