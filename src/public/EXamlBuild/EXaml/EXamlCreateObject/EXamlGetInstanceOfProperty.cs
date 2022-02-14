using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tizen.NUI.EXaml.Build.Tasks;

namespace Tizen.NUI.EXaml
{
    internal class EXamlGetInstanceOfProperty : EXamlCreateObject
    {
        internal override string Write()
        {
            string ret = "";

            ret += String.Format("({0} ({1} {2}))\n",
                                      eXamlContext.GetValueString((int)EXamlOperationType.GetProperty),
                                      eXamlContext.GetValueString(instanceIndex),
                                      eXamlContext.GetValueString(eXamlContext.definedProperties.GetIndex(InstanceType, Property)));

            return ret;
        }

        public EXamlGetInstanceOfProperty(EXamlContext context, EXamlCreateObject instance, PropertyDefinition property) : base(context, property)
        {
            InstanceType = instance.GetType();
            instanceIndex = instance.Index;
            Property = property;
        }

        internal TypeReference InstanceType
        {
            get;
        }

        internal PropertyDefinition Property
        {
            get;
        }

        private int instanceIndex;
    }
}
