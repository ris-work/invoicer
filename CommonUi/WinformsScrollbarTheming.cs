// File: CustomDrawableScrollbars.cs

using System;
using System.Linq;
using SD = System.Drawing;           // System.Drawing types
using SWF = System.Windows.Forms;     // WinForms types
using Eto.Forms;                      // Eto.Forms.Scrollable, Drawable, MouseEventArgs, MouseButtons
using EP = Eto;             // Eto.Platform.Get
using ED = Eto.Drawing;               // Eto.Drawing.Graphics, Color, Point, PointF, Size, Rectangle
using Eto;

namespace MyApp
{
  static class ColorExtensions
  {
    public static SD.Color ToSystemColor(this ED.Color c)
      => SD.Color.FromArgb(c.Ab, c.Rb, c.Gb, c.Bb);

    public static ED.Color ToEtoColor(this SD.Color c)
      => ED.Color.FromArgb(c.A, c.R, c.G, c.B);
  }

  // ==================================================================
  // Eto‐drawn vertical scrollbar
  // ==================================================================
  public class EtoVScrollBar : Drawable
  {
    public ED.Color ThumbColor     { get; set; }
    public ED.Color TrackColor     { get; set; }
    public ED.Color ArrowColor     { get; set; }
    public int     ScrollbarWidth  { get; set; }
    public int     ArrowButtonSize { get; set; }

    public int Minimum     { get; set; }
    public int Maximum     { get; set; }
    public int LargeChange { get; set; }
    public int Value       { get; set; }

    public event EventHandler ValueChanged;

    bool            _dragging;
    ED.PointF       _dragStart;
    double          _valueStart;

    public EtoVScrollBar()
    {
      ScrollbarWidth  = 12;
      ArrowButtonSize = 16;
      Size            = new ED.Size(ScrollbarWidth, 0);

      MouseDown += (object s, MouseEventArgs e) =>
      {
        if (e.Buttons.HasFlag(MouseButtons.Primary))
        {
          _dragging   = true;
          _dragStart  = e.Location;   // Eto.Drawing.PointF
          _valueStart = Value;
        }
      };
      MouseMove += (object s, MouseEventArgs e) =>
      {
        if (_dragging && Maximum > 0 && e.Buttons.HasFlag(MouseButtons.Primary))
        {
          float dy = e.Location.Y - _dragStart.Y;
          float trackLen = Height - ArrowButtonSize*2;
          if (trackLen <= 0) return;
          double delta = dy*(Maximum - Minimum)/trackLen;
          Value = (int)Math.Max(Minimum,
                    Math.Min(Maximum,
                      Math.Round(_valueStart + delta)));
          Invalidate();
          ValueChanged?.Invoke(this, EventArgs.Empty);
        }
      };
      MouseUp += (object s, MouseEventArgs e) => { _dragging = false; };
    }

    protected override void OnPaint(Eto.Forms.PaintEventArgs pe)
    {
      var g = pe.Graphics;
      int w = Width, h = Height;

      // clear
      g.Clear(BackgroundColor);

      // arrow region
      int a = ArrowButtonSize > 0 ? ArrowButtonSize
        : SWF.SystemInformation.VerticalScrollBarArrowHeight;
      if (a <= 0) a = Math.Max(8, h/10);

      // track
      var trackRect = new ED.Rectangle(0, a, w, h - a*2);
      g.FillRectangle(TrackColor, trackRect);

      // thumb
      if (Maximum > 0)
      {
        double trackLen = trackRect.Height;
        double thumbLen = Math.Max(8.0,
          LargeChange/(double)(Maximum + LargeChange)*trackLen);
        double maxOff   = trackLen - thumbLen;
        double frac     = Value/(double)Maximum;
        double y        = trackRect.Y + frac*maxOff;
        var thumbRect   = new ED.Rectangle(0, (int)y, w, (int)thumbLen);
        g.FillRectangle(ThumbColor, thumbRect);
      }

      // up arrow
      var upPts = new[]
      {
        new ED.PointF(w/2f,    a/4f),
        new ED.PointF(w/4f,    a - a/4f),
        new ED.PointF(3f*w/4f, a - a/4f),
      };
      g.FillPolygon(ArrowColor, upPts);

      // down arrow
      var dnPts = new[]
      {
        new ED.PointF(w/2f,         h - a/4f),
        new ED.PointF(w/4f,         h - a + a/4f),
        new ED.PointF(3f*w/4f,      h - a + a/4f),
      };
      g.FillPolygon(ArrowColor, dnPts);
    }
  }

