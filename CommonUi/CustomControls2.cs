using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Forms;
using Eto.Drawing;
using RV.InvNew.Common;

namespace CommonUi
{

    #region ILookupSupportedChildPanel Interface


    #endregion

    #region DatePickerPanel

    /// <summary>
    /// An independent date-picker panel that uses Eto.Forms’ DateTimePicker.
    /// Mapping order (from the string array):
    ///   • fieldNames[0] : The date value from the DateTimePicker.
    ///   • fieldNames[1] : Text from the lookup result (for example, from a VAT lookup).
    /// 
    /// The panel exposes a public delegate (DateLookupHandler) so that you can wire up a LINQ lookup or any other logic.
    /// </summary>
    public class DatePickerPanel : Panel, ILookupSupportedChildPanel
    {
        // Built-in DateTimePicker control.
        private DateTimePicker datePicker;
        // An optional read-only TextBox that may display lookup results.
        private TextBox lookupResultTextBox;

        private readonly Dictionary<string, Func<object>> actionsMap = new Dictionary<string, Func<object>>();
        private readonly Dictionary<string, Action<object>> setMap = new Dictionary<string, Action<object>>();
        private Action? moveNext;

        /// <summary>
        /// Optional lookup delegate that takes the selected date and returns lookup information.
        /// For example, you might use it to query a List&lt;VatCategory&gt;.
        /// </summary>
        public Func<DateTime, string>? DateLookupHandler { get; set; }

        public DatePickerPanel(string[]? mappings = null)
        {
            datePicker = new DateTimePicker();
            lookupResultTextBox = new TextBox
            {
                ReadOnly = true,
                BackgroundColor = Color.Parse("#EFEFEF"),
                TextColor = Colors.Black
            };

            // When the date changes, invoke the lookup function if available.
            datePicker.ValueChanged += (sender, e) =>
            {
                if (DateLookupHandler != null)
                    lookupResultTextBox.Text = DateLookupHandler(datePicker?.Value ?? DateTime.Now);
                else
                    lookupResultTextBox.Text = datePicker?.Value.ToString();
            };

            // Create a vertical layout.
            Content = new TableLayout
            {
                Spacing = new Size(5, 5),
                Rows =
            {
                new TableRow(new Label { Text = "Select Date:" }),
                new TableRow(datePicker),
                new TableRow(new Label { Text = "Lookup Result:" }),
                new TableRow(lookupResultTextBox)
            }
            };

            if (mappings != null)
            {
                MapLookupValues(mappings);
            }
        }

        public void MapLookupValues(string[] fieldNames)
        {
            // Map the DateTimePicker value and the lookup-result TextBox.
            actionsMap.Add(fieldNames[0], () => datePicker.Value);
            if(fieldNames.Length > 1)
            actionsMap.Add(fieldNames[1], () => lookupResultTextBox.Text);
        }

        public object LookupValue(string fieldName)
        {
            if (actionsMap.TryGetValue(fieldName, out var getter))
            {
                return getter();
            }
            return null!;
        }

        public void SetMoveNext(Action moveNext) => this.moveNext = moveNext;

        public List<Control> GetFocusableControls() => new List<Control> { datePicker };

        public (bool isValid, string errorDescription) Validate() => (true, string.Empty);

        public void FocusChild() => datePicker.Focus();

        public void SetOriginalValues(object[] originalValues)
        {
            if (originalValues.Length > 0 && originalValues[0] is DateTime dt)
                datePicker.Value = dt;
            if (originalValues.Length > 1 && originalValues[1] is string s)
                lookupResultTextBox.Text = s;
        }

        public void SetOriginalValue(string key, object value)
        {
            if (setMap.ContainsKey(key))
            {
                setMap[key](value);
            }
        }

        public void MapSetValues(string[] fieldNames)
        {
            setMap.Add(fieldNames[0], (val) => datePicker.Value = (DateTime)val);
            setMap.Add(fieldNames[1], (val) => lookupResultTextBox.Text = (string)val);
        }
    }

    #endregion

    #region VatCategoryLookupPanel

