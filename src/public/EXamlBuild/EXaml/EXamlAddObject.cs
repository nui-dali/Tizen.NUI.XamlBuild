using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Tizen.NUI.Binding;

namespace Tizen.NUI.EXaml
{
    //use ^^
    internal class EXamlAddObject : EXamlOperation
    {
        internal override string Write()
        {
            if (false == Parent.IsValid || false == Child.IsValid)
            {
                return "";
            }

            string ret = "";
            ret += String.Format("^({0} {1} d{2}d)^\n",
                GetValueString(Parent), 
                GetValueString(Child),
                definedMethods.GetIndex(Method.DeclaringType, Method));
            return ret;
        }

        public EXamlAddObject(EXamlCreateObject parent, EXamlCreateObject child, MethodDefinition addMethod)
        {
            Parent = parent;
            Child = child;
            Method = addMethod;

            EXamlOperation.eXamlOperations.Add(this);
            eXamlAddObjectList.Add(this);
        }

        public EXamlCreateObject Parent
        {
            get;
        }

        public EXamlCreateObject Child
        {
            get;
        }

        public MethodDefinition Method
        {
            get;
        }

        internal static List<EXamlAddObject> eXamlAddObjectList
        {
            get;
        } = new List<EXamlAddObject>();
    }
}
