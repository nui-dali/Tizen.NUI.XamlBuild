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
    //use []
    internal class EXamlSetProperty : EXamlOperation
    {
        internal override string Write()
        {
            if (false == instance.IsValid)
            {
                return "";
            }

            string ret = "";

            ret += "[";

            ret += String.Format("({0} {1} {2})", 
                GetValueString(instance),
                GetValueString(definedProperties.GetIndex(property.DeclaringType, property)),
                GetValueString(value));

            ret += "]\n";

            return ret;
        }

        public EXamlSetProperty(EXamlCreateObject instance, string propertyName, object value)
        {
            var property = instance.Type.GetProperty(fi=>fi.Name==propertyName, out declareTypeRef);
            if (null != property)
            {
                this.instance = instance;
                this.property = property;
                this.value = value;

                if (null != this.instance.Instance)
                {
                    var propertyInfo = this.instance.Instance.GetType().GetProperty(property.Name);
                    propertyInfo.SetMethod.Invoke(this.instance.Instance, new object[] { value });
                }

                this.instance.AddProperty(declareTypeRef, property);

                EXamlOperation.eXamlOperations.Add(this);
            }
            else
            {
                throw new Exception("Property is not element");
            }
        }

        private EXamlCreateObject instance;
        private TypeReference declareTypeRef;
        private PropertyDefinition property;
        private object value;
    }
}
