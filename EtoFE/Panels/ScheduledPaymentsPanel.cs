using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CommonUi;
using Eto.Drawing;
using Eto.Forms;
using EtoFE;
using RV.InvNew.Common;

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

        // ctors
        public ScheduledPaymentPanel()
            : this(columns: 2) { }

        public ScheduledPaymentPanel(long id)
            : this(columns: 2)
        {
            LoadData(id);
        }

        ScheduledPaymentPanel(int columns)
        {
            while (true)
            {
                var req = (
                    SendAuthenticatedRequest<string, common.BackOfficeAccountingDataTransfer>.Send(
                        "Refresh",
                        "/BackOfficeAccountingRefresh",
                        true
                    )
                );
                //req.ShowModal();
                if (req.Error == false)
                {
                    GlobalState.BAT = req.Out;
                    MessageBox.Show(
                        JsonSerializer.Serialize(req.Out),
                        "Got this",
                        MessageBoxType.Information
                    );
                    break;
                }
            }
            // sizes from theme
            var lw = ColorSettings.InnerLabelWidth ?? 150;
            var lh = ColorSettings.InnerLabelHeight ?? 25;
            var cw = ColorSettings.InnerControlWidth ?? 300;
            var ch = ColorSettings.InnerControlHeight ?? 25;
            int hw = (int)Math.Floor(cw * 0.2);

            // NA‐fields
            txtId = new TextBox { Width = cw, Height = ch };
            btnIdSearch = new Button
            {
                Text = "...",
                Height = ch,
                Width = hw,
            };
            lblIdHuman = new Label { Width = cw, Height = ch };

            txtCompanyId = new TextBox { Width = cw, Height = ch };
            btnCompanySearch = new Button
            {
                Text = "...",
                Height = ch,
                Width = hw,
            };
            lblCompanyHuman = new Label { Width = cw, Height = ch };

            txtVendorId = new TextBox { Width = cw, Height = ch };
            btnVendorSearch = new Button
            {
                Text = "...",
                Height = ch,
                Width = hw,
            };
            lblVendorHuman = new Label { Width = cw, Height = ch };

            txtInvoiceId = new TextBox { Width = cw, Height = ch };
            btnInvoiceSearch = new Button
            {
                Text = "...",
                Height = ch,
                Width = hw,
            };
            lblInvoiceHuman = new Label { Width = cw, Height = ch };

            txtBatchId = new TextBox { Width = cw, Height = ch };
            btnBatchSearch = new Button
            {
                Text = "...",
                Height = ch,
                Width = hw,
            };
            lblBatchHuman = new Label { Width = cw, Height = ch };

            // scalar inputs
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

            // dates
            dtpNextRunDate = new DateTimePicker
            {
                Mode = DateTimePickerMode.Date,
                Value = DateTime.Now,
            };
            dtpLastRunDate = new DateTimePicker
            {
                Mode = DateTimePickerMode.Date,
                Value = DateTime.Now,
            };
            dtpReconciliationDate = new DateTimePicker
            {
                Mode = DateTimePickerMode.Date,
                Value = DateTime.Now,
            };

            // statuses
            chkIsPending = new CheckBox { Text = TranslationHelper.Translate("IsPending") };
            chkIsProcessing = new CheckBox { Text = TranslationHelper.Translate("IsProcessing") };
            chkIsCompleted = new CheckBox { Text = TranslationHelper.Translate("IsCompleted") };
            chkIsFailed = new CheckBox { Text = TranslationHelper.Translate("IsFailed") };
            chkIsCancelled = new CheckBox { Text = TranslationHelper.Translate("IsCancelled") };

            // reconciliation
            txtExternalPaymentId = new TextBox { Width = cw, Height = ch };
            txtFeeAmount = new TextBox { Width = cw, Height = ch };
            txtNetAmount = new TextBox { Width = cw, Height = ch };
            chkIsReconciled = new CheckBox { Text = TranslationHelper.Translate("IsReconciled") };
            chkIsExcluded = new CheckBox { Text = TranslationHelper.Translate("IsExcluded") };
            chkIsAutomaticClear = new CheckBox
            {
                Text = TranslationHelper.Translate("IsAutomaticClear"),
            };

            // actions
            btnNew = new Button { Text = TranslationHelper.Translate("New") };
            btnLoad = new Button { Text = TranslationHelper.Translate("Load") };
            btnEdit = new Button { Text = TranslationHelper.Translate("Edit") };
            btnSave = new Button { Text = TranslationHelper.Translate("Save") };
            btnCancel = new Button { Text = TranslationHelper.Translate("Cancel") };
            btnReset = new Button { Text = TranslationHelper.Translate("Reset") };

            // wire lookups
            btnIdSearch.Click += (_, __) =>
                DoLookup(txtId, lblIdHuman, LookupHumanFriendlyScheduledPaymentId);
            btnCompanySearch.Click += (_, __) =>
                DoLookup(txtCompanyId, lblCompanyHuman, LookupHumanFriendlyCompanyId);
            btnVendorSearch.Click += (_, __) =>
                DoLookup(txtVendorId, lblVendorHuman, LookupHumanFriendlyVendorId);
            btnInvoiceSearch.Click += (_, __) =>
                DoLookup(txtInvoiceId, lblInvoiceHuman, LookupHumanFriendlyInvoiceId);
            btnBatchSearch.Click += (_, __) =>
                DoLookup(txtBatchId, lblBatchHuman, LookupHumanFriendlyBatchId);

            // mutually‐exclusive statuses
            var statuses = new[]
            {
                chkIsPending,
                chkIsProcessing,
                chkIsCompleted,
                chkIsFailed,
                chkIsCancelled,
            };
            statuses
                .ToList()
                .ForEach(cb =>
                    cb.CheckedChanged += (s, e) =>
                    {
                        if (cb.Checked == true)
                            statuses.Where(x => x != cb).ToList().ForEach(x => x.Checked = false);
                    }
                );

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
                txtId,
                btnIdSearch,
                txtCompanyId,
                btnCompanySearch,
                txtVendorId,
                btnVendorSearch,
                txtInvoiceId,
                btnInvoiceSearch,
                txtBatchId,
                btnBatchSearch,
                txtPaymentReference,
                txtDescription,
                txtCurrency,
                txtAmount,
                txtExchangeRate,
                txtBeneficiaryName,
                txtBeneficiaryBankName,
                txtBeneficiaryBranch,
                txtBeneficiaryAccountNo,
                txtBeneficiaryRoutingNo,
                txtPaymentMethod,
                txtFrequency,
                txtIntervalValue,
                dtpNextRunDate,
                dtpLastRunDate,
                dtpReconciliationDate,
                chkIsPending,
                chkIsProcessing,
                chkIsCompleted,
                chkIsFailed,
                chkIsCancelled,
                txtExternalPaymentId,
                txtFeeAmount,
                txtNetAmount,
                chkIsReconciled,
                chkIsExcluded,
                chkIsAutomaticClear,
                btnNew,
                btnLoad,
                btnEdit,
                btnSave,
                btnCancel,
                btnReset,
            };
            foreach (var c in focusable)
                c.GotFocus += (s, e) => lastFocused = (Control)s;

            Content = BuildLayout(columns);
            NewRecord();
        }

        StackLayout BuildLayout(int columns)
        {
            var lw = ColorSettings.InnerLabelWidth ?? 150;
            var lh = ColorSettings.InnerLabelHeight ?? 25;
            var cw = ColorSettings.InnerControlWidth ?? 300;
            var ch = ColorSettings.InnerControlHeight ?? 25;

            // create all fields as (key, control)
            var fields = new List<(string Key, Control Ctrl)>
            {
                (
                    "Id",
                    new StackLayout(
                        new StackLayoutItem(txtId),
                        new StackLayoutItem(btnIdSearch),
                        new StackLayoutItem(lblIdHuman)
                    )
                    {
                        Spacing = 2,
                        Orientation = Orientation.Horizontal,
                    }
                ),
                (
                    "CompanyId",
                    new StackLayout(
                        new StackLayoutItem(txtCompanyId),
                        new StackLayoutItem(btnCompanySearch),
                        new StackLayoutItem(lblCompanyHuman)
                    )
                    {
                        Spacing = 2,
                        Orientation = Orientation.Horizontal,
                    }
                ),
                (
                    "VendorId",
                    new StackLayout(
                        new StackLayoutItem(txtVendorId),
                        new StackLayoutItem(btnVendorSearch),
                        new StackLayoutItem(lblVendorHuman)
                    )
                    {
                        Spacing = 2,
                        Orientation = Orientation.Horizontal,
                    }
                ),
                (
                    "InvoiceId",
                    new StackLayout(
                        new StackLayoutItem(txtInvoiceId),
                        new StackLayoutItem(btnInvoiceSearch),
                        new StackLayoutItem(lblInvoiceHuman)
                    )
                    {
                        Spacing = 2,
                        Orientation = Orientation.Horizontal,
                    }
                ),
                (
                    "BatchId",
                    new StackLayout(
                        new StackLayoutItem(txtBatchId),
                        new StackLayoutItem(btnBatchSearch),
                        new StackLayoutItem(lblBatchHuman)
                    )
                    {
                        Spacing = 2,
                        Orientation = Orientation.Horizontal,
                    }
                ),
                ("PaymentReference", txtPaymentReference),
                ("Description", txtDescription),
                ("Currency", txtCurrency),
                ("Amount", txtAmount),
                ("ExchangeRate", txtExchangeRate),
                ("BeneficiaryName", txtBeneficiaryName),
                ("BeneficiaryBankName", txtBeneficiaryBankName),
                ("BeneficiaryBranch", txtBeneficiaryBranch),
                ("BeneficiaryAccountNo", txtBeneficiaryAccountNo),
                ("BeneficiaryRoutingNo", txtBeneficiaryRoutingNo),
                ("PaymentMethod", txtPaymentMethod),
                ("Frequency", txtFrequency),
                ("IntervalValue", txtIntervalValue),
                ("NextRunDate", dtpNextRunDate),
                ("LastRunDate", dtpLastRunDate),
                (
                    "Status",
                    new StackLayout(
                        new StackLayoutItem(chkIsPending),
                        new StackLayoutItem(chkIsProcessing),
                        new StackLayoutItem(chkIsCompleted),
                        new StackLayoutItem(chkIsFailed),
                        new StackLayoutItem(chkIsCancelled)
                    )
                    {
                        Spacing = 5,
                        Orientation = Orientation.Horizontal,
                    }
                ),
                ("ExternalPaymentId", txtExternalPaymentId),
                ("FeeAmount", txtFeeAmount),
                ("NetAmount", txtNetAmount),
                ("IsReconciled", chkIsReconciled),
                ("IsExcluded", chkIsExcluded),
                ("ReconciliationDate", dtpReconciliationDate),
                ("IsAutomaticClear", chkIsAutomaticClear),
            };

            var outerButtons = new StackLayout()
            {
                Height = ColorSettings.InnerControlHeight ?? 30,
            };
            var outerLayout = new StackLayout()
            {
                Orientation = Orientation.Vertical,
                Width = (ColorSettings.InnerControlWidth ?? 150) * (columns + 4),
            }; //, Width = (ColorSettings.InnerControlWidth ?? 150) * (columns + 50) };
            var layout = new TableLayout()
            {
                Spacing = new Size(10, 10),
                Padding = 10,
                Width = (ColorSettings.InnerControlWidth ?? 150) * (columns + 4),
            };
            // chunk into rows of 'columns' pairs
            for (int i = 0; i < fields.Count; i += columns)
            {
                var slice = fields.Skip(i).Take(columns).ToList();
                var cells = new List<Control>();

                foreach (var (key, ctrl) in slice)
                {
                    cells.Add(
                        new Label
                        {
                            Text = TranslationHelper.Translate(key),
                            Width = lw,
                            Height = lh,
                            TextAlignment = TextAlignment.Right,
                        }
                    );
                    ctrl.Width = (int)Math.Floor(1.5 * cw);
                    ctrl.Height = (int)Math.Floor(1.0 * ch);
                    cells.Add(ctrl);
                }

                // fill remainder
                if (slice.Count < columns)
                {
                    var missing = columns - slice.Count;
                    for (int m = 0; m < missing; m++)
                    {
                        cells.Add(new Label()); // empty label
                        cells.Add(new Label()); // empty cell
                    }
                }

                layout.Rows.Add(
                    new TableRow(
                        cells.ToArray().Select(a => new TableCell(a) { ScaleWidth = false })
                    )
                    {
                        ScaleHeight = false,
                    }
                );
            }

            // action buttons row
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
                HorizontalContentAlignment = HorizontalAlignment.Center,
            };

            // first cell blank label, second cell the buttons, rest empty
            var actionCells = new List<Control> { new Label(), btns };
            for (int j = 1; j < columns; j++)
            {
                actionCells.Add(new Label());
                actionCells.Add(new Label());
            }
            outerButtons.Items.Add(btns);
            outerLayout.Items.Add(outerButtons);
            outerLayout.Items.Add(layout);

            return outerLayout;
        }

        void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (new[] { Keys.F1, Keys.F2, Keys.F3, Keys.F4 }.Contains(e.Key))
            {
                var map = new Dictionary<Control, Action>
                {
                    { txtId, () => btnIdSearch.PerformClick() },
                    { txtCompanyId, () => btnCompanySearch.PerformClick() },
                    { txtVendorId, () => btnVendorSearch.PerformClick() },
                    { txtInvoiceId, () => btnInvoiceSearch.PerformClick() },
                    { txtBatchId, () => btnBatchSearch.PerformClick() },
                };
                if (lastFocused != null && map.TryGetValue(lastFocused, out var act))
                    act();
            }

            if (new[] { Keys.F9, Keys.F10, Keys.F11, Keys.F12 }.Contains(e.Key))
                Save();
            if (new[] { Keys.F5, Keys.F6 }.Contains(e.Key))
                Edit();
            if (new[] { Keys.F7, Keys.F8 }.Contains(e.Key))
                ResetForm();
        }

        void DoLookup(TextBox tb, Label human, Func<long, string> lookup)
        {
            var items = new List<object>(); // TODO: fetch real items
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
            var items = new List<ScheduledPayment>(); // TODO: fetch real list
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
            // TODO: fetch actual ScheduledPayment from backend
            originalDto = new ScheduledPayment();
            PopulateFields(originalDto);
        }

        void PopulateFields(ScheduledPayment x)
        {
            // NA
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

            // scalars
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
            txtId.Visible = btnIdSearch.Visible = lblIdHuman.Visible = show;
        }

        ScheduledPayment BuildDto()
        {
            return new ScheduledPayment
            {
                Id = isNew ? 0 : Convert.ToInt64(txtId.Text),
                CompanyId = Convert.ToInt64(txtCompanyId.Text),
                VendorId = string.IsNullOrWhiteSpace(txtVendorId.Text)
                    ? null
                    : Convert.ToInt64(txtVendorId.Text),
                InvoiceId = string.IsNullOrWhiteSpace(txtInvoiceId.Text)
                    ? null
                    : Convert.ToInt64(txtInvoiceId.Text),
                BatchId = string.IsNullOrWhiteSpace(txtBatchId.Text)
                    ? null
                    : Convert.ToInt64(txtBatchId.Text),
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
                LastRunDate =
                    dtpLastRunDate.Value != default
                        ? DateOnly.FromDateTime(dtpLastRunDate.Value ?? DateTime.Now)
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
                ReconciliationDate =
                    dtpReconciliationDate.Value != default
                        ? DateOnly.FromDateTime(dtpReconciliationDate.Value ?? DateTime.Now)
                        : (DateOnly?)null,
                IsAutomaticClear = chkIsAutomaticClear.Checked == true,
            };
        }

        (bool, string) ValidateInputs()
        {
            var errs = new List<string>();
            if (!isNew && !long.TryParse(txtId.Text, out _))
                errs.Add("Id");
            if (!long.TryParse(txtCompanyId.Text, out _))
                errs.Add("CompanyId");
            if (
                !string.IsNullOrWhiteSpace(txtVendorId.Text)
                && !long.TryParse(txtVendorId.Text, out _)
            )
                errs.Add("VendorId");
            if (
                !string.IsNullOrWhiteSpace(txtInvoiceId.Text)
                && !long.TryParse(txtInvoiceId.Text, out _)
            )
                errs.Add("InvoiceId");
            if (
                !string.IsNullOrWhiteSpace(txtBatchId.Text)
                && !long.TryParse(txtBatchId.Text, out _)
            )
                errs.Add("BatchId");

            if (string.IsNullOrWhiteSpace(txtPaymentReference.Text))
                errs.Add("PaymentReference");
            if (string.IsNullOrWhiteSpace(txtCurrency.Text))
                errs.Add("Currency");
            if (!double.TryParse(txtAmount.Text, out _))
                errs.Add("Amount");
            if (!double.TryParse(txtExchangeRate.Text, out _))
                errs.Add("ExchangeRate");

            if (
                !int.TryParse(txtIntervalValue.Text, out _)
                && !string.IsNullOrWhiteSpace(txtIntervalValue.Text)
            )
                errs.Add("IntervalValue");
            if (
                !double.TryParse(txtFeeAmount.Text, out _)
                && !string.IsNullOrWhiteSpace(txtFeeAmount.Text)
            )
                errs.Add("FeeAmount");
            if (
                !double.TryParse(txtNetAmount.Text, out _)
                && !string.IsNullOrWhiteSpace(txtNetAmount.Text)
            )
                errs.Add("NetAmount");

            var statusCount = new[]
            {
                chkIsPending,
                chkIsProcessing,
                chkIsCompleted,
                chkIsFailed,
                chkIsCancelled,
            }.Count(c => c.Checked == true);
            if (statusCount > 1)
                errs.Add("Multiple statuses");

            return (errs.Count == 0, string.Join(", ", errs));
        }

        void Save()
        {
            var (ok, errors) = ValidateInputs();
            var msgx = $"Valid: {ok}\n{(ok ? "" : "Errors: " + errors)}\n\n";
            MessageBox.Show(this, msgx);
            if (ok)
            {
                var dto = BuildDto();
                var json = JsonSerializer.Serialize(
                    dto,
                    new JsonSerializerOptions { WriteIndented = true }
                );
                var msg = $"Valid: {ok}\n{(ok ? "" : "Errors: " + errors)}\n\n{json}";
                MessageBox.Show(this, msg);
            }
            Log("Save triggered");
        }

        void Edit()
        {
            txtPaymentReference.Focus();
            Log("Edit mode");
        }

        void Cancel()
        {
            if (isNew)
                NewRecord();
            else
                PopulateFields(originalDto);
            Log("Cancel triggered");
        }

        void ResetForm()
        {
            if (
                MessageBox.Show(
                    this,
                    TranslationHelper.Translate("Are you sure?"),
                    MessageBoxButtons.YesNo
                ) == DialogResult.Yes
            )
                NewRecord();
        }

        void ClearFields()
        {
            foreach (
                var tb in new[]
                {
                    txtId,
                    txtCompanyId,
                    txtVendorId,
                    txtInvoiceId,
                    txtBatchId,
                    txtPaymentReference,
                    txtDescription,
                    txtCurrency,
                    txtAmount,
                    txtExchangeRate,
                    txtBeneficiaryName,
                    txtBeneficiaryBankName,
                    txtBeneficiaryBranch,
                    txtBeneficiaryAccountNo,
                    txtBeneficiaryRoutingNo,
                    txtPaymentMethod,
                    txtFrequency,
                    txtIntervalValue,
                    txtExternalPaymentId,
                    txtFeeAmount,
                    txtNetAmount,
                }
            )
                tb.Text = "";

            foreach (
                var cb in new[]
                {
                    chkIsPending,
                    chkIsProcessing,
                    chkIsCompleted,
                    chkIsFailed,
                    chkIsCancelled,
                    chkIsReconciled,
                    chkIsExcluded,
                    chkIsAutomaticClear,
                }
            )
                cb.Checked = false;

            dtpNextRunDate.Value = DateTime.Now;
            dtpLastRunDate.Value = DateTime.Now;
            dtpReconciliationDate.Value = DateTime.Now;
        }

        void Log(string message) => Console.WriteLine($"[ScheduledPaymentPanel] {message}");

        string LookupHumanFriendlyScheduledPaymentId(long id) => $"SP#{id}";

        string LookupHumanFriendlyCompanyId(long id) => $"Company#{id}";

        string LookupHumanFriendlyVendorId(long id) => $"Vendor#{id}";

        string LookupHumanFriendlyInvoiceId(long id) => $"Invoice#{id}";

        string LookupHumanFriendlyBatchId(long id) => $"Batch#{id}";
    }
}
