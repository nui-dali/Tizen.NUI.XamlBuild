using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Tizen.NUI.Binding;

namespace Tizen.NUI.EXaml
{
    //use %%
    internal class EXamlAddToResourceDictionary : EXamlOperation
    {
        internal override string Write()
        {
            if (instance.IsValid)
            {
                string ret = "";
                if (null != key)
                {
                    ret += String.Format("*({0} {1} {2})*\n",
                       GetValueString(instance), GetValueString(key), GetValueString(value));
                }
                else
                {
                    int temp = 0;
                }
                return ret;
            }
            else
            {
                return "";
            }
        }

        public EXamlAddToResourceDictionary(EXamlCreateObject @object, string key, object value)
        {
            instance = @object;
            this.key = key;
            this.value = value;
            EXamlOperation.eXamlOperations.Add(this);

            resourceDictionary.Add(key, value);
        }

        public EXamlAddToResourceDictionary(EXamlCreateObject @object, EXamlCreateObject value)
        {
            EXamlOperation.eXamlOperations.Add(this);
        }

        internal static Dictionary<string, object> resourceDictionary = new Dictionary<string, object>();

        private EXamlCreateObject instance;
        private string key;
        private object value;
    }
}
