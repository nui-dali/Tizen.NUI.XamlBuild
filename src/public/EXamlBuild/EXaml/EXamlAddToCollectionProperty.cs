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
    //use ~~
    internal class EXamlAddToCollectionProperty : EXamlOperation
    {
        internal override string Write()
        {
            string ret = "";
            ret += String.Format("~({0} {1})~\n",
                   GetValueString(instance), GetValueString(value));
            return ret;
        }

        public EXamlAddToCollectionProperty(EXamlGetObjectByProperty instance, object value)
        {
            this.instance = instance;
            this.value = value;

            EXamlOperation.eXamlOperations.Add(this);
        }

        private EXamlGetObjectByProperty instance;
        private object value;
    }
}
