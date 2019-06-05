using System;
using Tizen.NUI.XamlBinding;

namespace Tizen.NUI.StyleSheets
{
    internal interface IStyle
    {
        Type TargetType { get; }

        void Apply(BindableObject bindable);
        void UnApply(BindableObject bindable);
    }
}
