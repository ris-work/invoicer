using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Eto.Forms;
using Eto.Drawing;
using RV.InvNew.Common;
using CommonUi;

namespace RV.InvNew.UI
{
    public class ScheduledPaymentPanel : Panel
    {
        // controls
        readonly TextBox txtId;
        readonly Button btnIdSearch;
        readonly Label lblIdHuman;

        readonly TextBox txtCompanyId;
        readonly Button btnCompanySearch;
        readonly Label lblCompanyHuman;

        readonly TextBox txtVendorId;
        readonly Button btnVendorSearch;
        readonly Label lblVendorHuman;

        readonly TextBox txtInvoiceId;
        readonly Button btnInvoiceSearch;
        readonly Label lblInvoiceHuman;

        readonly TextBox txtBatchId;
        readonly Button btnBatchSearch;
        readonly Label lblBatchHuman;

        readonly TextBox txtPaymentReference;
        readonly TextBox txtDescription;
        readonly TextBox txtCurrency;
        readonly TextBox txtAmount;
        readonly TextBox txtExchangeRate;

        readonly TextBox txtBeneficiaryName;
        readonly TextBox txtBeneficiaryBankName;
        readonly TextBox txtBeneficiaryBranch;
        readonly TextBox txtBeneficiaryAccountNo;
        readonly TextBox txtBeneficiaryRoutingNo;

        readonly TextBox txtPaymentMethod;
        readonly TextBox txtFrequency;
        readonly TextBox txtIntervalValue;

        readonly DateTimePicker dtpNextRunDate;
        readonly DateTimePicker dtpLastRunDate;
        readonly DateTimePicker dtpReconciliationDate;

        readonly CheckBox chkIsPending;
        readonly CheckBox chkIsProcessing;
        readonly CheckBox chkIsCompleted;
        readonly CheckBox chkIsFailed;
        readonly CheckBox chkIsCancelled;

        readonly TextBox txtExternalPaymentId;
        readonly TextBox txtFeeAmount;
        readonly TextBox txtNetAmount;
        readonly CheckBox chkIsReconciled;
        readonly CheckBox chkIsExcluded;
        readonly CheckBox chkIsAutomaticClear;

        readonly Button btnNew;
        readonly Button btnLoad;
        readonly Button btnEdit;
        readonly Button btnSave;
        readonly Button btnCancel;
        readonly Button btnReset;

        ScheduledPayment originalDto;
        bool isNew;
        Control lastFocused;

        // public ctors
        public ScheduledPaymentPanel() : this(2) { }
        public ScheduledPaymentPanel(long id) : this(2) { LoadData(id); }

