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
using System.IO;
using System.Reflection;
using System.Text;
using Tizen.NUI.Binding;
using Tizen.NUI.Xaml;

namespace Tizen.NUI.EXaml
{
    public abstract class EXamlOperation
    {
        public static List<EXamlOperation> eXamlOperations = new List<EXamlOperation>();

        public static string GetAssemblyName(AssemblyDefinition assembly)
        {
            string assemblyName = "";
            if (assembly.FullName.StartsWith("Tizen.NUI.XamlBuild"))
            {
                assemblyName = "Tizen.NUI";
            }
            else
            {
                assemblyName = assembly.FullName;

                if (assemblyName.EndsWith(".dll"))
                {
                    assemblyName = assemblyName.Substring(0, assemblyName.Length - ".dll".Length);
                }
                else if (assemblyName.EndsWith(".exe"))
                {
                    assemblyName = assemblyName.Substring(0, assemblyName.Length - ".exe".Length);
                }
                else
                {
                    int firstIndex = assemblyName.IndexOf(',');
                    assemblyName = assemblyName.Substring(0, firstIndex);
                }

                if ("Tizen.NUI.Xaml" == assemblyName)
                {
                    assemblyName = "Tizen.NUI";
                }
            }

            return assemblyName + ", ";
        }

        public static string GetAssemblyName(Assembly assembly)
        {
            string assemblyName = "";
            if (assembly.FullName == typeof(EXamlOperation).Assembly.FullName)
            {
                assemblyName = "Tizen.NUI";
            }
            else
            {
                assemblyName = assembly.FullName;

                if (assemblyName.Substring(assemblyName.Length - ".dll".Length) == ".dll")
                {
                    assemblyName = assemblyName.Substring(0, assemblyName.Length - ".dll".Length);
                }
                else if (assemblyName.Substring(assemblyName.Length - ".exe".Length) == ".exe")
                {
                    assemblyName = assemblyName.Substring(0, assemblyName.Length - ".exe".Length);
                }

                if ("Tizen.NUI.Xaml" == assemblyName)
                {
                    assemblyName = "Tizen.NUI";
                }
            }

            return assemblyName + ", ";
        }

        public static void Clear()
        {
            eXamlOperations.Clear();

            definedAssemblies.Clear();
            definedTypes.Clear();
            definedProperties.Clear();
            definedEvents.Clear();
            definedBindableProperties.Clear();
            definedMethods.Clear();

            EXamlCreateObject.ClearStaticThing();
            EXamlAddObject.eXamlAddObjectList.Clear();
            EXamlAddEvent.eXamlAddEventList.Clear();
            EXamlValueConverterFromString.ClearStaticThing();
            EXamlRegisterXName.ClearStaticThing();
            EXamlAddToResourceDictionary.resourceDictionary.Clear();
            EXamlGetObjectByProperty.ClearList();
        }

        public static void WriteOpertions(string filePath)
        {
            var ret = WriteOpertions();
            if (filePath.Contains("\\"))
            {
                OutputDir = filePath.Substring(0, filePath.LastIndexOf('\\'));
            }
            else
            {
                OutputDir = filePath.Substring(0, filePath.LastIndexOf('/'));
            }
            if (!Directory.Exists(OutputDir))
            {
                Directory.CreateDirectory(OutputDir);
            }

            var stream = File.CreateText(filePath);
            stream.Write(ret);
            stream.Close();
        }

        public static string OutputDir
        {
            get;
            private set;
        }

        private static void GatherType(TypeReference type)
        {
            if (-1 == GetTypeIndex(type))
            {
                var assemblyName = GetAssemblyName(type.Resolve().Module.Assembly);
                if (!definedAssemblies.Contains(assemblyName))
                {
                    definedAssemblies.Add(assemblyName);
                }

                definedTypes.Add(new TypeData(type));
            }
        }

        private static void GatherType(Type type)
        {
            var assemblyName = GetAssemblyName(type.Assembly);
            if (!definedAssemblies.Contains(assemblyName))
            {
                definedAssemblies.Add(assemblyName);
            }

            if (-1 == GetTypeIndex(type))
            {
                definedTypes.Add(new TypeData(type));
            }
        }

