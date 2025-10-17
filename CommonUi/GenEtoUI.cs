using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using Microsoft.Maui.Graphics.Text;
using Microsoft.Maui.Platform;
using MyAOTFriendlyExtensions;
using Tomlyn;

namespace CommonUi
{
    public delegate long? ShowAndGetValue();
    public delegate string? LookupValue(long ValueToLookUp);

    public interface EtoValueSelector
    {
        public long Value { get; }
    }

    public class GenEtoUI : Eto.Forms.Panel
    {
        public PanelSettings? LocalColor = null;
        public delegate long SaveHandler(IReadOnlyDictionary<string, object> UserInput);
        public delegate ILookupSupportedChildPanel GeneratePanel(
            string[] FieldNames,
            TextBox? Parent
        );
        public bool ChangesOnly = false;
        public string Context = "default";
        public Action<string[]> AnythingChanged = (_) => { };
        public IReadOnlyDictionary<string, object> Values
        {
            get => _Values;
        }
        private Dictionary<string, object> _Values = new();
        public Dictionary<string, Eto.Forms.Control> _Controls = new();
        private bool _new = false;
        private SaveHandler _SaveNewHandler;
        private SaveHandler _SaveExistingHandler;
        List<Eto.Forms.TableRow> _EControlsAll = new();
        OrderedDictionary<
            (
                Eto.Forms.Control LabelControl,
                Eto.Forms.Control MainControl,
                Eto.Forms.Control Supplemental
            ),
            int
        > LayoutNext = new();
        List<Eto.Forms.TableRow> _EControlsL = new();
        List<Eto.Forms.TableRow> _EControlsR = new();
        public Dictionary<string, Eto.Forms.Control> _Einputs = new();
        Dictionary<string, Eto.Forms.Control?> _ELegends = new();
        Dictionary<string, bool> _EChangeTracker = new();
        Dictionary<string, object> ConvertedInputs = new();
        Dictionary<string, Control> _EFieldNames = new();
        Dictionary<string, Type> OriginalTypes = new();
        IReadOnlyDictionary<
                string,
                Func<string[], TextBox?, ILookupSupportedChildPanel>
            >? PanelGenerators = null;
        Dictionary<string, Func<object>> CustomPanelInputRetrievalFunctions = new();
        Dictionary<string, Type> OriginalTypesCustomPanels = new();
        Dictionary<string, ILookupSupportedChildPanel> FieldsHandlerMapping = new();
        List<List<(Eto.Forms.Control? LabelControl, Eto.Forms.Control MainControl)>> AllRows =
            new();
        int nColumns = 3;
        public OrderedDictionary<string, (string ControlName, object Value, string?)> _Inputs;
        public string Identity = "";
        IReadOnlyDictionary<string, object> Configuration;
        IReadOnlyDictionary<string, (ShowAndGetValue, LookupValue)> InputHandler;

        // Track the last changed field to prevent recursive updates
        private string _lastChangedField = "";
        private DateTime _lastChangeTime = DateTime.MinValue;
        private const int RECURSION_PREVENTION_MS = 100;

        // Add these fields at class level for caching
        private readonly Dictionary<string, Func<object>> _valueGetters = new();
        private readonly Dictionary<string, Action<object>> _valueSetters = new();
        private readonly Dictionary<Type, Func<string, object>> _parsers = new();

        IReadOnlyDictionary<
                string[],
                (string ControlName, string? ParentField)
            >? FieldsListHandledByGeneratedPanels = null;
        System.EventHandler<Eto.Forms.KeyEventArgs> GoToNext = (
                object o,
                Eto.Forms.KeyEventArgs ea
            ) => { };

        Action<Control> GoToNextFromPanel = (_) => { };

        // In constructor, initialize parsers once
        private void InitializeParsers()
        {
            _parsers[typeof(long)] = s => long.Parse(s);
            _parsers[typeof(int)] = s => int.Parse(s);
            _parsers[typeof(float)] = s => float.Parse(s);
            _parsers[typeof(double)] = s => double.Parse(s);
            _parsers[typeof(string)] = s => s;
            _parsers[typeof(bool)] = s => bool.Parse(s);
        }

        public void Disable(string key)
        {
            object? Out = null;
            if (_Inputs.ContainsKey(key))
            {
                var e = _Inputs.Where(e => e.Key == key).First();
                _Einputs[e.Key].Enabled = false;
            }
        }

        // Replace the original Lookup method with this optimized version
        public object Lookup(string key)
        {
            if (_valueGetters.TryGetValue(key, out var getter))
            {
                return getter();
            }

            // Fall back to custom controls
            if (CustomPanelInputRetrievalFunctions.TryGetValue(key, out var customGetter))
            {
                return customGetter();
            }

            throw new KeyNotFoundException($"Key '{key}' not found in value getters.");
        }


        // Replace the original SetValue method with this optimized version
        public void SetValue(string key, object value)
        {
            // Try built-in controls first
            if (_valueSetters.TryGetValue(key, out var setter))
            {
                setter(value);
                return;
            }

            // Fall back to custom controls if needed
            if (FieldsHandlerMapping.TryGetValue(key, out var customControl))
            {
                customControl.SetOriginalValue(key, value);
                return;
            }

            throw new KeyNotFoundException($"Key '{key}' not found in control mappings.");
        }

