using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Eto.Forms;
using Eto.Drawing;
using RV.InvNew.Common;
using CommonUi;

public class InventoryAdjustmentPanel : Panel
{
    private readonly int columnCount;
    private readonly GridView<InventoryAdjustment> grid;
    private readonly List<InventoryAdjustment> adjustments = new();

    private readonly Button btnNew, btnLoad, btnEdit, btnSave, btnCancel, btnReset, btnAdd, btnRemove;

    private readonly Label lblEntryId, lblItem, lblBatch, lblSIH;
    private readonly TextBox txtCount, txtPerItemValue, txtNetValue, txtReason, txtBeforeQty, txtAfterQty, txtReferenceCode;
    private readonly DateTimePicker dtCreatedAt, dtEditedAt;

    private long currentEntryId;
    private bool isNew = true;

    public InventoryAdjustmentPanel() : this(2) { }
    public InventoryAdjustmentPanel(int columnCount = 2)
    {
        this.columnCount = Math.Max(1, columnCount);

        // Buttons
        btnNew = MakeButton("New", OnNew);
        btnLoad = MakeButton("Load", OnLoad);
        btnEdit = MakeButton("Edit", OnEdit);
        btnSave = MakeButton("Save", OnSave);
        btnCancel = MakeButton("Cancel", OnCancel);
        btnReset = MakeButton("Reset", OnReset);
        btnAdd = MakeButton("Add", OnAdd);
        btnRemove = MakeButton("Remove", OnRemove);

        // Labels (NA fields)
        lblEntryId = new Label();
        lblItem = new Label();
        lblBatch = new Label();
        lblSIH = new Label();

        // Inputs
        txtCount = new TextBox();
        txtPerItemValue = new TextBox();
        txtNetValue = new TextBox();
        txtReason = new TextBox();
        txtBeforeQty = new TextBox();
        txtAfterQty = new TextBox();
        txtReferenceCode = new TextBox();
        dtCreatedAt = new DateTimePicker { Value = DateTime.Now };
        dtEditedAt = new DateTimePicker { Value = DateTime.Now };

        // Grid
        grid = new GridView<InventoryAdjustment>
        {
            ShowHeader = true,
            DataStore = adjustments
        };
        grid.Columns.Add(new GridColumn { HeaderText = "Item", DataCell = new TextBoxCell(nameof(InventoryAdjustment.Itemcode)) });
        grid.Columns.Add(new GridColumn { HeaderText = "Batch", DataCell = new TextBoxCell(nameof(InventoryAdjustment.Batchcode)) });
        grid.Columns.Add(new GridColumn { HeaderText = "Count", DataCell = new TextBoxCell(nameof(InventoryAdjustment.Count)) });
        grid.Columns.Add(new GridColumn { HeaderText = "PerItemValue", DataCell = new TextBoxCell(nameof(InventoryAdjustment.PerItemValue)) });
        grid.Columns.Add(new GridColumn { HeaderText = "NetValue", DataCell = new TextBoxCell(nameof(InventoryAdjustment.NetValue)) });
        grid.Columns.Add(new GridColumn { HeaderText = "BeforeQty", DataCell = new TextBoxCell(nameof(InventoryAdjustment.BeforeQty)) });
        grid.Columns.Add(new GridColumn { HeaderText = "AfterQty", DataCell = new TextBoxCell(nameof(InventoryAdjustment.AfterQty)) });
        grid.Columns.Add(new GridColumn { HeaderText = "Reason", DataCell = new TextBoxCell(nameof(InventoryAdjustment.Reason)) });

        Content = BuildLayout();
    }