        private static void GatherMethod((TypeReference, MethodDefinition) methodInfo)
        {
            GatherType(methodInfo.Item1);

            foreach (var param in methodInfo.Item2.Parameters)
            {
                GatherType(param.ParameterType);
            }

            definedMethods.Add(methodInfo.Item1, methodInfo.Item2);
        }

        private static string WriteOpertions()
        {
            string ret = "";

            int objectIndex = 0;

            foreach (var examlOp in EXamlCreateObject.eXamlCreateObjects)
            {
                if (examlOp.IsValid)
                {
                    examlOp.Index = objectIndex++;
                }
            }

            foreach (var examlOp in EXamlCreateObject.eXamlCreateObjects)
            {
                if (examlOp.IsValid)
                {
                    GatherType(examlOp.Type);

                    foreach (var property in examlOp.PropertyList)
                    {
                        GatherType(property.Item1);

                        definedProperties.Add(property.Item1, property.Item2);

                        if (true == property.Item1.Resolve()?.IsEnum)
                        {
                            GatherType(property.Item1.Resolve());
                        }
                    }

                    foreach (var eventDef in examlOp.EventList)
                    {
                        GatherType(eventDef.Item1);

                        definedEvents.Add(eventDef.Item1, eventDef.Item2);
                    }

                    foreach (var property in examlOp.BindableProperties)
                    {
                        if (!definedBindableProperties.Contains(property))
                        {
                            definedBindableProperties.Add(property);
                        }

                        var typeDef = property.DeclaringType;
                        if (-1 == GetTypeIndex(typeDef))
                        {
                            GatherType(property.DeclaringType);
                        }
                    }

                    foreach (var param in examlOp.paramsList)
                    {
                        if (null != param && param.GetType().IsEnum)
                        {
                            GatherType(param.GetType());
                        }
                    }

                    if (null != examlOp.XFactoryMethod)
                    {
                        GatherMethod((examlOp.XFactoryMethod.DeclaringType, examlOp.XFactoryMethod));
                    }
                }
            }

            foreach (var op in EXamlAddObject.eXamlAddObjectList)
            {
                if (op.Parent.IsValid && (!(op.Child is EXamlCreateObject eXamlCreateObject) || eXamlCreateObject.IsValid))
                {
                    GatherMethod((op.Method.DeclaringType, op.Method));
                }
            }

            foreach (var op in EXamlAddEvent.eXamlAddEventList)
            {
                if (op.Instance.IsValid)
                {
                    GatherMethod((op.Value.DeclaringType, op.Value));
                }
            }

            ret += "<\n";
            foreach (var ass in definedAssemblies)
            {
                ret += String.Format("\"{0}\"\n", ass);
            }
            ret += ">\n";

            ret += "<\n";
            foreach (var type in definedTypes)
            {
                ret += type.ToString() + "\n";
            }
            ret += ">\n";

            ret += "<\n";
            foreach (var property in definedProperties)
            {
                var typeDef = property.Item1;
                int typeIndex = GetTypeIndex(typeDef);
                ret += String.Format("(\"{0}\" \"{1}\")\n", typeIndex, property.Item2.Name);
            }
            ret += ">\n";

            ret += "<\n";
            foreach (var eventDef in definedEvents)
            {
                var typeDef = eventDef.Item1;
                int typeIndex = GetTypeIndex(typeDef);
                ret += String.Format("(\"{0}\" \"{1}\")\n", typeIndex, eventDef.Item2.Name);
            }
            ret += ">\n";

            ret += "<\n";
            foreach (var method in definedMethods)
            {
                var typeDef = method.Item1;
                int typeIndex = GetTypeIndex(typeDef);

                string strForParam = "(";
                foreach (var param in method.Item2.Parameters)
                {
                    int paramTypeIndex = GetTypeIndex(param.ParameterType);

                    if (-1 == paramTypeIndex)
                    {
                        throw new Exception($"Can't find index of param type {param.ParameterType.FullName}");
                    }

                    strForParam += GetValueString(paramTypeIndex) + " ";
                }
                strForParam += ")";

                ret += String.Format("(\"{0}\" \"{1}\" {2})\n", typeIndex, method.Item2.Name, strForParam);
            }
            ret += ">\n";

            ret += "<\n";
            foreach (var property in definedBindableProperties)
            {
                var typeDef = property.DeclaringType;
                int typeIndex = GetTypeIndex(typeDef);
                ret += String.Format("(\"{0}\" \"{1}\")\n", typeIndex, property.Name);
            }
            ret += ">\n";

            foreach (var op in eXamlOperations)
            {
                ret += op.Write();
            }

            return ret;
        }

