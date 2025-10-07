using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CommonUi;
using Eto.Drawing;
using Eto.Forms;
using EtoFE;
using EtoFE.Search;
using RV.InvNew.Common;

namespace RV.InvNew.UI
{
    public class ScheduledReceiptPanel : Panel
    {
        // --- Control Declarations (Grouped) ---

        // NA Fields (ID, Company, etc.)
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

        // NA Fields (Accounting)
        readonly TextBox txtDebitAccount;
        readonly Label lblDebitAccount = new Label();
        readonly Button btnDebitAccountSearch;
        readonly TextBox txtDebitAccountType;
        readonly Label lblDebitAccountType = new Label();
        readonly Button btnDebitAccountTypeSearch;
        readonly TextBox txtCreditAccount;
        readonly Label lblCreditAccount = new Label();
        readonly Button btnCreditAccountSearch;
        readonly TextBox txtCreditAccountType;
        readonly Label lblCreditAccountType = new Label();
        readonly Button btnCreditAccountTypeSearch;
        readonly TextBox txtJournalNumber;
        readonly Label lblJournalNumber = new();
        readonly Button btnJournalNumberSearch;

        // Scalar Inputs
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

        // Date Pickers
        readonly DateTimePicker dtpNextRunDate;
        readonly DateTimePicker dtpLastRunDate;
        readonly DateTimePicker dtpReconciliationDate;

        // Status Checkboxes
        readonly CheckBox chkIsPending;
        readonly CheckBox chkIsProcessing;
        readonly CheckBox chkIsCompleted;
        readonly CheckBox chkIsFailed;
        readonly CheckBox chkIsCancelled;

        // Reconciliation & External IDs
        readonly TextBox txtExternalPaymentId;
        readonly TextBox txtFeeAmount;
        readonly TextBox txtNetAmount;
        readonly TextBox txtReconciliationRef;
        readonly CheckBox chkIsReconciled;
        readonly CheckBox chkIsExcluded;
        readonly CheckBox chkIsAutomaticClear;

        // Action Buttons
        readonly Button btnNew;
        readonly Button btnLoad;
        readonly Button btnEdit;
        readonly Button btnSave;
        readonly Button btnCancel;
        readonly Button btnReset;

        // --- State Variables ---
        ScheduledReceipt originalDto;
        bool isNew;
        Control lastFocused;
        readonly List<Control> essentialControls;
        readonly Dictionary<Control, Color> originalButtonColors = new();

        public ScheduledReceiptPanel() : this(2) { }
        public ScheduledReceiptPanel(long id) : this(2) { LoadData(id); }

