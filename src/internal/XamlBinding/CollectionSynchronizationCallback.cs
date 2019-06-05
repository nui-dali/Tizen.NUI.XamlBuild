using System;
using System.Collections;

namespace Tizen.NUI.XamlBinding
{
    public delegate void CollectionSynchronizationCallback(IEnumerable collection, object context, Action accessMethod, bool writeAccess);
}