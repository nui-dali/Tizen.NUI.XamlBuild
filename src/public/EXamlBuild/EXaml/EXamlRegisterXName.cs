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
