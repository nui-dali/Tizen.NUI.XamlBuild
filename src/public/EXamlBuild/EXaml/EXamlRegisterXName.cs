using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Tizen.NUI.Binding;

namespace Tizen.NUI.EXaml
{
    //use &&
    internal class EXamlRegisterXName : EXamlOperation
    {
        internal override string Write()
        {
            string ret = "";
            ret += String.Format("&({0} \"{1}\")&\n", GetValueString(Instance), XName);
            return ret;
        }

        public EXamlRegisterXName(object @object, string xName)
        {
            Instance = @object;
            XName = xName;
            EXamlOperation.eXamlOperations.Add(this);

            xNameToInstance.Add(xName, @object);
        }

        public object Instance
        {
            get;
        }

        public string XName
        {
            get;
        }

        public static object GetObjectByXName(string xName)
        {
            object ret = null;
            xNameToInstance.TryGetValue(xName, out ret);
            return ret;
        }

        internal static void ClearStaticThing()
        {
            xNameToInstance.Clear();
        }
        private static Dictionary<string, object> xNameToInstance
        {
            get;
        } = new Dictionary<string, object>();
    }
}
