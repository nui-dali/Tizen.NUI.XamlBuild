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
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Text;
using Tizen.NUI.Xaml.Build.Tasks;

namespace Tizen.NUI.EXaml
{
    //use ``
    internal class EXamlGetObjectByProperty : EXamlOperation
    {
        internal override string Write()
        {
            if (instance.IsValid)
            {
                string ret = "";
                ret += String.Format("`({0} {1})`\n",
                       GetValueString(instance),
                       GetValueString(propertyName));
                return ret;
            }
            else
            {
                return "";
            }
        }

        internal EXamlGetObjectByProperty(EXamlCreateObject instance, string propertyName)
        {
            this.instance = instance;
            this.propertyName = propertyName;
            objects.Add(this);

            EXamlOperation.eXamlOperations.Add(this);
        }

        internal static int GetIndex(EXamlGetObjectByProperty eXamlObjectFromProperty)
        {
            return objects.IndexOf(eXamlObjectFromProperty);
        }

        internal static void ClearList()
        {
            objects.Clear();
        }

        private static List<EXamlGetObjectByProperty> objects = new List<EXamlGetObjectByProperty>();

        private EXamlCreateObject instance;
        private string propertyName;
    }
}