    private Control BuildLayout()
    {
        var table = new TableLayout { Spacing = new Size(10, 10) };

        // Top buttons row
        table.Rows.Add(new TableRow(
            new TableCell(btnNew),
            new TableCell(btnLoad),
            new TableCell(btnEdit),
            new TableCell(btnSave),
            new TableCell(btnCancel),
            new TableCell(btnReset)
        ));

        // Field definitions
        var fields = new List<(string label, Control input)>
        {
            ("EntryId", lblEntryId),
            ("Item", lblItem),
            ("Batch", lblBatch),
            ("SIH", lblSIH),
            ("Count", txtCount),
            ("PerItemValue", txtPerItemValue),
            ("NetValue", txtNetValue),
            ("Reason", txtReason),
            ("BeforeQty", txtBeforeQty),
            ("AfterQty", txtAfterQty),
            ("ReferenceCode", txtReferenceCode),
            ("CreatedAt", dtCreatedAt),
            ("EditedAt", dtEditedAt)
        };

        // Build rows dynamically
        for (int i = 0; i < fields.Count; i += columnCount)
        {
            var cells = new List<TableCell>();
            for (int j = 0; j < columnCount && i + j < fields.Count; j++)
            {
                var (label, input) = fields[i + j];
                var inner = new TableLayout { Spacing = new Size(5, 5) };
                inner.Rows.Add(new TableRow(new Label { Text = TranslationHelper.Translate(label) }));
                inner.Rows.Add(new TableRow(input));
                cells.Add(new TableCell(inner));
            }
            table.Rows.Add(new TableRow(cells));
        }

        // Add/Remove buttons row
        table.Rows.Add(new TableRow(new TableCell(btnAdd), new TableCell(btnRemove)));

        // Grid full width
        table.Rows.Add(new TableRow(grid) { ScaleHeight = true });

        return table;
    }

    private Button MakeButton(string text, EventHandler<EventArgs> handler)
    {
        var b = new Button { Text = TranslationHelper.Translate(text) };
        b.Click += handler;
        return b;
    }

    private void OnNew(object? sender, EventArgs e)
    {
        if (MessageBox.Show("Start new entry?", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            isNew = true;
            currentEntryId = 0;
            lblEntryId.Text = "";
            adjustments.Clear();
            grid.Invalidate();
        }
    }

    private void OnLoad(object? sender, EventArgs e) => LoadData(1);

    private void OnEdit(object? sender, EventArgs e) => isNew = false;

    private void OnSave(object? sender, EventArgs e)
    {
        var (valid, errors) = ValidateInputs();
        if (!valid)
        {
            MessageBox.Show(errors, MessageBoxButtons.OK);
            return;
        }
        foreach (var adj in adjustments) adj.Posted = false;
        var json = JsonSerializer.Serialize(adjustments);
        MessageBox.Show(json, MessageBoxButtons.OK);
    }

    private void OnCancel(object? sender, EventArgs e)
    {
        adjustments.Clear();
        grid.Invalidate();
    }

    private void OnReset(object? sender, EventArgs e)
    {
        adjustments.Clear();
        grid.Invalidate();
    }

    private void OnAdd(object? sender, EventArgs e)
    {
        var adj = new InventoryAdjustment
        {
            EntryId = currentEntryId,
            Itemcode = 0,
            Batchcode = 0,
            Count = long.TryParse(txtCount.Text, out var c) ? c : 0,
            PerItemValue = double.TryParse(txtPerItemValue.Text, out var piv) ? piv : 0,
            NetValue = double.TryParse(txtNetValue.Text, out var nv) ? nv : 0,
            Reason = txtReason.Text,
            BeforeQty = double.TryParse(txtBeforeQty.Text, out var bq) ? bq : 0,
            AfterQty = double.TryParse(txtAfterQty.Text, out var aq) ? aq : 0,
            ReferenceCode = txtReferenceCode.Text,
            CreatedAt = dtCreatedAt.Value ?? DateTime.Now,
            EditedAt = dtEditedAt.Value ?? DateTime.Now,
            Posted = false
        };
        adjustments.Add(adj);
        grid.Invalidate();
    }

    private void OnRemove(object? sender, EventArgs e)
    {
        if (grid.SelectedItem is InventoryAdjustment adj)
        {
            adjustments.Remove(adj);
            grid.Invalidate();
        }
    }

    private (bool, string) ValidateInputs()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(txtReason.Text))
            errors.Add("Reason required");
        if (!long.TryParse(txtCount.Text, out _))
            errors.Add("Count invalid");
        if (!double.TryParse(txtPerItemValue.Text, out _))
            errors.Add("PerItemValue invalid");
        return (errors.Count == 0, string.Join(Environment.NewLine, errors));
    }

    private void LoadData(long id)
    {
        currentEntryId = id;
        lblEntryId.Text = id.ToString(CultureInfo.InvariantCulture);
    }

    private void Log(string msg) => Console.WriteLine(msg);
}
