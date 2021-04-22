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
    //use ()
    internal class EXamlImplicitOperator : EXamlOperation
    {
        internal override string Write()
        {
            if (false == Instance.IsValid)
            {
                return "";
            }

            string ret = "";
            ret += String.Format("({0} {1})\n", GetValueString(Instance), GetValueString(Value));
            return ret;
        }

        public EXamlImplicitOperator(EXamlCreateObject @object, object value)
        {
            Instance = @object;
            Value = value;
            EXamlOperation.eXamlOperations.Add(this);
        }

        public EXamlCreateObject Instance
        {
            get;
        }

        public object Value
        {
            get;
        }
    }
}
