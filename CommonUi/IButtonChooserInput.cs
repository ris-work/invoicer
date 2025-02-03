using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RV.InvNew.CommonUi
{
    public interface IButtonChooserInput
    {
        public List<string[]> OutputList { get; }
        public string[] Selected { get;}
    }
}
