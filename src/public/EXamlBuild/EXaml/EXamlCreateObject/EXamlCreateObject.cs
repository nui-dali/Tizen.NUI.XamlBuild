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
using System.Reflection;
using System.Text;
using Tizen.NUI.Binding;
using Tizen.NUI.EXaml.Build.Tasks;

namespace Tizen.NUI.EXaml
{
    //use {}
    internal class EXamlCreateObject : EXamlOperation
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

            if (true == isStaticInstance)
            {
                int typeIndex = eXamlContext.GetTypeIndex(Type);

                if (0 > typeIndex)
                {
                    throw new Exception($"Can't get type index of {Type.FullName}");
                }

                if (MemberOfStaticInstance is FieldReference field)
                {
                    ret += $"{{{eXamlContext.GetValueString(null)} {eXamlContext.GetValueString(field.Name)}}} {eXamlContext.GetValueString(typeIndex)}";
                }
                else if (MemberOfStaticInstance is PropertyReference property)
                {
                    ret += $"{{{eXamlContext.GetValueString(property.Name)} {eXamlContext.GetValueString(null)}}} {eXamlContext.GetValueString(typeIndex)}";
                }
            }
            else if (true == isTypeObject)
            {
                int typeIndex = eXamlContext.GetTypeIndex(Type);

                if (0 > typeIndex)
                {
                    throw new Exception($"Can't get type index of {Type.FullName}");
                }

                ret += $"`{eXamlContext.GetValueString(typeIndex)}`";
            }
            else
            {
                if (null != XFactoryMethod)
                {
                    ret += "[" + eXamlContext.GetValueString(eXamlContext.definedMethods.IndexOf((XFactoryMethod.DeclaringType, XFactoryMethod))) + "] ";
                }

                if (0 < paramsList.Count)
                {
                    ret += "(";

                    foreach (var param in paramsList)
                    {
                        ret += eXamlContext.GetValueString(param);
                    }

                    ret += ")";
                }

                if (Instance is EXamlValueConverterFromString valueConverterFromString)
                {
                    ret += "q(" + valueConverterFromString.GetString() + ")q";
                }
                else if (true == Type.Resolve()?.IsEnum)
                {
                    ret += String.Format("o({0} {1})o ",
                        eXamlContext.GetValueString(eXamlContext.GetTypeIndex(Type)),
                        eXamlContext.GetValueString(Instance));
                }
                else
                {
                    int typeIndex = eXamlContext.GetTypeIndex(Type);

                    if (-1 == typeIndex)
                    {
                        string message = String.Format("Can't find type {0}\n", Type.FullName);
                        throw new Exception(message);
                    }
                    ret += eXamlContext.GetValueString(typeIndex);
                }
            }

            ret += signEnd + "\n";
            return ret;
        }

        internal new TypeReference GetType()
        {
            if (isStaticInstance)
            {
                if (MemberOfStaticInstance is FieldReference field)
                {
                    return field.FieldType;
                }
                else if (MemberOfStaticInstance is PropertyReference property)
                {
                    return property.PropertyType;
                }
                else
                {
                    throw new Exception($"Invalid static instance, type is {Type.FullName}");
                }
            }
            else
            {
                return Type;
            }
        }

        public EXamlCreateObject(EXamlContext context, object instance, TypeReference type, object[] @params = null)
            : base(context)
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

            eXamlContext.eXamlOperations.Add(this);

            Index = eXamlContext.eXamlCreateObjects.Count;
            eXamlContext.eXamlCreateObjects.Add(this);
        }

        public EXamlCreateObject(EXamlContext context, TypeReference type) : base(context)
        {

            isTypeObject = true;
            Type = type;

            eXamlContext.eXamlOperations.Add(this);

            Index = eXamlContext.eXamlCreateObjects.Count;
            eXamlContext.eXamlCreateObjects.Add(this);
        }

        public EXamlCreateObject(EXamlContext context, object instance, TypeReference type, MethodDefinition xFactoryMethod, object[] @params = null)
            : base(context)
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

            eXamlContext.eXamlOperations.Add(this);

            Index = eXamlContext.eXamlCreateObjects.Count;
            XFactoryMethod = xFactoryMethod;
            eXamlContext.eXamlCreateObjects.Add(this);
        }

        public static EXamlCreateObject GetStaticInstance(EXamlContext context, TypeReference type, FieldReference field, PropertyReference property)
        {
            MemberReference memberRef = null;

            if (null != field)
            {
                memberRef = field;
            }
            else if (null != property)
            {
                memberRef = property;
            }

            if (null == memberRef)
            {
                return null;
            }

            if (context.StaticInstances.ContainsKey((type, memberRef)))
            {
                return context.StaticInstances[(type, memberRef)];
            }
            else
            {
                var staticInstance = new EXamlCreateObject(context, type, field, property);
                context.StaticInstances.Add((type, memberRef), staticInstance);
                return staticInstance;
            }
        }

        public EXamlCreateObject(EXamlContext context, TypeReference type, FieldReference field, PropertyReference property)
            : base(context)
        {
            MemberReference memberRef = null;

            if (null != field)
            {
                memberRef = field;
            }
            else if (null != property)
            {
                memberRef = property;
            }

            Type = type;
            MemberOfStaticInstance = memberRef;
            isStaticInstance = true;

            eXamlContext.eXamlOperations.Add(this);

            Index = eXamlContext.eXamlCreateObjects.Count;
            eXamlContext.eXamlCreateObjects.Add(this);
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

        internal MethodDefinition XFactoryMethod
        {
            get;
            set;
        }

        internal MemberReference MemberOfStaticInstance
        {
            get;
            set;
        }

        internal List<object> paramsList
        {
            get;
        } = new List<object>();

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

        private bool isStaticInstance = false;

        private bool isTypeObject = false;
    }
}