        ScheduledReceiptPanel(int columns)
        {
            GlobalState.RefreshPR();
            GlobalState.RefreshBAT();
            var lw = ColorSettings.InnerLabelWidth ?? 150;
            var lh = ColorSettings.InnerLabelHeight ?? 25;
            var cw = ColorSettings.InnerControlWidth ?? 300;
            var ch = ColorSettings.InnerControlHeight ?? 25;
            int hw = (int)Math.Floor(cw * 0.2);

            // Initialize Controls
            txtId = new TextBox { Width = cw, Height = ch };
            btnIdSearch = new Button { Text = "...", Height = ch, Width = hw };
            lblIdHuman = new Label { Width = cw, Height = ch };
            txtCompanyId = new TextBox { Width = cw, Height = ch };
            btnCompanySearch = new Button { Text = "...", Height = ch, Width = hw };
            lblCompanyHuman = new Label { Width = cw, Height = ch };
            txtVendorId = new TextBox { Width = cw, Height = ch };
            btnVendorSearch = new Button { Text = "...", Height = ch, Width = hw };
            lblVendorHuman = new Label { Width = cw, Height = ch };
            txtInvoiceId = new TextBox { Width = cw, Height = ch };
            btnInvoiceSearch = new Button { Text = "...", Height = ch, Width = hw };
            lblInvoiceHuman = new Label { Width = cw, Height = ch };
            txtBatchId = new TextBox { Width = cw, Height = ch };
            btnBatchSearch = new Button { Text = "...", Height = ch, Width = hw };
            lblBatchHuman = new Label { Width = cw, Height = ch };
            txtCreditAccount = new TextBox { Width = cw, Height = ch };
            btnCreditAccountSearch = new Button { Text = "...", Height = ch, Width = hw };
            txtCreditAccountType = new TextBox { Width = cw, Height = ch };
            btnCreditAccountTypeSearch = new Button { Text = "...", Height = ch, Width = hw };
            txtDebitAccount = new TextBox { Width = cw, Height = ch };
            btnDebitAccountSearch = new Button { Text = "...", Height = ch, Width = hw };
            txtDebitAccountType = new TextBox { Width = cw, Height = ch };
            btnDebitAccountTypeSearch = new Button { Text = "...", Height = ch, Width = hw };
            txtJournalNumber = new TextBox { Width = cw, Height = ch };
            btnJournalNumberSearch = new Button { Text = "...", Height = ch, Width = hw };
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
            dtpNextRunDate = new DateTimePicker { Mode = DateTimePickerMode.Date, Value = DateTime.Now };
            dtpLastRunDate = new DateTimePicker { Mode = DateTimePickerMode.Date, Value = null };
            dtpReconciliationDate = new DateTimePicker { Mode = DateTimePickerMode.Date, Value = null };
            chkIsPending = new CheckBox { Text = TranslationHelper.Translate("IsPending") };
            chkIsProcessing = new CheckBox { Text = TranslationHelper.Translate("IsProcessing") };
            chkIsCompleted = new CheckBox { Text = TranslationHelper.Translate("IsCompleted") };
            chkIsFailed = new CheckBox { Text = TranslationHelper.Translate("IsFailed") };
            chkIsCancelled = new CheckBox { Text = TranslationHelper.Translate("IsCancelled") };
            txtExternalPaymentId = new TextBox { Width = cw, Height = ch };
            txtFeeAmount = new TextBox { Width = cw, Height = ch };
            txtNetAmount = new TextBox { Width = cw, Height = ch };
            txtReconciliationRef = new TextBox { Width = cw, Height = ch };
            chkIsReconciled = new CheckBox { Text = TranslationHelper.Translate("IsReconciled") };
            chkIsExcluded = new CheckBox { Text = TranslationHelper.Translate("IsExcluded") };
            chkIsAutomaticClear = new CheckBox { Text = TranslationHelper.Translate("IsAutomaticClear") };
            btnNew = new Button { Text = TranslationHelper.Translate("New") };
            btnLoad = new Button { Text = TranslationHelper.Translate("Load") };
            btnEdit = new Button { Text = TranslationHelper.Translate("Edit") };
            btnSave = new Button { Text = TranslationHelper.Translate("Save") };
            btnCancel = new Button { Text = TranslationHelper.Translate("Cancel") };
            btnReset = new Button { Text = TranslationHelper.Translate("Reset") };

            essentialControls = new List<Control>
            {
                txtPaymentReference, txtAmount, txtCurrency, txtExchangeRate, btnJournalNumberSearch,
                btnDebitAccountTypeSearch, btnDebitAccountSearch, btnCreditAccountTypeSearch,
                btnCreditAccountSearch, txtDescription, btnSave
            };

            var naButtons = new[]
            {
                btnIdSearch, btnCompanySearch, btnVendorSearch, btnInvoiceSearch, btnBatchSearch,
                btnDebitAccountSearch, btnDebitAccountTypeSearch, btnCreditAccountSearch,
                btnCreditAccountTypeSearch, btnJournalNumberSearch
            };

            foreach (var btn in naButtons)
            {
                originalButtonColors[btn] = btn.BackgroundColor;
                btn.GotFocus += OnNAButtonGotFocus;
                btn.LostFocus += OnNAButtonLostFocus;
            }

            btnIdSearch.Click += (_, __) => DoLookup(txtId, lblIdHuman, BackOfficeAccounting.SearchScheduledReceipt, LookupHumanFriendlyScheduledReceiptId);
            btnCompanySearch.Click += (_, __) => DoLookup(txtCompanyId, lblCompanyHuman, BackOfficeAccounting.SearchAccounts, LookupHumanFriendlyCompanyId);
            btnVendorSearch.Click += (_, __) => DoLookup(txtVendorId, lblVendorHuman, BackOfficeAccounting.SearchAccounts, LookupHumanFriendlyVendorId);
            btnInvoiceSearch.Click += (_, __) => DoLookup(txtInvoiceId, lblInvoiceHuman, BackOfficeAccounting.SearchReceivedInvoices, LookupHumanFriendlyInvoiceId);
            btnBatchSearch.Click += (_, __) => DoLookup(txtBatchId, lblBatchHuman, BackOfficeAccounting.SearchBatches, LookupHumanFriendlyBatchId);
            btnDebitAccountTypeSearch.Click += (_, __) => DoLookup(txtDebitAccountType, lblDebitAccountType, BackOfficeAccounting.SearchAccountTypes, BackOfficeAccounting.LookupAccountType);
            btnCreditAccountTypeSearch.Click += (_, __) => DoLookup(txtCreditAccountType, lblCreditAccountType, BackOfficeAccounting.SearchAccountTypes, BackOfficeAccounting.LookupAccountType);
            btnDebitAccountSearch.Click += (_, __) => DoLookup(txtDebitAccount, lblDebitAccount, BackOfficeAccounting.SearchAccounts, BackOfficeAccounting.LookupAccount);
            btnCreditAccountSearch.Click += (_, __) => DoLookup(txtCreditAccount, lblCreditAccount, BackOfficeAccounting.SearchAccounts, BackOfficeAccounting.LookupAccount);
            btnJournalNumberSearch.Click += (_, __) => DoLookup(txtJournalNumber, lblJournalNumber, BackOfficeAccounting.SearchJournals, BackOfficeAccounting.LookupJournalNo);

            var statuses = new[] { chkIsPending, chkIsProcessing, chkIsCompleted, chkIsFailed, chkIsCancelled };
            foreach (var cb in statuses)
            {
                cb.CheckedChanged += (s, e) =>
                {
                    if (cb.Checked == true)
                        foreach (var otherCb in statuses.Where(x => x != cb))
                            otherCb.Checked = false;
                };
            }

            btnNew.Click += (_, __) => NewRecord();
            btnLoad.Click += (_, __) => LoadDialog();
            btnEdit.Click += (_, __) => Edit();
            btnSave.Click += (_, __) => Save();
            btnCancel.Click += (_, __) => Cancel();
            btnReset.Click += (_, __) => ResetForm();

            var focusableControls = new Control[]
            {
                txtId, btnIdSearch, txtCompanyId, btnCompanySearch, txtVendorId, btnVendorSearch, txtInvoiceId,
                btnInvoiceSearch, txtBatchId, btnBatchSearch, txtPaymentReference, txtDescription, txtCurrency,
                txtAmount, txtExchangeRate, txtBeneficiaryName, txtBeneficiaryBankName, txtBeneficiaryBranch,
                txtBeneficiaryAccountNo, txtBeneficiaryRoutingNo, txtDebitAccount, btnDebitAccountSearch, txtDebitAccountType, btnDebitAccountTypeSearch,
                txtCreditAccount, btnCreditAccountSearch, txtCreditAccountType, btnCreditAccountTypeSearch, txtJournalNumber, btnJournalNumberSearch,
                txtPaymentMethod, txtFrequency, txtIntervalValue, dtpNextRunDate, dtpLastRunDate, dtpReconciliationDate,
                chkIsPending, chkIsProcessing, chkIsCompleted, chkIsFailed, chkIsCancelled, txtExternalPaymentId,
                txtFeeAmount, txtNetAmount, txtReconciliationRef, chkIsReconciled, chkIsExcluded, chkIsAutomaticClear, btnNew,
                btnLoad, btnEdit, btnSave, btnCancel, btnReset,
            };

            foreach (var c in focusableControls)
                c.GotFocus += (s, e) => lastFocused = (Control)s;

            this.KeyUp += OnKeyUp;
            Content = BuildLayout(columns);
            NewRecord();
        }

