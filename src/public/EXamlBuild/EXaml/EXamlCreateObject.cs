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
using System.Reflection;
using System.Text;
using Tizen.NUI.Binding;

namespace Tizen.NUI.EXaml
{
    //use {}
    public class EXamlCreateObject : EXamlOperation
    {
        internal override string Write()
        {
            if (false == IsValid)
            {
                return "";
            }

            string ret = "";

            string signBegin = "", signEnd = "";

            if (Instance is EXamlValueConverterFromString
                ||
                true == Type.Resolve()?.IsEnum)
            {
                signBegin = signEnd = "@";
            }
            else
            {
                signBegin = "{";
                signEnd = "}";
            }

            ret += signBegin;

            if (0 < paramsList.Count)
            {
                ret += "(";

                foreach (var param in paramsList)
                {
                    ret += GetValueString(param);
                }

                ret += ")";
            }

            if (Instance is EXamlValueConverterFromString)
            {
                ret += "q(" + (Instance as EXamlValueConverterFromString).GetString() + ")q";
            }
            else if (true == Type.Resolve()?.IsEnum)
            {
                ret += String.Format("o({0} {1})o ",
                    GetValueString(GetTypeIndex(Type)),
                    GetValueString(Instance));
            }
            else
            {
                int typeIndex = GetTypeIndex(Type);

                if (-1 == typeIndex)
                {
                    string message = String.Format("Can't find type {0}\n", Type.FullName);
                    message += String.Format("All defined types are:\n{0}", GetAllDefinedTypesName());
                    throw new Exception(message);
                }
                ret += GetValueString(typeIndex);
            }

            ret += signEnd + "\n";
            return ret;
        }

        internal new TypeReference GetType()
        {
            return Type;
        }

        public EXamlCreateObject(object instance, TypeReference type, object[] @params = null)
        {
            if (null == type.Resolve())
            {
                throw new Exception("Type can't be null when create object");
            }

            Instance = instance;
            Type = type;

            if (null != @params)
            {
                foreach (var obj in @params)
                {
                    paramsList.Add(obj);
                }
            }

            EXamlOperation.eXamlOperations.Add(this);

            Index = eXamlCreateObjects.Count;
            eXamlCreateObjects.Add(this);
        }

        internal bool IsValid
        {
            get;
            set;
        } = true;

        internal object Instance
        {
            get;
            private set;
        }

        internal TypeReference Type
        {
            get;
        }

        internal int Index
        {
            get;
            set;
        }

        internal List<object> paramsList
        {
            get;
        } = new List<object>();

        internal static List<EXamlCreateObject> eXamlCreateObjects
        {
            get;
        } = new List<EXamlCreateObject>();

        internal EXamlDefinitionList<PropertyDefinition> PropertyList
        {
            get;
        } = new EXamlDefinitionList<PropertyDefinition>();

        internal EXamlDefinitionList<EventDefinition> EventList
        {
            get;
        } = new EXamlDefinitionList<EventDefinition>();

        internal HashSet<IMemberDefinition> BindableProperties
        {
            get;
        } = new HashSet<IMemberDefinition>();

        internal void AddProperty(TypeReference declareTypeRef, PropertyDefinition property)
        {
            PropertyList.Add(declareTypeRef, property);
        }

        internal void AddEvent(TypeReference declareTypeRef, EventDefinition eventDefinition)
        {
            EventList.Add(declareTypeRef, eventDefinition);
        }

        internal void AddBindableProperty(MemberReference bindalbeProperty)
        {
            if (!BindableProperties.Contains(bindalbeProperty.Resolve()))
            {
                BindableProperties.Add(bindalbeProperty.Resolve());
            }
        }
    }
}
