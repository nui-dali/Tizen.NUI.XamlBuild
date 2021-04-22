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

namespace Tizen.NUI.EXaml
{
    internal class EXamlValueConverterFromString
    {
        internal string GetString()
        {
            string ret = "";
            ret += String.Format("{0} {1}", EXamlOperation.GetValueString(converterInstance), EXamlOperation.GetValueString(Value));
            return ret;
        }

        internal EXamlValueConverterFromString(TypeDefinition converterType, string value)
        {
            ConverterType = converterType;
            Value = value;

            if (!typeToInstance.ContainsKey(converterType))
            {
                converterInstance = new EXamlCreateObject(null, converterType);
                typeToInstance.Add(converterType, converterInstance);
            }
            else
            {
                converterInstance = typeToInstance[converterType];
            }
        }

        internal TypeDefinition ConverterType
        {
            get;
            private set;
        }

        internal string Value
        {
            get;
            private set;
        }

        internal static void ClearStaticThing()
        {
            typeToInstance.Clear();
        }

        private EXamlCreateObject converterInstance;

        private static Dictionary<TypeDefinition, EXamlCreateObject> typeToInstance
        {
            get;
        } = new Dictionary<TypeDefinition, EXamlCreateObject>();
    }
}
