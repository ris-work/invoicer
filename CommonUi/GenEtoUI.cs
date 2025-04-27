using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using Microsoft.Win32.SafeHandles;
using Tomlyn;

//using Xceed.Wpf.Toolkit.Converters;

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
        public delegate long SaveHandler(IReadOnlyDictionary<string, object> UserInput);
        public bool ChangesOnly = false;
        public string Context = "default";
        public IReadOnlyDictionary<string, object> Values
        {
            get => _Values;
        }
        private Dictionary<string, object> _Values = new();
        private Dictionary<string, Eto.Forms.Control> _Controls = new();
        private bool _new = false;
        private SaveHandler _SaveNewHandler;
        private SaveHandler _SaveExistingHandler;
        List<Eto.Forms.TableRow> _EControls = new();
        Dictionary<string, Eto.Forms.Control> _Einputs = new();
        Dictionary<string, Eto.Forms.Control?> _ELegends = new();
        Dictionary<string, bool> _EChangeTracker = new();
        Dictionary<string, object> ConvertedInputs = new();
        IReadOnlyDictionary<string, (string, object, string?)> _Inputs;
        public string Identity = "";
        IReadOnlyDictionary<string, object> Configuration;

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
            Eto.Drawing.Size CSize,
            Eto.Drawing.Size TSize
        ) GetThemeForComponent(string ControlName)
        {
            Color FG = EtoThemingUtilities.GetNestedColor(
                Configuration,
                Context,
                ControlName,
                "foreground_color",
                Eto.Drawing.Colors.DarkBlue
            );
            Color BG = EtoThemingUtilities.GetNestedColor(
                Configuration,
                Context,
                ControlName,
                "background_color",
                Eto.Drawing.Colors.White
            );
            Color FGc = EtoThemingUtilities.GetNestedColor(
                Configuration,
                Context,
                ControlName,
                "foreground_color_changed",
                Eto.Drawing.Colors.DarkSlateGray
            );
            Color BGc = EtoThemingUtilities.GetNestedColor(
                Configuration,
                Context,
                ControlName,
                "background_color_changed",
                Eto.Drawing.Colors.LightYellow
            );
            Font EFont = EtoThemingUtilities.GetNestedFont(
                Configuration,
                Context,
                ControlName,
                Eto.Drawing.Fonts.Monospace(12)
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
                new Eto.Drawing.Size(12, 30)
            );
            return (FG, BG, FGc, BGc, EFont, TSize, CSize);
        }

        //public delegate long SaveExistingHandler(IReadOnlyDictionary<string, object> UserInput);
        public void ConvertInputs()
        {
            ConvertedInputs = new();
            foreach (var e in _Inputs)
            {
                if (!ChangesOnly || _EChangeTracker.TryGetValue(e.Key, out bool x) && x)
                {
                    Type T = e.Value.Item2.GetType();
                    if (T == typeof(long))
                    {
                        if (e.Value.Item3 == null)
                        {
                            ConvertedInputs.Add(e.Key, long.Parse(((TextBox)_Einputs[e.Key]).Text));
                        }
                        else
                        {
                            ConvertedInputs.Add(e.Key, long.Parse(((Button)_Einputs[e.Key]).Text));
                        }
                    }
                    else if (T == typeof(int))
                    {
                        if (e.Value.Item3 == null)
                        {
                            ConvertedInputs.Add(e.Key, int.Parse(((TextBox)_Einputs[e.Key]).Text));
                        }
                        else
                        {
                            ConvertedInputs.Add(e.Key, long.Parse(((Button)_Einputs[e.Key]).Text));
                        }
                    }
                    else if (T == typeof(float))
                    {
                        if (e.Value.Item3 == null)
                        {
                            ConvertedInputs.Add(
                                e.Key,
                                float.Parse(((TextBox)_Einputs[e.Key]).Text)
                            );
                        }
                        else
                        {
                            ConvertedInputs.Add(e.Key, float.Parse(((Button)_Einputs[e.Key]).Text));
                        }
                    }
                    else if (T == typeof(double))
                    {
                        if (e.Value.Item3 == null)
                        {
                            ConvertedInputs.Add(
                                e.Key,
                                double.Parse(((TextBox)_Einputs[e.Key]).Text)
                            );
                        }
                        else
                        {
                            ConvertedInputs.Add(
                                e.Key,
                                double.Parse(((Button)_Einputs[e.Key]).Text)
                            );
                        }
                    }
                    else if (T == typeof(string))
                    {
                        ConvertedInputs.Add(e.Key, ((TextBox)_Einputs[e.Key]).Text);
                    }
                    else if (T == typeof(bool))
                    {
                        ConvertedInputs.Add(e.Key, ((CheckBox)_Einputs[e.Key]).Checked == true);
                    }
                }
            }
        }

        public GenEtoUI(
            IReadOnlyDictionary<string, (string, object, string?)> Inputs,
            SaveHandler SaveNewHandler,
            SaveHandler SaveExistingHandler,
            IReadOnlyDictionary<string, (ShowAndGetValue, LookupValue)> InputHandler,
            string? IdentityColumn,
            bool ChangesOnly = false
        )
        {
            InitializeConfiguration();
            (var FGF, var BGF, var FGcF, var BGcF, var TFontF, var TSizeF, var CSizeF) =
                GetThemeForComponent("form");
            var SaveButtonTheme = GetThemeForComponent("save");
            var NewButtonTheme = GetThemeForComponent("new");
            var ViewButtonTheme = GetThemeForComponent("view");
            var CancelButtonTheme = GetThemeForComponent("cancel");
            BackgroundColor = BGF;

            this.ChangesOnly = ChangesOnly;
            //Eto.Drawing.Color BackgroundColor, ForegroundColor, ChangedBackgroundColor, ChangedForegroundColor;
            //BackgroundColor = Eto.Drawing.Colors.White;
            //ForegroundColor = Eto.Drawing.Colors.Black;
            //ChangedBackgroundColor = Eto.Drawing.Colors.LightCyan;
            //ChangedForegroundColor = Eto.Drawing.Colors.DarkGoldenrod;
            List<Eto.Forms.TableRow> EControls = new();
            _Inputs = Inputs;

            var E = Inputs.AsEnumerable();
            foreach (var kv in E)
            {
                (var FG, var BG, var FGc, var BGc, var TFont, var TSize, var CSize) =
                    GetThemeForComponent(kv.Key);
                var BackgroundColor = BG;
                var ForegroundColor = FG;
                var ChangedBackgroundColor = BGc;
                var ChangedForegroundColor = FGc;
                Eto.Forms.TableRow EControl;
                Eto.Forms.Label? ELegend = null;
                Eto.Forms.Control EInput = new Label();
                if (
                    kv.Value.Item2.GetType() == typeof(long)
                    || kv.Value.Item2.GetType() == typeof(int)
                    || kv.Value.Item2.GetType() == typeof(double)
                    || kv.Value.Item2.GetType() == typeof(float)
                )
                {
                    if (
                        kv.Value.Item2.GetType() == typeof(long)
                        || kv.Value.Item2.GetType() == typeof(Int64)
                    )
                    {
                        if (kv.Value.Item3 == null)
                        {
                            EventHandler<TextInputEventArgs> ChangedIndication = (e, a) =>
                            {
                                if (e is TextBox tb)
                                {
                                    tb.BackgroundColor = ChangedBackgroundColor;
                                    tb.TextColor = ChangedForegroundColor;
                                    _EChangeTracker.TryAdd(kv.Key, true);
                                }
                            };
                            EInput = new TextBox()
                            {
                                Text = (kv.Value.Item2).ToString(),
                                TextAlignment = TextAlignment.Right,
                            };
                            ((TextBox)EInput).TextInput += ChangedIndication;
                            ((TextBox)EInput).BackgroundColor = BG;
                            ((TextBox)EInput).TextColor = FG;
                            ((TextBox)EInput).Font = TFont;
                            ((TextBox)EInput).Size = CSize;
                            if (kv.Key == IdentityColumn)
                            {
                                ((TextBox)EInput).Enabled = false;
                            }
                        }
                        else
                        {
                            EventHandler<TextInputEventArgs> ChangedIndication = (e, a) =>
                            {
                                if (e is TextBox tb)
                                {
                                    tb.BackgroundColor = ChangedBackgroundColor;
                                    tb.TextColor = ChangedForegroundColor;
                                    _EChangeTracker.TryAdd(kv.Key, true);
                                }
                            };
                            EInput = new Button() { Text = ((long)kv.Value.Item2).ToString() };
                            ELegend = new Label() { };
                            ((Button)EInput).Click += (_, _) =>
                            {
                                long? IHS = InputHandler[kv.Value.Item3].Item1();
                                if (IHS != null)
                                {
                                    ((Button)EInput).Text = IHS.ToString();
                                    ((Label)ELegend).Text = InputHandler[kv.Value.Item3]
                                        .Item2(IHS.GetValueOrDefault(0));
                                    ChangedIndication(EInput, null);
                                }
                            };
                        }
                    }
                    else if (kv.Value.Item2.GetType() == typeof(double))
                    {
                        EventHandler<TextInputEventArgs> ChangedIndication = (e, a) =>
                        {
                            if (e is TextBox tb)
                            {
                                tb.BackgroundColor = ChangedBackgroundColor;
                                tb.TextColor = ChangedForegroundColor;
                                _EChangeTracker.TryAdd(kv.Key, true);
                            }
                        };
                        EInput = new TextBox()
                        {
                            Text = ((double)kv.Value.Item2).ToString(),
                            TextAlignment = TextAlignment.Right,
                        };
                        ((TextBox)EInput).TextInput += ChangedIndication;
                        ((TextBox)EInput).BackgroundColor = BG;
                        ((TextBox)EInput).TextColor = FG;
                        ((TextBox)EInput).Font = TFont;
                        ((TextBox)EInput).Size = CSize;
                    }
                    else if (kv.Value.Item2.GetType() == typeof(bool))
                    {
                        EventHandler<TextInputEventArgs> ChangedIndication = (e, a) =>
                        {
                            if (e is CheckBox cb)
                            {
                                cb.BackgroundColor = ChangedBackgroundColor;
                                cb.TextColor = ChangedForegroundColor;
                                _EChangeTracker.TryAdd(kv.Key, true);
                            }
                        };
                        EInput = new CheckBox() { Text = ((bool)kv.Value.Item2).ToString() };
                        ((CheckBox)EInput).TextInput += ChangedIndication;
                        ((CheckBox)EInput).TextColor = FG;
                        ((CheckBox)EInput).BackgroundColor = BG;
                        ((CheckBox)EInput).Size = CSize;
                        ((CheckBox)EInput).Font = TFont;
                    }
                    else if (kv.Value.Item2.GetType() == typeof(string))
                    {
                        EventHandler<TextInputEventArgs> ChangedIndication = (e, a) =>
                        {
                            if (e is TextBox tb)
                            {
                                tb.BackgroundColor = ChangedBackgroundColor;
                                tb.TextColor = ChangedForegroundColor;
                                _EChangeTracker.TryAdd(kv.Key, true);
                            }
                        };
                        EInput = new TextBox() { Text = ((string)kv.Value.Item2).ToString() };
                        if (kv.Key == IdentityColumn)
                        {
                            ((TextBox)EInput).Enabled = false;
                        }
                        ((TextBox)EInput).TextInput += ChangedIndication;
                        ((TextBox)EInput).BackgroundColor = BG;
                        ((TextBox)EInput).TextColor = FG;
                        ((TextBox)EInput).Font = TFont;
                        ((TextBox)EInput).Size = CSize;
                    }
                }
                else if (kv.Value.Item2.GetType() == typeof(bool))
                {
                    EventHandler<System.EventArgs> ChangedIndication = (e, a) =>
                    {
                        if (e is CheckBox cb)
                        {
                            cb.BackgroundColor = ChangedBackgroundColor;
                            cb.TextColor = ChangedForegroundColor;

                            _EChangeTracker.TryAdd(kv.Key, true);
                        }
                    };
                    EInput = new CheckBox() { Checked = ((bool)kv.Value.Item2) };
                    ((CheckBox)EInput).CheckedChanged += ChangedIndication;
                    ((CheckBox)EInput).TextColor = FG;
                    ((CheckBox)EInput).BackgroundColor = BG;
                    ((CheckBox)EInput).Size = CSize;
                    ((CheckBox)EInput).Font = TFont;
                }
                else
                {
                    EventHandler<TextInputEventArgs> ChangedIndication = (e, a) =>
                    {
                        if (e is TextBox tb)
                        {
                            tb.BackgroundColor = ChangedBackgroundColor;
                            tb.TextColor = ChangedForegroundColor;
                            _EChangeTracker.TryAdd(kv.Key, true);
                        }
                    };
                    EInput = new TextBox() { Text = ((string)kv.Value.Item2).ToString() };
                    ((TextBox)EInput).TextInput += ChangedIndication;
                    ((TextBox)EInput).TextColor = FG;
                    ((TextBox)EInput).BackgroundColor = BG;
                    ((TextBox)EInput).Size = CSize;
                    ((TextBox)EInput).Font = TFont;
                }
                EInput.Width = 300;
                EControl = new TableRow(new Label() { Text = kv.Value.Item1 }, EInput, ELegend) { };
                _Einputs.Add(kv.Key, EInput);
                _ELegends.Add(kv.Key, ELegend);
                EControls.Add(EControl);
            }
            _EControls = EControls;

            Button NewButton = new Button() { Text = "New" };
            Button SaveButton = new Button() { Text = "Save" };
            Button ViewButton = new Button() { Text = "View" };
            Button CancelButton = new Button() { Text = "Cancel" };
            NewButton.Font = NewButtonTheme.TFont;
            NewButton.BackgroundColor = NewButtonTheme.BG;
            SaveButton.Font = SaveButtonTheme.TFont;
            SaveButton.BackgroundColor = SaveButtonTheme.BG;
            ViewButton.Font = ViewButtonTheme.TFont;
            ViewButton.BackgroundColor = ViewButtonTheme.BG;
            CancelButton.Font = CancelButtonTheme.TFont;
            CancelButton.BackgroundColor = CancelButtonTheme.BG;

            NewButton.Click += (_, _) =>
            {
                ConvertInputs();
            };
            SaveButton.Click += (_, _) =>
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
            };
            ViewButton.Click += (_, _) =>
            {
                ConvertInputs();
                MessageBox.Show(
                    $"New: {_new.ToString()}, Serialized: {JsonSerializer.Serialize(ConvertedInputs)}",
                    "Serialized",
                    MessageBoxType.Information
                );
            };
            var ActionButtons = new StackLayout(NewButton, SaveButton, ViewButton, CancelButton)
            {
                Orientation = Orientation.Horizontal,
            };
            var GeneratedControls = new TableLayout(_EControls.ToArray())
            {
                Padding = 10,
                Spacing = new Eto.Drawing.Size(10, 3),
            };
            Content = new StackLayout(ActionButtons, GeneratedControls)
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Center,
            };
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
