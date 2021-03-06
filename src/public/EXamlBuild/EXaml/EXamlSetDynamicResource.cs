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

namespace Tizen.NUI.EXaml
{
    //use $$
    internal class EXamlSetDynamicResource : EXamlOperation
    {
        internal override string Write()
        {
            if (@object.IsValid)
            {
                string ret = "";
                ret += String.Format("$({0} {1} {2})$\n",
                    GetValueString(@object),
                    GetValueString(definedBindableProperties.IndexOf(bindableProperty.Resolve())),
                    GetValueString(key));
                return ret;
            }
            else
            {
                return "";
            }
        }

        public EXamlSetDynamicResource(EXamlCreateObject @object, MemberReference bindalbeProperty, string key)
        {
            this.@object = @object;
            this.bindableProperty = bindalbeProperty;
            this.key = key;
            EXamlOperation.eXamlOperations.Add(this);

            @object.AddBindableProperty(bindableProperty);
        }

        private EXamlCreateObject @object;
        private MemberReference bindableProperty;
        private string key;
    }
}
