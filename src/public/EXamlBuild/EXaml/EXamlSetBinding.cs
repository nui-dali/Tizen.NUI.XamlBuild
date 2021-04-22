using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Tizen.NUI.Binding;

namespace Tizen.NUI.EXaml
{
    //use **
    internal class EXamlSetBinding : EXamlOperation
    {
        internal override string Write()
        {
            if (Instance.IsValid)
            {
                string ret = "";
                ret += String.Format("%({0} {1} {2})%\n",
                    GetValueString(Instance),
                    GetValueString(definedBindableProperties.IndexOf(BindableProperty.Resolve())),
                    GetValueString(Value));
                return ret;
            }
            else
            {
                return "";
            }
        }

        public EXamlSetBinding(EXamlCreateObject @object, FieldReference bindableProperty, object binding)
        {
            Instance = @object;
            BindableProperty = bindableProperty;
            Value = binding;
            EXamlOperation.eXamlOperations.Add(this);

            Instance.AddBindableProperty(bindableProperty);
        }

        public EXamlCreateObject Instance
        {
            get;
        }

        public FieldReference BindableProperty
        {
            get;
        }

        public object Value
        {
            get;
        }
    }
}
