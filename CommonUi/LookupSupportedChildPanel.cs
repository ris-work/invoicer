using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonUi
{
    public interface ILookupSupportedChildPanel
    {
        void MapLookupValues(string[] fieldNames);
        object LookupValue(string fieldName);
        public void SetMoveNext(Action MoveNext);
    }
}
