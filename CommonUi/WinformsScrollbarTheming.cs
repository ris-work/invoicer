// File: CustomDrawableScrollbars.cs
using System;
using System.Linq;
using SD = System.Drawing;            // System.Drawing.Point, Size, Rectangle
using SWF = System.Windows.Forms;     // WinForms.Panel, SystemInformation, Control, DockStyle
using Eto.Forms;                      // Scrollable, Drawable, MouseEventArgs
using ED = Eto.Drawing;               // Graphics, Color, Point, PointF, Size, Rectangle
using Eto;                  // Platforms, Platform

namespace MyApp
{
  public static class ScrollableExtensions
  {
    /// <summary>
    /// Replaces WinForms' built-in scrollbars on an Eto.Forms.Scrollable
    /// with fully custom vector‐drawn Eto.Forms.Drawable bars.
    /// Logs every event to Console.Error for debugging.
    /// </summary>
    public static void UseModernDrawnScrollbars(
      this Scrollable scrollable,
      ED.Color thumbColor,
      ED.Color trackColor,
      ED.Color arrowColor,
      int scrollbarSize,
      int arrowSize)
    {
      // 1) Only on WinForms backend
      var wfId = Platform.Get(Platforms.WinForms)?.ToString();
      Console.Error.WriteLine($"[DBG] Platform.Instance={Platform.Instance}, WinFormsID={wfId}");
      if (Platform.Instance.ToString() != wfId)
      {
        Console.Error.WriteLine("[DBG] Not WinForms – skipping custom scrollbars");
        return;
      }
      Console.Error.WriteLine("[DBG] WinForms backend – injecting custom scrollbars");

      // 2) Grab native WinForms.Panel inside the Eto Scrollable
      var panel = scrollable.ControlObject as SWF.Panel;
      if (panel == null)
      {
        Console.Error.WriteLine("[ERR] ControlObject is not WinForms.Panel");
        return;
      }
      // hide native
      panel.AutoScroll = false;

      // log layout changes
      panel.Resize            += (s,e)=> Console.Error.WriteLine("[DBG] panel.Resize");
      scrollable.SizeChanged  += (s,e)=> Console.Error.WriteLine("[DBG] scrollable.SizeChanged");
      panel.ControlAdded      += (s,e)=> Console.Error.WriteLine("[DBG] panel.ControlAdded");
      panel.ControlRemoved    += (s,e)=> Console.Error.WriteLine("[DBG] panel.ControlRemoved");

      bool injected = false;
      EtoVScrollBar vbar = null;
      EtoHScrollBar hbar = null;
      bool syncing = false;

      // Recompute ranges + redraw
      void UpdateRanges()
      {
        Console.Error.WriteLine("[DBG] UpdateRanges");
        // find content union
        var union = SD.Rectangle.Empty;
        foreach (SWF.Control c in panel.Controls)
        {
          var nv = vbar?.ControlObject as SWF.Control;
          var nh = hbar?.ControlObject as SWF.Control;
          if (c == nv || c == nh) continue;
          union = SD.Rectangle.Union(union, c.Bounds);
        }
        var content = union.Size;
        var visible = panel.ClientSize;
        Console.Error.WriteLine($"[DBG] content={content.Width}×{content.Height}, visible={visible.Width}×{visible.Height}");

        // vertical
        vbar.Minimum     = 0;
        vbar.LargeChange = visible.Height;
        vbar.Maximum     = Math.Max(0, content.Height - visible.Height);
        vbar.Enabled     = vbar.Maximum > 0;
        // horizontal
        hbar.Minimum     = 0;
        hbar.LargeChange = visible.Width;
        hbar.Maximum     = Math.Max(0, content.Width - visible.Width);
        hbar.Enabled     = hbar.Maximum > 0;

        Console.Error.WriteLine(
          $"[DBG] Ranges → VMax={vbar.Maximum}, VPage={vbar.LargeChange}, " +
          $"HMax={hbar.Maximum}, HPage={hbar.LargeChange}");

        // sync thumb positions
        vbar.Value = Math.Min(vbar.Maximum, scrollable.ScrollPosition.Y);
        hbar.Value = Math.Min(hbar.Maximum, scrollable.ScrollPosition.X);
        vbar.Invalidate();
        hbar.Invalidate();
      }

      // Try injection once panel is parented & ready
      void TryInject(object s, EventArgs e)
      {
        Console.Error.WriteLine(
          $"[DBG] TryInject – parent?{panel.Parent!=null}, handle?{panel.IsHandleCreated}");
        if (injected || panel.Parent == null || !panel.IsHandleCreated) return;
        injected = true;

        panel.ParentChanged    -= TryInject;
        panel.HandleCreated    -= TryInject;
        panel.SizeChanged      -= TryInject;
        scrollable.SizeChanged -= TryInject;

        Console.Error.WriteLine("[DBG] Creating Eto-drawn scrollbars");
        vbar = new EtoVScrollBar
        {
          ThumbColor     = thumbColor,
          TrackColor     = trackColor,
          ArrowColor     = arrowColor,
          ScrollbarWidth = scrollbarSize,
          ArrowSize      = arrowSize
        };
        hbar = new EtoHScrollBar
        {
          ThumbColor      = thumbColor,
          TrackColor      = trackColor,
          ArrowColor      = arrowColor,
          ScrollbarHeight = scrollbarSize,
          ArrowSize       = arrowSize
        };

        var nativeV = vbar.ControlObject as SWF.Control;
        var nativeH = hbar.ControlObject as SWF.Control;
        var parent  = panel.Parent;

        // Dock before adding for WinForms layout
        nativeV.Dock = SWF.DockStyle.Right;
        nativeH.Dock = SWF.DockStyle.Bottom;

        parent.SuspendLayout();
          parent.Controls.Add(nativeV);
          parent.Controls.Add(nativeH);
        parent.ResumeLayout(true);

        nativeV.BringToFront();
        nativeH.BringToFront();

        // WinForms → Eto.Scrollable
        vbar.ValueChanged += (vs, ve) =>
        {
          if (syncing) return;
          syncing = true;
          Console.Error.WriteLine($"[DBG] vbar.ValueChanged → {vbar.Value}");
          scrollable.ScrollPosition = new ED.Point(scrollable.ScrollPosition.X, vbar.Value);
          syncing = false;
        };
        hbar.ValueChanged += (hs, he) =>
        {
          if (syncing) return;
          syncing = true;
          Console.Error.WriteLine($"[DBG] hbar.ValueChanged → {hbar.Value}");
          scrollable.ScrollPosition = new ED.Point(hbar.Value, scrollable.ScrollPosition.Y);
          syncing = false;
        };

        // Eto.Scrollable → WinForms-drawn
        scrollable.Scroll += (ss, se) =>
        {
          if (syncing) return;
          syncing = true;
          Console.Error.WriteLine($"[DBG] scrollable.Scroll → {scrollable.ScrollPosition}");
          vbar.Value = Math.Min(vbar.Maximum, scrollable.ScrollPosition.Y);
          hbar.Value = Math.Min(hbar.Maximum, scrollable.ScrollPosition.X);
          syncing = false;
        };

        // recalc on changes
        panel.SizeChanged      += (ss, ea) => UpdateRanges();
        panel.ControlAdded     += (ss, ea) => UpdateRanges();
        panel.ControlRemoved   += (ss, ea) => UpdateRanges();
        scrollable.SizeChanged += (ss, ea) => UpdateRanges();

        // initial
        UpdateRanges();
      }

      panel.ParentChanged    += TryInject;
      panel.HandleCreated    += TryInject;
      panel.SizeChanged      += TryInject;
      scrollable.SizeChanged += TryInject;
      TryInject(panel, EventArgs.Empty);
    }
  }