        // core ctor
        ScheduledPaymentPanel(int columns)
        {
            var lw = ColorSettings.InnerLabelWidth ?? 120;
            var lh = ColorSettings.InnerLabelHeight ?? 25;
            var cw = ColorSettings.InnerControlWidth ?? 200;
            var ch = ColorSettings.InnerControlHeight ?? 25;

            // identity lookups
            txtId = new TextBox { Width = cw, Height = ch };
            btnIdSearch = new Button { Text = "...", Height = ch };
            lblIdHuman = new Label { Width = cw, Height = ch };

            txtCompanyId = new TextBox { Width = cw, Height = ch };
            btnCompanySearch = new Button { Text = "...", Height = ch };
            lblCompanyHuman = new Label { Width = cw, Height = ch };

            txtVendorId = new TextBox { Width = cw, Height = ch };
            btnVendorSearch = new Button { Text = "...", Height = ch };
            lblVendorHuman = new Label { Width = cw, Height = ch };

            txtInvoiceId = new TextBox { Width = cw, Height = ch };
            btnInvoiceSearch = new Button { Text = "...", Height = ch };
            lblInvoiceHuman = new Label { Width = cw, Height = ch };

            txtBatchId = new TextBox { Width = cw, Height = ch };
            btnBatchSearch = new Button { Text = "...", Height = ch };
            lblBatchHuman = new Label { Width = cw, Height = ch };

            // scalar fields
            txtPaymentReference = new TextBox { Width = cw, Height = ch };
            txtDescription = new TextBox { Width = cw, Height = ch };
            txtCurrency = new TextBox { Width = cw, Height = ch };
            txtAmount = new TextBox { Width = cw, Height = ch };
            txtExchangeRate = new TextBox { Width = cw, Height = ch };

            txtBeneficiaryName = new TextBox { Width = cw, Height = ch };
            txtBeneficiaryBankName = new TextBox { Width = cw, Height = ch };
            txtBeneficiaryBranch = new TextBox { Width = cw, Height = ch };
            txtBeneficiaryAccountNo = new TextBox { Width = cw, Height = ch };
            txtBeneficiaryRoutingNo = new TextBox { Width = cw, Height = ch };

            txtPaymentMethod = new TextBox { Width = cw, Height = ch };
            txtFrequency = new TextBox { Width = cw, Height = ch };
            txtIntervalValue = new TextBox { Width = cw, Height = ch };

            // date pickers
            dtpNextRunDate = new DateTimePicker { Mode = DateTimePickerMode.Date, Value = DateTime.Now };
            dtpLastRunDate = new DateTimePicker { Mode = DateTimePickerMode.Date, Value = DateTime.Now };
            dtpReconciliationDate = new DateTimePicker { Mode = DateTimePickerMode.Date, Value = DateTime.Now };

            // status checkboxes
            chkIsPending = new CheckBox { Text = TranslationHelper.Translate("IsPending") };
            chkIsProcessing = new CheckBox { Text = TranslationHelper.Translate("IsProcessing") };
            chkIsCompleted = new CheckBox { Text = TranslationHelper.Translate("IsCompleted") };
            chkIsFailed = new CheckBox { Text = TranslationHelper.Translate("IsFailed") };
            chkIsCancelled = new CheckBox { Text = TranslationHelper.Translate("IsCancelled") };

            // reconciliation fields
            txtExternalPaymentId = new TextBox { Width = cw, Height = ch };
            txtFeeAmount = new TextBox { Width = cw, Height = ch };
            txtNetAmount = new TextBox { Width = cw, Height = ch };
            chkIsReconciled = new CheckBox { Text = TranslationHelper.Translate("IsReconciled") };
            chkIsExcluded = new CheckBox { Text = TranslationHelper.Translate("IsExcluded") };
            chkIsAutomaticClear = new CheckBox { Text = TranslationHelper.Translate("IsAutomaticClear") };

            // action buttons
            btnNew = new Button { Text = TranslationHelper.Translate("New") };
            btnLoad = new Button { Text = TranslationHelper.Translate("Load") };
            btnEdit = new Button { Text = TranslationHelper.Translate("Edit") };
            btnSave = new Button { Text = TranslationHelper.Translate("Save") };
            btnCancel = new Button { Text = TranslationHelper.Translate("Cancel") };
            btnReset = new Button { Text = TranslationHelper.Translate("Reset") };

            // wire lookups
            btnIdSearch.Click += (_, __) => DoLookup(txtId, lblIdHuman, LookupHumanFriendlyScheduledPaymentId);
            btnCompanySearch.Click += (_, __) => DoLookup(txtCompanyId, lblCompanyHuman, LookupHumanFriendlyCompanyId);
            btnVendorSearch.Click += (_, __) => DoLookup(txtVendorId, lblVendorHuman, LookupHumanFriendlyVendorId);
            btnInvoiceSearch.Click += (_, __) => DoLookup(txtInvoiceId, lblInvoiceHuman, LookupHumanFriendlyInvoiceId);
            btnBatchSearch.Click += (_, __) => DoLookup(txtBatchId, lblBatchHuman, LookupHumanFriendlyBatchId);

            // mutually exclusive statuses
            var statuses = new[] { chkIsPending, chkIsProcessing, chkIsCompleted, chkIsFailed, chkIsCancelled };
            statuses.ToList().ForEach(cb =>
                cb.CheckedChanged += (s, e) =>
                {
                    if (cb.Checked == true)
                        statuses.Where(x => x != cb).ToList().ForEach(x => x.Checked = false);
                });

            // wire actions
            btnNew.Click += (_, __) => NewRecord();
            btnLoad.Click += (_, __) => LoadDialog();
            btnEdit.Click += (_, __) => Edit();
            btnSave.Click += (_, __) => Save();
            btnCancel.Click += (_, __) => Cancel();
            btnReset.Click += (_, __) => ResetForm();

            // track focus
            var focusable = new Control[]
            {
                txtId, btnIdSearch, txtCompanyId, btnCompanySearch,
                txtVendorId, btnVendorSearch, txtInvoiceId, btnInvoiceSearch,
                txtBatchId, btnBatchSearch,
                txtPaymentReference, txtDescription, txtCurrency, txtAmount, txtExchangeRate,
                txtBeneficiaryName, txtBeneficiaryBankName, txtBeneficiaryBranch,
                txtBeneficiaryAccountNo, txtBeneficiaryRoutingNo,
                txtPaymentMethod, txtFrequency, txtIntervalValue,
                dtpNextRunDate, dtpLastRunDate, dtpReconciliationDate,
                chkIsPending, chkIsProcessing, chkIsCompleted, chkIsFailed, chkIsCancelled,
                txtExternalPaymentId, txtFeeAmount, txtNetAmount,
                chkIsReconciled, chkIsExcluded, chkIsAutomaticClear,
                btnNew, btnLoad, btnEdit, btnSave, btnCancel, btnReset
            };
            foreach (var c in focusable)
                c.GotFocus += (s, e) => lastFocused = (Control)s;

            KeyUp += OnKeyUp;
            Content = BuildLayout();
            NewRecord();
        }

