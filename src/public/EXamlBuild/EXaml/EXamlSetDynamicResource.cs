using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Tizen.NUI.Binding;

namespace Tizen.NUI.EXaml
{
    //use $$
    internal class EXamlSetDynamicResource : EXamlOperation
    {
        internal override string Write()
        {
            if (@object.IsValid)
            {
                string ret = "";
                ret += String.Format("$({0} {1} {2})$\n",
                    GetValueString(@object),
                    GetValueString(definedBindableProperties.IndexOf(bindableProperty.Resolve())),
                    GetValueString(key));
                return ret;
            }
            else
            {
                return "";
            }
        }

        public EXamlSetDynamicResource(EXamlCreateObject @object, FieldReference bindalbeProperty, string key)
        {
            this.@object = @object;
            this.bindableProperty = bindalbeProperty;
            this.key = key;
            EXamlOperation.eXamlOperations.Add(this);

            @object.AddBindableProperty(bindableProperty);
        }

        private EXamlCreateObject @object;
        private FieldReference bindableProperty;
        private string key;
    }
}
