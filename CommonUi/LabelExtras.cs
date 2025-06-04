using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using Eto.Drawing;
using Eto.Forms;
using Microsoft.Maui.Platform;

namespace CommonUi
{
    public class RoundedDrawable : Drawable
    {
        /// <summary>
        /// Gets or sets the corner radius for the rounded border.
        /// </summary>
        public float CornerRadius { get; set; } = 5f;

        /// <summary>
        /// Gets or sets the border color when not hovered.
        /// </summary>
        public Color BorderColor { get; set; } = Colors.Gray;

        /// <summary>
        /// Gets or sets the border color when hovered.
        /// </summary>
        public Color HoverBorderColor { get; set; } = Colors.Blue;

        /// <summary>
        /// Gets or sets the border thickness.
        /// </summary>
        public float BorderWidth { get; set; } = 1f;

        /// <summary>
        /// Tracks whether the mouse is over the control.
        /// </summary>
        public bool IsHovered { get; private set; }

        public RoundedDrawable()
        {
            // Wire up the mouse events for hover tracking.
            this.MouseEnter += (sender, e) =>
            {
                IsHovered = true;
                Invalidate(); // Request a redraw so the border appears in hover color.
            };

            this.MouseLeave += (sender, e) =>
            {
                IsHovered = false;
                Invalidate();
            };

            // You can also attach MouseDown, Click, etc., as needed.
            CanFocus = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Define the full area of this control.
            var rect = new RectangleF(0, 0, (float)Width, (float)Height);

            // Clamp the corner radius:
            // The maximum allowed is half the smallest dimension.
            float maxAllowedRadius = Math.Min(rect.Width, rect.Height) / 2f;
            float effectiveRadius = Math.Min(CornerRadius, maxAllowedRadius);

            // Choose the effective border color based on the hover state.
            Color effectiveBorderColor = IsHovered ? HoverBorderColor : BorderColor;

            using (var pen = new Pen(effectiveBorderColor, BorderWidth))
            {
                using (var path = GetRoundedRectanglePath(rect, effectiveRadius))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }

        /// <summary>
        /// Returns a GraphicsPath for a rounded rectangle drawn in the specified rectangle with the given radius.
        /// </summary>
        private GraphicsPath GetRoundedRectanglePath(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            float diameter = radius * 2;

            // Ensure the diameter does not exceed the rectangle dimensions.
            if (diameter > rect.Width)
                diameter = rect.Width;
            if (diameter > rect.Height)
                diameter = rect.Height;

            // Top-left arc.
            var arc = new RectangleF(rect.Left, rect.Top, diameter, diameter);
            path.AddArc(arc, 180, 90);

            // Top-right arc.
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);

            // Bottom-right arc.
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // Bottom-left arc.
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        public override bool Enabled
        {
            get => base.Enabled;
            set => base.Enabled = value;
        }
    }

    public class RoundedLabel : Drawable
    {
        /// <summary>
        /// The text displayed in the label.
        /// </summary>
        public string Text { get; set; } = "";

        /// <summary>
        /// The font used to render the text.
        /// </summary>
        public Font Font { get; set; } = SystemFonts.Default();

        /// <summary>
        /// The color of the text.
        /// </summary>
        public Color TextColor { get; set; } = Colors.Black;

        /// <summary>
        /// Radius used for rounding the label’s corners.
        /// Will be clamped so it never exceeds half the control’s width or height.
        /// </summary>
        public float CornerRadius { get; set; } = 5f;

        /// <summary>
        /// Border color when the mouse is not hovering.
        /// </summary>
        public Color BorderColor { get; set; } = Colors.Gray;

        /// <summary>
        /// Border color when the mouse is hovering over the label.
        /// </summary>
        public Color HoverBorderColor { get; set; } = Colors.Blue;

        /// <summary>
        /// Thickness of the border.
        /// </summary>
        public float BorderWidth { get; set; } = 1f;

        private bool IsHovered { get; set; } = false;

        public RoundedLabel()
        {
            // Enable hover tracking.
            this.MouseEnter += (sender, e) =>
            {
                IsHovered = true;
                Invalidate(); // Redraw to reflect hover changes.
            };

            this.MouseLeave += (sender, e) =>
            {
                IsHovered = false;
                Invalidate();
            };

            // Click events and additional mouse events remain available.
            CanFocus = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Define the control’s full bounds.
            var rect = new RectangleF(0, 0, (float)Width, (float)Height);

            // Optionally, fill the background if BackgroundColor is set.
            if (this.BackgroundColor != null)
            {
                using (var brush = new SolidBrush(this.BackgroundColor))
                {
                    e.Graphics.FillRectangle(brush, rect);
                }
            }

            // Clamp CornerRadius to ensure it never exceeds half the smaller dimension.
            float maxAllowedRadius = Math.Min(rect.Width, rect.Height) / 2f;
            float effectiveRadius = Math.Min(CornerRadius, maxAllowedRadius);

            // Choose the border color based on the hover state.
            Color effectiveBorderColor = IsHovered ? HoverBorderColor : BorderColor;
            using (var pen = new Pen(effectiveBorderColor, BorderWidth))
            {
                using (var path = GetRoundedRectanglePath(rect, effectiveRadius))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }

            // Draw the text, centered within the label.
            if (!string.IsNullOrEmpty(Text))
            {
                // Measure the text size.
                var textSize = e.Graphics.MeasureString(Font, Text);
                float x = (rect.Width - textSize.Width) / 2;
                float y = (rect.Height - textSize.Height) / 2;
                x = 0;
                //y = 0;
                using (var brush = new SolidBrush(TextColor))
                {
                    e.Graphics.DrawText(Font, TextColor, x, y, Text);
                }
            }
        }

