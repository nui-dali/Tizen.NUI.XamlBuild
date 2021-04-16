using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tizen.NUI.EXaml
{
    internal class EXamlValueConverterFromString
    {
        internal string GetString()
        {
            string ret = "";
            ret += String.Format("{0} {1}", EXamlOperation.GetValueString(converterInstance), EXamlOperation.GetValueString(Value));
            return ret;
        }

        internal EXamlValueConverterFromString(TypeDefinition converterType, string value)
        {
            ConverterType = converterType;
            Value = value;

            if (!typeToInstance.ContainsKey(converterType))
            {
                converterInstance = new EXamlCreateObject(null, converterType);
                typeToInstance.Add(converterType, converterInstance);
            }
            else
            {
                converterInstance = typeToInstance[converterType];
            }
        }

        internal TypeDefinition ConverterType
        {
            get;
            private set;
        }

        internal string Value
        {
            get;
            private set;
        }

        internal static void ClearStaticThing()
        {
            typeToInstance.Clear();
        }

        private EXamlCreateObject converterInstance;

        private static Dictionary<TypeDefinition, EXamlCreateObject> typeToInstance
        {
            get;
        } = new Dictionary<TypeDefinition, EXamlCreateObject>();
    }
}
