using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui;
using System.Text.Json;


namespace CommonUi
{
    public class GenMaUI : ContentView
    {
        public delegate long SaveHandler(IReadOnlyDictionary<string, object> UserInput);
        public IReadOnlyDictionary<string, object> Values
        {
            get => _Values;
        }
        private Dictionary<string, object> _Values = new();
        private Dictionary<string, Eto.Forms.Control> _Controls = new();
        private bool _new = false;
        private SaveHandler _SaveNewHandler;
        private SaveHandler _SaveExistingHandler;
        Grid _MControls = new();
        Dictionary<string, View> _Einputs = new();
        Dictionary<string, View> _ELegends = new();
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
                        ConvertedInputs.Add(e.Key, long.Parse(((Entry)_Einputs[e.Key]).Text));
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
                        ConvertedInputs.Add(e.Key, int.Parse(((Entry)_Einputs[e.Key]).Text));
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
                        ConvertedInputs.Add(e.Key, float.Parse(((Entry)_Einputs[e.Key]).Text));
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
                        ConvertedInputs.Add(e.Key, double.Parse(((Entry)_Einputs[e.Key]).Text));
                    }
                    else
                    {
                        ConvertedInputs.Add(e.Key, double.Parse(((Button)_Einputs[e.Key]).Text));
                    }
                }
                else if (T == typeof(string))
                {
                    ConvertedInputs.Add(e.Key, ((Entry)_Einputs[e.Key]).Text);
                }
                else if (T == typeof(bool))
                {
                    ConvertedInputs.Add(e.Key, ((CheckBox)_Einputs[e.Key]).IsChecked);
                }
            }
        }

        public GenMaUI(
            IReadOnlyDictionary<string, (string, object, string?)> Inputs,
            SaveHandler SaveNewHandler,
            SaveHandler SaveExistingHandler,
            IReadOnlyDictionary<string, (ShowAndGetValue, LookupValue)> InputHandler,
            string? IdentityColumn
        )
        {
            int i = 0;
            Grid MControls = new();
            List<Eto.Forms.TableRow> EControls = new();
            _Inputs = Inputs;

            var E = Inputs.AsEnumerable();
            foreach (var kv in E)
            {
                Eto.Forms.TableRow EControl;
                
                View? ELegend = null;
                View EInput = new Label();
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
                            EInput = new Entry()
                            {
                                Text = (kv.Value.Item2).ToString(),
                                HorizontalTextAlignment = TextAlignment.End,
                            };
                        }
                        else
                        {
                            EInput = new Button() { Text = ((long)kv.Value.Item2).ToString() };
                            ELegend = new Label() { };
                            ((Button)EInput).Clicked += (_, _) =>
                            {
                                long? IHS = InputHandler[kv.Value.Item3].Item1();
                                if (IHS != null)
                                {
                                    ((Button)EInput).Text = IHS.ToString();
                                    ((Label)ELegend).Text = InputHandler[kv.Value.Item3]
                                        .Item2(IHS.GetValueOrDefault(0));
                                }
                            };
                        }
                    }
                    else if (kv.Value.Item2.GetType() == typeof(double))
                    {
                        EInput = new Entry()
                        {
                            Text = ((double)kv.Value.Item2).ToString(),
                            HorizontalTextAlignment = TextAlignment.End,
                        };
                    }
                    else if (kv.Value.Item2.GetType() == typeof(bool))
                    {
                        EInput = new CheckBox() { };// = ((bool)kv.Value.Item2).ToString() };
                    }
                    else if (kv.Value.Item2.GetType() == typeof(string))
                    {
                        EInput = new Entry() { Text = ((string)kv.Value.Item2).ToString() };
                    }
                }
                else
                {
                    EInput = new Entry() { Text = ((string)kv.Value.Item2).ToString() };
                }
                //EInput.Width = 300;
                //EControl = new Eto.Forms.TableRow(new Label() { Text = kv.Value.Item1 }, EInput, ELegend) { };
                _Einputs.Add(kv.Key, EInput);
                _ELegends.Add(kv.Key, ELegend);
                MControls.Add(ELegend, 0, i);
                MControls.Add(EInput, 1, i);
                //MControls.Add(EInput, 1, i);
                //MControls.Add(ELegend);
                i++;
            }
            _MControls = MControls;

            Button NewButton = new Button() { Text = "New" };
            Button SaveButton = new Button() { Text = "Save" };
            Button ViewButton = new Button() { Text = "View" };
            Button CancelButton = new Button() { Text = "Cancel" };
            NewButton.Clicked += (_, _) =>
            {
                ConvertInputs();
            };
            SaveButton.Clicked += (_, _) =>
            {
                ConvertInputs();
            };
            ViewButton.Clicked += (_, _) =>
            {
                ConvertInputs();
                Eto.Forms.MessageBox.Show(
                    $"New: {_new.ToString()}, Serialized: {JsonSerializer.Serialize(ConvertedInputs)}",
                    "Serialized",
                    Eto.Forms.MessageBoxType.Information
                );
            };
            var ActionButtons = new StackLayout()
            {
                Orientation = StackOrientation.Vertical,
            };
            ActionButtons.Add(NewButton);
            ActionButtons.Add(SaveButton);
            ActionButtons.Add(ViewButton);
            ActionButtons.Add(CancelButton);
            var GeneratedControls = _MControls;
            var SLContent = (new StackLayout()
            {
                Orientation = StackOrientation.Vertical,
                Spacing = 2,
            });
            SLContent.Add(ActionButtons);
            SLContent.Add(GeneratedControls);
            Content = SLContent;
        }
    }
}