  // ==================================================================
  // Eto‐drawn horizontal scrollbar
  // ==================================================================
  public class EtoHScrollBar : Drawable
  {
    public ED.Color ThumbColor      { get; set; }
    public ED.Color TrackColor      { get; set; }
    public ED.Color ArrowColor      { get; set; }
    public int     ScrollbarHeight  { get; set; }
    public int     ArrowButtonSize  { get; set; }

    public int Minimum     { get; set; }
    public int Maximum     { get; set; }
    public int LargeChange { get; set; }
    public int Value       { get; set; }

    public event EventHandler ValueChanged;

    bool            _dragging;
    ED.PointF       _dragStart;
    double          _valueStart;

    public EtoHScrollBar()
    {
      ScrollbarHeight = 12;
      ArrowButtonSize = 16;
      Size             = new ED.Size(0, ScrollbarHeight);

      MouseDown += (object s, MouseEventArgs e) =>
      {
        if (e.Buttons.HasFlag(MouseButtons.Primary))
        {
          _dragging   = true;
          _dragStart  = e.Location;  // Eto.Drawing.PointF
          _valueStart = Value;
        }
      };
      MouseMove += (object s, MouseEventArgs e) =>
      {
        if (_dragging && Maximum > 0 && e.Buttons.HasFlag(MouseButtons.Primary))
        {
          float dx = e.Location.X - _dragStart.X;
          float trackLen = Width - ArrowButtonSize*2;
          if (trackLen <= 0) return;
          double delta = dx*(Maximum - Minimum)/trackLen;
          Value = (int)Math.Max(Minimum,
                    Math.Min(Maximum,
                      Math.Round(_valueStart + delta)));
          Invalidate();
          ValueChanged?.Invoke(this, EventArgs.Empty);
        }
      };
      MouseUp += (object s, MouseEventArgs e) => { _dragging = false; };
    }

    protected override void OnPaint(Eto.Forms.PaintEventArgs pe)
    {
      var g = pe.Graphics;
      int w = Width, h = Height;

      g.Clear(BackgroundColor);

      int a = ArrowButtonSize > 0 ? ArrowButtonSize
        : SWF.SystemInformation.HorizontalScrollBarArrowWidth;
      if (a <= 0) a = Math.Max(8, w/10);

      // track
      var trackRect = new ED.Rectangle(a, 0, w - a*2, h);
      g.FillRectangle(TrackColor, trackRect);

      // thumb
      if (Maximum > 0)
      {
        double trackLen = trackRect.Width;
        double thumbLen = Math.Max(8.0,
          LargeChange/(double)(Maximum + LargeChange)*trackLen);
        double maxOff   = trackLen - thumbLen;
        double frac     = Value/(double)Maximum;
        double x        = trackRect.X + frac*maxOff;
        var thumbRect   = new ED.Rectangle((int)x, 0, (int)thumbLen, h);
        g.FillRectangle(ThumbColor, thumbRect);
      }

      // left arrow
      var lfPts = new[]
      {
        new ED.PointF(a/4f,    h/2f),
        new ED.PointF(a - a/4f,  h/4f),
        new ED.PointF(a - a/4f,  3f*h/4f),
      };
      g.FillPolygon(ArrowColor, lfPts);

      // right arrow
      var rtPts = new[]
      {
        new ED.PointF(w - a/4f,    h/2f),
        new ED.PointF(w - a + a/4f,  h/4f),
        new ED.PointF(w - a + a/4f,  3f*h/4f),
      };
      g.FillPolygon(ArrowColor, rtPts);
    }
  }

