using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommonUi;
using Eto.Drawing;
using Eto.Forms;
using RV.InvNew.Common;

namespace EtoFE
{
    public class JournalEntriesPanel : Eto.Forms.Panel
    {
        TextBox txtRefNo;
        NumericUpDown numAmount;
        ComboBox ddlDebitType;
        ComboBox ddlDebitNo;
        ComboBox ddlCreditType;
        ComboBox ddlCreditNo;
        TextArea txtDescription;
        DateTimePicker dtpEntered;
        TextBox txtRef;

        Button btnSave;
        Button btnLoad;
        Button btnUpdate;
        Button btnDelete;
        Button btnClear;

        List<Control> focusList;
        List<Func<(bool Valid, string Error)>> rules;

        public JournalEntriesPanel()
        {
            var LocalColor = ColorSettings.GetPanelSettings(
                "Editor",
                (IReadOnlyDictionary<string, object>)Program.ConfigDict
            );
            LocalColor = ColorSettings.RotateAllToPanelSettings(0);
            BackgroundColor = LocalColor?.BackgroundColor ?? ColorSettings.BackgroundColor;
            TextBox txtRefNo;
            NumericUpDown numAmount;
            ComboBox ddlDebitType;
            ComboBox ddlDebitNo;
            ComboBox ddlCreditType;
            ComboBox ddlCreditNo;
            TextArea txtDescription;
            DateTimePicker dtpEntered;
            TextBox txtRef;

            Button btnSave;
            Button btnLoad;
            Button btnUpdate;
            Button btnDelete;
            Button btnClear;

            List<Control> focusList;
            List<Func<(bool Valid, string Error)>> rules;

            //Title = "Journal Entry";
            ClientSize = new Size(600, 400);

            // instantiate controls
            txtRefNo = new TextBox();
            numAmount = new NumericUpDown { MinValue = 0, DecimalPlaces = 2 };
            ddlDebitType = new ComboBox { DataStore = LoadAccountTypes() };
            ddlDebitNo = new ComboBox();
            ddlCreditType = new ComboBox { DataStore = LoadAccountTypes() };
            ddlCreditNo = new ComboBox();
            txtDescription = new TextArea { Height = 60 };
            dtpEntered = new DateTimePicker { Mode = DateTimePickerMode.DateTime };
            txtRef = new TextBox();

            btnSave = new Button { Text = "Save" };
            btnLoad = new Button { Text = "Load" };
            btnUpdate = new Button { Text = "Update" };
            btnDelete = new Button { Text = "Delete" };
            btnClear = new Button { Text = "Clear" };

            // layout
            Content = new TableLayout
            {
                Padding = 10,
                Spacing = Size.Empty,
                Rows =
                {
                    new TableRow("Ref No:", txtRefNo),
                    new TableRow("Amount:", numAmount),
                    new TableRow("Debit Type:", ddlDebitType, "Debit No:", ddlDebitNo),
                    new TableRow("Credit Type:", ddlCreditType, "Credit No:", ddlCreditNo),
                    new TableRow("Description:", new TableCell(txtDescription, true)),
                    new TableRow("Time Entered:", dtpEntered),
                    new TableRow("Reference:", txtRef),
                    null,
                    new TableRow(
                        null,
                        new StackLayout
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 5,
                            Items = { btnSave, btnLoad, btnUpdate, btnDelete, btnClear },
                        }
                    ),
                },
            };

            // focus list & Enter→Next
            focusList = new List<Control>
            {
                txtRefNo,
                numAmount,
                ddlDebitType,
                ddlDebitNo,
                ddlCreditType,
                ddlCreditNo,
                txtDescription,
                dtpEntered,
                txtRef,
            };
            foreach (var c in focusList)
            {
                ApplyStyle(c);
                c.KeyDown += GoToNext;
            }

            // dropdown linkage
            ddlDebitType.SelectedValueChanged += (s, e) =>
                ddlDebitNo.DataStore = LoadAccountNumbers((int)ddlDebitType.SelectedValue);
            ddlCreditType.SelectedValueChanged += (s, e) =>
                ddlCreditNo.DataStore = LoadAccountNumbers((int)ddlCreditType.SelectedValue);

            // validations
            rules = new List<Func<(bool Valid, string Error)>>
            {
                () =>
                    !string.IsNullOrWhiteSpace(txtRefNo.Text)
                        ? (true, null)
                        : (false, "Reference Number is required."),
                () => numAmount.Value > 0 ? (true, null) : (false, "Amount must be > 0."),
                () =>
                    ddlDebitNo.SelectedValue != null
                        ? (true, null)
                        : (false, "Select a Debit Account."),
                () =>
                    ddlCreditNo.SelectedValue != null
                        ? (true, null)
                        : (false, "Select a Credit Account."),
            };

