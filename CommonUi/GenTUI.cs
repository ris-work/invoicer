using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Terminal.Gui;

namespace CommonUi
{
    public delegate long SaveHandler(IReadOnlyDictionary<string, object> UserInput);

    public class GenTopLevel : Toplevel
    {
        public GenTopLevel(
            IReadOnlyDictionary<string, (string, object, string?)> Inputs,
            SaveHandler SaveNewHandler,
            SaveHandler SaveExistingHandler,
            IReadOnlyDictionary<string, (ShowAndGetValue, LookupValue)> InputHandler,
            string? IdentityColumn
        )
        {
            //Add(new Button() { Text = "Hello" });
            //Add(new Button() { Text = "Hello" }, new Button() { Text = "Hello" }, new Button() { Text = "Hello" });
            Enabled = true;
            Add(
                new GenTUI(
                    Inputs,
                    SaveNewHandler,
                    SaveExistingHandler,
                    InputHandler,
                    IdentityColumn
                )
            );
            LayoutSubviews();
        }
    }

    public class GenTUI : Terminal.Gui.View
    {
        public IReadOnlyDictionary<string, object> Values
        {
            get => _Values;
        }
        private Dictionary<string, object> _Values = new();
        private Dictionary<string, Terminal.Gui.View> _Controls = new();
        private bool _new = false;
        private SaveHandler _SaveNewHandler;
        private SaveHandler _SaveExistingHandler;
        List<Terminal.Gui.View> _EControls = new();
        Dictionary<string, Terminal.Gui.View> _Einputs = new();
        Dictionary<string, object> ConvertedInputs = new();
        IReadOnlyDictionary<string, (string, object, string?)> _Inputs;

        //public delegate long SaveExistingHandler(IReadOnlyDictionary<string, object> UserInput);
        public void ConvertInputs()
        {
            ConvertedInputs = new();
            foreach (var e in _Inputs)
            {
                Type T = e.Value.Item2.GetType();
                if (T == typeof(long))
                {
                    if (e.Value.Item3 == null)
                    {
                        ConvertedInputs.Add(e.Key, long.Parse(((TextField)_Einputs[e.Key]).Text));
                    }
                    else
                    {
                        ConvertedInputs.Add(e.Key, long.Parse(((Button)_Einputs[e.Key]).Text));
                    }
                }
                else if (T == typeof(UInt64))
                {
                    if (e.Value.Item3 == null)
                    {
                        ConvertedInputs.Add(e.Key, long.Parse(((TextField)_Einputs[e.Key]).Text));
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
                        ConvertedInputs.Add(e.Key, int.Parse(((TextField)_Einputs[e.Key]).Text));
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
                        ConvertedInputs.Add(e.Key, float.Parse(((TextField)_Einputs[e.Key]).Text));
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
                        ConvertedInputs.Add(e.Key, double.Parse(((TextField)_Einputs[e.Key]).Text));
                    }
                    else
                    {
                        ConvertedInputs.Add(e.Key, double.Parse(((Button)_Einputs[e.Key]).Text));
                    }
                }
                else if (T == typeof(string))
                {
                    ConvertedInputs.Add(e.Key, ((TextField)_Einputs[e.Key]).Text);
                }
                else if (T == typeof(bool))
                {
                    ConvertedInputs.Add(
                        e.Key,
                        ((CheckBox)_Einputs[e.Key]).CheckedState == CheckState.Checked
                    );
                }
            }
        }

