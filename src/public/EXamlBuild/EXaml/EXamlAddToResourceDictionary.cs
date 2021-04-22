/*
 * Copyright(c) 2021 Samsung Electronics Co., Ltd.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 */
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