        TableLayout BuildLayout()
        {
            var lw = ColorSettings.InnerLabelWidth ?? 120;
            var lh = ColorSettings.InnerLabelHeight ?? 25;

            TableRow na(string key, TextBox tb, Button btn, Label lbl) =>
                new TableRow(
                    new Label { Text = TranslationHelper.Translate(key), Width = lw, Height = lh, TextAlignment = TextAlignment.Right },
                    new StackLayout(
                        new StackLayoutItem(tb),
                        new StackLayoutItem(btn),
                        new StackLayoutItem(lbl)
                    )
                    {
                        Spacing = 2,
                        Orientation = Orientation.Horizontal
                    }
                );

            var rows = new List<TableRow>
            {
                na("Id", txtId, btnIdSearch, lblIdHuman),
                na("CompanyId", txtCompanyId, btnCompanySearch, lblCompanyHuman),
                na("VendorId", txtVendorId, btnVendorSearch, lblVendorHuman),
                na("InvoiceId", txtInvoiceId, btnInvoiceSearch, lblInvoiceHuman),
                na("BatchId", txtBatchId, btnBatchSearch, lblBatchHuman),

                new TableRow(new Label { Text = TranslationHelper.Translate("PaymentReference"), Width = lw, Height = lh, TextAlignment = TextAlignment.Right }, txtPaymentReference),
                new TableRow(new Label { Text = TranslationHelper.Translate("Description"),       Width = lw, Height = lh, TextAlignment = TextAlignment.Right }, txtDescription),
                new TableRow(new Label { Text = TranslationHelper.Translate("Currency"),          Width = lw, Height = lh, TextAlignment = TextAlignment.Right }, txtCurrency),
                new TableRow(new Label { Text = TranslationHelper.Translate("Amount"),            Width = lw, Height = lh, TextAlignment = TextAlignment.Right }, txtAmount),
                new TableRow(new Label { Text = TranslationHelper.Translate("ExchangeRate"),      Width = lw, Height = lh, TextAlignment = TextAlignment.Right }, txtExchangeRate),

                new TableRow(new Label { Text = TranslationHelper.Translate("BeneficiaryName"),      Width = lw, Height = lh, TextAlignment = TextAlignment.Right }, txtBeneficiaryName),
                new TableRow(new Label { Text = TranslationHelper.Translate("BeneficiaryBankName"),  Width = lw, Height = lh, TextAlignment = TextAlignment.Right }, txtBeneficiaryBankName),
                new TableRow(new Label { Text = TranslationHelper.Translate("BeneficiaryBranch"),    Width = lw, Height = lh, TextAlignment = TextAlignment.Right }, txtBeneficiaryBranch),
                new TableRow(new Label { Text = TranslationHelper.Translate("BeneficiaryAccountNo"), Width = lw, Height = lh, TextAlignment = TextAlignment.Right }, txtBeneficiaryAccountNo),
                new TableRow(new Label { Text = TranslationHelper.Translate("BeneficiaryRoutingNo"), Width = lw, Height = lh, TextAlignment = TextAlignment.Right }, txtBeneficiaryRoutingNo),

                new TableRow(new Label { Text = TranslationHelper.Translate("PaymentMethod"), Width = lw, Height = lh, TextAlignment = TextAlignment.Right }, txtPaymentMethod),
                new TableRow(new Label { Text = TranslationHelper.Translate("Frequency"),     Width = lw, Height = lh, TextAlignment = TextAlignment.Right }, txtFrequency),
                new TableRow(new Label { Text = TranslationHelper.Translate("IntervalValue"), Width = lw, Height = lh, TextAlignment = TextAlignment.Right }, txtIntervalValue),

                new TableRow(new Label { Text = TranslationHelper.Translate("NextRunDate"), Width = lw, Height = lh, TextAlignment = TextAlignment.Right }, dtpNextRunDate),
                new TableRow(new Label { Text = TranslationHelper.Translate("LastRunDate"), Width = lw, Height = lh, TextAlignment = TextAlignment.Right }, dtpLastRunDate),

                new TableRow(
                    new Label { Text = TranslationHelper.Translate("Status"), Width = lw, Height = lh, TextAlignment = TextAlignment.Right },
                    new StackLayout(
                        new StackLayoutItem(chkIsPending),
                        new StackLayoutItem(chkIsProcessing),
                        new StackLayoutItem(chkIsCompleted),
                        new StackLayoutItem(chkIsFailed),
                        new StackLayoutItem(chkIsCancelled)
                    )
                    {
                        Spacing     = 5,
                        Orientation = Orientation.Horizontal
                    }
                ),

                new TableRow(new Label { Text = TranslationHelper.Translate("ExternalPaymentId"), Width = lw, Height = lh, TextAlignment = TextAlignment.Right }, txtExternalPaymentId),
                new TableRow(new Label { Text = TranslationHelper.Translate("FeeAmount"),           Width = lw, Height = lh, TextAlignment = TextAlignment.Right }, txtFeeAmount),
                new TableRow(new Label { Text = TranslationHelper.Translate("NetAmount"),           Width = lw, Height = lh, TextAlignment = TextAlignment.Right }, txtNetAmount),
                new TableRow(new Label { Text = TranslationHelper.Translate("IsReconciled"),        Width = lw, Height = lh, TextAlignment = TextAlignment.Right }, chkIsReconciled),
                new TableRow(new Label { Text = TranslationHelper.Translate("IsExcluded"),          Width = lw, Height = lh, TextAlignment = TextAlignment.Right }, chkIsExcluded),
                new TableRow(new Label { Text = TranslationHelper.Translate("ReconciliationDate"),  Width = lw, Height = lh, TextAlignment = TextAlignment.Right }, dtpReconciliationDate),
                new TableRow(new Label { Text = TranslationHelper.Translate("IsAutomaticClear"),    Width = lw, Height = lh, TextAlignment = TextAlignment.Right }, chkIsAutomaticClear)
            };

            var layout = new TableLayout();
            foreach (var row in rows)
                layout.Rows.Add(row);

            // buttons row
            var btns = new StackLayout(
                new StackLayoutItem(btnNew),
                new StackLayoutItem(btnLoad),
                new StackLayoutItem(btnEdit),
                new StackLayoutItem(btnSave),
                new StackLayoutItem(btnCancel),
                new StackLayoutItem(btnReset)
            )
            {
                Spacing = 5,
                Orientation = Orientation.Horizontal,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            layout.Rows.Add(new TableRow(new Label(), btns));
            return layout;
        }

        void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (new[] { Keys.F1, Keys.F2, Keys.F3, Keys.F4 }.Contains(e.Key))
            {
                var map = new Dictionary<Control, Action>
                {
                    { txtId,        () => btnIdSearch.PerformClick() },
                    { txtCompanyId, () => btnCompanySearch.PerformClick() },
                    { txtVendorId,  () => btnVendorSearch.PerformClick() },
                    { txtInvoiceId, () => btnInvoiceSearch.PerformClick() },
                    { txtBatchId,   () => btnBatchSearch.PerformClick() }
                };
                if (lastFocused != null && map.TryGetValue(lastFocused, out var act))
                    act();
            }

            if (new[] { Keys.F9, Keys.F10, Keys.F11, Keys.F12 }.Contains(e.Key)) Save();
            if (new[] { Keys.F5, Keys.F6 }.Contains(e.Key)) Edit();
            if (new[] { Keys.F7, Keys.F8 }.Contains(e.Key)) ResetForm();
        }

