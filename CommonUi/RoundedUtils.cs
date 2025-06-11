using System;
using Eto.Drawing;
using Eto.Forms;
using Microsoft.Maui.ApplicationModel.Communication;

//
// Helper methods for drawing rounded rectangles using arcs
//
public static class GraphicsExtensions
{
    public static GraphicsPath CreateRoundedPath(this Graphics graphics, RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0f)
        {
            path.AddRectangle(rect);
        }
        else
        {
            float diameter = radius * 2;
            var arc = new RectangleF(rect.Location, new SizeF(diameter, diameter));
            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
        }
        return path;
    }

    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle rect, float radius)
    {
        //using (var path = graphics.CreateRoundedPath(rect, radius))
            //graphics.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle rect, float radius)
    {
        using (var path = graphics.CreateRoundedPath(rect, radius))
            graphics.DrawPath(pen, path);
    }
}


//
// RoundedD: a Drawable-based control that supports full custom drawing and a hosted child.
// It propagates size changes (minus Padding) to its hosted child.
//
public class RoundedD : Drawable
{
    public int BorderRadius { get; set; } = 10;
    public int BorderThickness { get; set; } = 1;
    public Padding Padding { get; set; } = Padding.Empty;
    // OffsetBottomRight is kept for your own layout logic if needed.
    public Size OffsetBottomRight { get; set; } = Size.Empty;
    public Color FocusedBackgroundColor { get; set; } = Colors.LightBlue;
    public Color FocusedForegroundColor { get; set; } = Colors.White;
    public Color FocusedBorderColor { get; set; } = Colors.Blue;
    public Color BorderColor { get; set; } = Colors.Black;
    public Color BackgroundColor { get; set; } = Colors.White;
    public Color ForegroundColor { get; set; } = Colors.Black;
    public new int Width { get; set; } = 100;
    public new int Height { get; set; } = 50;
    public HorizontalAlignment HorizontalAlignment { get; set; }
    public VerticalAlignment VerticalAlignment { get; set; }

    public new event EventHandler<MouseEventArgs> MouseUp;
    public new event EventHandler<MouseEventArgs> MouseDown;
    public new event EventHandler<EventArgs> GotFocus;
    public new event EventHandler<EventArgs> LostFocus;
    public new event EventHandler<KeyEventArgs> KeyUp;
    public new event EventHandler<KeyEventArgs> KeyDown;
    public event EventHandler<MouseEventArgs> MouseOver;
    public event EventHandler<MouseEventArgs> MouseLeave;
    public event EventHandler<MouseEventArgs> MouseEnter;
    public event EventHandler<EventArgs> TextChanged;

    Control _child;
    bool _focused = false;

    // Optionally host a child control.
    public RoundedD(Control child = null)
    {
        if (child != null)
        {
            ClientSize = new Size(Width, Height);
            _child = child;
            Content = child;
            // Remove borders when hosting a Button or TextBox.
            if (child is Button || child is TextBox)
                BorderThickness = 1;
            if(child is TextBox tb)
            {
                tb.ShowBorder = false;
            }
            if (child is TextBox bb)
            {
                bb.ShowBorder = false;
            }
        }
        // Set the initial size.
        Size = new Size(Width, Height);
        
        
    }

    // Returns the hosted child.
    public Control GetChildren() => _child;

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        // Determine inner available size (subtracting Padding).
        var avail = new Size(Width - Padding.Horizontal, Height - Padding.Vertical);
        if (_child != null)
            _child.Size = (Size)( (Width > 0 && Height > 0) ? avail : _child.GetPreferredSize(Size.Empty));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var rect = new Rectangle(0, 0, Width, Height);
        var bg = _focused ? FocusedBackgroundColor : BackgroundColor;
        var border = _focused ? FocusedBorderColor : BorderColor;
        e.Graphics.FillRoundedRectangle(new SolidBrush(bg), rect, BorderRadius);
        if (BorderThickness > 0)
            e.Graphics.DrawRoundedRectangle(new Pen(border, BorderThickness), rect, BorderRadius);
        base.OnPaint(e);
    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        _focused = true;
        MouseEnter?.Invoke(this, e);
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        _focused = false;
        MouseLeave?.Invoke(this, e);
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        _focused = true;
        GotFocus?.Invoke(this, e);
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        _focused = false;
        LostFocus?.Invoke(this, e);
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        MouseDown?.Invoke(this, e);
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        MouseUp?.Invoke(this, e);
        base.OnMouseUp(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        KeyDown?.Invoke(this, e);
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        KeyUp?.Invoke(this, e);
        base.OnKeyUp(e);
    }
}


