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

            if (true == isStaticInstance)
            {
                int typeIndex = GetTypeIndex(Type);

                if (0 > typeIndex)
                {
                    throw new Exception($"Can't get type index of {Type.FullName}");
                }

                if (MemberOfStaticInstance is FieldReference field)
                {
                    ret += $"{{{GetValueString(null)} {GetValueString(field.Name)}}} {GetValueString(typeIndex)}";
                }
                else if (MemberOfStaticInstance is PropertyReference property)
                {
                    ret += $"{{{GetValueString(property.Name)} {GetValueString(null)}}} {GetValueString(typeIndex)}";
                }
            }
            else
            {
                if (null != XFactoryMethod)
                {
                    ret += "[" + GetValueString(definedMethods.IndexOf((XFactoryMethod.DeclaringType, XFactoryMethod))) + "] ";
                }

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
                        throw new Exception(message);
                    }
                    ret += GetValueString(typeIndex);
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

        public EXamlCreateObject(object instance, TypeReference type, MethodDefinition xFactoryMethod, object[] @params = null)
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
            XFactoryMethod = xFactoryMethod;
            eXamlCreateObjects.Add(this);
        }

        public static EXamlCreateObject GetStaticInstance(TypeReference type, FieldReference field, PropertyReference property)
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

            if (StaticInstances.ContainsKey((type, memberRef)))
            {
                return StaticInstances[(type, memberRef)];
            }
            else
            {
                var staticInstance = new EXamlCreateObject(type, field, property);
                StaticInstances.Add((type, memberRef), staticInstance);
                return staticInstance;
            }
        }

        public EXamlCreateObject(TypeReference type, FieldReference field, PropertyReference property)
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

        private static Dictionary<(TypeReference, MemberReference), EXamlCreateObject> StaticInstances
        {
            get;
        } = new Dictionary<(TypeReference, MemberReference), EXamlCreateObject>();

        private bool isStaticInstance = false;
    }
}
