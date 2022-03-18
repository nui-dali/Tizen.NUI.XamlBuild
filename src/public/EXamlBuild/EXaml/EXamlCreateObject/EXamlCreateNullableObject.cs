using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Text;
using Tizen.NUI.EXaml.Build.Tasks;

namespace Tizen.NUI.EXaml
{
    internal class EXamlCreateNullableObject : EXamlCreateObject
    {
        public EXamlCreateNullableObject(EXamlContext context, object value, TypeReference nullableType, TypeReference genericArgumentType) : base(context, null, genericArgumentType)
        {
            this.value = value;
            NullableType = nullableType;
        }

        internal override string Write()
        {
            if (false == IsValid)
            {
                return "";
            }

            string ret = String.Format("({0} ({1} {2}))\n",
                         eXamlContext.GetValueString((int)EXamlOperationType.CreateNullableObject),
                         eXamlContext.GetValueString(value),
                         eXamlContext.GetValueString(eXamlContext.GetTypeIndex(Type)));

            return ret;
        }

        private object value;

        internal TypeReference NullableType
        {
            get;
        }
    }
}
