using System;
using System.Collections.Generic;
using Eto.Drawing;
using Eto.Forms;

namespace CommonUi
{
    public class DiscountMarkupPanel : Panel, ILookupSupportedChildPanel
    {
        private Action? MoveNext = null;

        // Private controls
        private readonly Label absoluteLabel;
        private readonly Label percentageLabel;
        private readonly TextBox absoluteTextBox;
        private readonly TextBox percentageTextBox;
        private readonly TextBox parentTextBox; // Provides the base value via its Text property

        // Configuration fields
        private readonly int precisionDigits;
        private readonly Orientation layoutOrientation;
        private readonly string discountTypeText; // e.g. "Discount" or "Markup" after translation
        private Dictionary<string, Func<object>> ActionsMap = new();
        private Dictionary<string, Action<object>> SetMap = new();

        // Flag to prevent re-entrant updates.
        private bool isUpdating;

        /// <summary>
        /// Constructs a discount/markup panel.
        /// </summary>
        /// <param name="parentTextBox">
        /// The TextBox whose Text is used as the base value.
        /// </param>
        /// <param name="discountType">
        /// The discount/markup type (e.g., "Discount" or "Markup"), passed through Translate().
        /// </param>
        /// <param name="orientation">
        /// The layout orientation of the panel (Orientation.Horizontal or Orientation.Vertical).
        /// </param>
        /// <param name="parentTextChangedHandler">
        /// (Optional) An external delegate to also subscribe to the parent's TextChanged event.
        /// </param>
        /// <param name="precisionDigits">
        /// Number of decimal digits to display (default is 2).
        /// </param>
        public DiscountMarkupPanel(
            TextBox parentTextBox,
            string discountType,
            Orientation orientation,
            EventHandler<EventArgs> parentTextChangedHandler = null,
            int precisionDigits = 2,
            string[]? Mappings = null
        )
        {
            Console.WriteLine($"{Mappings[0]}");
            if (parentTextBox == null)
                throw new ArgumentNullException(nameof(parentTextBox));

            this.parentTextBox = parentTextBox;
            this.precisionDigits = precisionDigits;
            this.layoutOrientation = orientation;
            // Pass the supplied discount/markup text through Translate for localization.
            this.discountTypeText = Translate(discountType);

            // Create labels using localized text.
            absoluteLabel = new Label
            {
                Text = Translate("Absolute " + discountTypeText),
                BackgroundColor = ColorSettings.BackgroundColor,
                TextColor = ColorSettings.ForegroundColor,
            };
            percentageLabel = new Label
            {
                Text = Translate("Percentage " + discountTypeText),
                BackgroundColor = ColorSettings.BackgroundColor,
                TextColor = ColorSettings.ForegroundColor,
            };

            // Create the two textboxes.
            absoluteTextBox = new TextBox()
            {
                Width = ColorSettings.InnerControlWidth ?? 200,
                BackgroundColor = ColorSettings.LesserBackgroundColor,
                TextColor = ColorSettings.ForegroundColor,
            };
            percentageTextBox = new TextBox()
            {
                Width = ColorSettings.InnerControlWidth ?? 200,
                BackgroundColor = ColorSettings.LesserBackgroundColor,
                TextColor = ColorSettings.ForegroundColor,
            };

            // Attach change events for interlinked updating.
            absoluteTextBox.TextChanged += AbsoluteTextBox_TextChanged;
            percentageTextBox.TextChanged += PercentageTextBox_TextChanged;

            // Attach to the parent's TextChanged event; the parent is now guaranteed to be a TextBox.
            parentTextBox.TextChanged += ParentTextBox_TextChanged;
            // Optionally, also attach an external delegate.
            if (parentTextChangedHandler != null)
            {
                parentTextBox.TextChanged += parentTextChangedHandler;
            }
            SetMap.Add(Mappings[0], (o) => AbsoluteValue = (double)o);
            SetMap.Add(Mappings[1], (o) => PercentageValue = (double)o);

            BuildLayout();
            GotFocus += (_, _) =>
            {
                absoluteTextBox.Focus();
            };
            if (Mappings != null)
                MapLookupValues(Mappings);
        }

