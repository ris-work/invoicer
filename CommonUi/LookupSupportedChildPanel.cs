using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;

namespace CommonUi
{
    public interface ILookupSupportedChildPanel
    {
        void MapLookupValues(string[] fieldNames);
        object LookupValue(string fieldName);
        public void SetMoveNext(Action MoveNext);
        List<Control> GetFocusableControls();
        public (bool isValid, string errorDescription) Validate();
        public void FocusChild();
        public void SetOriginalValues(object[] OriginalValues);
        public void SetOriginalValue(string Key, object Value);
        public void MapSetValues(string[] fieldNames);
        public void SetGlobalChangeWatcher(Action SomethingChanged);
    }
}