  // ────────────────────────────────────────────────────────────────
  // Eto-drawn vertical scrollbar
  // ────────────────────────────────────────────────────────────────
  public class EtoVScrollBar : Drawable
  {
    public ED.Color ThumbColor     { get; set; }
    public ED.Color TrackColor     { get; set; }
    public ED.Color ArrowColor     { get; set; }
    public int     ScrollbarWidth  { get; set; } = 50;
    public int     ArrowSize       { get; set; } = 25;

    public int Minimum     { get; set; }
    public int Maximum     { get; set; }
    public int LargeChange { get; set; }
    public int Value       { get; set; }

    public event EventHandler ValueChanged;

    bool        _dragging;
    ED.PointF   _dragStart;
    double      _valueStart;

    public EtoVScrollBar()
    {
      BackgroundColor = ED.Colors.Transparent;
      Size            = new ED.Size(ScrollbarWidth, 0);
      MouseDown       += OnMouseDown;
      MouseMove       += OnMouseMove;
      MouseUp         += OnMouseUp;
      Paint           += OnPaint;
    }

    void OnMouseDown(object s, MouseEventArgs e)
    {
      var pt = new ED.Point((int)e.Location.X, (int)e.Location.Y);
      Console.Error.WriteLine($"[V] MouseDown {pt}");
      int w = Width, h = Height, a = ArrowSize;

      var up    = new ED.Rectangle(0, 0, w, a);
      var down  = new ED.Rectangle(0, h - a, w, a);
      var track = new ED.Rectangle(0, a, w, h - 2*a);
      var thumb = ComputeThumbRect();

      if (up.Contains(pt))
      {
        Console.Error.WriteLine("[V] Up-arrow");
        ScrollLine(-1); return;
      }
      if (down.Contains(pt))
      {
        Console.Error.WriteLine("[V] Down-arrow");
        ScrollLine(+1); return;
      }
      if (track.Contains(pt))
      {
        if (thumb.Contains(pt))
        {
          Console.Error.WriteLine("[V] Start drag");
          _dragging   = true;
          _dragStart  = e.Location;
          _valueStart = Value;
          return;
        }
        if (pt.Y < thumb.Y)
        {
          Console.Error.WriteLine("[V] PageUp");
          ScrollPage(-1);
        }
        else
        {
          Console.Error.WriteLine("[V] PageDown");
          ScrollPage(+1);
        }
      }
    }