        void DoLookup(TextBox tb, Label human, Func<long, string> lookup)
        {
            var items = new List<object>(); // TODO: load real items
            var sel = SearchPanelUtility.GenerateSearchDialog(items, this);
            if (sel?.Length > 0 && long.TryParse(sel[0], out var id))
            {
                tb.Text = id.ToString();
                human.Text = lookup(id);
            }
        }

        void NewRecord()
        {
            isNew = true;
            ToggleId(false);
            ClearFields();
            dtpNextRunDate.Value = DateTime.Now;
            Log("New record");
        }

        void LoadDialog()
        {
            var items = new List<ScheduledPayment>(); // TODO: load real list
            var sel = SearchPanelUtility.GenerateSearchDialog(items, this);
            if (sel?.Length > 0 && long.TryParse(sel[0], out var id))
                LoadData(id);
        }

        void LoadData(long id)
        {
            Log($"LoadData {id}");
            isNew = false;
            ToggleId(true);
            txtId.Text = id.ToString();
            lblIdHuman.Text = LookupHumanFriendlyScheduledPaymentId(id);
            // TODO: fetch actual ScheduledPayment
            originalDto = new ScheduledPayment();
            PopulateFields(originalDto);
        }

        void PopulateFields(ScheduledPayment x)
        {
            txtCompanyId.Text = x.CompanyId.ToString();
            lblCompanyHuman.Text = LookupHumanFriendlyCompanyId(x.CompanyId);

            txtVendorId.Text = x.VendorId?.ToString() ?? "";
            lblVendorHuman.Text = x.VendorId.HasValue
                                   ? LookupHumanFriendlyVendorId(x.VendorId.Value)
                                   : "";

            txtInvoiceId.Text = x.InvoiceId?.ToString() ?? "";
            lblInvoiceHuman.Text = x.InvoiceId.HasValue
                                   ? LookupHumanFriendlyInvoiceId(x.InvoiceId.Value)
                                   : "";

            txtBatchId.Text = x.BatchId?.ToString() ?? "";
            lblBatchHuman.Text = x.BatchId.HasValue
                                   ? LookupHumanFriendlyBatchId(x.BatchId.Value)
                                   : "";

            txtPaymentReference.Text = x.PaymentReference;
            txtDescription.Text = x.Description ?? "";
            txtCurrency.Text = x.Currency;
            txtAmount.Text = x.Amount.ToString();
            txtExchangeRate.Text = x.ExchangeRate.ToString();

            txtBeneficiaryName.Text = x.BeneficiaryName ?? "";
            txtBeneficiaryBankName.Text = x.BeneficiaryBankName ?? "";
            txtBeneficiaryBranch.Text = x.BeneficiaryBranch ?? "";
            txtBeneficiaryAccountNo.Text = x.BeneficiaryAccountNo ?? "";
            txtBeneficiaryRoutingNo.Text = x.BeneficiaryRoutingNo ?? "";

            txtPaymentMethod.Text = x.PaymentMethod;
            txtFrequency.Text = x.Frequency;
            txtIntervalValue.Text = x.IntervalValue?.ToString() ?? "";

            dtpNextRunDate.Value = x.NextRunDate.ToDateTime(TimeOnly.MinValue);
            dtpLastRunDate.Value = x.LastRunDate.HasValue
                                          ? x.LastRunDate.Value.ToDateTime(TimeOnly.MinValue)
                                          : DateTime.Now;

            chkIsPending.Checked = x.IsPending;
            chkIsProcessing.Checked = x.IsProcessing;
            chkIsCompleted.Checked = x.IsCompleted;
            chkIsFailed.Checked = x.IsFailed;
            chkIsCancelled.Checked = x.IsCancelled;

            txtExternalPaymentId.Text = x.ExternalPaymentId ?? "";
            txtFeeAmount.Text = x.FeeAmount?.ToString() ?? "";
            txtNetAmount.Text = x.NetAmount?.ToString() ?? "";
            chkIsReconciled.Checked = x.IsReconciled;
            chkIsExcluded.Checked = x.IsExcluded;

            dtpReconciliationDate.Value = x.ReconciliationDate.HasValue
                                          ? x.ReconciliationDate.Value.ToDateTime(TimeOnly.MinValue)
                                          : DateTime.Now;

            chkIsAutomaticClear.Checked = x.IsAutomaticClear;
        }