        public void InitializeConfiguration()
        {
            string ConfigText;
            if (!File.Exists("theme.toml"))
            {
                ConfigText = "";
            }
            else
            {
                ConfigText = File.ReadAllText("theme.toml");
            }
            Configuration = Tomlyn.Toml.Parse(ConfigText).ToModel().ToDictionary();
        }

        public (
            Eto.Drawing.Color FG,
            Eto.Drawing.Color BG,
            Eto.Drawing.Color FGc,
            Eto.Drawing.Color BGc,
            Eto.Drawing.Font TFont,
            Eto.Drawing.Size TSize,
            Eto.Drawing.Size CSize
        ) GetThemeForComponent(string ControlName)
        {
            Color FG = EtoThemingUtilities.GetNestedColor(
                Configuration,
                Context,
                ControlName,
                "foreground_color",
                LocalColor?.ForegroundColor ?? ColorSettings.ForegroundColor
            );
            Color BG = EtoThemingUtilities.GetNestedColor(
                Configuration,
                Context,
                ControlName,
                "background_color",
                LocalColor?.LesserBackgroundColor ?? ColorSettings.LesserBackgroundColor
            );
            Color FGc = EtoThemingUtilities.GetNestedColor(
                Configuration,
                Context,
                ControlName,
                "foreground_color_changed",
                LocalColor?.BackgroundColor ?? ColorSettings.BackgroundColor
            );
            Color BGc = EtoThemingUtilities.GetNestedColor(
                Configuration,
                Context,
                ControlName,
                "background_color_changed",
                LocalColor?.LesserForegroundColor ?? ColorSettings.LesserForegroundColor
            );
            Font EFont = EtoThemingUtilities.GetNestedFont(
                Configuration,
                Context,
                ControlName,
                new Eto.Drawing.Font(ColorSettings.UIFont ?? Eto.Drawing.FontFamilies.Monospace, 12)
            );
            Size TSize = EtoThemingUtilities.GetNestedSize(
                Configuration,
                Context,
                ControlName,
                "font_size",
                new Eto.Drawing.Size(12, 12)
            );
            Size CSize = EtoThemingUtilities.GetNestedSize(
                Configuration,
                Context,
                ControlName,
                "control_size",
                new Eto.Drawing.Size(
                    ColorSettings.ControlWidth ?? 10,
                    ColorSettings.ControlHeight ?? 30
                )
            );
            if (ControlName == "Legend")
                return (
                    FG,
                    LocalColor?.BackgroundColor ?? ColorSettings.BackgroundColor,
                    FGc,
                    BGc,
                    EFont,
                    TSize,
                    CSize
                );
            else
                return (FG, BG, FGc, BGc, EFont, TSize, CSize);
        }

        // Optimize the SerializeIfValid method
        public string SerializeIfValid()
        {
            if (!ValidateInputs())
                return "";

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter() }
            };

            // Use pre-cached getters for better performance
            Dictionary<string, object> _LocalDict = new();

            foreach (var getter in _valueGetters)
            {
                if (!ChangesOnly || _EChangeTracker.TryGetValue(getter.Key, out bool x) && x)
                {
                    _LocalDict[getter.Key] = getter.Value();
                }
            }

            // Add custom panel values
            foreach (var kvp in CustomPanelInputRetrievalFunctions)
            {
                if (!ChangesOnly || _EChangeTracker.TryGetValue(kvp.Key, out bool x) && x)
                {
                    _LocalDict[kvp.Key] = kvp.Value();
                }
            }

