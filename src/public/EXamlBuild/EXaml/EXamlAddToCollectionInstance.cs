﻿/*
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
using Tizen.NUI.EXaml.Build.Tasks;

namespace Tizen.NUI.EXaml
{
    //use ~~
    internal class EXamlAddToCollectionInstance : EXamlOperation
    {
        internal override string Write()
        {
            string ret = "";
            string sign = "a";

            ret += sign + "(";

            ret += $"{eXamlContext.GetValueString(2)} ";
            ret += $"{eXamlContext.GetValueString(instance)} ";
            ret += $"{eXamlContext.GetValueString(value)} ";

            ret += ")" + sign + "\n";

            return ret;
        }

        public EXamlAddToCollectionInstance(EXamlContext context, EXamlCreateObject instance, object value)
            : base(context)
        {
            this.instance = instance;
            this.value = value;

            eXamlContext.eXamlOperations.Add(this);
        }

        private EXamlCreateObject instance;
        private object value;
    }
}