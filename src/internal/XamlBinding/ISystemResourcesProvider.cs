using System.ComponentModel;

namespace Tizen.NUI.XamlBinding
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal interface ISystemResourcesProvider
    {
        IResourceDictionary GetSystemResources();
    }
}