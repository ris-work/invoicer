
using System;
using System.ComponentModel;
using System.Drawing;               // System.Drawing.Point/Rectangle/Size/SolidBrush
using System.Linq;
using System.Windows.Forms;         // Panel, VScrollBar, HScrollBar, PaintEventArgs, ScrollEventArgs
using Eto.Forms;
using Eto;
using CommonUi;

namespace MyApp
{
  static class ColorExtensions
  {
    public static System.Drawing.Color ToSystemColor(this Eto.Drawing.Color c)
      => System.Drawing.Color.FromArgb(c.Ab, c.Rb, c.Gb, c.Bb);
  }

  // -------------------------------------------------------------------
  // Your owner-drawn vertical scrollbar
  // -------------------------------------------------------------------
  public class ModernVScrollBar : System.Windows.Forms.VScrollBar
  {
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Eto.Drawing.Color ThumbColor { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Eto.Drawing.Color TrackColor { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Eto.Drawing.Color ArrowColor { get; set; }

    public ModernVScrollBar()
    {
      SetStyle(
        System.Windows.Forms.ControlStyles.UserPaint |
        System.Windows.Forms.ControlStyles.OptimizedDoubleBuffer,
        true);
      BackColor = ColorSettings.BackgroundColor.ToSystemColor();
    }

    protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
    {
      var g = e.Graphics;
      int w = Width, h = Height;
      g.Clear(BackColor);

      // Track
      int arrowH = System.Windows.Forms.SystemInformation.VerticalScrollBarArrowHeight;
      if (arrowH <= 0) arrowH = Math.Max(8, h / 10);
      var trackRect = new Rectangle(0, arrowH, w, h - 2 * arrowH);
      using (var br = new System.Drawing.SolidBrush(TrackColor.ToSystemColor()))
        g.FillRectangle(br, trackRect);

      // Thumb
      if (Maximum > 0)
      {
        double trackLen = trackRect.Height;
        double thumbLen = Math.Max(8.0,
          LargeChange / (double)(Maximum + LargeChange) * trackLen);
        double maxOff   = trackLen - thumbLen;
        double frac     = Value / (double)Maximum;
        double y        = trackRect.Top + frac * maxOff;
        var thumbRect   = new Rectangle(0, (int)y, w, (int)thumbLen);
        using (var br = new System.Drawing.SolidBrush(ThumbColor.ToSystemColor()))
          g.FillRectangle(br, thumbRect);
      }

      // Up arrow
      var upPts = new[]
      {
        new Point(w/2,      arrowH/4),
        new Point(w/4,      arrowH - arrowH/4),
        new Point(3*w/4,    arrowH - arrowH/4),
      };
      using (var br = new System.Drawing.SolidBrush(ArrowColor.ToSystemColor()))
        g.FillPolygon(br, upPts);

      // Down arrow
      var dnPts = new[]
      {
        new Point(w/2,      h - arrowH/4),
        new Point(w/4,      h - arrowH + arrowH/4),
        new Point(3*w/4,    h - arrowH + arrowH/4),
      };
      using (var br = new System.Drawing.SolidBrush(ArrowColor.ToSystemColor()))
        g.FillPolygon(br, dnPts);
    }
  }

  // -------------------------------------------------------------------
  // Your owner-drawn horizontal scrollbar
  // -------------------------------------------------------------------
  public class ModernHScrollBar : System.Windows.Forms.HScrollBar
  {
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Eto.Drawing.Color ThumbColor { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Eto.Drawing.Color TrackColor { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Eto.Drawing.Color ArrowColor { get; set; }

    public ModernHScrollBar()
    {
      SetStyle(
        System.Windows.Forms.ControlStyles.UserPaint |
        System.Windows.Forms.ControlStyles.OptimizedDoubleBuffer,
        true);
      BackColor = ColorSettings.BackgroundColor.ToSystemColor();
    }

    protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
    {
      var g = e.Graphics;
      int w = Width, h = Height;
      g.Clear(BackColor);

      // Track
      int arrowW = System.Windows.Forms.SystemInformation.HorizontalScrollBarArrowWidth;
      if (arrowW <= 0) arrowW = Math.Max(8, w / 10);
      var trackRect = new Rectangle(arrowW, 0, w - 2 * arrowW, h);
      using (var br = new System.Drawing.SolidBrush(TrackColor.ToSystemColor()))
        g.FillRectangle(br, trackRect);

      // Thumb
      if (Maximum > 0)
      {
        double trackLen = trackRect.Width;
        double thumbLen = Math.Max(8.0,
          LargeChange / (double)(Maximum + LargeChange) * trackLen);
        double maxOff   = trackLen - thumbLen;
        double frac     = Value / (double)Maximum;
        double x        = trackRect.Left + frac * maxOff;
        var thumbRect   = new Rectangle((int)x, 0, (int)thumbLen, h);
        using (var br = new System.Drawing.SolidBrush(ThumbColor.ToSystemColor()))
          g.FillRectangle(br, thumbRect);
      }

      // Left arrow
      var lfPts = new[]
      {
        new Point(arrowW/4,      h/2),
        new Point(arrowW - arrowW/4,  h/4),
        new Point(arrowW - arrowW/4,  3*h/4),
      };
      using (var br = new System.Drawing.SolidBrush(ArrowColor.ToSystemColor()))
        g.FillPolygon(br, lfPts);

      // Right arrow
      var rtPts = new[]
      {
        new Point(w - arrowW/4,      h/2),
        new Point(w - arrowW + arrowW/4,  h/4),
        new Point(w - arrowW + arrowW/4,  3*h/4),
      };
      using (var br = new System.Drawing.SolidBrush(ArrowColor.ToSystemColor()))
        g.FillPolygon(br, rtPts);
    }
  }

  // -------------------------------------------------------------------
  // Extension to inject ModernV/HScrollBar into an Eto.Forms.Scrollable
  // -------------------------------------------------------------------
  public static class ScrollableExtensions
  {
    public static void UseModernScrollbars(
      this Eto.Forms.Scrollable scrollable,
      Eto.Drawing.Color thumbColor,
      Eto.Drawing.Color trackColor,
      Eto.Drawing.Color arrowColor,
      int thickness = 12)
    {
      // only on WinForms backend
      var wfId = Platform.Get(Platforms.WinForms)?.ToString();
      if (Platform.Instance.ToString() != wfId)
      {
        Console.Error.WriteLine("[ERR] Skipping – not WinForms backend");
        return;
      }
      Console.Error.WriteLine("[ERR] WinForms backend confirmed");

      // prevent Eto from auto‐expanding content
      scrollable.ExpandContentWidth  = false;
      scrollable.ExpandContentHeight = false;

      // grab native panel
      var panel = scrollable.ControlObject as System.Windows.Forms.Panel;
      if (panel == null)
      {
        Console.Error.WriteLine("[ERR] scrollable.ControlObject is not Panel");
        return;
      }
      // disable the built‐in WinForms scrollbars
      panel.AutoScroll = false;

      bool injected = false;
      ModernVScrollBar vbar = null;
      ModernHScrollBar hbar = null;
      bool syncing = false;

      // union child‐bounds to find the real content size
      System.Drawing.Size GetContentSize()
      {
        var union = System.Drawing.Rectangle.Empty;
        foreach (System.Windows.Forms.Control c in panel.Controls)
        {
          if (c is ModernVScrollBar || c is ModernHScrollBar) continue;
          union = System.Drawing.Rectangle.Union(union, c.Bounds);
        }
        return union.Size;
      }

      // recompute ranges/enablement/initial values
      void UpdateRanges()
      {
        var contentSize = GetContentSize();
        var visibleSize = panel.ClientSize;
        Console.Error.WriteLine(
          $"[ERR] Content={contentSize.Width}×{contentSize.Height}, Viewport={visibleSize.Width}×{visibleSize.Height}");

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

        Console.Error.WriteLine(
          $"[ERR] Ranges → VMax={vbar.Maximum}, VPage={vbar.LargeChange}, " +
          $"HMax={hbar.Maximum}, HPage={hbar.LargeChange}");
      }

      // inject as soon as the panel is parented & has a handle (or resized)
      void TryInject(object s, EventArgs e)
      {
        Console.Error.WriteLine(
          $"[ERR] TryInject – parent? {panel.Parent!=null}, handle? {panel.IsHandleCreated}");
        if (injected || panel.Parent == null || !panel.IsHandleCreated) return;
        injected = true;

        panel.ParentChanged    -= TryInject;
        panel.HandleCreated    -= TryInject;
        panel.SizeChanged      -= TryInject;
        scrollable.SizeChanged -= TryInject;

        Console.Error.WriteLine("[ERR] Injecting modern scrollbars");
        var parent = panel.Parent;

        // create & dock
        vbar = new ModernVScrollBar
        {
          Dock       = DockStyle.Right,
          Width      = thickness,
          ThumbColor = thumbColor,
          TrackColor = trackColor,
          ArrowColor = arrowColor
        };
        hbar = new ModernHScrollBar
        {
          Dock       = DockStyle.Bottom,
          Height     = thickness,
          ThumbColor = thumbColor,
          TrackColor = trackColor,
          ArrowColor = arrowColor
        };

        parent.SuspendLayout();
          parent.Controls.Add(vbar);
          parent.Controls.Add(hbar);
        parent.ResumeLayout();

        vbar.BringToFront();
        hbar.BringToFront();

        // WinForms → Eto
        vbar.Scroll += (vs, ve) =>
        {
          Console.Error.WriteLine($"[ERR] vbar.Scroll → NewValue={ve.NewValue}");
          if (syncing) return;
          syncing = true;
          scrollable.ScrollPosition = new Eto.Drawing.Point(
            scrollable.ScrollPosition.X, ve.NewValue);
          syncing = false;
        };
        hbar.Scroll += (hs, he) =>
        {
          Console.Error.WriteLine($"[ERR] hbar.Scroll → NewValue={he.NewValue}");
          if (syncing) return;
          syncing = true;
          scrollable.ScrollPosition = new Eto.Drawing.Point(
            he.NewValue, scrollable.ScrollPosition.Y);
          syncing = false;
        };

        // Eto → WinForms
        scrollable.Scroll += (ss, se) =>
        {
          var p = scrollable.ScrollPosition;
          Console.Error.WriteLine($"[ERR] Eto.Scroll → {p}");
          if (syncing) return;
          syncing = true;
          vbar.Value = Math.Min(vbar.Maximum, p.Y);
          hbar.Value = Math.Min(hbar.Maximum, p.X);
          syncing = false;
        };

        // recalc on size or content‐control changes
        panel.SizeChanged      += (ss, ea) => { Console.Error.WriteLine("[ERR] panel.SizeChanged"); UpdateRanges(); };
        scrollable.SizeChanged += (ss, ea) => { Console.Error.WriteLine("[ERR] scrollable.SizeChanged"); UpdateRanges(); };
        panel.ControlAdded     += (ss, ea) => { Console.Error.WriteLine("[ERR] panel.ControlAdded"); UpdateRanges(); };
        panel.ControlRemoved   += (ss, ea) => { Console.Error.WriteLine("[ERR] panel.ControlRemoved"); UpdateRanges(); };

        // initial
        UpdateRanges();
        var pos = scrollable.ScrollPosition;
        vbar.Value = Math.Min(vbar.Maximum, pos.Y);
        hbar.Value = Math.Min(hbar.Maximum, pos.X);
        Console.Error.WriteLine(
          $"[ERR] Initial thumb → v={vbar.Value}, h={hbar.Value}");
      }

      panel.ParentChanged    += TryInject;
      panel.HandleCreated    += TryInject;
      panel.SizeChanged      += TryInject;
      scrollable.SizeChanged += TryInject;
      TryInject(panel, EventArgs.Empty);
    }
  }
}
