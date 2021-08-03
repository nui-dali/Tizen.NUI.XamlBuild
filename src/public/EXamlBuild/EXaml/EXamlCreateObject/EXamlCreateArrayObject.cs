﻿using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Text;
using Tizen.NUI.EXaml.Build.Tasks;

namespace Tizen.NUI.EXaml
{
    internal class EXamlCreateArrayObject : EXamlCreateObject
    {
        public EXamlCreateArrayObject(EXamlContext context, TypeReference type, List<object> items) : base(context, null, type)
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

            ret += $"{eXamlContext.GetValueString(0)} ";
            ret += $"{eXamlContext.GetValueString(eXamlContext.GetTypeIndex(Type))} ";

            ret += "(";
            foreach (var item in items)
            {
                ret += $"{eXamlContext.GetValueString(item)} ";
            }
            ret += ")";

            ret += ")" + sign + "\n";

            return ret;
        }

        private List<object> items;
    }
}