    /// <summary>
    /// An independent VAT-category lookup panel.
    /// Mapping order (from the string array):
    ///   • fieldNames[0] : User input for VAT Category ID.
    ///   • fieldNames[1] : The lookup result (e.g. the category name and percentage).
    /// 
    /// The panel expects an external list of VatCategory objects (which may be set via the property VatCategories).
    /// </summary>
    public class VatCategoryLookupPanel : Panel, ILookupSupportedChildPanel
    {
        private TextBox vatCategoryIdTextBox;
        private TextBox vatDetailsTextBox;

        private readonly Dictionary<string, Func<object>> actionsMap = new Dictionary<string, Func<object>>();
        private readonly Dictionary<string, Action<object>> setMap = new Dictionary<string, Action<object>>();
        private Action? moveNext;

        /// <summary>
        /// Set this list externally so that the lookup can use LINQ queries against it.
        /// </summary>
        public List<VatCategory> VatCategories { get; set; } = new List<VatCategory>();

        public VatCategoryLookupPanel(string[]? mappings = null)
        {
            vatCategoryIdTextBox = new TextBox
            {
                BackgroundColor = Color.Parse("#EFEFEF"),
                TextColor = Colors.Black
            };
            vatDetailsTextBox = new TextBox
            {
                ReadOnly = true,
                BackgroundColor = Color.Parse("#EFEFEF"),
                TextColor = Colors.Black
            };

            // When the VAT Category ID text changes, perform a LINQ lookup.
            vatCategoryIdTextBox.TextChanged += (sender, e) =>
            {
                if (long.TryParse(vatCategoryIdTextBox.Text, out long id))
                {
                    var vat = VatCategories.FirstOrDefault(v => v.VatCategoryId == id);
                    if (vat != null)
                    {
                        vatDetailsTextBox.Text = $"Name: {vat.VatName}, Rate: {vat.VatPercentage}%";
                    }
                    else
                        vatDetailsTextBox.Text = "Not found";
                }
                else
                {
                    vatDetailsTextBox.Text = "Invalid ID";
                }
            };

            Content = new TableLayout
            {
                Spacing = new Size(5, 5),
                Rows =
            {
                new TableRow(new Label { Text = "VAT Category ID:" }),
                new TableRow(vatCategoryIdTextBox),
                new TableRow(new Label { Text = "VAT Details:" }),
                new TableRow(vatDetailsTextBox)
            }
            };

            if (mappings != null)
            {
                MapLookupValues(mappings);
            }
        }

        public void MapLookupValues(string[] fieldNames)
        {
            // Map the input text and the lookup result.
            actionsMap.Add(fieldNames[0], () => vatCategoryIdTextBox.Text);
            actionsMap.Add(fieldNames[1], () => vatDetailsTextBox.Text);
        }

        public object LookupValue(string fieldName)
        {
            if (actionsMap.TryGetValue(fieldName, out var getter))
            {
                return getter();
            }
            return null!;
        }

        public void SetMoveNext(Action moveNext) => this.moveNext = moveNext;

        public List<Control> GetFocusableControls() => new List<Control> { vatCategoryIdTextBox, vatDetailsTextBox };

        public (bool isValid, string errorDescription) Validate()
        {
            if (!long.TryParse(vatCategoryIdTextBox.Text, out _))
                return (false, "VAT Category ID is not a valid number.");
            if (string.IsNullOrEmpty(vatDetailsTextBox.Text) ||
                vatDetailsTextBox.Text == "Not found" ||
                vatDetailsTextBox.Text == "Invalid ID")
                return (false, "VAT category lookup failed.");
            return (true, string.Empty);
        }

        public void FocusChild() => vatCategoryIdTextBox.Focus();

        public void SetOriginalValues(object[] originalValues)
        {
            if (originalValues.Length > 0)
                vatCategoryIdTextBox.Text = originalValues[0]?.ToString() ?? "";
            if (originalValues.Length > 1 && originalValues[1] is string s)
                vatDetailsTextBox.Text = s;
        }

        public void SetOriginalValue(string key, object value)
        {
            if (setMap.ContainsKey(key))
                setMap[key](value);
        }

        public void MapSetValues(string[] fieldNames)
        {
            setMap.Add(fieldNames[0], (val) => vatCategoryIdTextBox.Text = val.ToString());
            setMap.Add(fieldNames[1], (val) => vatDetailsTextBox.Text = (string)val);
        }
    }