        /// <summary>
        /// Creates a GraphicsPath for a rounded rectangle within the given bounds and radius.
        /// </summary>
        private GraphicsPath GetRoundedRectanglePath(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            float diameter = radius * 2;

            // Ensure the diameter fits within the control’s bounds.
            if (diameter > rect.Width)
                diameter = rect.Width;
            if (diameter > rect.Height)
                diameter = rect.Height;

            // Top-left arc.
            var arc = new RectangleF(rect.Left, rect.Top, diameter, diameter);
            path.AddArc(arc, 180, 90);

            // Top-right arc.
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);

            // Bottom-right arc.
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // Bottom-left arc.
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnGotFocus(e);
        }
    }

    public class RoundedDrawable<T> : Drawable
        where T : Control, new()
    {
        /// <summary>
        /// Gets the wrapped inner control. (This won’t be auto‐hosted in this Drawable—you must add it manually.)
        /// </summary>
        public T InnerControl { get; set; }

        /// <summary>
        /// Extra space between the outer border and the inner control.
        /// When manually positioning the inner control, leave room for this padding.
        /// </summary>
        public Padding ContentPadding { get; set; } = new Padding(0);

        /// <summary>
        /// Corner radius for the rounded border.
        /// </summary>
        public float CornerRadius { get; set; } = 5f;

        /// <summary>
        /// Border color when not hovered.
        /// </summary>
        public Color BorderColor { get; set; } = Colors.Gray;

        /// <summary>
        /// Border color when the mouse is hovering.
        /// </summary>
        public Color HoverBorderColor { get; set; } = Colors.Blue;

        /// <summary>
        /// Border line thickness.
        /// </summary>
        public float BorderWidth { get; set; } = 1f;

        /// <summary>
        /// Indicates whether the mouse pointer is currently over this control.
        /// </summary>
        public bool IsHovered { get; private set; } = false;

        public RoundedDrawable()
        {
            InnerControl = new T();

            // Set a transparent background so only the border is drawn.
            BackgroundColor = Colors.Transparent;

            // Enable keyboard focus.
            //TabStop = true;
            //Focusable = true;
            GotFocus += (sender, e) => Invalidate();
            LostFocus += (sender, e) => Invalidate();

            // Track hover state.
            MouseEnter += (sender, e) =>
            {
                IsHovered = true;
                Invalidate();
            };
            MouseLeave += (sender, e) =>
            {
                IsHovered = false;
                Invalidate();
            };
            Content = InnerControl;
            InnerControl.Width = Width;
        }

        /// <summary>
        /// Override OnPaint to draw the rounded border (and focus indicator) over the full bounds.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Draw the rounded border around the entire bounds of this Drawable.
            var rect = new RectangleF(0, 0, (float)Width, (float)Height);

            // Clamp the corner radius so it never exceeds half the control’s width or height.
            float maxAllowedRadius = Math.Min(rect.Width, rect.Height) / 2f;
            float effectiveRadius = Math.Min(CornerRadius, maxAllowedRadius);

            // Choose border color based on hover state.
            Color effectiveColor = IsHovered ? HoverBorderColor : BorderColor;

            using (var pen = new Pen(effectiveColor, BorderWidth))
            {
                using (var path = GetRoundedRectanglePath(rect, effectiveRadius))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }

            // If the control has keyboard focus, draw a dotted focus rectangle inside the border.
            if (HasFocus)
            {
                float inset = BorderWidth + 2;
                var focusRect = new RectangleF(
                    inset,
                    inset,
                    rect.Width - inset * 2,
                    rect.Height - inset * 2
                );
                using (var focusPen = new Pen(Colors.Black, 1) { DashStyle = DashStyles.Dot })
                {
                    e.Graphics.DrawRectangle(focusPen, focusRect);
                }
            }
        }

        /// <summary>
        /// Helper method to create a rounded rectangle GraphicsPath for the specified bounds and radius.
        /// </summary>
        private GraphicsPath GetRoundedRectanglePath(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            float diameter = radius * 2;

            if (diameter > rect.Width)
                diameter = rect.Width;
            if (diameter > rect.Height)
                diameter = rect.Height;

            // Top-left arc.
            var arc = new RectangleF(rect.Left, rect.Top, diameter, diameter);
            path.AddArc(arc, 180, 90);

            // Top-right arc.
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);

            // Bottom-right arc.
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // Bottom-left arc.
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// Handle keyboard events. Pressing Enter or Space can, for example, trigger a click-like action.
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Keys.Enter || e.Key == Keys.Space)
            {
                // Raise a click event or perform an action here.
                // (You can define and invoke your own Click event if desired.)
                e.Handled = true;
            }
        }

        /// <summary>
        /// Implicit conversion operator allows a RoundedDrawable&lt;T&gt; to be cast directly to T (the inner control).
        /// </summary>
        /*public static implicit operator T(RoundedDrawable<T> rounded)
        {
            return rounded.InnerControl;
        }*/
        public static explicit operator T(RoundedDrawable<T> rounded)
        {
            return rounded.InnerControl;
        }

        public RoundedDrawable(T control)
            : this() // Call the default constructor to perform common initialization.
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            // Override the inner control with the provided control.
            InnerControl = control;
        }
    }
}