  // ==================================================================
  // Extension to inject these drawables into a WinForms Scrollable
  // ==================================================================
  public static class ScrollableExtensions
  {
    public static void UseModernDrawnScrollbars(
      this Scrollable scrollable,
      ED.Color thumbColor,
      ED.Color trackColor,
      ED.Color arrowColor,
      int scrollbarSize,
      int arrowButtonSize)
    {
      // only on WinForms backend
      var wfId = EP.Platform.Get(Platforms.WinForms)?.ToString();
      if (Platform.Instance.ToString() != wfId) return;

      var panel = scrollable.ControlObject as SWF.Panel;
      if (panel == null) return;
      panel.AutoScroll = false;

      bool injected = false;
      EtoVScrollBar vbar = null;
      EtoHScrollBar hbar = null;
      bool syncing = false;

      void UpdateRanges()
      {
        var union = SD.Rectangle.Empty;
        foreach (SWF.Control c in panel.Controls)
        {
          var nv = vbar?.ControlObject as SWF.Control;
          var nh = hbar?.ControlObject as SWF.Control;
          if (c == nv || c == nh) continue;
          union = SD.Rectangle.Union(union, c.Bounds);
        }
        var contentSize = union.Size;
        var visibleSize = panel.ClientSize;

        // vertical
        vbar.Minimum     = 0;
        vbar.LargeChange = visibleSize.Height;
        vbar.Maximum     = Math.Max(0, contentSize.Height - visibleSize.Height);
        vbar.Enabled     = vbar.Maximum > 0;

        // horizontal
        hbar.Minimum     = 0;
        hbar.LargeChange = visibleSize.Width;
        hbar.Maximum     = Math.Max(0, contentSize.Width - visibleSize.Width);
        hbar.Enabled     = hbar.Maximum > 0;

        // sync thumbs
        vbar.Value = Math.Min(vbar.Maximum, scrollable.ScrollPosition.Y);
        hbar.Value = Math.Min(hbar.Maximum, scrollable.ScrollPosition.X);

        vbar.Invalidate();
        hbar.Invalidate();
      }

      void TryInject(object s, System.EventArgs e)
      {
        if (injected) return;
        if (panel.Parent == null || !panel.IsHandleCreated) return;
        injected = true;

        panel.ParentChanged    -= TryInject;
        panel.HandleCreated    -= TryInject;
        panel.SizeChanged      -= TryInject;
        scrollable.SizeChanged -= TryInject;

        vbar = new EtoVScrollBar
        {
          BackgroundColor  = scrollable.BackgroundColor,
          ThumbColor       = thumbColor,
          TrackColor       = trackColor,
          ArrowColor       = arrowColor,
          ScrollbarWidth   = scrollbarSize,
          ArrowButtonSize  = arrowButtonSize
        };
        hbar = new EtoHScrollBar
        {
          BackgroundColor  = scrollable.BackgroundColor,
          ThumbColor       = thumbColor,
          TrackColor       = trackColor,
          ArrowColor       = arrowColor,
          ScrollbarHeight  = scrollbarSize,
          ArrowButtonSize  = arrowButtonSize
        };

        var nativeV = vbar.ControlObject as SWF.Control;
        var nativeH = hbar.ControlObject as SWF.Control;
        var parent  = panel.Parent;

        parent.SuspendLayout();
          parent.Controls.Add(nativeV);
          parent.Controls.Add(nativeH);
          nativeV.BringToFront();
          nativeH.BringToFront();
        parent.ResumeLayout(true);

        nativeV.Dock = SWF.DockStyle.Right;
        nativeH.Dock = SWF.DockStyle.Bottom;

        // WinForms → Eto.Forms.Scrollable
        vbar.ValueChanged += (vs, ve) =>
        {
          if (syncing) return;
          syncing = true;
          scrollable.ScrollPosition =
            new ED.Point(scrollable.ScrollPosition.X, vbar.Value);
          syncing = false;
        };
        hbar.ValueChanged += (hs, he) =>
        {
          if (syncing) return;
          syncing = true;
          scrollable.ScrollPosition =
            new ED.Point(hbar.Value, scrollable.ScrollPosition.Y);
          syncing = false;
        };

        // Eto.Forms.Scrollable → WinForms‐drawn
        scrollable.Scroll += (ss, se) =>
        {
          if (syncing) return;
          syncing = true;
          vbar.Value = Math.Min(vbar.Maximum, scrollable.ScrollPosition.Y);
          hbar.Value = Math.Min(hbar.Maximum, scrollable.ScrollPosition.X);
          syncing = false;
        };

        panel.SizeChanged      += (ss, ea2) => UpdateRanges();
        panel.ControlAdded     += (ss, ea2) => UpdateRanges();
        panel.ControlRemoved   += (ss, ea2) => UpdateRanges();
        scrollable.SizeChanged += (ss, ea2) => UpdateRanges();

        UpdateRanges();
      }

      panel.ParentChanged    += TryInject;
      panel.HandleCreated    += TryInject;
      panel.SizeChanged      += TryInject;
      scrollable.SizeChanged += TryInject;
      TryInject(panel, System.EventArgs.Empty);
    }
  }
}
