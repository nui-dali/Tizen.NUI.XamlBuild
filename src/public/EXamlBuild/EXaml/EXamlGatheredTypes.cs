using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Tizen.NUI.EXaml
{
    internal class EXamlGatheredTypes
    {
        static void GatherType(Type type)
        {

        }

        private class TypeInfo
        {
            public TypeInfo(Type type)
            {
                List<MethodInfo> methodInfos = new List<MethodInfo>();

                for (int i = 0; i < type.GetMethods().Length; i++)
                {
                    var method = type.GetMethods()[i];

                    if (method.IsConstructor && method.IsPublic)
                    {
                        methodInfos.Add(method);
                    }
                }
            }
        }
    }
}
