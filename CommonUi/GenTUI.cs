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
            this.Add(new GenTUI(Inputs, SaveNewHandler, SaveExistingHandler, InputHandler));
        }
    }
    public class GenTUI: Terminal.Gui.FrameView
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
            foreach (var kv in E)
            {
                View EControl;
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
                EControl = (new View()).Add(new Label() { Text = kv.Value.Item1 }).Add(EInput);
                _Einputs.Add(kv.Key, EInput);
                EControls.Add(EControl);
            }
            _EControls = EControls;

            Button NewButton = new Button() { Text = "New" };
            Button SaveButton = new Button() { Text = "Save", };
            Button ViewButton = new Button() { Text = "View" };
            Button CancelButton = new Button() { Text = "Cancel" };
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
            View ActionButtons = (new View());
            ActionButtons.Add(NewButton, SaveButton, ViewButton, CancelButton);
            var GeneratedControls = new View();
            /*foreach(var c in _EControls)
            {
                GeneratedControls.Add(c);
            }*/
            this.Add(GeneratedControls, ActionButtons);
            //Content = new StackLayout(ActionButtons, GeneratedControls) { Orientation = Orientation.Vertical, HorizontalContentAlignment = HorizontalAlignment.Center };
        }

        public long Save()
        {
            if (_new) return _SaveNewHandler(ConvertedInputs);
            else return _SaveExistingHandler(ConvertedInputs);
        }
    }
}