        /// <summary>
        /// Builds the UI layout based on the specified orientation.
        /// </summary>
        private void BuildLayout()
        {
            TableLayout layout;

            if (layoutOrientation == Orientation.Horizontal)
            {
                // Two rows: each row has a label and its corresponding textbox.
                layout = new TableLayout
                {
                    Spacing = new Size(5, 5),
                    Rows =
                    {
                        new TableRow(absoluteLabel, absoluteTextBox),
                        new TableRow(percentageLabel, percentageTextBox),
                    },
                };
            }
            else // Orientation.Vertical
            {
                // One column: label above textbox for each field.
                layout = new TableLayout
                {
                    Spacing = new Size(5, 5),
                    Rows =
                    {
                        new TableRow(new TableCell(absoluteLabel, true)),
                        new TableRow(new TableCell(absoluteTextBox, true)),
                        new TableRow(new TableCell(percentageLabel, true)),
                        new TableRow(new TableCell(percentageTextBox, true)),
                    },
                };
            }
            absoluteTextBox.KeyUp += (_, a) =>
            {
                if (a.Key == Keys.Enter)
                    percentageTextBox.Focus();
            };
            percentageTextBox.KeyUp += (_, a) =>
            {
                if (a.Key == Keys.Enter && this.MoveNext != null)
                    this.MoveNext();
            };
            this.Content = layout;
        }

        /// <summary>
        /// A stub translation function (to be replaced with real localization logic).
        /// </summary>
        /// <param name="input">The string to translate.</param>
        /// <returns>The "translated" string (currently simply returns the input).</returns>
        private string Translate(string input)
        {
            return input; // Stub implementation.
        }

        /// <summary>
        /// Handler for changes in the parent's Text property.
        /// When the parent's base value changes, update the discount fields.
        /// </summary>
        private void ParentTextBox_TextChanged(object sender, EventArgs e)
        {
            if (isUpdating)
                return;

            isUpdating = true;
            try
            {
                if (!double.TryParse(parentTextBox.Text, out double baseValue) || baseValue == 0)
                    return;

                // If the percentage textbox contains a valid number, update the absolute textbox.
                if (double.TryParse(percentageTextBox.Text, out double perc))
                {
                    double computedAbsolute = baseValue * (perc / 100.0);
                    absoluteTextBox.Text = computedAbsolute.ToString("F" + precisionDigits);
                }
                // Otherwise, if the absolute textbox contains a valid number, update the percentage textbox.
                else if (double.TryParse(absoluteTextBox.Text, out double abs))
                {
                    double computedPercentage = (abs / baseValue) * 100.0;
                    percentageTextBox.Text = computedPercentage.ToString("F" + precisionDigits);
                }
            }
            finally
            {
                isUpdating = false;
            }
        }

        /// <summary>
        /// Called when the percentage textbox changes.
        /// It recalculates the absolute value.
        /// </summary>
        private void PercentageTextBox_TextChanged(object sender, EventArgs e)
        {
            if (isUpdating)
                return;

            isUpdating = true;
            try
            {
                if (!double.TryParse(parentTextBox.Text, out double baseValue) || baseValue == 0)
                    return;

                if (double.TryParse(percentageTextBox.Text, out double perc))
                {
                    double computedAbsolute = baseValue * (perc / 100.0);
                    absoluteTextBox.Text = computedAbsolute.ToString("F" + precisionDigits);
                }
            }
            finally
            {
                isUpdating = false;
            }
        }

        /// <summary>
        /// Called when the absolute textbox changes.
        /// It recalculates the percentage value.
        /// </summary>
        private void AbsoluteTextBox_TextChanged(object sender, EventArgs e)
        {
            if (isUpdating)
                return;

            isUpdating = true;
            try
            {
                if (!double.TryParse(parentTextBox.Text, out double baseValue) || baseValue == 0)
                    return;

                if (double.TryParse(absoluteTextBox.Text, out double abs))
                {
                    double computedPercentage = (abs / baseValue) * 100.0;
                    percentageTextBox.Text = computedPercentage.ToString("F" + precisionDigits);
                }
            }
            finally
            {
                isUpdating = false;
            }
        }

