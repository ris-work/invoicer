using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto;
using Eto.Forms;

namespace HealthMonitorLogViewer
{
    public interface ILoadOncePanel<out T>
    {
        public object GetInnerAsObject();
        public T GetInner();
    }

    public class LoadOncePanel<T> : ILoadOncePanel<T>
        where T : new()
    {
        //interface IContravariant<in T>;
        T _Inner;
        public object Inner
        {
            get
            {
                _Inner ??= new T();
                return _Inner;
            }
            set { Inner = value; }
        }

        public T GetInner()
        {
            return (T)Inner;
        }

        public object GetInnerAsObject()
        {
            return Inner;
        }

        public LoadOncePanel() { }

        public LoadOncePanel(object A)
        {
            _Inner = (T)A;
        }

        public static explicit operator LoadOncePanel<object>(LoadOncePanel<T> instance)
        {
            return new LoadOncePanel<object>(instance.Inner as object);
        }
    }
}
