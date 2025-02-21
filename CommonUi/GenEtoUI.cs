﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Eto.Forms;

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
                        ConvertedInputs.Add(e.Key, float.Parse(((TextBox)_Einputs[e.Key]).Text));
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
                        ConvertedInputs.Add(e.Key, double.Parse(((TextBox)_Einputs[e.Key]).Text));
                    }
                    else
                    {
                        ConvertedInputs.Add(e.Key, double.Parse(((Button)_Einputs[e.Key]).Text));
                    }
                }
                else if (T == typeof(string))
                {
                    ConvertedInputs.Add(e.Key, ((TextBox)_Einputs[e.Key]).Text);
                }
                else if (T == typeof(bool))
                {
                    ConvertedInputs.Add(e.Key, ((CheckBox)_Einputs[e.Key]).Checked);
                }
            }
        }

        public GenEtoUI(
            IReadOnlyDictionary<string, (string, object, string?)> Inputs,
            SaveHandler SaveNewHandler,
            SaveHandler SaveExistingHandler,
            IReadOnlyDictionary<string, (ShowAndGetValue, LookupValue)> InputHandler,
            string? IdentityColumn
        )
        {
            List<Eto.Forms.TableRow> EControls = new();
            _Inputs = Inputs;

            var E = Inputs.AsEnumerable();
            foreach (var kv in E)
            {
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
                            EInput = new TextBox()
                            {
                                Text = (kv.Value.Item2).ToString(),
                                TextAlignment = TextAlignment.Right,
                            };
                        }
                        else
                        {
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
                                }
                            };
                        }
                    }
                    else if (kv.Value.Item2.GetType() == typeof(double))
                    {
                        EInput = new TextBox()
                        {
                            Text = ((double)kv.Value.Item2).ToString(),
                            TextAlignment = TextAlignment.Right,
                        };
                    }
                    else if (kv.Value.Item2.GetType() == typeof(bool))
                    {
                        EInput = new CheckBox() { Text = ((bool)kv.Value.Item2).ToString() };
                    }
                    else if (kv.Value.Item2.GetType() == typeof(string))
                    {
                        EInput = new TextBox() { Text = ((string)kv.Value.Item2).ToString() };
                    }
                }
                else
                {
                    EInput = new TextBox() { Text = ((string)kv.Value.Item2).ToString() };
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
            NewButton.Click += (_, _) =>
            {
                ConvertInputs();
            };
            SaveButton.Click += (_, _) =>
            {
                ConvertInputs();
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
