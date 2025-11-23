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
        void Detach();
        void Destroy();
    }

    public class LoadOncePanel<T> : ILoadOncePanel<T>
        where T : new()
    {
        //interface IContravariant<in T>;
        T _Inner;
        bool _isDetached = false;
        bool _isDestroyed = false;
        string _panelId;

        public object Inner
        {
            get
            {
                if (_isDestroyed || _isDetached || _Inner == null)
                {
                    _Inner = new T();
                    _isDestroyed = false;
                    _isDetached = false;

                    // Register the panel when created
                    if (_Inner is Control control)
                    {
                        _panelId = Guid.NewGuid().ToString();
                        PanelManager.RegisterPanel(_Inner.GetType().Name, control);
                    }
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

            // Register the panel when set
            if (_Inner is Control control)
            {
                _panelId = Guid.NewGuid().ToString();
                PanelManager.RegisterPanel(_Inner.GetType().Name, control);
            }
        }

        public void Detach()
        {
            // Mark as detached but don't destroy the content
            _isDetached = true;
        }

        public void Destroy()
        {
            // Unregister the panel when destroyed
            if (!string.IsNullOrEmpty(_panelId))
            {
                PanelManager.UnregisterPanel(_panelId);
            }

            _isDestroyed = true;
            _isDetached = false;
            _Inner = default(T);
        }

        public static explicit operator LoadOncePanel<object>(LoadOncePanel<T> instance)
        {
            return new LoadOncePanel<object>(instance.Inner as object);
        }
    }
}
