using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using System.Text.Json;

namespace CommonUi
{
    public delegate long SaveHandler(IReadOnlyDictionary<string, object> UserInput);
    public class GenTopLevel : Toplevel
    {
        
        public GenTopLevel(IReadOnlyDictionary<string, (string, object, string?)> Inputs, SaveHandler SaveNewHandler, SaveHandler SaveExistingHandler, IReadOnlyDictionary<string, ShowAndGetValue> InputHandler){
            //Add(new Button() { Text = "Hello" });
            //Add(new Button() { Text = "Hello" }, new Button() { Text = "Hello" }, new Button() { Text = "Hello" });
            Add(new GenTUI(Inputs, SaveNewHandler, SaveExistingHandler, InputHandler));
            
        }
    }
    public class GenTUI: Terminal.Gui.View
    {
        public IReadOnlyDictionary<string, object> Values { get => _Values; }
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
                else if (T == typeof(string))
                {
                    ConvertedInputs.Add(e.Key, ((TextField)_Einputs[e.Key]).Text);
                }
                else if (T == typeof(bool))
                {
                    ConvertedInputs.Add(e.Key, ((CheckBox)_Einputs[e.Key]).CheckedState);
                }
            }
        }
        public GenTUI(IReadOnlyDictionary<string, (string, object, string?)> Inputs, SaveHandler SaveNewHandler, SaveHandler SaveExistingHandler, IReadOnlyDictionary<string, ShowAndGetValue> InputHandler)
        {
            List<View> EControls = new();
            _Inputs = Inputs;

            var E = Inputs.AsEnumerable();
            KeyValuePair<string, (string, object, string?)> oldkv;
            foreach (var kv in E)
            {
                View EControl;
                var ELabel = new Label() { Text = kv.Value.Item1, Width = Dim.Auto(), Height = Dim.Auto() };
                if (EControls.Count > 0) ELabel.Y = Pos.Bottom(EControls.Last())+1;
                Terminal.Gui.View EInput;
                if (kv.Value.Item2.GetType() == typeof(long) || kv.Value.Item2.GetType() == typeof(int))
                {
                    if (kv.Value.Item3 == null)
                    {
                        EInput = new TextField() { Text = ((long)kv.Value.Item2).ToString() };

                    }
                    else
                    {
                        EInput = new Button() { Text = ((long)kv.Value.Item2).ToString() };
                        ((Button)EInput).MouseClick += (_, _) =>
                        {
                            long? IHS = InputHandler[kv.Value.Item3]();
                            if (IHS != null) ((Button)EInput).Text = IHS.ToString();
                        };
                    }
                }
                else if (kv.Value.Item2.GetType() == typeof(bool))
                {
                    EInput = new CheckBox() { Text = ((bool)kv.Value.Item2).ToString() };
                }
                else
                {
                    EInput = new TextField() { Text = ((string)kv.Value.Item2).ToString() };
                }
                
                EControl = (new View() { Width = Dim.Auto(), Height = Dim.Auto() }).Add(ELabel);
                EInput.Width = Dim.Fill();
                EInput.X = Pos.Right(ELabel);
                EControl.Add(EInput);
                _Einputs.Add(kv.Key, EInput);
                EControls.Add(EControl);
                oldkv = kv;
            }
            _EControls = EControls;

            Button NewButton = new Button() { Text = "New" };
            Button SaveButton = new Button() { Text = "Save", X = Pos.Right(NewButton) };
            Button ViewButton = new Button() { Text = "View", X = Pos.Right(SaveButton) };
            Button CancelButton = new Button() { Text = "Cancel", X = Pos.Right(ViewButton) };
            NewButton.MouseClick += (_, _) => {
                ConvertInputs();
            };
            SaveButton.MouseClick += (_, _) => {
                ConvertInputs();
            };
            ViewButton.MouseClick += (_, _) => {
                ConvertInputs();
                MessageBox.Query($"New: {_new.ToString()}, Serialized: {JsonSerializer.Serialize(ConvertedInputs)}", "Serialized", "Ok");
            };
            View ActionButtons = new View(){ Width = Dim.Auto(), Height = Dim.Auto() };
            ActionButtons.Add(NewButton, SaveButton, ViewButton, CancelButton);
            var GeneratedControls = new View() { Width = Dim.Auto(), Height = Dim.Auto(), X = 0, Y = Pos.Bottom(ActionButtons)+1 };
            //GeneratedControls.Add(_EControls.First());
            foreach(var c in _EControls)
            {
                GeneratedControls.Add(c);
            }
            Add(ActionButtons, GeneratedControls);
            //Add(new Button() { Text = "Hello" });
            Width = Dim.Auto();
            Height = Dim.Auto();
            //Content = new StackLayout(ActionButtons, GeneratedControls) { Orientation = Orientation.Vertical, HorizontalContentAlignment = HorizontalAlignment.Center };
        }

        public long Save()
        {
            if (_new) return _SaveNewHandler(ConvertedInputs);
            else return _SaveExistingHandler(ConvertedInputs);
        }
    }
}