        void ToggleId(bool show)
        {
            txtId.Visible =
            btnIdSearch.Visible =
            lblIdHuman.Visible = show;
        }

        ScheduledPayment BuildDto()
        {
            return new ScheduledPayment
            {
                Id = isNew ? 0 : Convert.ToInt64(txtId.Text),
                CompanyId = Convert.ToInt64(txtCompanyId.Text),
                VendorId = string.IsNullOrWhiteSpace(txtVendorId.Text) ? null : Convert.ToInt64(txtVendorId.Text),
                InvoiceId = string.IsNullOrWhiteSpace(txtInvoiceId.Text) ? null : Convert.ToInt64(txtInvoiceId.Text),
                BatchId = string.IsNullOrWhiteSpace(txtBatchId.Text) ? null : Convert.ToInt64(txtBatchId.Text),
                PaymentReference = txtPaymentReference.Text,
                Description = txtDescription.Text,
                Currency = txtCurrency.Text,
                Amount = double.Parse(txtAmount.Text),
                ExchangeRate = double.Parse(txtExchangeRate.Text),
                BeneficiaryName = txtBeneficiaryName.Text,
                BeneficiaryBankName = txtBeneficiaryBankName.Text,
                BeneficiaryBranch = txtBeneficiaryBranch.Text,
                BeneficiaryAccountNo = txtBeneficiaryAccountNo.Text,
                BeneficiaryRoutingNo = txtBeneficiaryRoutingNo.Text,
                PaymentMethod = txtPaymentMethod.Text,
                Frequency = txtFrequency.Text,
                IntervalValue = int.TryParse(txtIntervalValue.Text, out var iv) ? iv : null,
                NextRunDate = DateOnly.FromDateTime(dtpNextRunDate.Value),
                LastRunDate = dtpLastRunDate.Value != default
                                       ? DateOnly.FromDateTime(dtpLastRunDate.Value)
                                       : (DateOnly?)null,
                IsPending = chkIsPending.Checked == true,
                IsProcessing = chkIsProcessing.Checked == true,
                IsCompleted = chkIsCompleted.Checked == true,
                IsFailed = chkIsFailed.Checked == true,
                IsCancelled = chkIsCancelled.Checked == true,
                ExternalPaymentId = txtExternalPaymentId.Text,
                FeeAmount = double.TryParse(txtFeeAmount.Text, out var fa) ? fa : (double?)null,
                NetAmount = double.TryParse(txtNetAmount.Text, out var na) ? na : (double?)null,
                IsReconciled = chkIsReconciled.Checked == true,
                IsExcluded = chkIsExcluded.Checked == true,
                ReconciliationDate = dtpReconciliationDate.Value != default
                                      ? DateOnly.FromDateTime(dtpReconciliationDate.Value)
                                      : (DateOnly?)null,
                IsAutomaticClear = chkIsAutomaticClear.Checked == true
            };
        }