        internal abstract string Write();

        private static List<string> definedAssemblies
        {
            get;
        } = new List<string>();

        private class TypeData
        {
            internal TypeData(Type type)
            {
                Type = type;
                AssemblyName = GetAssemblyName(type.Assembly);

                if (type.IsNested)
                {
                    FullName = type.FullName.Replace('/', '+');
                }
                else
                {
                    FullName = type.FullName;
                }
            }

            internal TypeData(TypeReference typeReference)
            {
                TypeReference = typeReference;

                AssemblyName = GetAssemblyName(typeReference.Resolve().Module.Assembly);

                if (typeReference is GenericInstanceType)
                {
                    GenericArgumentTypes = new List<TypeData>();

                    var genericType = typeReference as GenericInstanceType;

                    foreach (var type in genericType.GenericArguments)
                    {
                        GatherType(type);
                        GenericArgumentTypes.Add(new TypeData(type));
                    }

                    FullName = typeReference.Resolve().FullName;
                }
                else
                {
                    FullName = typeReference.FullName;
                }

                if (typeReference.IsNested)
                {
                    FullName = FullName.Replace('/', '+');
                }
            }

            public override string ToString()
            {
                string ret = "";
                int assemblyIndex = definedAssemblies.IndexOf(AssemblyName);

                if (null == GenericArgumentTypes)
                {
                    ret += String.Format("(\"{0}\" \"{1}\")", assemblyIndex, FullName);
                }
                else
                {
                    string strForGenericTypes = "(";

                    foreach (var type in GenericArgumentTypes)
                    {
                        strForGenericTypes += GetValueString(GetTypeIndex(type)) + " ";
                    }

                    strForGenericTypes += ")";

                    ret += String.Format("(\"{0}\" {1} \"{2}\")", assemblyIndex, strForGenericTypes, FullName);
                }

                return ret;
            }

            internal TypeReference TypeReference
            {
                get;
            }

            internal Type Type
            {
                get;
            }

            internal string AssemblyName
            {
                get;
            }

            internal string FullName
            {
                get;
            }

            internal List<TypeData> GenericArgumentTypes
            {
                get;
            }
        }

        private static List<TypeData> definedTypes
        {
            get;
        } = new List<TypeData>();

        private static int GetTypeIndex(TypeData typeData)
        {
            if (null != typeData.TypeReference)
            {
                return GetTypeIndex(typeData.TypeReference);
            }

            if (null != typeData.Type)
            {
                return GetTypeIndex(typeData.Type);
            }

            return -1;
        }

        internal static int GetTypeIndex(TypeReference typeReference)
        {
            for (int i = 0; i < definedTypes.Count; i++)
            {
                if (EXamlUtility.IsSameTypeReference(typeReference, definedTypes[i].TypeReference))
                {
                    return i;
                }
            }

            int ret = -1;
            switch (typeReference.FullName)
            {
                case "System.SByte":
                    ret = -2;
                    break;
                case "System.Int16":
                    ret = -3;
                    break;
                case "System.Int32":
                    ret = -4;
                    break;
                case "System.Int64":
                    ret = -5;
                    break;
                case "System.Byte":
                    ret = -6;
                    break;
                case "System.UInt16":
                    ret = -7;
                    break;
                case "System.UInt32":
                    ret = -8;
                    break;
                case "System.UInt64":
                    ret = -9;
                    break;
                case "System.Boolean":
                    ret = -10;
                    break;
                case "System.String":
                    ret = -11;
                    break;
                case "System.Object":
                    ret = -12;
                    break;
                case "System.Char":
                    ret = -13;
                    break;
                case "System.Decimal":
                    ret = -14;
                    break;
                case "System.Single":
                    ret = -15;
                    break;
                case "System.Double":
                    ret = -16;
                    break;
                case "System.TimeSpan":
                    ret = -17;
                    break;
                case "System.Uri":
                    ret = -18;
                    break;
            }
            
            return ret;
        }