            // button hooks
            btnSave.Click += (s, e) => OnSave();
            btnLoad.Click += (s, e) => OnLoad();
            btnUpdate.Click += (s, e) => UpdateEntry();
            btnDelete.Click += (s, e) => DeleteEntry();
            btnClear.Click += (s, e) => ClearForm();
        }

        void OnSave()
        {
            var (ok, err) = ValidateAll();
            if (!ok)
            {
                MessageBox.Show(err, MessageBoxButtons.OK, MessageBoxType.Error);
                return;
            }

            var entry = new JournalEntryDto
            {
                RefNo = txtRefNo.Text,
                Amount = (decimal)numAmount.Value,
                DebitType = Convert.ToInt32(ddlDebitType.SelectedValue),
                DebitNo = Convert.ToInt64(ddlDebitNo.SelectedValue),
                CreditType = Convert.ToInt32(ddlCreditType.SelectedValue),
                CreditNo = Convert.ToInt64(ddlCreditNo.SelectedValue),
                Description = txtDescription.Text,
                TimeAsEntered = dtpEntered.Value ?? DateTime.UtcNow,
                Ref = txtRef.Text,
            };

            string json = JsonSerializer.Serialize(
                entry,
                new JsonSerializerOptions { WriteIndented = true }
            );
            MessageBox.Show(json, MessageBoxButtons.OK);

            // you could rehydrate to verify
            var clone = JsonSerializer.Deserialize<JournalEntryDto>(json);
            SaveEntry(clone);
        }

        void OnLoad()
        {
            // stub: fixed JSON for now
            string json = """
                {
                  "RefNo":"LOADED123",
                  "Amount":456.78,
                  "DebitType":1,
                  "DebitNo":1001,
                  "CreditType":2,
                  "CreditNo":2002,
                  "Description":"Test Entry",
                  "TimeAsEntered":"2025-01-01T12:00:00Z",
                  "Ref":"XREF"
                }
                """;

            JournalEntryDto entry = JsonSerializer.Deserialize<JournalEntryDto>(json);

            txtRefNo.Text = entry.RefNo;
            numAmount.Value = (double)entry.Amount;
            ddlDebitType.SelectedValue = entry.DebitType;
            ddlDebitNo.DataStore = LoadAccountNumbers(entry.DebitType);
            ddlDebitNo.SelectedValue = entry.DebitNo;
            ddlCreditType.SelectedValue = entry.CreditType;
            ddlCreditNo.DataStore = LoadAccountNumbers(entry.CreditType);
            ddlCreditNo.SelectedValue = entry.CreditNo;
            txtDescription.Text = entry.Description;
            dtpEntered.Value = entry.TimeAsEntered;
            txtRef.Text = entry.Ref;

            MessageBox.Show($"Loaded entry '{entry.RefNo}'", MessageBoxButtons.OK);
        }

        (bool Valid, string Error) ValidateAll()
        {
            for (int i = 0; i < rules.Count; i++)
            {
                var (v, msg) = rules[i]();
                if (!v)
                    return (false, $"Rule {i + 1}: {msg}");
            }
            return (true, null);
        }

        void GoToNext(object sender, KeyEventArgs e)
        {
            if (e.Key == Keys.Enter)
            {
                int idx = focusList.IndexOf((Control)sender);
                if (idx >= 0 && idx < focusList.Count - 1)
                {
                    var next = focusList[idx + 1];
                    if (next.Enabled)
                        next.Focus();
                    else
                        GoToNext(next, e);
                }
                else
                    btnSave.Focus();
                e.Handled = true;
            }
        }

        static void ApplyStyle(Control c)
        {
            c.BackgroundColor = Colors.White;
            c.Width = Math.Max(c.Width, 120);
            c.Height = Math.Max(c.Height, 24);
            switch (c)
            {
                case TextBox tb:
                    tb.PlaceholderText = "...";
                    break;
                case NumericUpDown nu:
                    nu.DecimalPlaces = 2;
                    break;
                //case ComboBox cb: cb.DropDownStyle = DropDownStyle.DropDownList; break;
            }
        }

        // stubs
        void SaveEntry(
            JournalEntryDto e
        ) { /* persist */
        }

        void UpdateEntry() { /* ... */
        }

        void DeleteEntry() { /* ... */
        }

        void ClearForm()
        {
            txtRefNo.Text = "";
            numAmount.Value = 0;
            ddlDebitType.SelectedValue = null;
            ddlDebitNo.DataStore = null;
            ddlCreditType.SelectedValue = null;
            ddlCreditNo.DataStore = null;
            txtDescription.Text = "";
            dtpEntered.Value = DateTime.Now;
            txtRef.Text = "";
            txtRefNo.Focus();
        }

        IEnumerable<object> LoadAccountTypes()
        {
            yield return 1;
            yield return 2;
        }

        IEnumerable<object> LoadAccountNumbers(int t)
        {
            yield return 1001;
            yield return 2002;
        }
    }
}
