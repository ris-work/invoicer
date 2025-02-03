using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Eto.Forms;

namespace CommonUi
{
    public delegate long? ShowAndGetValue();
    public interface EtoValueSelector
    {
        public long Value { get; }
    }
    public class GenEtoUI : Eto.Forms.Control
    {
        public delegate long SaveHandler(IReadOnlyDictionary<string, object> UserInput);
        public IReadOnlyDictionary<string, object> Values { get => _Values; }
        private Dictionary<string, object> _Values = new();
        private Dictionary<string, Eto.Forms.Control> _Controls = new();
        private bool _new = false;
        private SaveHandler _SaveNewHandler;
        private SaveHandler _SaveExistingHandler;
        List<Eto.Forms.Control> _EControls = new();
        Dictionary<string, Eto.Forms.Control> _Einputs = new();
        Dictionary<string, object> ConvertedInputs = new();
        IReadOnlyDictionary<string, (string, object, string?)> _Inputs;

        //public delegate long SaveExistingHandler(IReadOnlyDictionary<string, object> UserInput);
        public void ConvertInputs()
        {
            foreach(var e in _Inputs)
            {
                Type T = e.Value.Item2.GetType();
                if (T == typeof(long)) {
                    if (e.Value.Item3 == null) {
                        ConvertedInputs.Add(e.Key, long.Parse(((TextBox)_Einputs[e.Key]).Text));
                    }
                    else
                    {
                        ConvertedInputs.Add(e.Key, long.Parse(((Button)_Einputs[e.Key]).Text));
                    }
                }
            }
        }
        public GenEtoUI(IReadOnlyDictionary<string, (string, object, string?)> Inputs, SaveHandler SaveNewHandler, SaveHandler SaveExistingHandler, IReadOnlyDictionary<string, ShowAndGetValue> InputHandler)
        {
            List<Eto.Forms.Control> EControls = new();
            _Inputs = Inputs;
            
            var E = Inputs.AsEnumerable();
            foreach(var kv in E)
            {
                Eto.Forms.Control EControl;
                Eto.Forms.Control EInput;
                if (kv.Value.Item2.GetType() == typeof (long) || kv.Value.Item2.GetType() == typeof(int)) { 
                    if(kv.Value.Item3 == null)
                    {
                        EInput = new TextBox() { Text = ((long)kv.Value.Item2).ToString() };

                    }
                    else {
                        EInput = new Button() { Text = ((long)kv.Value.Item2).ToString() };
                        ((Button)EInput).Click += (_,_) => {
                            long? IHS = InputHandler[kv.Value.Item3]();
                            if (IHS != null) ((Button)EInput).Text = IHS.ToString();
                        };
                    }
                }
                else
                {
                    EInput = new Button() { Text = ((string)kv.Value.Item2).ToString() };
                }
                EControl = new StackLayout(new Label() { Text = kv.Value.Item1 }, EInput) { Padding = 10, Spacing = 10, Orientation = Orientation.Horizontal, HorizontalContentAlignment = HorizontalAlignment.Stretch };
                _Einputs.Add(kv.Key, EInput);
            }

            Button NewButton = new Button() { Text = "New" };
            Button SaveButton = new Button() { Text = "Save",  };
            Button ViewButton = new Button() { Text = "View" };
            Button CancelButton = new Button() { Text = "Cancel" };
            NewButton.Click += (_, _) => {
                ConvertInputs();
            };
            SaveButton.Click += (_, _) => {
                ConvertInputs();
            };
            ViewButton.Click += (_, _) => {
                ConvertInputs();
                MessageBox.Show($"New: {_new.ToString()}, Serialized: {JsonSerializer.Serialize(ConvertedInputs)}", "Serialized", MessageBoxType.Information);
            };
        }

        public long Save()
        {
            if (_new) return _SaveNewHandler(ConvertedInputs);
            else return _SaveExistingHandler(ConvertedInputs);
        }
    }
}