        StackLayout BuildLayout(int columns)
        {
            var lw = ColorSettings.InnerLabelWidth ?? 150;
            var lh = ColorSettings.InnerLabelHeight ?? 25;

            var fields = new List<(string Key, Control Ctrl)>
            {
                ("Id", new StackLayout { Spacing = 2, Orientation = Orientation.Horizontal, Items = { txtId, btnIdSearch, lblIdHuman } }),
                ("CompanyId", new StackLayout { Spacing = 2, Orientation = Orientation.Horizontal, Items = { txtCompanyId, btnCompanySearch, lblCompanyHuman } }),
                ("VendorId", new StackLayout { Spacing = 2, Orientation = Orientation.Horizontal, Items = { txtVendorId, btnVendorSearch, lblVendorHuman } }),
                ("InvoiceId", new StackLayout { Spacing = 2, Orientation = Orientation.Horizontal, Items = { txtInvoiceId, btnInvoiceSearch, lblInvoiceHuman } }),
                ("BatchId", new StackLayout { Spacing = 2, Orientation = Orientation.Horizontal, Items = { txtBatchId, btnBatchSearch, lblBatchHuman } }),
                ("JournalNo", new StackLayout { Spacing = 2, Orientation = Orientation.Horizontal, Items = { txtJournalNumber, btnJournalNumberSearch, lblJournalNumber } }),
                ("DebitAccountType", new StackLayout { Spacing = 2, Orientation = Orientation.Horizontal, Items = { txtDebitAccountType, btnDebitAccountTypeSearch, lblDebitAccountType } }),
                ("DebitAccount", new StackLayout { Spacing = 2, Orientation = Orientation.Horizontal, Items = { txtDebitAccount, btnDebitAccountSearch, lblDebitAccount } }),
                ("CreditAccountType", new StackLayout { Spacing = 2, Orientation = Orientation.Horizontal, Items = { txtCreditAccountType, btnCreditAccountTypeSearch, lblCreditAccountType } }),
                ("CreditAccount", new StackLayout { Spacing = 2, Orientation = Orientation.Horizontal, Items = { txtCreditAccount, btnCreditAccountSearch, lblCreditAccount } }),
                ("PaymentReference", txtPaymentReference),
                ("Description", txtDescription),
                ("Currency", txtCurrency),
                ("Amount", txtAmount),
                ("ExchangeRate", txtExchangeRate),
                ("PaymentMethod", txtPaymentMethod),
                ("Frequency", txtFrequency),
                ("IntervalValue", txtIntervalValue),
                ("BeneficiaryName", txtBeneficiaryName),
                ("BeneficiaryBankName", txtBeneficiaryBankName),
                ("BeneficiaryBranch", txtBeneficiaryBranch),
                ("BeneficiaryAccountNo", txtBeneficiaryAccountNo),
                ("BeneficiaryRoutingNo", txtBeneficiaryRoutingNo),
                ("NextRunDate", dtpNextRunDate),
                ("LastRunDate", dtpLastRunDate),
                ("Status", new StackLayout { Spacing = 5, Orientation = Orientation.Horizontal, Items = { chkIsPending, chkIsProcessing, chkIsCompleted, chkIsFailed, chkIsCancelled } }),
                ("ExternalPaymentId", txtExternalPaymentId),
                ("FeeAmount", txtFeeAmount),
                ("NetAmount", txtNetAmount),
                ("ReconciliationRef", txtReconciliationRef),
                ("IsReconciled", chkIsReconciled),
                ("IsExcluded", chkIsExcluded),
                ("ReconciliationDate", dtpReconciliationDate),
                ("IsAutomaticClear", chkIsAutomaticClear),
            };

            var outerLayout = new StackLayout { Orientation = Orientation.Vertical };
            var layout = new TableLayout { Spacing = new Size(10, 10), Padding = 10 };

            for (int i = 0; i < fields.Count; i += columns)
            {
                var slice = fields.Skip(i).Take(columns).ToList();
                var cells = new List<Control>();
                foreach (var (key, ctrl) in slice)
                {
                    cells.Add(new Label { Text = TranslationHelper.Translate(key), Width = lw, Height = lh, TextAlignment = TextAlignment.Right });
                    cells.Add(ctrl);
                }
                if (slice.Count < columns)
                    for (int m = 0; m < columns - slice.Count; m++)
                    {
                        cells.Add(new Label());
                        cells.Add(new Label());
                    }
                layout.Rows.Add(new TableRow(cells.Select(c => new TableCell(c))));
            }

            var btns = new StackLayout(
                new StackLayoutItem(btnNew), new StackLayoutItem(btnLoad), new StackLayoutItem(btnEdit),
                new StackLayoutItem(btnSave), new StackLayoutItem(btnCancel), new StackLayoutItem(btnReset))
            {
                Spacing = 5,
                Orientation = Orientation.Horizontal,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Height = ColorSettings.InnerControlHeight ?? 30
            };
            outerLayout.Items.Add(btns);
            outerLayout.Items.Add(layout);
            return outerLayout;
        }