            return JsonSerializer.Serialize(_LocalDict, options);
        }


        // Optimize ConvertInputs method
        public void ConvertInputs()
        {
            ConvertedInputs = new();

            // Use pre-cached getters for better performance
            foreach (var getter in _valueGetters)
            {
                if (!ChangesOnly || _EChangeTracker.TryGetValue(getter.Key, out bool x) && x)
                {
                    ConvertedInputs[getter.Key] = getter.Value();
                }
            }

            // Add custom panel values
            foreach (var kvp in CustomPanelInputRetrievalFunctions)
            {
                if (!ChangesOnly || _EChangeTracker.TryGetValue(kvp.Key, out bool x) && x)
                {
                    ConvertedInputs[kvp.Key] = kvp.Value();
                }
            }
        }


        // Prevent recursive updates by tracking the last changed field and time
        private bool ShouldUpdateField(string fieldName)
        {
            var now = DateTime.Now;
            var timeSinceLastChange = (now - _lastChangeTime).TotalMilliseconds;

            if (fieldName == _lastChangedField && timeSinceLastChange < RECURSION_PREVENTION_MS)
            {
                return false; // Prevent recursion
            }

            _lastChangedField = fieldName;
            _lastChangeTime = now;
            return true;
        }

        private void CreateControlsForInput(
    KeyValuePair<string, (string ControlName, object Value, string? LookupFunctionCallback)> kv,
    List<Eto.Forms.Control> EFocusableList,
    string[] DenyList,
    string? IdentityColumn)
        {
            Console.WriteLine($"CreateControlsForInput called for key: {kv.Key}");

            // Get theme for this control
            (var FG, var BG, var FGc, var BGc, var TFont, var TSize, var CSize) = GetThemeForComponent(kv.Key);
            var valueType = kv.Value.Value?.GetType() ?? typeof(long);
            OriginalTypes[kv.Key] = valueType;

            // Create label with proper text
            Control EFieldName = CreateFieldLabel(kv.Value.ControlName, kv.Key);
            _EFieldNames.Add(kv.Key, EFieldName);

            // Create input control based on type
            Eto.Forms.Control EInput = CreateInputControl(
                kv, valueType, FG, BG, FGc, BGc, TFont, CSize,
                DenyList, IdentityColumn, EFocusableList);

            // Cache getters and setters for this control
            CacheValueAccessors(kv.Key, valueType, EInput);

            // Create legend if needed
            Eto.Forms.Control ELegend = kv.Value.Item3 != null ? new Label() { Width = 10 } : null;
            if (ELegend != null) _ELegends.Add(kv.Key, ELegend);

            // Create table row
            var EControl = CreateTableRow(EFieldName, EInput, ELegend);

            _Einputs.Add(kv.Key, EInput);
            _EControlsAll.Add(EControl);

            // Add to LayoutNext - this is the key part that might be missing
            Console.WriteLine($"Adding to LayoutNext: {kv.Key}, Control type: {EInput.GetType().Name}");
            LayoutNext.Add((EFieldName, EInput, ELegend), LayoutNext.Count);
        }



        public GenEtoUI(
            IReadOnlyDictionary<
                string,
                (string ControlName, object Value, string? LookupFunctionCallback)
            > Inputs,
            SaveHandler SaveNewHandler,
            SaveHandler SaveExistingHandler,
            IReadOnlyDictionary<string, (ShowAndGetValue, LookupValue)> InputHandler,
            string? IdentityColumn,
            bool ChangesOnly = false,
            string[]? DenyList = null,
            PanelSettings PanelColours = null,
            IReadOnlyDictionary<
                string,
                Func<string[], TextBox?, ILookupSupportedChildPanel>
            >? PanelGenerators = null,
            IReadOnlyDictionary<
                string[],
                (string ControlName, string? ParentField)
            >? FieldsListHandledByGeneratedPanels = null,
            string[]? order = null
        )
        {
            this.SuspendLayout();
            this.FieldsListHandledByGeneratedPanels = FieldsListHandledByGeneratedPanels;
            this.InputHandler = InputHandler;
            this.PanelGenerators = PanelGenerators;
            InitializeParsers();
            OrderedDictionary<
                string,
                (string ControlName, object Value, string? LookupFunctionCallback)
            > InputsOrdered = new();
            var InputsUnordered = Inputs.ToDictionary();
            if (order != null)
            {
                foreach (var element in order)
                {
                    InputsOrdered.Add(element, InputsUnordered[element]);
                    InputsUnordered.Remove(element);
                }
            }
            foreach (var UnorderedInput in InputsUnordered)
            {
                InputsOrdered.Add(UnorderedInput.Key, UnorderedInput.Value);
            }
            _Inputs = InputsOrdered;
            List<string> NotInNormalFlow = new();
            int rowSpanFromCustomPanels = 0;
            if (FieldsListHandledByGeneratedPanels != null)
                foreach (var kv in FieldsListHandledByGeneratedPanels)
                {
                    if (kv.Value.ParentField != null)
                        NotInNormalFlow = NotInNormalFlow.Concat(kv.Key.ToList()).ToList();
                }
            NotInNormalFlow
                .Select(e =>
                {
                    _EChangeTracker.Add(e, true);
                    _Inputs.Remove(e);
                    return true;
                })
                .ToList();
            if (DenyList == null)
                DenyList = new string[] { };
            InitializeConfiguration();
            PanelSettings CurrentPanelColours = new PanelSettings()
            {
                BackgroundColor = ColorSettings.BackgroundColor,
                ForegroundColor = ColorSettings.ForegroundColor,
            };
            if (PanelColours != null)
            {
                CurrentPanelColours.BackgroundColor = PanelColours.BackgroundColor;
                CurrentPanelColours.ForegroundColor = PanelColours.ForegroundColor;
                LocalColor = PanelColours;
            }

            (var FGF, var BGF, var FGcF, var BGcF, var TFontF, var TSizeF, var CSizeF) =
                GetThemeForComponent("form");
            BackgroundColor = BGF;
            BackgroundColor = LocalColor?.BackgroundColor ?? ColorSettings.BackgroundColor;
            var SaveButtonTheme = GetThemeForComponent("save");
            var NewButtonTheme = GetThemeForComponent("new");
            var ViewButtonTheme = GetThemeForComponent("view");
            var CancelButtonTheme = GetThemeForComponent("cancel");

            this.ChangesOnly = ChangesOnly;
            List<Eto.Forms.TableRow> EControlsL = new() { };
            List<Eto.Forms.TableRow> EControlsR = new();
            List<Eto.Forms.Control> EFocusableList = new();

            var E = InputsOrdered.AsEnumerable();
            var ECount = E.Count();
            var EMid = ECount / 2;
            var CurrentNo = 0;
            
            Button SaveButton = new Button()
            {
                Text = TranslationHelper.Translate("Save", "Save", TranslationHelper.Lang),
                BackgroundColor = CurrentPanelColours.BackgroundColor,
                TextColor = CurrentPanelColours.ForegroundColor,
                Width = ColorSettings.ControlWidth ?? 100,
                Height = ColorSettings.ControlHeight ?? 30,
            };
            GoToNext = (e, a) =>
            {
                if (a.Key == Keys.Enter)
                {
                    if (EFocusableList.IndexOf((Eto.Forms.Control)e) < EFocusableList.Count() - 1)
                        if (((Eto.Forms.Control)e).Enabled)
                        {
                            if (
                                EFocusableList[EFocusableList.IndexOf((Eto.Forms.Control)e) + 1]
                                is ILookupSupportedChildPanel FocusChild
                            )
                                FocusChild.FocusChild();
                            else
                            {
                                EFocusableList[EFocusableList.IndexOf((Eto.Forms.Control)e) + 1]
                                    .Focus();
                            }
                        }
                        else
                            GoToNext(e, a);
                    else
                        SaveButton.Focus();
                }
            };
            Action<Control> GoToNextFromPanel = (_) => { };
            GoToNextFromPanel = (Control PreviousObject) =>
            {
                if (EFocusableList.IndexOf(PreviousObject) < EFocusableList.Count() - 1)
                    if ((PreviousObject).Enabled)
                    {
                        if (
                            EFocusableList[EFocusableList.IndexOf(PreviousObject) + 1]
                            is ILookupSupportedChildPanel FocusChild
                        )
                            FocusChild.FocusChild();
                        else
                        {
                            EFocusableList[EFocusableList.IndexOf(PreviousObject) + 1].Focus();
                        }
                    }
                    else
                        GoToNextFromPanel(
                            EFocusableList[EFocusableList.IndexOf(PreviousObject) + 1]
                        );
                else
                    SaveButton.Focus();
            };
            (
                var LegendFG,
                var LegendBG,
                var LegendFGc,
                var LegendBGc,
                var LegendTFont,
                var LehendTSize,
                var LegendCSize
            ) = GetThemeForComponent("Legend");
            
            foreach (var kv in E)
            {
                if (!NotInNormalFlow.Contains(kv.Key))
                {
                    Console.WriteLine($"Attempt to construct with: {kv.Key}");

                    // Create the controls using optimized helper methods
                    CreateControlsForInput(kv, EFocusableList, DenyList, IdentityColumn);

                    // Handle custom panels
                    HandleCustomPanels(kv, EFocusableList, ref CurrentNo, ref rowSpanFromCustomPanels, GoToNextFromPanel);

                    CurrentNo++;
                }
            }
            _EControlsL = EControlsL;
            _EControlsR = EControlsR;
            // Use optimized layout generation
            var tableLayout = new Scrollable() { Content = OptimizeLayoutGeneration() };

            Button NewButton = new Button()
            {
                Text = TranslationHelper.Translate("New", "New", TranslationHelper.Lang),
                BackgroundColor = CurrentPanelColours.BackgroundColor,
                TextColor = CurrentPanelColours.ForegroundColor,
                Width = ColorSettings.ControlWidth ?? 100,
                Height = ColorSettings.ControlHeight ?? 30,
            };
            Button ViewButton = new Button()
            {
                Text = TranslationHelper.Translate("View", "View", TranslationHelper.Lang),
                BackgroundColor = CurrentPanelColours.BackgroundColor,
                TextColor = CurrentPanelColours.ForegroundColor,
                Width = ColorSettings.ControlWidth ?? 100,
                Height = ColorSettings.ControlHeight ?? 30,
            };
            Button CancelButton = new Button()
            {
                Text = TranslationHelper.Translate("Cancel", "Cancel", TranslationHelper.Lang),
                BackgroundColor = CurrentPanelColours.BackgroundColor,
                TextColor = CurrentPanelColours.ForegroundColor,
                Width = ColorSettings.ControlWidth ?? 100,
                Height = ColorSettings.ControlHeight ?? 30,
            };
            NewButton.Font = NewButtonTheme.TFont;
            NewButton.BackgroundColor = NewButtonTheme.BG;
            SaveButton.Font = SaveButtonTheme.TFont;
            SaveButton.BackgroundColor = SaveButtonTheme.BG;
            ViewButton.Font = ViewButtonTheme.TFont;
            ViewButton.BackgroundColor = ViewButtonTheme.BG;
            CancelButton.Font = CancelButtonTheme.TFont;
            CancelButton.BackgroundColor = CancelButtonTheme.BG;
            SaveButton.ConfigureForPlatform();
            NewButton.ConfigureForPlatform();
            ViewButton.ConfigureForPlatform();
            CancelButton.ConfigureForPlatform();

            NewButton.Click += (_, _) =>
            {
                if (ValidateInputs())
                    ConvertInputs();
                else
                    MessageBox.Show(
                        $"An input has been of the wrong type{Environment.NewLine}Field names with errors are highlighted",
                        "Wrong type",
                        MessageBoxType.Error
                    );
            };
            SaveButton.Click += (_, _) =>
            {
                if (ValidateInputs())
                {
                    ConvertInputs();
                    if (_new)
                    {
                        SaveNewHandler(ConvertedInputs);
                    }
                    else
                    {
                        SaveExistingHandler(ConvertedInputs);
                    }
                }
                else
                    MessageBox.Show(
                        $"An input has been of the wrong type{Environment.NewLine}Field names with errors are highlighted",
                        "Wrong type",
                        MessageBoxType.Error
                    );
            };
            ViewButton.Click += (_, _) =>
            {
                if (ValidateInputs())
                {
                    ConvertInputs();
                    MessageBox.Show(
                        $"New: {_new.ToString()}, Serialized: {JsonSerializer.Serialize(ConvertedInputs)}",
                        "Serialized",
                        MessageBoxType.Information
                    );
                }
                else
                    MessageBox.Show(
                        $"An input has been of the wrong type{Environment.NewLine}Field names with errors are highlighted",
                        "Wrong type",
                        MessageBoxType.Error
                    );
            };
            var ActionButtons = new StackLayout(
                null,
                NewButton,
                SaveButton,
                ViewButton,
                CancelButton,
                null
            )
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Height = ColorSettings.ControlHeight ?? -1,
            };

            

            

            Content = new StackLayout(
    ActionButtons,
    new StackLayoutItem(
        new Scrollable()
        {
            Content = tableLayout,
            Border = BorderType.None,
            ExpandContentHeight = ColorSettings.ExpandContentHeight,
            ExpandContentWidth = ColorSettings.ExpandContentWidth,
        },
        false
    )
)
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Top,
            };
            this.ResumeLayout();
        }

        private Control CreateFieldLabel(string controlName, string key)
        {
            // Get legend theme
            var (LegendFG, LegendBG, _, _, LegendTFont, _, _) = GetThemeForComponent("Legend");

            Control label;

            if (ColorSettings.InnerLabelWidth != null &&
                ColorSettings.InnerLabelHeight != null &&
                !ColorSettings.ForceNativeLabels)
            {
                var customLabel = new CustomLabel()
                {
                    Width = ColorSettings.InnerLabelWidth ?? -1,
                    Height = ColorSettings.InnerLabelHeight ?? -1,
                    Text = ColorSettings.DebugDontRenderLabels
                        ? ""
                        : TranslationHelper.Translate(controlName, controlName, TranslationHelper.Lang),
                    ForegroundColor = LegendFG,
                };
                label = customLabel;
            }
            else
            {
                var regularLabel = new Label()
                {
                    Width = ColorSettings.InnerLabelWidth ?? -1,
                    Height = ColorSettings.InnerLabelHeight ?? -1,
                    Text = ColorSettings.DebugDontRenderLabels
                        ? ""
                        : TranslationHelper.Translate(controlName, controlName, TranslationHelper.Lang),
                    TextColor = LegendFG,
                    BackgroundColor = LegendBG,
                    Font = LegendTFont,
                    Wrap = WrapMode.None,
                };
                regularLabel.ConfigureForPlatform();
                label = regularLabel;
            }

            return label;
        }

        private void HandleCustomPanels(
    KeyValuePair<string, (string ControlName, object Value, string? LookupFunctionCallback)> kv,
    List<Eto.Forms.Control> EFocusableList,
    ref int CurrentNo,
    ref int rowSpanFromCustomPanels,
    Action<Control> GoToNextFromPanel)
        {
            Console.WriteLine($"HandleCustomPanels called for key: {kv.Key}");

            ILookupSupportedChildPanel? GeneratedCustom = null;
            var (LegendFG, LegendBG, _, _, LegendTFont, _, _) = GetThemeForComponent("Legend");

            // Check if this field is a parent for custom panels
            if (FieldsListHandledByGeneratedPanels != null)
            {
                var parentFields = FieldsListHandledByGeneratedPanels
                    .Where(ikvp => ikvp.Value.ParentField == kv.Key)
                    .ToList();

                if (parentFields.Any())
                {
                    var Fields = parentFields.First();
                    var parentTextBox = _Einputs.TryGetValue(kv.Key, out var control) && control is TextBox tb
                        ? tb
                        : null;

                    GeneratedCustom = PanelGenerators[Fields.Value.ControlName](Fields.Key, parentTextBox);

                    SetupCustomPanel(GeneratedCustom, Fields.Key, Fields.Value.ControlName, kv.Key, EFocusableList, GoToNextFromPanel);

                    rowSpanFromCustomPanels += GeneratedCustom.RowSpan();
                    CurrentNo += GeneratedCustom.RowSpan();

                    // Add to LayoutNext - only once
                    Console.WriteLine($"Adding custom panel to LayoutNext: {kv.Key}");
                    LayoutNext.Add((_EFieldNames[kv.Key], _Einputs[kv.Key], (Control)GeneratedCustom), CurrentNo);
                }
                else
                {
                    // Check if this field is part of a custom panel
                    var childFields = FieldsListHandledByGeneratedPanels
                        .Where(ikvp => ikvp.Key.Contains(kv.Key))
                        .ToList();

                    if (childFields.Any())
                    {
                        var Fields = childFields.First();
                        GeneratedCustom = PanelGenerators[Fields.Value.ControlName](Fields.Key, null);

                        SetupCustomPanel(GeneratedCustom, Fields.Key, Fields.Value.ControlName, null, EFocusableList, GoToNextFromPanel);

                        rowSpanFromCustomPanels += GeneratedCustom.RowSpan();
                        CurrentNo += GeneratedCustom.RowSpan();

                        // Update the table row to use the custom panel
                        if (_EFieldNames.TryGetValue(kv.Key, out var fieldLabel) && _ELegends.TryGetValue(kv.Key, out var legend))
                        {
                            var tableRow = new TableRow(fieldLabel, (Control)GeneratedCustom, legend)
                            {
                                ScaleHeight = ColorSettings.ExpandContentHeight,
                            };

                            // Replace the existing row in _EControlsAll
                            var existingIndex = _EControlsAll.FindIndex(tr =>
                                tr.Cells != null && tr.Cells.Count > 0 && tr.Cells[0].Control == fieldLabel);

                            if (existingIndex >= 0)
                            {
                                _EControlsAll[existingIndex] = tableRow;
                            }

                            // Add to LayoutNext - only once
                            Console.WriteLine($"Adding custom panel to LayoutNext: {kv.Key}");
                            LayoutNext.Add((fieldLabel, (Control)GeneratedCustom, legend), CurrentNo);
                        }
                    }
                }
            }

            // Remove the redundant addition at the end of the method
            // This was causing the duplicate key error
        }

        private void SetupCustomPanel(
    ILookupSupportedChildPanel GeneratedCustom,
    string[] Fields,
    string ControlName,
    string? ParentField,
    List<Eto.Forms.Control> EFocusableList,
    Action<Control> GoToNextFromPanel)  // Add this parameter
        {
            Console.WriteLine($"{Fields[0]}");

            foreach (string s in Fields)
            {
                CustomPanelInputRetrievalFunctions.Add(
                    s,
                    () => GeneratedCustom.LookupValue(s)
                );
                FieldsHandlerMapping.Add(
                    s,
                    GeneratedCustom
                );

                if (_Inputs.TryGetValue(s, out var input))
                {
                    GeneratedCustom.SetOriginalValue(s, input.Value);
                }
            }

            GeneratedCustom.SetGlobalChangeWatcher(() =>
            {
                var changedFields = ParentField != null
                    ? Fields.Concat([ParentField]).ToArray()
                    : Fields;
                AnythingChanged(changedFields);
            });

            EFocusableList.Add((Control)GeneratedCustom);
            GeneratedCustom.SetMoveNext(() =>
            {
                GoToNextFromPanel((Control)GeneratedCustom);  // Use the delegate parameter
            });
        }


        private Eto.Forms.Control CreateInputControl(
            KeyValuePair<string, (string ControlName, object Value, string? LookupFunctionCallback)> kv,
            Type valueType, Color FG, Color BG, Color FGc, Color BGc, Font TFont, Size CSize,
            string[] DenyList, string? IdentityColumn, List<Eto.Forms.Control> EFocusableList)
        {
            // Create a delegate for field change notification
            Action notifyFieldChanged = () => {
                if (ShouldUpdateField(kv.Key))
                {
                    AnythingChanged([kv.Key]);
                    _EChangeTracker.TryAdd(kv.Key, true);
                }
            };

            // Handle numeric types
            if (valueType == typeof(long) || valueType == typeof(int) ||
                valueType == typeof(double) || valueType == typeof(float))
            {
                if (kv.Value.Item3 == null)
                {
                    var _textBox = new TextBox()
                    {
                        Text = kv.Value.Item2?.ToString() ?? "0",
                        TextAlignment = TextAlignment.Right,
                        BackgroundColor = BG,
                        TextColor = FG,
                        Font = TFont,
                        Size = CSize,
                    };

                    _textBox.TextInput += (sender, e) => {
                        notifyFieldChanged();
                        _textBox.BackgroundColor = BGc;
                        _textBox.TextColor = FGc;
                    };

                    _textBox.KeyUp += GoToNext;

                    if (DenyList.Contains(kv.Key))
                        _textBox.ReadOnly = true;
                    if (kv.Key == IdentityColumn)
                        _textBox.Enabled = false;

                    EFocusableList.Add(_textBox);
                    return _textBox;
                }
                else
                {
                    // Button with lookup functionality
                    var button = new Button() { Text = kv.Value.Item2?.ToString() ?? "0" };
                    var legend = new Label();

                    if (DenyList.Contains(kv.Key))
                        button.Enabled = false;

                    button.Click += (_, _) => {
                        var lookupResult = InputHandler[kv.Value.Item3].Item1();
                        if (lookupResult.HasValue)
                        {
                            button.Text = lookupResult.ToString();
                            legend.Text = InputHandler[kv.Value.Item3].Item2(lookupResult.Value);
                            notifyFieldChanged();
                        }
                    };

                    EFocusableList.Add(button);
                    return button;
                }
            }

            // Handle boolean type
            if (valueType == typeof(bool))
            {
                var checkBox = new CheckBox() { Checked = (bool)(kv.Value.Item2 ?? false) };

                checkBox.CheckedChanged += (sender, e) => {
                    notifyFieldChanged();
                    checkBox.BackgroundColor = BGc;
                    checkBox.TextColor = FGc;
                };

                checkBox.TextColor = FG;
                checkBox.BackgroundColor = BG;
                checkBox.Size = CSize;
                checkBox.Font = TFont;
                checkBox.KeyUp += GoToNext;

                EFocusableList.Add(checkBox);
                return checkBox;
            }

            // Handle string type (default)
            var textBox = new TextBox() { Text = kv.Value.Item2?.ToString() ?? "" };

            if (kv.Key == IdentityColumn)
                textBox.Enabled = false;

            textBox.TextInput += (sender, e) => {
                notifyFieldChanged();
                textBox.BackgroundColor = BGc;
                textBox.TextColor = FGc;
            };

            textBox.BackgroundColor = BG;
            textBox.TextColor = FG;
            textBox.Font = TFont;
            textBox.Size = CSize;
            textBox.KeyUp += GoToNext;

            EFocusableList.Add(textBox);
            return textBox;
        }

        private void CacheValueAccessors(string key, Type valueType, Control control)
        {
            // Cache getter
            if (valueType == typeof(bool) && control is CheckBox cb)
            {
                _valueGetters[key] = () => cb.Checked ?? false;
                _valueSetters[key] = value => cb.Checked = (bool)value;
            }
            else if (control is TextBox tb)
            {
                if (_parsers.TryGetValue(valueType, out var parser))
                {
                    _valueGetters[key] = () => parser(tb.Text);
                    _valueSetters[key] = value => tb.Text = value?.ToString() ?? "";
                }
            }
            else if (control is Button btn && _parsers.TryGetValue(valueType, out var parser))
            {
                _valueGetters[key] = () => parser(btn.Text);
                _valueSetters[key] = value => btn.Text = value?.ToString() ?? "";
            }
        }

        private TableRow CreateTableRow(Control fieldLabel, Control inputControl, Control legend)
        {
            if (inputControl is TextBox tb)
            {
                tb.ShowBorder = false;
                return new TableRow(fieldLabel, new TableCell(tb, true), legend)
                {
                    ScaleHeight = ColorSettings.ExpandContentHeight,
                };
            }

            return new TableRow(fieldLabel, new TableCell(inputControl, true), legend)
            {
                ScaleHeight = ColorSettings.ExpandContentHeight,
            };
        }


        // Optimize ValidateInputs method
        public bool ValidateInputs()
        {
            var (LegendFG, LegendBG, _, _, LegendTFont, _, _) = GetThemeForComponent("Legend");
            bool allValid = true;

            // Use pre-cached getters and type information for validation
            foreach (var kvp in OriginalTypes)
            {
                var key = kvp.Key;
                var expectedType = kvp.Value;

                if (!ChangesOnly || _EChangeTracker.TryGetValue(key, out bool changed) && changed)
                {
                    bool isValid = true;

                    try
                    {
                        // Try to get the value using our cached getter
                        var value = _valueGetters.TryGetValue(key, out var getter)
                            ? getter()
                            : CustomPanelInputRetrievalFunctions[key]();

                        // Verify type matches expected
                        if (value == null && expectedType.IsValueType)
                        {
                            isValid = false;
                        }
                        else if (value != null && value.GetType() != expectedType)
                        {
                            // Try to convert to expected type
                            try
                            {
                                Convert.ChangeType(value, expectedType);
                            }
                            catch
                            {
                                isValid = false;
                            }
                        }
                    }
                    catch
                    {
                        isValid = false;
                    }

                    if (!isValid)
                    {
                        allValid = false;
                        HighlightInvalidField(key, LegendFG);
                    }
                    else
                    {
                        ResetFieldHighlight(key, LegendBG, LegendFG);
                    }
                }
            }

            return allValid;
        }

        private void HighlightInvalidField(string key, Color legendFG)
        {
            if (_EFieldNames.TryGetValue(key, out var fieldControl))
            {
                fieldControl.BackgroundColor = ColorSettings.AlternatingColor1;

                if (fieldControl is Label label)
                {
                    label.TextColor = LocalColor?.ForegroundColor ?? legendFG;
                }
                else if (fieldControl is CustomLabel customLabel)
                {
                    customLabel.ForegroundColor = LocalColor?.ForegroundColor ?? legendFG;
                    customLabel.Invalidate();
                }
            }
        }

        private void ResetFieldHighlight(string key, Color legendBG, Color legendFG)
        {
            if (_EFieldNames.TryGetValue(key, out var fieldControl))
            {
                fieldControl.BackgroundColor = LocalColor?.BackgroundColor ?? legendBG;

                if (fieldControl is Label label)
                {
                    label.TextColor = LocalColor?.ForegroundColor ?? legendFG;
                }
                else if (fieldControl is CustomLabel customLabel)
                {
                    customLabel.ForegroundColor = LocalColor?.ForegroundColor ?? legendFG;
                    customLabel.Invalidate();
                }
            }
        }


        private TableLayout OptimizeLayoutGeneration()
        {
            // Debug output to see what we're working with
            Console.WriteLine($"OptimizeLayoutGeneration called with {LayoutNext.Count} items and {nColumns} columns");

            int maxRowOffset = LayoutNext.Count > 0 ? LayoutNext.Max(x => x.Value) : 0;
            Console.WriteLine($"Max row offset: {maxRowOffset}");

            // Create bins (one per column) to temporarily hold (leftControl, rightControl) pairs.
            var columnBins = new List<List<(Control, Control)>>();
            for (int col = 0; col < nColumns; col++)
                columnBins.Add(new List<(Control, Control)>());

            // Distribute each item into the appropriate column based on its row offset.
            foreach (var kvp in LayoutNext)
            {
                // Calculate which column this control should go in
                int currentColumnIndex = (int)Math.Floor((double)nColumns * kvp.Value / (maxRowOffset + 1));
                if (currentColumnIndex >= nColumns)
                    currentColumnIndex = nColumns - 1;

                Console.WriteLine($"Distributing to column {currentColumnIndex}: {kvp.Key.MainControl?.GetType().Name ?? "null"}");

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

            // Create the outer TableLayout
            var outerLayout = new TableLayout()
            {
                Spacing = new Size(2, 2),
                Padding = new Padding(2),
            };

            // Create a single row with all columns side by side
            var outerRow = new TableRow();

            // Build each column and add it to the outer row
            for (int i = 0; i < nColumns; i++)
            {
                if (i < columnBins.Count && columnBins[i].Count > 0)
                {
                    var rows = new List<TableRow>();

                    foreach (var pair in columnBins[i])
                    {
                        var row = new TableRow() { ScaleHeight = ColorSettings.ExpandContentHeight };

                        // Create new controls based on the original controls
                        Control newLeftControl = null;
                        Control newRightControl = null;

                        // If the left control is not null, create a new label based on the original
                        if (pair.Item1 != null)
                        {
                            if (pair.Item1 is Label originalLabel)
                            {
                                var newLabel = new Label()
                                {
                                    Text = originalLabel.Text,
                                    TextColor = originalLabel.TextColor,
                                    BackgroundColor = originalLabel.BackgroundColor,
                                    Font = originalLabel.Font,
                                    Width = originalLabel.Width,
                                    Height = originalLabel.Height,
                                    Wrap = originalLabel.Wrap
                                };
                                newLeftControl = newLabel;
                            }
                            else if (pair.Item1 is CustomLabel originalCustomLabel)
                            {
                                var newCustomLabel = new CustomLabel()
                                {
                                    Text = originalCustomLabel.Text,
                                    ForegroundColor = originalCustomLabel.ForegroundColor,
                                    Width = originalCustomLabel.Width,
                                    Height = originalCustomLabel.Height
                                };
                                newLeftControl = newCustomLabel;
                            }
                            else
                            {
                                // For other control types, create a new Panel with the same size
                                newLeftControl = new Panel() { Size = pair.Item1.Size };
                            }
                        }
                        else
                        {
                            // Create an empty control to preserve alignment
                            newLeftControl = new Panel() { Size = new Size(0, 0) };
                        }

                        // Create a new right control based on the original
                        if (pair.Item2 is TextBox originalTextBox)
                        {
                            var newTextBox = new TextBox()
                            {
                                Text = originalTextBox.Text,
                                TextAlignment = originalTextBox.TextAlignment,
                                TextColor = originalTextBox.TextColor,
                                BackgroundColor = originalTextBox.BackgroundColor,
                                Font = originalTextBox.Font,
                                Width = originalTextBox.Width,
                                Height = originalTextBox.Height,
                                ReadOnly = originalTextBox.ReadOnly,
                                ShowBorder = false // Always set to false for consistent look
                            };
                            newRightControl = newTextBox;
                        }
                        else if (pair.Item2 is CheckBox originalCheckBox)
                        {
                            var newCheckBox = new CheckBox()
                            {
                                Text = originalCheckBox.Text,
                                TextColor = originalCheckBox.TextColor,
                                BackgroundColor = originalCheckBox.BackgroundColor,
                                Font = originalCheckBox.Font,
                                Width = originalCheckBox.Width,
                                Height = originalCheckBox.Height,
                                Checked = originalCheckBox.Checked
                            };
                            newRightControl = newCheckBox;
                        }
                        else if (pair.Item2 is Button originalButton)
                        {
                            var newButton = new Button()
                            {
                                Text = originalButton.Text,
                                TextColor = originalButton.TextColor,
                                BackgroundColor = originalButton.BackgroundColor,
                                Font = originalButton.Font,
                                Width = originalButton.Width,
                                Height = originalButton.Height,
                                Enabled = originalButton.Enabled
                            };
                            newRightControl = newButton;
                        }
                        else if (pair.Item2 is ILookupSupportedChildPanel originalPanel)
                        {
                            // For custom panels, we can't create a new one, so we'll use the original
                            // but we need to make sure it's not already part of another visual tree
                            // This is a workaround - ideally we should be able to create a new instance
                            newRightControl = (Control)originalPanel;
                        }
                        else
                        {
                            // For other control types, create a new Panel with the same size
                            newRightControl = new Panel() { Size = pair.Item2.Size };
                        }

                        // Set the width for the controls
                        if (newLeftControl != null)
                            newLeftControl.Width = ColorSettings.InnerLabelWidth ?? -1;
                        if (newRightControl != null)
                            newRightControl.Width = ColorSettings.ControlWidth ?? 100;

                        // Add the controls to the row
                        row.Cells.Add(new TableCell(newLeftControl, false)); // Don't expand label
                        row.Cells.Add(new TableCell(newRightControl, true)); // Expand main control

                        rows.Add(row);
                    }

                    var colLayout = new TableLayout(rows.ToArray())
                    {
                        Spacing = new Size(5, 3),
                        Padding = new Padding(4),
                    };

                    outerRow.Cells.Add(new TableCell(colLayout, true));
                }
                else
                {
                    // Add an empty column if there are fewer controls than columns
                    outerRow.Cells.Add(new TableCell(new Panel(), true));
                }
            }

            // Add the row to the outer layout
            outerLayout.Rows.Add(outerRow);

            return outerLayout;
        }

        public long Save()
        {
            if (_new)
                return _SaveNewHandler(ConvertedInputs);
            else
                return _SaveExistingHandler(ConvertedInputs);
        }
    }
}