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
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Tizen.NUI.Binding;
using Tizen.NUI.Xaml.Build.Tasks;

namespace Tizen.NUI.EXaml
{
    //use !!
    internal class EXamlSetBindalbeProperty : EXamlOperation
    {
        internal override string Write()
        {
            if (false == Instance.IsValid)
            {
                return "";
            }

            string ret = "";

            ret += "!";

            ret += String.Format("({0} {1} {2})", 
                GetValueString(Instance),
                GetValueString(definedBindableProperties.IndexOf(BindableProperty.Resolve())),
                GetValueString(Value));

            ret += "!\n";

            return ret;
        }

        public EXamlSetBindalbeProperty(EXamlCreateObject @object, MemberReference bindableProperty, object value)
        {
            Instance = @object;
            BindableProperty = bindableProperty;
            Value = value;

            Instance.AddBindableProperty(bindableProperty);

            EXamlOperation.eXamlOperations.Add(this);
        }

        public EXamlCreateObject Instance
        {
            get;
            private set;
        }

        public MemberReference BindableProperty
        {
            get;
            private set;
        }

        public object Value
        {
            get;
            private set;
        }
    }
}