        /// <summary>
        /// Gets or sets the Absolute Discount/Markup value.
        /// </summary>
        public double AbsoluteValue
        {
            get => double.TryParse(absoluteTextBox.Text, out double d) ? d : 0.0;
            set => absoluteTextBox.Text = value.ToString("F" + precisionDigits);
        }

        /// <summary>
        /// Gets or sets the Percentage Discount/Markup value.
        /// </summary>
        public double PercentageValue
        {
            get => double.TryParse(percentageTextBox.Text, out double d) ? d : 0.0;
            set => percentageTextBox.Text = value.ToString("F" + precisionDigits);
        }

        /// <summary>
        /// Validate the consistency of the two fields relative to the base value.
        /// Returns a tuple where isValid is true and errorDescription is null if valid;
        /// otherwise, errorDescription contains details.
        /// </summary>
        public (bool isValid, string errorDescription) Validate()
        {
            // Validate that the parent's base value, absolute, and percentage values can be parsed.
            if (!double.TryParse(parentTextBox.Text, out double baseValue))
                return (false, "Base value is invalid.");
            if (!double.TryParse(absoluteTextBox.Text, out double abs))
                return (false, "Absolute value is invalid.");
            if (!double.TryParse(percentageTextBox.Text, out double perc))
                return (false, "Percentage value is invalid.");

            // Use the primitive validation for consistency checking.
            return PrimitiveValidate(baseValue, abs, perc);
        }

        /// <summary>
        /// Primitive validate function which checks that the computed absolute value,
        /// baseValue * (percentage/100), is within 0.01% error of the provided absolute value.
        /// Returns a tuple where isValid is true and errorDescription is null if valid;
        /// otherwise, errorDescription contains an error message.
        /// </summary>
        /// <param name="baseValue">The base value from the parent TextBox.</param>
        /// <param name="absolute">The absolute discount/markup value.</param>
        /// <param name="percentage">The percentage discount/markup value.</param>
        public (bool isValid, string errorDescription) PrimitiveValidate(
            double baseValue,
            double absolute,
            double percentage
        )
        {
            double expectedAbsolute = baseValue * (percentage / 100.0);
            double tolerance = Math.Abs(expectedAbsolute) * 0.0001;
            if (expectedAbsolute == 0)
                tolerance = 0.0001;

            if (Math.Abs(expectedAbsolute - absolute) <= tolerance)
            {
                return (true, null);
            }
            else
            {
                string error =
                    $"Validation failed: expected absolute value to be {expectedAbsolute} "
                    + $"(within tolerance {tolerance}) for base value {baseValue} and percentage {percentage}%, "
                    + $"but got {absolute}.";
                return (false, error);
            }
        }

        public object LookupValue(string fieldName)
        {
            return ActionsMap[fieldName]();
        }

        public void MapLookupValues(string[] fieldNames)
        {
            ActionsMap.Add(fieldNames[0], () => AbsoluteValue);
            ActionsMap.Add(fieldNames[1], () => PercentageValue);
        }

        public void MapSetValues(string[] fieldNames)
        {
            SetMap.Add(fieldNames[0], (a) => AbsoluteValue = (double)a);
            SetMap.Add(fieldNames[1], (a) => PercentageValue = (double)a);
        }

        public void SetOriginalValue(string Key, object Value)
        {
            SetMap[Key](Value);
        }

        public void SetMoveNext(Action MoveNext)
        {
            this.MoveNext = MoveNext;
        }

        public void FocusChild()
        {
            absoluteTextBox.Focus();
        }

        public List<Control> GetFocusableControls()
        {
            return new List<Control>() { absoluteTextBox, percentageTextBox };
        }

        public void SetOriginalValues(object[] OriginalValues)
        {
            AbsoluteValue = (double)OriginalValues[0];
            PercentageValue = (double)OriginalValues[1];
        }
    }
}
