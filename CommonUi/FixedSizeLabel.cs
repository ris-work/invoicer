using System;
using Eto.Drawing;
using Eto.Forms;

public class CustomLabel : Drawable
{
    private string text;
    private Font font;

    /// <summary>
    /// The text to display.
    /// </summary>
    public string Text
    {
        get => text;
        set
        {
            if (text != value)
            {
                text = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// The font used by the label, defaulting to a new Font(FontFamilies.Sans, 12) if not set.
    /// </summary>
    public Font Font
    {
        get => font ?? new Font(FontFamilies.Sans, 12);
        set
        {
            if (font != value)
            {
                font = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Foreground (text) color.
    /// </summary>
    public Color ForegroundColor { get; set; } = Colors.Black;

    /// <summary>
    /// Background color.
    /// </summary>
    public Color BackgroundColor { get; set; } = Colors.Transparent;

    /// <summary>
    /// Horizontal text alignment as defined in Eto.
    /// </summary>
    public FormattedTextAlignment TextAlignment { get; set; } = FormattedTextAlignment.Left;

    /// <summary>
    /// Vertical text alignment.
    /// </summary>
    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Center;

    public CustomLabel()
    {
        BackgroundColor = Colors.Transparent;
    }

    /// <summary>
    /// Draws the text using a FormattedText instance, which respects MaximumWidth and automatically trims with ellipsis
    /// when the control is too narrow. Vertical alignment adjusts the final drawing point.
    /// </summary>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        var controlRect = new RectangleF(PointF.Empty, this.Size);

        // Set the clip to ensure that drawing is confined to our control area.
        g.SetClip(controlRect);

        // Draw the background.
        using (var bgBrush = new SolidBrush(BackgroundColor))
        {
            g.FillRectangle(bgBrush, controlRect);
        }

        // Create and configure a FormattedText instance.
        // The default constructor is used, then properties are set.
        var ft = new FormattedText
        {
            Text = Text ?? "",
            Font = Font,
            ForegroundBrush = new SolidBrush(ForegroundColor),
            MaximumWidth = controlRect.Width,
            Wrap = FormattedTextWrapMode.None, // Disable wrapping so text stays on one line.
            Trimming = FormattedTextTrimming.WordEllipsis,
            Alignment = TextAlignment,
        };

        // Measure the formatted text's size.
        SizeF measuredSize = ft.Measure();

        // Calculate the vertical offset for proper alignment.
        float y = controlRect.Y;
        if (VerticalAlignment == VerticalAlignment.Center)
        {
            y += (controlRect.Height - measuredSize.Height) / 2;
        }
        else if (VerticalAlignment == VerticalAlignment.Bottom)
        {
            y += (controlRect.Height - measuredSize.Height);
        }

        // Draw the formatted text at the computed point.
        g.DrawText(ft, new PointF(controlRect.X, y));

        // Reset the clip region.
        g.ResetClip();
    }
}
