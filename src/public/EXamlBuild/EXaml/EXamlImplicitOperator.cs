using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Tizen.NUI.Binding;

namespace Tizen.NUI.EXaml
{
    //use ()
    internal class EXamlImplicitOperator : EXamlOperation
    {
        internal override string Write()
        {
            if (false == Instance.IsValid)
            {
                return "";
            }

            string ret = "";
            ret += String.Format("({0} {1})\n", GetValueString(Instance), GetValueString(Value));
            return ret;
        }

        public EXamlImplicitOperator(EXamlCreateObject @object, object value)
        {
            Instance = @object;
            Value = value;
            EXamlOperation.eXamlOperations.Add(this);
        }

        public EXamlCreateObject Instance
        {
            get;
        }

        public object Value
        {
            get;
        }
    }
}
