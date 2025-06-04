using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;

//using Eto.Collections; // Ensure the proper namespace for OrderedDictionary

public static class MainTableLayoutGenerator
{
    /// <summary>
    /// Generates a TableLayout that arranges fields into columns.
    /// Each dictionary entry is interpreted as follows:
    ///   - If MainControl is non-null:
    ///       • Primary row: [LabelControl] [MainControl]
    ///       • If Supplemental is non-null, an extra row is added: [empty] [Supplemental]
    ///   - If MainControl is null:
    ///       • The Supplemental (if any) is used in place of MainControl in the primary row.
    /// The int value is used for normalizing placement into nColumns.
    /// </summary>
    /// <param name="layoutNext">
    /// An OrderedDictionary whose key is a tuple
    /// (Control LabelControl, Control MainControl, Control Supplemental)
    /// and whose value is the row offset (from the original vertical layout).
    /// </param>
    /// <param name="nColumns">The desired number of columns.</param>
    /// <param name="maxRowOffset">The maximum row offset (for normalization purposes).</param>
    /// <returns>A TableLayout that arranges the fields into multiple columns.</returns>
    public static TableLayout GenerateMainTableLayout(
        OrderedDictionary<
            (Control LabelControl, Control MainControl, Control Supplemental),
            int
        > layoutNext,
        int nColumns,
        int maxRowOffset
    )
    {
        // Create bins (one per column) to temporarily hold (leftControl, rightControl) pairs.
        var columnBins = new List<List<(Control, Control)>>();
        for (int col = 0; col < nColumns; col++)
            columnBins.Add(new List<(Control, Control)>());

        // Distribute each item into the appropriate column based on its row offset.
        foreach (var kvp in layoutNext)
        {
            int currentColumnIndex = (int)
                Math.Floor((double)nColumns * kvp.Value / (maxRowOffset + 1));
            if (currentColumnIndex >= nColumns)
                currentColumnIndex = nColumns - 1;

            var field = kvp.Key;

            if (field.MainControl != null)
            {
                // Primary row shows LabelControl and MainControl.
                columnBins[currentColumnIndex].Add((field.LabelControl, field.MainControl));

                // If Supplemental exists, add an extra row below:
                // The left cell will be empty to reserve label space.
                if (field.Supplemental != null)
                {
                    columnBins[currentColumnIndex].Add((null, field.Supplemental));
                }
            }
            else
            {
                // No MainControl: use Supplemental in its place.
                // If neither is provided, a placeholder (empty Panel) is used.
                Control rightControl = field.Supplemental ?? new Panel { Size = new Size(0, 0) };
                columnBins[currentColumnIndex].Add((field.LabelControl, rightControl));
            }
        }

        // Build a TableLayout for each column.
        var columnTables = new List<TableLayout>();
        foreach (var column in columnBins)
        {
            var rows = new List<TableRow>();
            foreach (var pair in column)
            {
                var row = new TableRow();

                // If the left control is null, insert an empty control to preserve alignment.
                Control leftControl = pair.Item1 ?? new Panel { Size = new Size(0, 0) };
                row.Cells.Add(new TableCell(leftControl, false)); // false => do not expand, so labels align
                row.Cells.Add(new TableCell(pair.Item2, true));
                rows.Add(row);
            }
            var colLayout = new TableLayout(rows.ToArray())
            {
                Spacing = new Size(5, 3),
                Padding = new Padding(4),
            };
            columnTables.Add(colLayout);
        }

        // Combine all column layouts into one outer TableLayout.
        var outerRow = new TableRow(columnTables.Select(ct => new TableCell(ct, true)).ToArray());
        var outerLayout = new TableLayout(new[] { outerRow })
        {
            Spacing = new Size(2, 2),
            Padding = new Padding(2),
        };

        return outerLayout;
    }
}
