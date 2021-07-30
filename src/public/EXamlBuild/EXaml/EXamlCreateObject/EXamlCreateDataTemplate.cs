using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Text;
using Tizen.NUI.EXaml.Build.Tasks;

namespace Tizen.NUI.EXaml
{
    internal class EXamlCreateDataTemplate : EXamlCreateObject
    {
        public EXamlCreateDataTemplate(EXamlContext context, TypeReference type, string content) : base(context, null, type)
        {
            indexRangeOfContent = eXamlContext.GetLongStringIndexs(content);
        }

        private (int, int) indexRangeOfContent;

        internal override string Write()
        {
            if (false == IsValid)
            {
                return "";
            }

            string ret = "";
            string sign = "a";

            ret += sign + "(";

            ret += $"{eXamlContext.GetValueString(1)} ";
            ret += $"{eXamlContext.GetValueString(eXamlContext.GetTypeIndex(Type))} ";
            ret += $"{eXamlContext.GetValueString(indexRangeOfContent.Item1)} ";
            ret += $"{eXamlContext.GetValueString(indexRangeOfContent.Item2)} ";

            ret += ")" + sign + "\n";

            return ret;
        }
    }
}