        (bool, string) ValidateInputs()
        {
            var errs = new List<string>();

            if (!isNew && !long.TryParse(txtId.Text, out _)) errs.Add("Id");
            if (!long.TryParse(txtCompanyId.Text, out _)) errs.Add("CompanyId");
            if (!string.IsNullOrWhiteSpace(txtVendorId.Text) && !long.TryParse(txtVendorId.Text, out _)) errs.Add("VendorId");
            if (!string.IsNullOrWhiteSpace(txtInvoiceId.Text) && !long.TryParse(txtInvoiceId.Text, out _)) errs.Add("InvoiceId");
            if (!string.IsNullOrWhiteSpace(txtBatchId.Text) && !long.TryParse(txtBatchId.Text, out _)) errs.Add("BatchId");

            if (string.IsNullOrWhiteSpace(txtPaymentReference.Text)) errs.Add("PaymentReference");
            if (string.IsNullOrWhiteSpace(txtCurrency.Text)) errs.Add("Currency");
            if (!double.TryParse(txtAmount.Text, out _)) errs.Add("Amount");
            if (!double.TryParse(txtExchangeRate.Text, out _)) errs.Add("ExchangeRate");

            if (!int.TryParse(txtIntervalValue.Text, out _) && !string.IsNullOrWhiteSpace(txtIntervalValue.Text))
                errs.Add("IntervalValue");
            if (!double.TryParse(txtFeeAmount.Text, out _) && !string.IsNullOrWhiteSpace(txtFeeAmount.Text))
                errs.Add("FeeAmount");
            if (!double.TryParse(txtNetAmount.Text, out _) && !string.IsNullOrWhiteSpace(txtNetAmount.Text))
                errs.Add("NetAmount");

            var statusCount = new[]
            {
                chkIsPending, chkIsProcessing, chkIsCompleted, chkIsFailed, chkIsCancelled
            }.Count(c => c.Checked == true);
            if (statusCount > 1) errs.Add("Multiple statuses");

            return (errs.Count == 0, string.Join(", ", errs));
        }

