using System;
using Eto.Drawing;
using Eto.Forms;

public class CustomLabel : Drawable
{
    private string text;
    private Font font;

    // Cached formatted text
    private FormattedText cachedFormattedText;
    private SizeF cachedMeasuredSize;
    private float cachedMaxWidth;

    // Cached rendered bitmap (graphics)
    private Bitmap cachedBitmap;
    private Size cachedBitmapSize;

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
                InvalidateCache();
                Invalidate();
            }
        }
    }

    /// <summary>
    /// The font used by the label; defaults to new Font(FontFamilies.Sans, 12).
    /// </summary>
    public Font Font
    {
        get => font ?? (font = new Font(FontFamilies.Sans, 12));
        set
        {
            if (font != value)
            {
                font = value;
                InvalidateCache();
                Invalidate();
            }
        }
    }

    /// <summary>
    /// The foreground (text) color.
    /// </summary>
    public Color ForegroundColor { get; set; } = Colors.Black;

    /// <summary>
    /// The background color.
    /// </summary>
    public Color BackgroundColor { get; set; } = Colors.Transparent;

    /// <summary>
    /// Horizontal text alignment.
    /// </summary>
    public FormattedTextAlignment TextAlignment { get; set; } = FormattedTextAlignment.Left;

    /// <summary>
    /// Vertical text alignment.
    /// </summary>
    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Center;

    public CustomLabel()
    {
        BackgroundColor = Colors.Transparent;

        // Invalidate caches when the control is resized.
        SizeChanged += (sender, e) =>
        {
            InvalidateCache();
            Console.WriteLine("SizeChanged: Cache invalidated due to size change.");
        };
    }

    /// <summary>
    /// Invalidate all caches (formatted text and rendered graphics).
    /// </summary>
    private void InvalidateCache()
    {
        // Invalidate cached layout.
        cachedFormattedText = null;

        // Dispose and invalidate cached graphics.
        if (cachedBitmap != null)
        {
            cachedBitmap.Dispose();
            cachedBitmap = null;
        }
    }

    /// <summary>
    /// Ensures the cached formatted text and its measured size are updated with the given available width.
    /// </summary>
    /// <param name="availableWidth">The available width for layout.</param>
    private void EnsureFormattedTextCache(float availableWidth)
    {
        // (Re)create the formatted text if missing or if available width has changed.
        if (cachedFormattedText == null || Math.Abs(cachedMaxWidth - availableWidth) > 0.1f)
        {
            var ft = new FormattedText
            {
                Text = Text ?? "",
                Font = Font,
                ForegroundBrush = new SolidBrush(ForegroundColor),
                // Set the maximum width for layout. This causes trimming if text is too long.
                MaximumWidth = availableWidth,
                // Disable wrapping; we want a single line.
                Wrap = FormattedTextWrapMode.None,
                Trimming = FormattedTextTrimming.WordEllipsis,
                Alignment = TextAlignment
            };

            cachedFormattedText = ft;
            cachedMeasuredSize = ft.Measure();
            cachedMaxWidth = availableWidth;
            Console.WriteLine("FormattedText cache rebuilt with width: {0}", availableWidth);
        }
    }

    /// <summary>
    /// Ensures the cached bitmap (graphics) is valid for the current control bounds. If not,
    /// it renders the control’s graphics into a new Bitmap.
    /// </summary>
    /// <param name="controlRect">The current control rectangle.</param>
    private void EnsureGraphicsCache(RectangleF controlRect)
    {
        // Re-create the cached bitmap if the bitmap size does not match the control's size.
        if (cachedBitmap == null || cachedBitmapSize != controlRect.Size)
        {
            // First, rebuild the FormattedText cache as needed.
            EnsureFormattedTextCache(controlRect.Width);

            // Create a new bitmap with the current control's size.
            cachedBitmap = new Bitmap((int)controlRect.Width, (int)controlRect.Height, PixelFormat.Format32bppRgba);
            cachedBitmapSize = (Size)controlRect.Size;
            Console.WriteLine("Graphics cache rebuilt with size: {0}x{1}", (int)controlRect.Width, (int)controlRect.Height);

            using (var bmpGraphics = new Graphics(cachedBitmap))
            {
                // Set clipping to the bounds of the bitmap.
                bmpGraphics.SetClip(new RectangleF(PointF.Empty, controlRect.Size));

                // Draw the background.
                using (var bgBrush = new SolidBrush(BackgroundColor))
                {
                    bmpGraphics.FillRectangle(bgBrush, new RectangleF(PointF.Empty, controlRect.Size));
                }

                // Compute the vertical alignment offset.
                float y = 0;
                if (VerticalAlignment == VerticalAlignment.Center)
                    y = (controlRect.Height - cachedMeasuredSize.Height) / 2;
                else if (VerticalAlignment == VerticalAlignment.Bottom)
                    y = (controlRect.Height - cachedMeasuredSize.Height);

                // Draw the cached formatted text into the bitmap.
                bmpGraphics.DrawText(cachedFormattedText, new PointF(0, y));
            }
        }
    }

    /// <summary>
    /// Paints the control. If caches are valid, reuse the cached bitmap.
    /// Otherwise, rebuild caches and render.
    /// </summary>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        var controlRect = new RectangleF(PointF.Empty, this.Size);

        // Clip drawing to the control's bounds.
        g.SetClip(controlRect);

        // Draw the background first.
        using (var bgBrush = new SolidBrush(BackgroundColor))
        {
            g.FillRectangle(bgBrush, controlRect);
        }

        // Ensure our caches (formatted text and graphics) are valid.
        EnsureGraphicsCache(controlRect);

        // Log when using cached graphics.
        Console.WriteLine("Using cached graphics.");

        // Draw the cached bitmap onto the control.
        g.DrawImage(cachedBitmap, controlRect);

        // Reset the clip.
        g.ResetClip();
    }
}
