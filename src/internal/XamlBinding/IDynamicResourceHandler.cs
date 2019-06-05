using System.ComponentModel;
using Tizen.NUI.XamlBinding;

namespace Tizen.NUI.XamlBinding
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal interface IDynamicResourceHandler
    {
        void SetDynamicResource(BindableProperty property, string key);
    }
}