        private static int GetTypeIndex(Type type)
        {
            for (int i = 0; i < definedTypes.Count; i++)
            {
                if (type == definedTypes[i].Type)
                {
                    return i;
                }
            }

            return -1;
        }

        internal static EXamlDefinitionList<PropertyDefinition> definedProperties
        {
            get;
        } = new EXamlDefinitionList<PropertyDefinition>();

        internal static EXamlDefinitionList<EventDefinition> definedEvents
        {
            get;
        } = new EXamlDefinitionList<EventDefinition>();

        internal static List<IMemberDefinition> definedBindableProperties
        {
            get;
        } = new List<IMemberDefinition>();

        internal static EXamlDefinitionList<MethodDefinition> definedMethods
        {
            get;
        } = new EXamlDefinitionList<MethodDefinition>();

        internal static string GetValueString(object valueObject)
        {
            //Fang: How to deal the Enum
            string ret = "";

            if (null == valueObject)
            {
                ret += "zz ";
            }
            else if (valueObject is List<object> listObjects)
            {
                ret += "(";

                foreach (var obj in listObjects)
                {
                    ret += GetValueString(obj);
                    ret += " ";
                }

                ret += ")";
            }
            else
            {
                //Fang
                var paramType = valueObject.GetType();

                string signBegin = "a", signEnd = "a";
                string value = "";

                if (valueObject is EXamlCreateObject)
                {
                    signBegin = signEnd = "a";
                    value = (valueObject as EXamlCreateObject).Index.ToString();
                }
                else if (valueObject is EXamlGetObjectByProperty)
                {
                    return GetValueString(EXamlGetObjectByProperty.GetIndex(valueObject as EXamlGetObjectByProperty));
                }
                else if (paramType == typeof(string) || paramType == typeof(char) || paramType == typeof(Uri))
                {
                    signBegin = signEnd = "\"";
                    value = valueObject.ToString();
                }
                else if (paramType == typeof(SByte))
                {
                    signBegin = signEnd = "b";
                    value = valueObject.ToString();
                }
                else if (paramType == typeof(Int16))
                {
                    signBegin = signEnd = "c";
                    value = valueObject.ToString();
                }
                else if (paramType == typeof(Int32))
                {
                    signBegin = signEnd = "d";
                    value = valueObject.ToString();
                }
                else if (paramType == typeof(Int64))
                {
                    signBegin = signEnd = "e";
                    value = valueObject.ToString();
                }
                else if (paramType == typeof(Byte))
                {
                    signBegin = signEnd = "f";
                    value = valueObject.ToString();
                }
                else if (paramType == typeof(UInt16))
                {
                    signBegin = signEnd = "g";
                    value = valueObject.ToString();
                }
                else if (paramType == typeof(UInt32))
                {
                    signBegin = signEnd = "h";
                    value = valueObject.ToString();
                }
                else if (paramType == typeof(UInt64))
                {
                    signBegin = signEnd = "i";
                    value = valueObject.ToString();
                }
                else if (paramType == typeof(Single))
                {
                    signBegin = signEnd = "j";
                    value = valueObject.ToString();
                }
                else if (paramType == typeof(Double))
                {
                    signBegin = signEnd = "k";
                    value = valueObject.ToString();
                }
                else if (paramType == typeof(TimeSpan))
                {
                    signBegin = signEnd = "l";
                    value = valueObject.ToString();
                }
                else if (paramType == typeof(Boolean))
                {
                    signBegin = signEnd = "m";
                    value = valueObject.ToString();
                }
                else if (paramType == typeof(decimal))
                {
                    signBegin = signEnd = "n";
                    value = valueObject.ToString();
                }
                else if (paramType.IsEnum)
                {
                    signBegin = "o(";
                    int typeIndex = GetTypeIndex(paramType);
                    value = String.Format("d{0}d \"{1}\"", typeIndex, valueObject.ToString());
                    signEnd = ")o";
                }
                else if (valueObject is EXamlValueConverterFromString)
                {
                    signBegin = "q(";
                    signEnd = ")q";
                    value = (valueObject as EXamlValueConverterFromString).GetString();
                }

                ret += String.Format("{0}{1}{2} ", signBegin, value, signEnd);
            }

            return ret;
        }
    }
}
