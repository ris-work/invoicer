using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto;
using Eto.Forms;

namespace CommonUi
{
    public interface ILoadOncePanel<out T>
    {
        public object GetInnerAsObject();
        public T GetInner();
        void Destroy();
    }

    public class LoadOncePanel<T> : ILoadOncePanel<T>
        where T : new()
    {
        //interface IContravariant<in T>;
        T _Inner;
        bool _isDestroyed = false;

        public object Inner
        {
            get
            {
                if (_isDestroyed || _Inner == null)
                {
                    _Inner = new T();
                    _isDestroyed = false;
                }
                return _Inner;
            }
            set { _Inner = (T)value; }
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

        public void Destroy()
        {
            _isDestroyed = true;
            _Inner = default(T);
        }

        public static explicit operator LoadOncePanel<object>(LoadOncePanel<T> instance)
        {
            return new LoadOncePanel<object>(instance.Inner as object);
        }
    }
}