    #endregion

    #region PhoneNumberLookupPanel

    /// <summary>
    /// An independent phone number lookup panel.
    /// Mapping order (from the string array):
    ///   • fieldNames[0] : Inputted phone number.
    ///   • fieldNames[1] : The lookup result (customer/supplier name).
    /// 
    /// The panel expects an external list of Pii objects to be provided (via the property PiiList).
    /// </summary>
    public class PhoneNumberLookupPanel : Panel, ILookupSupportedChildPanel
    {
        private TextBox phoneNumberTextBox;
        private TextBox customerNameTextBox;

        private readonly Dictionary<string, Func<object>> actionsMap = new Dictionary<string, Func<object>>();
        private readonly Dictionary<string, Action<object>> setMap = new Dictionary<string, Action<object>>();
        private Action? moveNext;

        /// <summary>
        /// Set this external list so that the lookup can query for a matching phone number.
        /// </summary>
        public List<Pii> PiiList { get; set; } = new List<Pii>();

        public PhoneNumberLookupPanel(string[]? mappings = null)
        {
            phoneNumberTextBox = new TextBox
            {
                BackgroundColor = Color.Parse("#EFEFEF"),
                TextColor = Colors.Black
            };
            customerNameTextBox = new TextBox
            {
                ReadOnly = true,
                BackgroundColor = Color.Parse("#EFEFEF"),
                TextColor = Colors.Black
            };

            phoneNumberTextBox.TextChanged += (sender, e) =>
            {
                string input = phoneNumberTextBox.Text;
                if (string.IsNullOrWhiteSpace(input))
                {
                    customerNameTextBox.Text = "";
                    return;
                }

                var person = PiiList.FirstOrDefault(p =>
                    (!string.IsNullOrEmpty(p.Telephone) && p.Telephone.Equals(input, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(p.Mobile) && p.Mobile.Equals(input, StringComparison.OrdinalIgnoreCase))
                );
                customerNameTextBox.Text = person != null ? person.Name : "Not found";
            };

            Content = new TableLayout
            {
                Spacing = new Size(5, 5),
                Rows =
            {
                new TableRow(new Label { Text = "Phone Number:" }),
                new TableRow(phoneNumberTextBox),
                new TableRow(new Label { Text = "Customer Name:" }),
                new TableRow(customerNameTextBox)
            }
            };

            if (mappings != null)
            {
                MapLookupValues(mappings);
            }
        }

        public void MapLookupValues(string[] fieldNames)
        {
            // Map the phone number input and its resulting customer name.
            actionsMap.Add(fieldNames[0], () => phoneNumberTextBox.Text);
            actionsMap.Add(fieldNames[1], () => customerNameTextBox.Text);
        }

        public object LookupValue(string fieldName)
        {
            if (actionsMap.TryGetValue(fieldName, out var getter))
            {
                return getter();
            }
            return null!;
        }

        public void SetMoveNext(Action moveNext) => this.moveNext = moveNext;

        public List<Control> GetFocusableControls() => new List<Control> { phoneNumberTextBox };

        public (bool isValid, string errorDescription) Validate()
        {
            if (string.IsNullOrWhiteSpace(phoneNumberTextBox.Text))
                return (false, "Phone number is required.");
            if (customerNameTextBox.Text == "Not found")
                return (false, "No customer found for the provided phone number.");
            return (true, string.Empty);
        }

        public void FocusChild() => phoneNumberTextBox.Focus();

        public void SetOriginalValues(object[] originalValues)
        {
            if (originalValues.Length > 0)
                phoneNumberTextBox.Text = originalValues[0]?.ToString() ?? "";
            if (originalValues.Length > 1 && originalValues[1] is string s)
                customerNameTextBox.Text = s;
        }

        public void SetOriginalValue(string key, object value)
        {
            if (setMap.ContainsKey(key))
                setMap[key](value);
        }

        public void MapSetValues(string[] fieldNames)
        {
            setMap.Add(fieldNames[0], (val) => phoneNumberTextBox.Text = val.ToString());
            setMap.Add(fieldNames[1], (val) => customerNameTextBox.Text = (string)val);
        }
    }

    #endregion

    

  

}