    void OnMouseMove(object s, MouseEventArgs e)
    {
      if (!_dragging || Maximum <= 0) return;
      var pt = new ED.Point((int)e.Location.X, (int)e.Location.Y);
      Console.Error.WriteLine($"[V] MouseMove {pt}");
      float dy = e.Location.Y - _dragStart.Y;
      int a = ArrowSize;
      double trackLen = Height - 2*a;
      var delta = dy*(Maximum - Minimum)/trackLen;
      SetValue((int)Math.Round(_valueStart + delta));
    }

    void OnMouseUp(object s, MouseEventArgs e)
    {
      if (_dragging) Console.Error.WriteLine("[V] End drag");
      _dragging = false;
    }

    ED.Rectangle ComputeThumbRect()
    {
      if (Maximum <= 0) return new ED.Rectangle();
      int w = Width, h = Height, a = ArrowSize;
      double trackLen = h - 2*a;
      double thumbLen = Math.Max(8.0, LargeChange/(double)(Maximum+LargeChange)*trackLen);
      double maxOff   = trackLen - thumbLen;
      double y        = a + (Value/(double)Maximum)*maxOff;
      var r = new ED.Rectangle(0, (int)Math.Round(y), w, (int)Math.Round(thumbLen));
      Console.Error.WriteLine($"[V] ComputeThumbRect → {r}");
      return r;
    }

    void ScrollLine(int dir) => SetValue(Value + dir * Math.Max(1, LargeChange/10));
    void ScrollPage(int dir) => SetValue(Value + dir * LargeChange);

    void SetValue(int v)
    {
      int nv = Math.Max(Minimum, Math.Min(Maximum, v));
      if (nv == Value) return;
      Value = nv;
      Console.Error.WriteLine($"[V] ValueChanged → {Value}");
      Invalidate();
      ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    void OnPaint(object s, PaintEventArgs pe)
    {
      var g = pe.Graphics;
      int w = Width, h = Height, a = ArrowSize;

      // track background
      g.FillRectangle(TrackColor, new ED.Rectangle(0, 0, w, h));

      // thumb
      var thumb = ComputeThumbRect();
      if (!thumb.IsEmpty)
        g.FillRectangle(ThumbColor, thumb);

      // up arrow
      var upPts = new[]
      {
        new ED.PointF(w/2f,     a/4f),
        new ED.PointF(a/4f,     a - a/4f),
        new ED.PointF(w - a/4f, a - a/4f),
      };
      g.FillPolygon(ArrowColor, upPts);

      // down arrow
      var dnPts = new[]
      {
        new ED.PointF(w/2f,        h - a/4f),
        new ED.PointF(a/4f,        h - a + a/4f),
        new ED.PointF(w - a/4f,    h - a + a/4f),
      };
      g.FillPolygon(ArrowColor, dnPts);
    }
  }

  // ────────────────────────────────────────────────────────────────
  // Eto-drawn horizontal scrollbar
  // ────────────────────────────────────────────────────────────────
  public class EtoHScrollBar : Drawable
  {
    public ED.Color ThumbColor      { get; set; }
    public ED.Color TrackColor      { get; set; }
    public ED.Color ArrowColor      { get; set; }
    public int     ScrollbarHeight  { get; set; } = 50;
    public int     ArrowSize        { get; set; } = 25;

    public int Minimum     { get; set; }
    public int Maximum     { get; set; }
    public int LargeChange { get; set; }
    public int Value       { get; set; }

    public event EventHandler ValueChanged;

    bool        _dragging;
    ED.PointF   _dragStart;
    double      _valueStart;

