using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tizen.NUI.EXaml
{
    public class EXamlCreateArrayObject : EXamlCreateObject
    {
        public EXamlCreateArrayObject(TypeReference type, List<object> items) : base(null, type)
        {
            this.items = items;
        }

        internal override string Write()
        {
            if (false == IsValid)
            {
                return "";
            }

            string ret = "";
            string sign = "a";

            ret += sign + "(";

            ret += $"{GetValueString(0)} ";
            ret += $"{GetValueString(GetTypeIndex(Type))} ";

            ret += "(";
            foreach (var item in items)
            {
                ret += $"{GetValueString(item)} ";
            }
            ret += ")";

            ret += ")" + sign + "\n";

            return ret;
        }

        private List<object> items;
    }
}