        void Save()
        {
            var (ok, errors) = ValidateInputs();
            var dto = BuildDto();
            var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
            var msg = $"Valid: {ok}\n{(ok ? "" : "Errors: " + errors)}\n\n{json}";
            MessageBox.Show(this, msg);
            Log("Save triggered");
        }

        void Edit()
        {
            txtPaymentReference.Focus();
            Log("Edit mode");
        }

        void Cancel()
        {
            if (isNew) NewRecord();
            else PopulateFields(originalDto);
            Log("Cancel triggered");
        }

        void ResetForm()
        {
            if (MessageBox.Show(this, TranslationHelper.Translate("Are you sure?"), MessageBoxButtons.YesNo)
                == DialogResult.Yes)
                NewRecord();
        }

        void ClearFields()
        {
            var boxes = new[]
            {
                txtId, txtCompanyId, txtVendorId, txtInvoiceId, txtBatchId,
                txtPaymentReference, txtDescription, txtCurrency, txtAmount, txtExchangeRate,
                txtBeneficiaryName, txtBeneficiaryBankName, txtBeneficiaryBranch,
                txtBeneficiaryAccountNo, txtBeneficiaryRoutingNo,
                txtPaymentMethod, txtFrequency, txtIntervalValue,
                txtExternalPaymentId, txtFeeAmount, txtNetAmount
            };
            foreach (var tb in boxes) tb.Text = string.Empty;

            var checks = new[]
            {
                chkIsPending, chkIsProcessing, chkIsCompleted, chkIsFailed, chkIsCancelled,
                chkIsReconciled, chkIsExcluded, chkIsAutomaticClear
            };
            foreach (var cb in checks) cb.Checked = false;

            dtpNextRunDate.Value = DateTime.Now;
            dtpLastRunDate.Value = DateTime.Now;
            dtpReconciliationDate.Value = DateTime.Now;
        }

        // stubs
        void Log(string msg) => Console.WriteLine($"[ScheduledPaymentPanel] {msg}");

        string LookupHumanFriendlyScheduledPaymentId(long id) => $"SP#{id}";
        string LookupHumanFriendlyCompanyId(long id) => $"Company#{id}";
        string LookupHumanFriendlyVendorId(long id) => $"Vendor#{id}";
        string LookupHumanFriendlyInvoiceId(long id) => $"Invoice#{id}";
        string LookupHumanFriendlyBatchId(long id) => $"Batch#{id}";
    }
}