    public EtoHScrollBar()
    {
      BackgroundColor = ED.Colors.Transparent;
      Size            = new ED.Size(0, ScrollbarHeight);
      MouseDown       += OnMouseDown;
      MouseMove       += OnMouseMove;
      MouseUp         += OnMouseUp;
      Paint           += OnPaint;
    }

    void OnMouseDown(object s, MouseEventArgs e)
    {
      var pt = new ED.Point((int)e.Location.X, (int)e.Location.Y);
      Console.Error.WriteLine($"[H] MouseDown {pt}");
      int w = Width, h = Height, a = ArrowSize;

      var leftArrow  = new ED.Rectangle(0, 0, a, h);
      var rightArrow = new ED.Rectangle(w - a, 0, a, h);
      var trackRect  = new ED.Rectangle(a, 0, w - 2*a, h);
      var thumbRect  = ComputeThumbRect();

      if (leftArrow.Contains(pt))
      {
        Console.Error.WriteLine("[H] Left-arrow clicked");
        ScrollLine(-1); return;
      }
      if (rightArrow.Contains(pt))
      {
        Console.Error.WriteLine("[H] Right-arrow clicked");
        ScrollLine(+1); return;
      }
      if (trackRect.Contains(pt))
      {
        if (thumbRect.Contains(pt))
        {
          Console.Error.WriteLine("[H] Start drag at " + pt);
          _dragging   = true;
          _dragStart  = e.Location;
          _valueStart = Value;
        }
        else if (pt.X < thumbRect.X)
        {
          Console.Error.WriteLine("[H] PageLeft");
          ScrollPage(-1);
        }
        else
        {
          Console.Error.WriteLine("[H] PageRight");
          ScrollPage(+1);
        }
      }
    }

    void OnMouseMove(object s, MouseEventArgs e)
    {
      if (!_dragging || Maximum <= 0) return;
      var pt = new ED.Point((int)e.Location.X, (int)e.Location.Y);
      Console.Error.WriteLine($"[H] MouseMove {pt}");
      float dx = e.Location.X - _dragStart.X;
      int a    = ArrowSize;
      double trackLen = Width - 2*a;
      double delta    = dx*(Maximum - Minimum)/trackLen;
      SetValue((int)Math.Round(_valueStart + delta));
    }

    void OnMouseUp(object s, MouseEventArgs e)
    {
      if (_dragging) Console.Error.WriteLine("[H] End drag");
      _dragging = false;
    }

    ED.Rectangle ComputeThumbRect()
    {
      if (Maximum <= 0) return new ED.Rectangle();
      int w = Width, h = Height, a = ArrowSize;
      double trackLen = w - 2*a;
      double thumbLen = Math.Max(8.0, LargeChange/(double)(Maximum+LargeChange)*trackLen);
      double maxOff   = trackLen - thumbLen;
      double frac     = Value/(double)Maximum;
      double x        = a + frac*maxOff;
      var r = new ED.Rectangle((int)Math.Round(x), 0, (int)Math.Round(thumbLen), h);
      Console.Error.WriteLine($"[H] ComputeThumbRect → {r}");
      return r;
    }

    void ScrollLine(int dir) => SetValue(Value + dir * Math.Max(1, LargeChange/10));
    void ScrollPage(int dir) => SetValue(Value + dir * LargeChange);

    void SetValue(int v)
    {
      int nv = Math.Max(Minimum, Math.Min(Maximum, v));
      if (nv == Value) return;
      Value = nv;
      Console.Error.WriteLine($"[H] ValueChanged → {Value}");
      Invalidate();
      ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    void OnPaint(object s, PaintEventArgs pe)
    {
      var g = pe.Graphics;
      int w = Width, h = Height, a = ArrowSize;

      // track background
      g.FillRectangle(TrackColor, new ED.Rectangle(0, 0, w, h));

      // thumb
      var thumb = ComputeThumbRect();
      if (!thumb.IsEmpty)
        g.FillRectangle(ThumbColor, thumb);

      // left‐arrow
      var lfPts = new[]
      {
        new ED.PointF(a/4f,      h/2f),
        new ED.PointF(a - a/4f,  h/4f),
        new ED.PointF(a - a/4f, 3f*h/4f),
      };
      g.FillPolygon(ArrowColor, lfPts);

      // right‐arrow
      var rtPts = new[]
      {
        new ED.PointF(w - a/4f,      h/2f),
        new ED.PointF(w - a + a/4f,  h/4f),
        new ED.PointF(w - a + a/4f,3f*h/4f),
      };
      g.FillPolygon(ArrowColor, rtPts);
    }
  }
}