        public GenTUI(
            IReadOnlyDictionary<string, (string, object, string?)> Inputs,
            SaveHandler SaveNewHandler,
            SaveHandler SaveExistingHandler,
            IReadOnlyDictionary<string, (ShowAndGetValue, LookupValue)> InputHandler,
            string? IdentityColumn
        )
        {
            List<View> EControls = new();
            _Inputs = Inputs;

            var E = Inputs.AsEnumerable();
            KeyValuePair<string, (string, object, string?)> oldkv;
            Terminal.Gui.Attribute TextFieldColors = new Terminal.Gui.Attribute(
                Terminal.Gui.Color.Black,
                Terminal.Gui.Color.BrightGreen
            );
            Terminal.Gui.Attribute TextFieldSelected = new Terminal.Gui.Attribute(
                Terminal.Gui.Color.BrightGreen,
                Terminal.Gui.Color.Black
            );
            var ColorSchemeTF = new ColorScheme(
                TextFieldColors,
                TextFieldSelected,
                TextFieldColors,
                TextFieldColors,
                TextFieldSelected
            );
            Terminal.Gui.Attribute CheckBoxColors = new Terminal.Gui.Attribute(
                Terminal.Gui.Color.Black,
                Terminal.Gui.Color.BrightBlue
            );
            Terminal.Gui.Attribute CheckBoxSelected = new Terminal.Gui.Attribute(
                Terminal.Gui.Color.BrightBlue,
                Terminal.Gui.Color.Black
            );
            var ColorSchemeCB = new ColorScheme(
                CheckBoxColors,
                CheckBoxSelected,
                CheckBoxColors,
                CheckBoxColors,
                CheckBoxSelected
            );
            Terminal.Gui.Attribute ButtonColors = new Terminal.Gui.Attribute(
                Terminal.Gui.Color.Black,
                Terminal.Gui.Color.BrightYellow
            );
            Terminal.Gui.Attribute ButtonSelected = new Terminal.Gui.Attribute(
                Terminal.Gui.Color.BrightYellow,
                Terminal.Gui.Color.Black
            );
            var ColorSchemeBTN = new ColorScheme(
                ButtonColors,
                ButtonSelected,
                ButtonColors,
                ButtonColors,
                ButtonSelected
            );
            Terminal.Gui.Attribute LabelColors = new Terminal.Gui.Attribute(
                Terminal.Gui.Color.Gray,
                Terminal.Gui.Color.Black
            );
            Terminal.Gui.Attribute LabelSelected = new Terminal.Gui.Attribute(
                Terminal.Gui.Color.Black,
                Terminal.Gui.Color.Gray
            );
            var ColorSchemeLabel = new ColorScheme(
                LabelColors,
                LabelSelected,
                LabelColors,
                LabelColors,
                LabelSelected
            );
            foreach (var kv in E)
            {
                View EControl;
                var ELabel = new Label()
                {
                    Text = kv.Value.Item1,
                    Width = Dim.Auto(),
                    Height = Dim.Auto(),
                };
                //if (EControls.Count > 0) ELabel.Y = Pos.Bottom(EControls.Last());
                Terminal.Gui.View EInput = new Label();

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
                            EInput = new TextField()
                            {
                                Text = (kv.Value.Item2).ToString(),
                                TextFormatter = new TextFormatter()
                                {
                                    Text = (kv.Value.Item2).ToString(),
                                    Alignment = Alignment.End,
                                },
                                ReadOnly = false,
                                ColorScheme = ColorSchemeTF,
                                TextAlignment = Alignment.Start,
                            };
                        }
                        else
                        {
                            EInput = new Button()
                            {
                                Text = ((long)kv.Value.Item2).ToString(),
                                ColorScheme = ColorSchemeBTN,
                            };
                            ((Button)EInput).MouseClick += (_, _) =>
                            {
                                long? IHS = InputHandler[kv.Value.Item3].Item1();
                                if (IHS != null)
                                    ((Button)EInput).Text = IHS.ToString();
                            };
                        }
                    }
                    else if (kv.Value.Item2.GetType() == typeof(bool))
                    {
                        EInput = new CheckBox()
                        {
                            Text = ((bool)kv.Value.Item2).ToString(),
                            Enabled = true,
                            ColorScheme = ColorSchemeCB,
                        };
                    }
                    else if (kv.Value.Item2.GetType() == typeof(double))
                    {
                        EInput = new TextField()
                        {
                            Text = ((double)kv.Value.Item2).ToString(),
                            ReadOnly = false,
                            ColorScheme = ColorSchemeTF,
                        };
                    }
                    else if (kv.Value.Item2.GetType() == typeof(float))
                    {
                        EInput = new TextField()
                        {
                            Text = ((float)kv.Value.Item2).ToString(),
                            ReadOnly = false,
                            ColorScheme = ColorSchemeTF,
                        };
                    }
                    else if (kv.Value.Item2.GetType() == typeof(string))
                    {
                        EInput = new TextField()
                        {
                            Text = ((string)kv.Value.Item2).ToString(),
                            ReadOnly = false,
                            ColorScheme = ColorSchemeTF,
                        };
                    }
                }
                else if (kv.Value.Item2.GetType() == typeof(bool))
                {
                    EInput = new CheckBox()
                    {
                        CheckedState = ((bool)kv.Value.Item2) ? CheckState.Checked: CheckState.UnChecked,
                        ColorScheme = ColorSchemeTF,
                    };
                }
                else
                {
                    EInput = new TextField()
                    {
                        Text = ((string)kv.Value.Item2).ToString(),
                        ReadOnly = false,
                        ColorScheme = ColorSchemeTF,
                    };
                }