        void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Keys.Enter)
            {
                var currentIndex = essentialControls.IndexOf(lastFocused);
                if (currentIndex > -1 && currentIndex < essentialControls.Count - 1)
                {
                    essentialControls[currentIndex + 1].Focus();
                    e.Handled = true;
                }
                else if (lastFocused == essentialControls.LastOrDefault(c => c != btnSave))
                {
                    btnSave.Focus();
                    e.Handled = true;
                }
            }
            else if (new[] { Keys.F1, Keys.F2, Keys.F3, Keys.F4 }.Contains(e.Key))
            {
                var map = new Dictionary<Control, Action>
                {
                    { txtId, () => btnIdSearch.PerformClick() }, { txtCompanyId, () => btnCompanySearch.PerformClick() },
                    { txtVendorId, () => btnVendorSearch.PerformClick() }, { txtInvoiceId, () => btnInvoiceSearch.PerformClick() },
                    { txtBatchId, () => btnBatchSearch.PerformClick() }, { txtJournalNumber, () => btnJournalNumberSearch.PerformClick() },
                    { txtDebitAccount, () => btnDebitAccountSearch.PerformClick() }, { txtDebitAccountType, () => btnDebitAccountTypeSearch.PerformClick() },
                    { txtCreditAccount, () => btnCreditAccountSearch.PerformClick() }, { txtCreditAccountType, () => btnCreditAccountTypeSearch.PerformClick() },
                };

                if (lastFocused == null) return;

                Control keyControl = lastFocused;
                if (lastFocused is Button && lastFocused.Parent is StackLayout sl)
                {
                    keyControl = sl.Items.Select(i => i.Control).OfType<TextBox>().FirstOrDefault();
                }

                if (keyControl != null && map.TryGetValue(keyControl, out var act))
                {
                    act();
                    e.Handled = true;
                }
            }
            else if (new[] { Keys.F9, Keys.F10, Keys.F11, Keys.F12 }.Contains(e.Key)) { Save(); e.Handled = true; }
            else if (new[] { Keys.F5, Keys.F6 }.Contains(e.Key)) { Edit(); e.Handled = true; }
            else if (new[] { Keys.F7, Keys.F8 }.Contains(e.Key)) { ResetForm(); e.Handled = true; }
        }

        void OnNAButtonGotFocus(object sender, EventArgs e)
        {
            if (sender is Button btn) btn.BackgroundColor = ColorSettings.AlternatingColor1;
        }

        void OnNAButtonLostFocus(object sender, EventArgs e)
        {
            if (sender is Button btn && originalButtonColors.TryGetValue(btn, out var color)) btn.BackgroundColor = color;
        }

        void DoLookup(TextBox tb, Label human, Func<Control, string[]> search, Func<long, string> lookup)
        {
            var sel = search(this);
            if (sel?.Length > 0 && long.TryParse(sel[0], out var id))
            {
                tb.Text = id.ToString();
                human.Text = lookup(id);
                lastFocused?.Focus();
            }
        }

        IEnumerable<T> FindControls<T>(Control control) where T : Control
        {
            if (control is T matchedControl)
                yield return matchedControl;

            if (control is Container container)
                foreach (var child in container.Controls)
                    foreach (var found in FindControls<T>(child))
                        yield return found;
        }

        void ClearFields()
        {
            FindControls<TextBox>(this).ToList().ForEach(tb => tb.Text = string.Empty);
            FindControls<Label>(this).Where(l => l.ID?.Contains("Human") ?? false).ToList().ForEach(l => l.Text = string.Empty);
            FindControls<CheckBox>(this).ToList().ForEach(cb => cb.Checked = false);

            dtpNextRunDate.Value = DateTime.Now;
            dtpLastRunDate.Value = null;
            dtpReconciliationDate.Value = null;

            txtCurrency.Text = "LKR";
            txtExchangeRate.Text = "1";
        }

        void NewRecord()
        {
            isNew = true;
            originalDto = new ScheduledReceipt();
            ToggleId(false);
            ClearFields();
            chkIsPending.Checked = true;
            Log("New record");
            txtPaymentReference.Focus();
        }

        void LoadDialog()
        {
            var sel = BackOfficeAccounting.SearchScheduledReceipt(this);
            if (sel?.Length > 0 && long.TryParse(sel[0], out var id))
                LoadData(id);
        }

        void LoadData(long id)
        {
            Log($"LoadData {id}");
            // TODO: fetch actual ScheduledReceipt from backend
            originalDto = new ScheduledReceipt { Id = id, CompanyId = 1, Currency = "USD", Amount = 100, ExchangeRate = 1.0, PaymentReference = "REF123", NextRunDate = DateOnly.FromDateTime(DateTime.Now), IsPending = true };
            PopulateFields(originalDto);
            isNew = false;
            ToggleId(true);
        }

        void PopulateFields(ScheduledReceipt x)
        {
            txtId.Text = x.Id.ToString();
            lblIdHuman.Text = LookupHumanFriendlyScheduledReceiptId(x.Id);
            txtCompanyId.Text = x.CompanyId.ToString();
            lblCompanyHuman.Text = LookupHumanFriendlyCompanyId(x.CompanyId);
            txtVendorId.Text = x.VendorId?.ToString() ?? "";
            lblVendorHuman.Text = x.VendorId.HasValue ? LookupHumanFriendlyVendorId(x.VendorId.Value) : "";
            txtInvoiceId.Text = x.InvoiceId?.ToString() ?? "";
            lblInvoiceHuman.Text = x.InvoiceId.HasValue ? LookupHumanFriendlyInvoiceId(x.InvoiceId.Value) : "";
            txtBatchId.Text = x.BatchId?.ToString() ?? "";
            lblBatchHuman.Text = x.BatchId.HasValue ? LookupHumanFriendlyBatchId(x.BatchId.Value) : "";
            txtJournalNumber.Text = x.JournalNo.ToString();
            lblJournalNumber.Text = BackOfficeAccounting.LookupJournalNo(x.JournalNo);
            txtDebitAccountType.Text = x.DebitAccountType.ToString();
            lblDebitAccountType.Text = BackOfficeAccounting.LookupAccountType(x.DebitAccountType);
            txtDebitAccount.Text = x.DebitAccountId.ToString();
            lblDebitAccount.Text = BackOfficeAccounting.LookupAccount(x.DebitAccountId);
            txtCreditAccountType.Text = x.CreditAccountType.ToString();
            lblCreditAccountType.Text = BackOfficeAccounting.LookupAccountType(x.CreditAccountType);
            txtCreditAccount.Text = x.CreditAccountId.ToString();
            lblCreditAccount.Text = BackOfficeAccounting.LookupAccount(x.CreditAccountId);
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
            dtpLastRunDate.Value = x.LastRunDate?.ToDateTime(TimeOnly.MinValue);
            dtpReconciliationDate.Value = x.ReconciliationDate?.ToDateTime(TimeOnly.MinValue);
            chkIsPending.Checked = x.IsPending;
            chkIsProcessing.Checked = x.IsProcessing;
            chkIsCompleted.Checked = x.IsCompleted;
            chkIsFailed.Checked = x.IsFailed;
            chkIsCancelled.Checked = x.IsCancelled;
            txtExternalPaymentId.Text = x.ExternalPaymentId ?? "";
            txtFeeAmount.Text = x.FeeAmount?.ToString() ?? "";
            txtNetAmount.Text = x.NetAmount?.ToString() ?? "";
            txtReconciliationRef.Text = x.ReconciliationRef ?? "";
            chkIsReconciled.Checked = x.IsReconciled;
            chkIsExcluded.Checked = x.IsExcluded;
            chkIsAutomaticClear.Checked = x.IsAutomaticClear;
        }

        void ToggleId(bool show)
        {
            txtId.Parent.Visible = show;
        }

        ScheduledReceipt BuildDto()
        {
            return new ScheduledReceipt
            {
                Id = isNew ? 0 : Convert.ToInt64(txtId.Text),
                CompanyId = Convert.ToInt64(txtCompanyId.Text),
                VendorId = string.IsNullOrWhiteSpace(txtVendorId.Text) ? null : Convert.ToInt64(txtVendorId.Text),
                InvoiceId = string.IsNullOrWhiteSpace(txtInvoiceId.Text) ? null : Convert.ToInt64(txtInvoiceId.Text),
                BatchId = string.IsNullOrWhiteSpace(txtBatchId.Text) ? null : Convert.ToInt64(txtBatchId.Text),
                JournalNo = Convert.ToInt32(txtJournalNumber.Text),
                DebitAccountId = long.Parse(txtDebitAccount.Text),
                DebitAccountType = long.Parse(txtDebitAccountType.Text),
                CreditAccountId = long.Parse(txtCreditAccount.Text),
                CreditAccountType = long.Parse(txtCreditAccountType.Text),
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
                NextRunDate = DateOnly.FromDateTime(dtpNextRunDate.Value ?? DateTime.Now),
                LastRunDate = dtpLastRunDate.Value.HasValue ? DateOnly.FromDateTime(dtpLastRunDate.Value.Value) : null,
                ReconciliationDate = dtpReconciliationDate.Value.HasValue ? DateOnly.FromDateTime(dtpReconciliationDate.Value.Value) : null,
                IsPending = chkIsPending.Checked ?? false,
                IsProcessing = chkIsProcessing.Checked ?? false,
                IsCompleted = chkIsCompleted.Checked ?? false,
                IsFailed = chkIsFailed.Checked ?? false,
                IsCancelled = chkIsCancelled.Checked ?? false,
                ExternalPaymentId = txtExternalPaymentId.Text,
                ReconciliationRef = txtReconciliationRef.Text,
                FeeAmount = double.TryParse(txtFeeAmount.Text, out var fa) ? fa : (double?)null,
                NetAmount = double.TryParse(txtNetAmount.Text, out var na) ? na : (double?)null,
                IsReconciled = chkIsReconciled.Checked ?? false,
                IsExcluded = chkIsExcluded.Checked ?? false,
                IsAutomaticClear = chkIsAutomaticClear.Checked ?? false,
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

            if (!long.TryParse(txtCreditAccountType.Text, out _)) errs.Add("CreditAccountType");
            if (!long.TryParse(txtDebitAccountType.Text, out _)) errs.Add("DebitAccountType");
            if (!long.TryParse(txtCreditAccount.Text, out _)) errs.Add("CreditAccount");
            if (!long.TryParse(txtDebitAccount.Text, out _)) errs.Add("DebitAccount");
            if (!long.TryParse(txtJournalNumber.Text, out _)) errs.Add("JournalNo");

            if (string.IsNullOrWhiteSpace(txtPaymentReference.Text)) errs.Add("PaymentReference");
            if (string.IsNullOrWhiteSpace(txtCurrency.Text)) errs.Add("Currency");
            if (!double.TryParse(txtAmount.Text, out _)) errs.Add("Amount");
            if (!double.TryParse(txtExchangeRate.Text, out _)) errs.Add("ExchangeRate");
            if (!string.IsNullOrWhiteSpace(txtIntervalValue.Text) && !int.TryParse(txtIntervalValue.Text, out _)) errs.Add("IntervalValue");
            if (!string.IsNullOrWhiteSpace(txtFeeAmount.Text) && !double.TryParse(txtFeeAmount.Text, out _)) errs.Add("FeeAmount");
            if (!string.IsNullOrWhiteSpace(txtNetAmount.Text) && !double.TryParse(txtNetAmount.Text, out _)) errs.Add("NetAmount");

            var statusCount = new[] { chkIsPending, chkIsProcessing, chkIsCompleted, chkIsFailed, chkIsCancelled, }.Count(c => c.Checked == true);
            if (statusCount == 0) errs.Add("A status must be selected");
            if (statusCount > 1) errs.Add("Only one status can be selected");

            return (errs.Count == 0, string.Join(", ", errs));
        }

        void Save()
        {
            var (ok, errors) = ValidateInputs();
            if (!ok)
            {
                MessageBox.Show(this, $"Validation Errors: {errors}", "Save Failed", MessageBoxType.Error);
                return;
            }
            var dto = BuildDto();
            var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
            var msg = $"Validation Passed.\n\n{json}";
            MessageBox.Show(this, msg, "Save Data");
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
            if (MessageBox.Show(this, TranslationHelper.Translate("Are you sure?"), MessageBoxButtons.YesNo) == DialogResult.Yes)
                NewRecord();
        }

        void Log(string message) => Console.WriteLine($"[ScheduledReceiptPanel] {message}");

        string LookupHumanFriendlyScheduledReceiptId(long id) => BackOfficeAccounting.SearchScheduledReceipt(this).FirstOrDefault(s => s.StartsWith(id.ToString())) ?? $"SR#{id}";
        string LookupHumanFriendlyCompanyId(long id) => $"Company#{id}"; // STUB
        string LookupHumanFriendlyVendorId(long id) => $"Vendor#{id}";   // STUB
        string LookupHumanFriendlyInvoiceId(long id) => $"Invoice#{id}"; // STUB
        string LookupHumanFriendlyBatchId(long id) => $"Batch#{id}";     // STUB
    }
}