//
// RoundedC: a Panel-based control that hosts one arbitrary child.
// Since Panel does not support custom painting by overriding OnPaint,
// we update its BackgroundImage on size or focus changes.
// Focus is tracked via GotFocus and LostFocus.
//
public class RoundedC : Panel
{
    public int BorderRadius { get; set; } = 10;
    public int BorderThickness { get; set; } = 1;
    // Use Panel.Padding (available by default) instead of a custom Padding.
    public new Color FocusedBackgroundColor { get; set; } = Colors.LightBlue;
    public Color FocusedForegroundColor { get; set; } = Colors.White;
    public Color FocusedBorderColor { get; set; } = Colors.Blue;
    public new Color BorderColor { get; set; } = Colors.Black;
    public new Color BackgroundColor { get; set; } = Colors.White;
    public new Color ForegroundColor { get; set; } = Colors.Black;
    public new int Width { get; set; } = 100;
    public new int Height { get; set; } = 50;
    public HorizontalAlignment HorizontalAlignment { get; set; }
    public VerticalAlignment VerticalAlignment { get; set; }

    public event EventHandler<MouseEventArgs> MouseUp;
    public event EventHandler<MouseEventArgs> MouseDown;
    public event EventHandler<EventArgs> GotFocus;
    public event EventHandler<EventArgs> LostFocus;
    public event EventHandler<KeyEventArgs> KeyUp;
    public event EventHandler<KeyEventArgs> KeyDown;
    public event EventHandler<MouseEventArgs> MouseOver;
    public event EventHandler<MouseEventArgs> MouseLeave;
    public event EventHandler<MouseEventArgs> MouseEnter;
    public event EventHandler<EventArgs> TextChanged;

    // The hosted child—use the Panel’s Content property.
    public Control Child
    {
        get => Content;
        set
        {
            Content = value;
            if (value is Button || value is TextBox)
                BorderThickness = 0;
        }
    }

    bool _focused = false;

    public RoundedC(Control child = null)
    {
        if (child != null)
            Child = child;
        // (Panel already has a Padding property; set it here if desired)
        // Track focus via the standard events.
        this.GotFocus += (s, e) =>
        {
            _focused = true;
            UpdateBackground();
            GotFocus?.Invoke(this, e);
        };
        this.LostFocus += (s, e) =>
        {
            _focused = false;
            UpdateBackground();
            LostFocus?.Invoke(this, e);
        };
    }

    // Update the BackgroundImage by drawing the rounded rectangle into a Bitmap.
    void UpdateBackground()
    {
        if (Width > 0 && Height > 0)
        {
            using (var bmp = new Bitmap(Width, Height, PixelFormat.Format32bppRgba))
            {
                using (var g = new Graphics(bmp))
                {
                    var rect = new Rectangle(0, 0, Width, Height);
                    var bg = _focused ? FocusedBackgroundColor : BackgroundColor;
                    var border = _focused ? FocusedBorderColor : BorderColor;
                    g.FillRoundedRectangle(new SolidBrush(bg), rect, BorderRadius);
                    if (BorderThickness > 0)
                        g.DrawRoundedRectangle(new Pen(border, BorderThickness), rect, BorderRadius);
                }
                // Since BackgroundImage is writeable (the Bitmap is cloned internally), assign it directly.
                
                //BackgroundImage = bmp;
            }
        }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        // The Panel will lay out its single child using its Padding.
        if (Child != null)
        {
            // Let the layout system work; if necessary, update the child's size.
            var avail = new Size(Width - Padding.Horizontal, Height - Padding.Vertical);
            Child.Size = (Size)((Width > 0 && Height > 0) ? avail : Child.GetPreferredSize(Size.Empty));
        }
        UpdateBackground();
    }

    // (Optional: you can override focus-handling methods if you wish to update background there too.)
}