                EControl = (new View() { Width = Dim.Fill(), Height = Dim.Auto() });
                EControl.Add(ELabel);

                //EInput.Width = Dim.Fill();
                //if (EControls.Count > 0) EInput.Y = Pos.Bottom(EControls.Last()) + 1;
                //else EInput.Y = 0;
                ELabel.ColorScheme = ColorSchemeLabel;
                ELabel.Width = Dim.Auto();
                ELabel.Height = Dim.Auto();
                EInput.X = 42;
                EInput.Width = 40;
                EInput.Enabled = true;
                EControl.Add(EInput);
                _Einputs.Add(kv.Key, EInput);
                EControls.Add(EControl);
                //EControl.Add(ELabel);
                oldkv = kv;
            }
            _EControls = EControls;

            Button NewButton = new Button() { Text = "New" };
            Button SaveButton = new Button() { Text = "Save", X = Pos.Right(NewButton) + 4 };
            Button ViewButton = new Button() { Text = "View", X = Pos.Right(SaveButton) + 4 };
            Button CancelButton = new Button() { Text = "Cancel", X = Pos.Right(ViewButton) + 4 };
            NewButton.MouseClick += (_, _) =>
            {
                ConvertInputs();
            };
            SaveButton.MouseClick += (_, _) =>
            {
                ConvertInputs();
            };
            ViewButton.MouseClick += (_, _) =>
            {
                ConvertInputs();
                MessageBox.Query(
                    "Serialized",
                    $"New: {_new.ToString()}, Serialized: {JsonSerializer.Serialize(ConvertedInputs)}",
                    "Ok"
                );
            };
            View ActionButtons = new View() { Width = Dim.Auto(), Height = Dim.Auto() };
            ActionButtons.Add(NewButton, SaveButton, ViewButton, CancelButton);
            ActionButtons.Width = 50;
            //var GeneratedControls = new View() { Width = Dim.Auto(), Height = Dim.Auto(), X = 0, Y = Pos.Bottom(ActionButtons)+1 };
            //GeneratedControls.Add(_EControls.First());
            int i = 0;
            Add(ActionButtons);
            View Contents = new FrameView()
            {
                Y = Pos.Bottom(ActionButtons),
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                Enabled = true,
            };
            View prev = null;
            foreach (var c in EControls)
            {
                i++;
                //MessageBox.Query("Attempt", $"{i}", "Dismiss");
                if (prev != null)
                    c.Y = Pos.Bottom(prev) + 1;
                prev = c;
                Contents.Add(c);
                //if (i == 10) break;
            }
            Contents.Width = Dim.Fill();
            Contents.Height = Dim.Auto();
            Add(Contents);
            //Add(GeneratedControls);
            //Add(new Button() { Text = "Hello" });
            Width = Dim.Fill();
            Height = Dim.Fill();
            Enabled = true;

            //Content = new StackLayout(ActionButtons, GeneratedControls) { Orientation = Orientation.Vertical, HorizontalContentAlignment = HorizontalAlignment.Center };
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
