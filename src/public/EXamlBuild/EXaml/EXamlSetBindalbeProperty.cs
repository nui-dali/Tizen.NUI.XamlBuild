using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Tizen.NUI.Binding;
using Tizen.NUI.Xaml.Build.Tasks;

namespace Tizen.NUI.EXaml
{
    //use !!
    internal class EXamlSetBindalbeProperty : EXamlOperation
    {
        internal override string Write()
        {
            if (false == Instance.IsValid)
            {
                return "";
            }

            string ret = "";

            ret += "!";

            ret += String.Format("({0} {1} {2})", 
                GetValueString(Instance),
                GetValueString(definedBindableProperties.IndexOf(BindableProperty.Resolve())),
                GetValueString(Value));

            ret += "!\n";

            return ret;
        }

        public EXamlSetBindalbeProperty(EXamlCreateObject @object, FieldReference bindableProperty, object value)
        {
            Instance = @object;
            BindableProperty = bindableProperty;
            Value = value;

            Instance.AddBindableProperty(bindableProperty);

            EXamlOperation.eXamlOperations.Add(this);
        }

        public EXamlCreateObject Instance
        {
            get;
            private set;
        }

        public FieldReference BindableProperty
        {
            get;
            private set;
        }

        public object Value
        {
            get;
            private set;
        }
    }
}
