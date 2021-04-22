using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Tizen.NUI.Binding;
using Tizen.NUI.Binding.Internals;
using Tizen.NUI.EXaml;
using Tizen.NUI.Xaml;
using Tizen.NUI.Xaml.Build.Tasks;
using static Mono.Cecil.Cil.Instruction;
using static Mono.Cecil.Cil.OpCodes;

namespace Tizen.NUI.EXaml.Build.Tasks
{
	class EXamlSetPropertiesVisitor : IXamlNodeVisitor
	{
		static int dtcount;
		static int typedBindingCount;

		static readonly IList<XmlName> skips = new List<XmlName>
		{
			XmlName.xKey,
			XmlName.xTypeArguments,
			XmlName.xArguments,
			XmlName.xFactoryMethod,
			XmlName.xName,
			XmlName.xDataType
		};

		public EXamlSetPropertiesVisitor(EXamlContext context, bool stopOnResourceDictionary = false)
		{
			Context = context;
			Module = context.Module;
			StopOnResourceDictionary = stopOnResourceDictionary;
		}

		public EXamlContext Context { get; }
		public bool StopOnResourceDictionary { get; }
		public TreeVisitingMode VisitingMode => TreeVisitingMode.BottomUp;
		public bool StopOnDataTemplate => true;
		public bool VisitNodeOnDataTemplate => true;
		public bool SkipChildren(INode node, INode parentNode) => false;

		public bool IsResourceDictionary(ElementNode node)
		{
			var parentVar = Context.Values[node] as EXamlCreateObject;
			return null != parentVar
					&&
					(parentVar.GetType().FullName == "Tizen.NUI.Binding.ResourceDictionary"
					|| parentVar.GetType().Resolve().BaseType?.FullName == "Tizen.NUI.Binding.ResourceDictionary");
		}

		ModuleDefinition Module { get; }

		public void Visit(ValueNode node, INode parentNode)
		{
			//TODO support Label text as element
			XmlName propertyName;
			if (!TryGetPropertyName(node, parentNode, out propertyName))
			{
				if (!IsCollectionItem(node, parentNode))
					return;
				string contentProperty;
				if (!Context.Variables.ContainsKey((IElementNode)parentNode))
					return;
				var parentVar = Context.Variables[(IElementNode)parentNode];
				if ((contentProperty = GetContentProperty(parentVar.VariableType)) != null)
					propertyName = new XmlName(((IElementNode)parentNode).NamespaceURI, contentProperty);
				else
					return;
			}

			if (TrySetRuntimeName(propertyName, Context.Values[parentNode] as EXamlCreateObject, node))
				return;
			if (skips.Contains(propertyName))
				return;
			if (parentNode is IElementNode && ((IElementNode)parentNode).SkipProperties.Contains (propertyName))
				return;
			if (propertyName.Equals(XamlParser.McUri, "Ignorable"))
				return;

			SetPropertyValue(Context.Values[parentNode] as EXamlCreateObject, propertyName, node, Context, node);
		}

		public void Visit(MarkupNode node, INode parentNode)
		{
		}

		public void Visit(ElementNode node, INode parentNode)
		{
			XmlName propertyName = XmlName.Empty;

			//Simplify ListNodes with single elements
			var pList = parentNode as ListNode;
			if (pList != null && pList.CollectionItems.Count == 1) {
				propertyName = pList.XmlName;
				parentNode = parentNode.Parent;
			}

			if ((propertyName != XmlName.Empty || TryGetPropertyName(node, parentNode, out propertyName)) && skips.Contains(propertyName))
				return;

			if (propertyName == XmlName._CreateContent) {
				SetDataTemplate((IElementNode)parentNode, node, Context, node);
				return;
			}

			//if this node is an IMarkupExtension, invoke ProvideValue() and replace the variable
			var vardef = Context.Values[node];

			var localName = propertyName.LocalName;
			TypeReference declaringTypeReference = null;
			FieldReference bpRef = null;
			var _ = false;
			PropertyDefinition propertyRef = null;
			if (parentNode is IElementNode && propertyName != XmlName.Empty) {
				bpRef = GetBindablePropertyReference(Context.Values[parentNode] as EXamlCreateObject, propertyName.NamespaceURI, ref localName, out _, Context, node);
				propertyRef = Context.Variables [(IElementNode)parentNode].VariableType.GetProperty(pd => pd.Name == localName, out declaringTypeReference);
			}

			if (vardef is EXamlCreateObject)
			{
				var realValue = ProvideValue(vardef as EXamlCreateObject, Context, Module, node, bpRef: bpRef, propertyRef: propertyRef, propertyDeclaringTypeRef: declaringTypeReference);
				if (null != realValue && vardef != realValue)
				{
					(vardef as EXamlCreateObject).IsValid = false;
					Context.Values[node] = realValue;
					vardef = realValue;
				}
			}

			if (propertyName != XmlName.Empty) {
				if (skips.Contains(propertyName))
					return;
				if (parentNode is IElementNode && ((IElementNode)parentNode).SkipProperties.Contains (propertyName))
					return;
				
				SetPropertyValue(Context.Values[parentNode] as EXamlCreateObject, propertyName, node, Context, node);
			}
			else if (IsCollectionItem(node, parentNode) && parentNode is IElementNode) {
				var parentVar = Context.Values[parentNode] as EXamlCreateObject;
				string contentProperty;

                if (CanAddToResourceDictionary(parentVar, parentVar.GetType(), node, node, Context))
                {
					var keyName = (node.Properties[XmlName.xKey] as ValueNode).Value as string;
					new EXamlAddToResourceDictionary(parentVar, keyName, Context.Values[node]);
                }
                // Collection element, implicit content, or implicit collection element.
                else if (parentVar.GetType().GetMethods(md => md.Name == "Add" && md.Parameters.Count == 1, Module).Any())
                {
                    var elementType = parentVar.GetType();
                    var adderTuple = elementType.GetMethods(md => md.Name == "Add" && md.Parameters.Count == 1, Module).First();
                    var adderRef = Module.ImportReference(adderTuple.Item1);
                    adderRef = Module.ImportReference(adderRef.ResolveGenericParameters(adderTuple.Item2, Module));
					new EXamlAddObject(parentVar, Context.Values[node] as EXamlCreateObject, adderRef.Resolve());
                }
                else if ((contentProperty = GetContentProperty(parentVar.GetType())) != null)
                {
                    var name = new XmlName(node.NamespaceURI, contentProperty);
                    if (skips.Contains(name))
                        return;
                    if (parentNode is IElementNode && ((IElementNode)parentNode).SkipProperties.Contains(propertyName))
                        return;
                    
					SetPropertyValue(Context.Values[parentNode] as EXamlCreateObject, name, node, Context, node);
                }
                else
                {
                    throw new XamlParseException($"Can not set the content of {((IElementNode)parentNode).XmlType.Name} as it doesn't have a ContentPropertyAttribute", node);
                }
			}
			else if (IsCollectionItem(node, parentNode) && parentNode is ListNode)
			{
				var parentList = (ListNode)parentNode;
				var parent = Context.Variables[((IElementNode)parentNode.Parent)];

				if (skips.Contains(parentList.XmlName))
					return;
				if (parentNode is IElementNode && ((IElementNode)parentNode).SkipProperties.Contains (propertyName))
					return;
				var elementType = parent.VariableType;
				var localname = parentList.XmlName.LocalName;

				//Fang: Need to deal
				//TypeReference propertyType;
				//Context.IL.Append(GetPropertyValue(parent, parentList.XmlName, Context, node, out propertyType));

				//if (CanAddToResourceDictionary(parent, propertyType, node, node, Context)) {
				//	Context.IL.Append(AddToResourceDictionary(node, node, Context));
				//	return;
				//} 
				//var adderTuple = propertyType.GetMethods(md => md.Name == "Add" && md.Parameters.Count == 1, Module).FirstOrDefault();
				//if (adderTuple == null)
				//	throw new XamlParseException($"Can not Add() elements to {parent.VariableType}.{localname}", node);
				//var adderRef = Module.ImportReference(adderTuple.Item1);
				//adderRef = Module.ImportReference(adderRef.ResolveGenericParameters(adderTuple.Item2, Module));

				//Context.IL.Emit(OpCodes.Ldloc, vardef);
				//Context.IL.Emit(OpCodes.Callvirt, adderRef);
				//if (adderRef.ReturnType.FullName != "System.Void")
				//		Context.IL.Emit(OpCodes.Pop);
			}
		}

		public void Visit(RootNode node, INode parentNode)
		{
		}

		public void Visit(ListNode node, INode parentNode)
		{
		}

		public static bool TryGetPropertyName(INode node, INode parentNode, out XmlName name)
		{
			name = default(XmlName);
			var parentElement = parentNode as IElementNode;
			if (parentElement == null)
				return false;
			foreach (var kvp in parentElement.Properties)
			{
				if (kvp.Value != node)
					continue;
				name = kvp.Key;
				return true;
			}
			return false;
		}

		static bool IsCollectionItem(INode node, INode parentNode)
		{
			var parentList = parentNode as IListNode;
			if (parentList == null)
				return false;
			return parentList.CollectionItems.Contains(node);
		}

		internal static string GetContentProperty(TypeReference typeRef)
		{
			var typeDef = typeRef.ResolveCached();
			var attributes = typeDef.CustomAttributes;
			var attr =
				attributes.FirstOrDefault(cad => ContentPropertyAttribute.ContentPropertyTypes.Contains(cad.AttributeType.FullName));
			if (attr != null)
				return attr.ConstructorArguments[0].Value as string;
			if (typeDef.BaseType == null)
				return null;
			return GetContentProperty(typeDef.BaseType);
		}

		public static object ProvideValue(EXamlCreateObject instance, EXamlContext context,
		                                  ModuleDefinition module, ElementNode node, FieldReference bpRef = null,
		                                  PropertyReference propertyRef = null, TypeReference propertyDeclaringTypeRef = null)
		{
			GenericInstanceType markupExtension;
			IList<TypeReference> genericArguments;
			if (instance.GetType().FullName == "Tizen.NUI.Xaml.ArrayExtension" &&
			    instance.GetType().ImplementsGenericInterface("Tizen.NUI.Xaml.IMarkupExtension`1", out markupExtension, out genericArguments))
			{
				
			}
			else if (instance.GetType().ImplementsGenericInterface("Tizen.NUI.Xaml.IMarkupExtension`1", out markupExtension, out genericArguments))
			{
				var nodeValue = context.Values[node] as EXamlCreateObject;
				if (nodeValue?.Instance is BindingExtension)
				{
					var newValue = (nodeValue.Instance as BindingExtension).ProvideValue(module);
					return newValue;
				}
				else if (nodeValue?.Instance is DynamicResourceExtension)
                {
					var dynamicResExtension = nodeValue.Instance as DynamicResourceExtension;
					var newValue = dynamicResExtension.ProvideValue();
					var newTypeRef = module.ImportReference(newValue.GetType());
					return new EXamlCreateObject(newValue, newTypeRef, new object[] { dynamicResExtension.Key });
				}
			}
			else
            {
				var nodeValue = context.Values[node] as EXamlCreateObject;
				if (null != nodeValue)
				{
					if (nodeValue.GetType().ImplementsInterface(module.ImportReference((XamlTask.xamlAssemblyName, XamlTask.xamlNameSpace, "IMarkupExtension"))))
					{
						var acceptEmptyServiceProvider = instance.GetType().GetCustomAttribute(module, (XamlTask.xamlAssemblyName, XamlTask.xamlNameSpace, "AcceptEmptyServiceProviderAttribute")) != null;
						if (nodeValue.Instance is ReferenceExtension)
                        {
							var newValue = (nodeValue.Instance as ReferenceExtension).ProvideValue();
							return newValue;
						}
						else if (nodeValue.Instance is StaticResourceExtension)
                        {
							var newValue = (nodeValue.Instance as StaticResourceExtension).ProvideValue();
							return newValue;
						}
					}
					else if (nodeValue.GetType().ImplementsInterface(module.ImportReference((XamlTask.xamlAssemblyName, XamlTask.xamlNameSpace, "IValueProvider"))))
					{
						//var acceptEmptyServiceProvider = context.Variables[node].VariableType.GetCustomAttribute(module, (XamlCTask.xamlAssemblyName, XamlCTask.xamlNameSpace, "AcceptEmptyServiceProviderAttribute")) != null;
						//var valueProviderType = context.Variables[node].VariableType;
						////If the IValueProvider has a ProvideCompiledAttribute that can be resolved, shortcut this
						//var compiledValueProviderName = valueProviderType?.GetCustomAttribute(module, (XamlCTask.xamlAssemblyName, XamlCTask.xamlNameSpace, "ProvideCompiledAttribute"))?.ConstructorArguments?[0].Value as string;
						//Type compiledValueProviderType;
						//if (compiledValueProviderName != null && (compiledValueProviderType = Type.GetType(compiledValueProviderName)) != null)
						//{
						//	var compiledValueProvider = Activator.CreateInstance(compiledValueProviderType);
						//	var cProvideValue = typeof(ICompiledValueProvider).GetMethods().FirstOrDefault(md => md.Name == "ProvideValue");
						//	var instructions = (IEnumerable<Instruction>)cProvideValue.Invoke(compiledValueProvider, new object[] {
						//instance,
						//context.Module,
						//node as BaseNode,
						//context});
						//foreach (var i in instructions)
						//	yield return i;
						//yield break;
						//}
					}
				}

				//instance.VariableDefinition = new VariableDefinition(module.TypeSystem.Object);
				//yield return Create(Ldloc, context.Variables[node]);
				//if (acceptEmptyServiceProvider)
				//	yield return Create(Ldnull);
				//else
				//	foreach (var instruction in node.PushServiceProvider(context, bpRef, propertyRef, propertyDeclaringTypeRef))
				//		yield return instruction;
				//yield return Create(Callvirt, module.ImportMethodReference((XamlCTask.xamlAssemblyName, XamlCTask.xamlNameSpace, "IValueProvider"),
				//														   methodName: "ProvideValue",
				//														   parameterTypes: new[] { ("System.ComponentModel", "System", "IServiceProvider") }));
				//yield return Create(Stloc, instance.VariableDefinition);
			}

			return null;
		}

		static IList<Tuple<PropertyDefinition, string>> ParsePath(string path, TypeReference tSourceRef, IXmlLineInfo lineInfo, ModuleDefinition module)
		{
			if (string.IsNullOrWhiteSpace(path))
				return null;
			path = path.Trim(' ', '.'); //trim leading or trailing dots
			var parts = path.Split(new [] { '.' }, StringSplitOptions.RemoveEmptyEntries);
			var properties = new List<Tuple<PropertyDefinition, string>>();

			var previousPartTypeRef = tSourceRef;
			TypeReference _;
			foreach (var part in parts) {
				var p = part;
				string indexArg = null;
				var lbIndex = p.IndexOf('[');
				if (lbIndex != -1) {
					var rbIndex = p.LastIndexOf(']');
					if (rbIndex == -1)
						throw new XamlParseException("Binding: Indexer did not contain closing bracket", lineInfo);
					
					var argLength = rbIndex - lbIndex - 1;
					if (argLength == 0)
						throw new XamlParseException("Binding: Indexer did not contain arguments", lineInfo);

					indexArg = p.Substring(lbIndex + 1, argLength).Trim();
					if (indexArg.Length == 0)
						throw new XamlParseException("Binding: Indexer did not contain arguments", lineInfo);
					
					p = p.Substring(0, lbIndex);
					p = p.Trim();
				}

				if (p.Length > 0) {
					var property = previousPartTypeRef.GetProperty(pd => pd.Name == p && pd.GetMethod != null && pd.GetMethod.IsPublic, out _)
					                                  ?? throw new XamlParseException($"Binding: Property '{p}' not found on '{previousPartTypeRef}'", lineInfo);
					properties.Add(new Tuple<PropertyDefinition, string>(property,null));
					previousPartTypeRef = property.PropertyType;
				}
				if (indexArg != null) {
					var defaultMemberAttribute = previousPartTypeRef.GetCustomAttribute(module, ("mscorlib", "System.Reflection", "DefaultMemberAttribute"));
					var indexerName = defaultMemberAttribute?.ConstructorArguments?.FirstOrDefault().Value as string ?? "Item";
					var indexer = previousPartTypeRef.GetProperty(pd => pd.Name == indexerName && pd.GetMethod != null && pd.GetMethod.IsPublic, out _);
					properties.Add(new Tuple<PropertyDefinition, string>(indexer, indexArg));
					if (indexer.PropertyType != module.TypeSystem.String && indexer.PropertyType != module.TypeSystem.Int32)
						throw new XamlParseException($"Binding: Unsupported indexer index type: {indexer.PropertyType.FullName}", lineInfo);
					previousPartTypeRef = indexer.PropertyType;
				}
			}
			return properties;
		}

		public static void SetPropertyValue(EXamlCreateObject parent, XmlName propertyName, INode valueNode, EXamlContext context, IXmlLineInfo iXmlLineInfo)
		{
			var module = context.Module;
			var localName = propertyName.LocalName;
			bool attached;

			var bpRef = GetBindablePropertyReference(parent, propertyName.NamespaceURI, ref localName, out attached, context, iXmlLineInfo);

			//If the target is an event, connect
			if (CanConnectEvent(parent, localName, attached))
			{
				ConnectEvent(parent, localName, valueNode, iXmlLineInfo, context);
			}
			//If Value is DynamicResource, SetDynamicResource
			else if (CanSetDynamicResource(bpRef, valueNode, context))
			{
				SetDynamicResource(parent, bpRef, valueNode as IElementNode, iXmlLineInfo, context);
			}
			//If Value is a BindingBase and target is a BP, SetBinding
			else if (CanSetBinding(bpRef, valueNode, context))
			{
				SetBinding(parent, bpRef, valueNode as IElementNode, iXmlLineInfo, context);
			}
			//If it's a property, set it
			else if (CanSet(parent, localName, valueNode, context))
			{
				Set(parent, localName, valueNode, iXmlLineInfo, context);
			}
			//If it's a BP, SetValue ()
			else if (CanSetValue(bpRef, attached, valueNode, iXmlLineInfo, context))
			{
				SetValue(parent, bpRef, valueNode, iXmlLineInfo, context);
			}
			//If it's an already initialized property, add to it
			//if (CanAdd(parent, propertyName, valueNode, iXmlLineInfo, context))
			//	return Add(parent, propertyName, valueNode, iXmlLineInfo, context);
			else
			{
				throw new XamlParseException($"No property, bindable property, or event found for '{localName}', or mismatching type between value and property.", iXmlLineInfo);
			}
		}

		//public static IEnumerable<Instruction> GetPropertyValue(EXamlCreateObject parent, XmlName propertyName, ILContext context, IXmlLineInfo lineInfo, out TypeReference propertyType)
		//{
		//	var module = context.Module;
		//	var localName = propertyName.LocalName;
		//	bool attached;
		//	var bpRef = GetBindablePropertyReference(parent, propertyName.NamespaceURI, ref localName, out attached, context, lineInfo);

		//	//If it's a BP, GetValue ()
		//	if (CanGetValue(parent, bpRef, attached, lineInfo, context, out _))
		//		return GetValue(parent, bpRef, lineInfo, context, out propertyType);

		//	//If it's a property, set it
		//	if (CanGet(parent, localName, context, out _))
		//		return Get(parent, localName, lineInfo, context, out propertyType);

		//	throw new XamlParseException($"Property {localName} is not found or does not have an accessible getter", lineInfo);
		//}

		static FieldReference GetBindablePropertyReference(EXamlCreateObject parent, string namespaceURI, ref string localName, out bool attached, EXamlContext context, IXmlLineInfo iXmlLineInfo)
		{
			var module = context.Module;
			TypeReference declaringTypeReference;

			//If it's an attached BP, update elementType and propertyName
			var bpOwnerType = parent.GetType();
			attached = GetNameAndTypeRef(ref bpOwnerType, namespaceURI, ref localName, context, iXmlLineInfo);
			var name = $"{localName}Property";
			FieldReference bpRef = bpOwnerType.GetField(fd => fd.Name == name &&
														fd.IsStatic &&
														(fd.IsPublic || fd.IsAssembly), out declaringTypeReference);
			if (bpRef != null) {
				bpRef = module.ImportReference(bpRef.ResolveGenericParameters(declaringTypeReference));
				bpRef.FieldType = module.ImportReference(bpRef.FieldType);
			}
			return bpRef;
		}

		static bool CanConnectEvent(EXamlCreateObject parent, string localName, bool attached)
		{
			return !attached && parent.GetType().GetEvent(ed => ed.Name == localName, out _) != null;
		}

		static void ConnectEvent(EXamlCreateObject parent, string localName, INode valueNode, IXmlLineInfo iXmlLineInfo, EXamlContext context)
		{
			//Fang: Need to deal connect event
			var elementType = parent.GetType();
			var module = context.Module;
			TypeReference eventDeclaringTypeRef;

            var eventinfo = elementType.GetEvent(ed => ed.Name == localName, out eventDeclaringTypeRef);

//			IL_0007:  ldloc.0 
//			IL_0008:  ldarg.0 
//
//			IL_0009:  ldftn instance void class Tizen.NUI.Xaml.XamlcTests.MyPage::OnButtonClicked(object, class [mscorlib]System.EventArgs)
//OR, if the handler is virtual
//			IL_000x:  ldarg.0 
//			IL_0009:  ldvirtftn instance void class Tizen.NUI.Xaml.XamlcTests.MyPage::OnButtonClicked(object, class [mscorlib]System.EventArgs)
//
//			IL_000f:  newobj instance void class [mscorlib]System.EventHandler::'.ctor'(object, native int)
//			IL_0014:  callvirt instance void class [Tizen.NUI.Xaml.Core]Tizen.NUI.Xaml.Button::add_Clicked(class [mscorlib]System.EventHandler)

			var value = ((ValueNode)valueNode).Value;

            //if (context.Root is VariableDefinition)
            //    yield return Create(Ldloc, context.Root as VariableDefinition);
            //else if (context.Root is FieldDefinition)
            //{
            //    yield return Create(Ldarg_0);
            //    yield return Create(Ldfld, context.Root as FieldDefinition);
            //}
            //else
            //    throw new InvalidProgramException();
            var declaringType = context.Type;
            while (declaringType.IsNested)
                declaringType = declaringType.DeclaringType;
            var handler = declaringType.AllMethods().FirstOrDefault(md => md.Name == value as string);

            //check if the handler signature matches the Invoke signature;
            var invoke = module.ImportReference(eventinfo.EventType.ResolveCached().GetMethods().First(md => md.Name == "Invoke"));
            invoke = invoke.ResolveGenericParameters(eventinfo.EventType, module);
            if (!handler.ReturnType.InheritsFromOrImplements(invoke.ReturnType))
            {
                TypeDefinition realType = eventinfo.EventType.ResolveCached();

                GenericInstanceType genericInstanceType = eventinfo.EventType as GenericInstanceType;

                if (null != genericInstanceType
                    && genericInstanceType.GenericArguments.Count == realType.GenericParameters.Count)
                {
                    Dictionary<string, TypeReference> dict = new Dictionary<string, TypeReference>();

                    for (int i = 0; i < realType.GenericParameters.Count; i++)
                    {
                        string p = realType.GenericParameters[i].Name;
                        TypeReference type = genericInstanceType.GenericArguments[i];

                        dict.Add(p, type);
                    }

                    if (dict.ContainsKey(invoke.ReturnType.Name))
                    {
                        invoke.ReturnType = dict[invoke.ReturnType.Name];
                    }

                    for (int i = 0; i < invoke.Parameters.Count; i++)
                    {
                        if (dict.ContainsKey(invoke.Parameters[i].ParameterType.Name))
                        {
                            invoke.Parameters[i].ParameterType = dict[invoke.Parameters[i].ParameterType.Name];
                        }
                    }
                }
            }

            if (!handler.ReturnType.InheritsFromOrImplements(invoke.ReturnType))
                throw new XamlParseException($"Signature (return type) of EventHandler \"{context.Type.FullName}.{value}\" doesn't match the event type", iXmlLineInfo);
            if (invoke.Parameters.Count != handler.Parameters.Count)
                throw new XamlParseException($"Signature (number of arguments) of EventHandler \"{context.Type.FullName}.{value}\" doesn't match the event type", iXmlLineInfo);
            if (!invoke.ContainsGenericParameter)
                for (var i = 0; i < invoke.Parameters.Count; i++)
                    if (!handler.Parameters[i].ParameterType.InheritsFromOrImplements(invoke.Parameters[i].ParameterType))
                        throw new XamlParseException($"Signature (parameter {i}) of EventHandler \"{context.Type.FullName}.{value}\" doesn't match the event type", iXmlLineInfo);

			new EXamlAddEvent(parent, context.Values[context.RootNode] as EXamlCreateObject, localName, handler);
        }

		static bool CanSetDynamicResource(FieldReference bpRef, INode valueNode, EXamlContext context)
		{
			if (bpRef == null)
				return false;
			var elementNode = valueNode as IElementNode;
			if (elementNode == null)
				return false;

			var valueInstance = context.Values[valueNode] as EXamlCreateObject;
			if (null == valueInstance)
				return false;

			return valueInstance.GetType().FullName == typeof(DynamicResource).FullName;
		}

		static void SetDynamicResource(EXamlCreateObject parent, FieldReference bpRef, IElementNode elementNode, IXmlLineInfo iXmlLineInfo, EXamlContext context)
		{
			var instance = context.Values[elementNode] as EXamlCreateObject;
			if (null != instance)
            {
				var dynamicResource = instance.Instance as DynamicResource;

				if (null != dynamicResource)
				{
					instance.IsValid = false;
					new EXamlSetDynamicResource(parent, bpRef, dynamicResource.Key);
				}
			}
		}

		static bool CanSetBinding(FieldReference bpRef, INode valueNode, EXamlContext context)
		{
			var module = context.Module;

			if (bpRef == null)
				return false;
			var elementNode = valueNode as IElementNode;
			if (elementNode == null)
				return false;

			var valueInstance = context.Values[valueNode] as EXamlCreateObject;
			if (null == valueInstance)
				return false;

            var implicitOperator = valueInstance.GetType().GetImplicitOperatorTo(module.ImportReference((XamlTask.bindingAssemblyName, XamlTask.bindingNameSpace, "BindingBase")), module);
			if (implicitOperator != null)
				return true;

			return valueInstance.GetType().InheritsFromOrImplements(XamlTask.bindingNameSpace + ".BindingBase");
		}

		static void SetBinding(EXamlCreateObject parent, FieldReference bpRef, IElementNode elementNode, IXmlLineInfo iXmlLineInfo, EXamlContext context)
		{
			new EXamlSetBinding(parent, bpRef, context.Values[elementNode]);
		}

		static bool CanSetValue(FieldReference bpRef, bool attached, INode node, IXmlLineInfo iXmlLineInfo, EXamlContext context)
		{
			var module = context.Module;

			if (bpRef == null)
				return false;

			var valueNode = node as ValueNode;
			if (valueNode != null && valueNode.CanConvertValue(context.Module, bpRef))
				return true;

			var elementNode = node as IElementNode;
			if (elementNode == null)
				return false;

			return context.Values.ContainsKey(elementNode);

			//VariableDefinition varValue;
			//if (!context.Variables.TryGetValue(elementNode, out varValue))
			//	return false;

   //         var bpTypeRef = bpRef.GetBindablePropertyType(iXmlLineInfo, module);
			//// If it's an attached BP, there's no second chance to handle IMarkupExtensions, so we try here.
			//// Worst case scenario ? InvalidCastException at runtime
			//if (attached && varValue.VariableType.FullName == "System.Object") 
			//	return true;
			//var implicitOperator = varValue.VariableType.GetImplicitOperatorTo(bpTypeRef, module);
			//if (implicitOperator != null)
			//	return true;

			////as we're in the SetValue Scenario, we can accept value types, they'll be boxed
			//if (varValue.VariableType.IsValueType && bpTypeRef.FullName == "System.Object")
			//	return true;

			//return varValue.VariableType.InheritsFromOrImplements(bpTypeRef);
		}

		static bool CanGetValue(EXamlCreateObject parent, FieldReference bpRef, bool attached, IXmlLineInfo iXmlLineInfo, EXamlContext context, out TypeReference propertyType)
		{
			var module = context.Module;
			propertyType = null;

			if (bpRef == null)
				return false;

			if (!parent.GetType().InheritsFromOrImplements(module.ImportReference((XamlTask.bindingAssemblyName, XamlTask.bindingNameSpace, "BindableObject"))))
				return false;

			propertyType = bpRef.GetBindablePropertyType(iXmlLineInfo, module);
			return true;
		}

		static void SetValue(EXamlCreateObject parent, FieldReference bpRef, INode node, IXmlLineInfo iXmlLineInfo, EXamlContext context)
		{
			//Fang: need to deal set value to field;
			var value = context.Values[node];
			new EXamlSetBindalbeProperty(parent, bpRef, value);
		}

		static void GetValue(EXamlCreateObject parent, FieldReference bpRef, IXmlLineInfo iXmlLineInfo, EXamlContext context, out TypeReference propertyType)
		{
			var module = context.Module;
			propertyType = bpRef.GetBindablePropertyType(iXmlLineInfo, module);

			//return new[] {
			//	Create(Ldloc, parent),
			//	Create(Ldsfld, bpRef),
			//	Create(Callvirt,  module.ImportMethodReference((XamlCTask.bindingAssemblyName, XamlCTask.bindingNameSpace, "BindableObject"),
			//												   methodName: "GetValue",
			//												   parameterTypes: new[] { (XamlCTask.bindingAssemblyName, XamlCTask.bindingNameSpace, "BindableProperty") })),
			//};
		}

		static bool CanSet(EXamlCreateObject parent, string localName, INode node, EXamlContext context)
		{
			var module = context.Module;
			TypeReference declaringTypeReference;
			var property = parent.GetType().GetProperty(pd => pd.Name == localName, out declaringTypeReference);
			if (property == null)
				return false;
			var propertyType = property.ResolveGenericPropertyType(declaringTypeReference, module);
			var propertySetter = property.SetMethod;
			if (propertySetter == null || !propertySetter.IsPublic || propertySetter.IsStatic)
				return false;

			var valueNode = node as ValueNode;
			if (valueNode != null && valueNode.CanConvertValue(context.Module, propertyType, new ICustomAttributeProvider[] { property, propertyType.ResolveCached()}))
				return true;

			var elementNode = node as IElementNode;
			if (elementNode == null)
				return false;

			var vardef = context.Variables[elementNode];
			var implicitOperator = vardef.VariableType.GetImplicitOperatorTo(propertyType, module);

			if (vardef.VariableType.InheritsFromOrImplements(propertyType))
				return true;
			if (implicitOperator != null)
				return true;
			if (propertyType.FullName == "System.Object")
				return true;

			//I'd like to get rid of this condition. This comment used to be //TODO replace latest check by a runtime type check
			if (vardef.VariableType.FullName == "System.Object")
				return true;

			var realValue = context.Values[elementNode] as EXamlCreateObject;
			if (null != realValue)
            {
				if (realValue.Type.InheritsFromOrImplements(propertyType))
                {
					return true;
                }
            }

			return false;
		}

		static bool CanGet(EXamlCreateObject parent, string localName, EXamlContext context, out TypeReference propertyType)
		{
			var module = context.Module;
			propertyType = null;
			TypeReference declaringTypeReference;
			var property = parent.GetType().GetProperty(pd => pd.Name == localName, out declaringTypeReference);
			if (property == null)
				return false;
			var propertyGetter = property.GetMethod;
			if (propertyGetter == null || !propertyGetter.IsPublic || propertyGetter.IsStatic)
				return false;

			module.ImportReference(parent.GetType().ResolveCached());
			var propertyGetterRef = module.ImportReference(module.ImportReference(propertyGetter).ResolveGenericParameters(declaringTypeReference, module));
			propertyGetterRef.ImportTypes(module);
			propertyType = propertyGetterRef.ReturnType.ResolveGenericParameters(declaringTypeReference);

			return true;
		}

		static void Set(EXamlCreateObject parent, string localName, INode node, IXmlLineInfo iXmlLineInfo, EXamlContext context)
		{
            var module = context.Module;
            TypeReference declaringTypeReference;
            var property = parent.Type.GetProperty(pd => pd.Name == localName, out declaringTypeReference);
            var propertySetter = property.SetMethod;

            ////			IL_0007:  ldloc.0
            ////			IL_0008:  ldstr "foo"
            ////			IL_000d:  callvirt instance void class [Tizen.NUI.Xaml.Core]Tizen.NUI.Xaml.Label::set_Text(string)

            var propertySetterRef = module.ImportReference(module.ImportReference(propertySetter).ResolveGenericParameters(declaringTypeReference, module));
            propertySetterRef.ImportTypes(module);
            var propertyType = property.ResolveGenericPropertyType(declaringTypeReference, module);
            var valueNode = node as ValueNode;
            var elementNode = node as IElementNode;

			//if it's a value type, load the address so we can invoke methods on it
			if (parent.Type.IsValueType)
			{
			//	yield return Instruction.Create(OpCodes.Ldloca, parent);
			}
			else
			{
			//	yield return Instruction.Create(OpCodes.Ldloc, parent);
			}

            if (valueNode != null)
            {
				var converterType = valueNode.GetConverterType(new ICustomAttributeProvider[] { property, propertyType.ResolveCached() });
				//foreach (var instruction in valueNode.PushConvertedValue(context, propertyType, new ICustomAttributeProvider[] { property, propertyType.ResolveCached() }, valueNode.PushServiceProvider(context, propertyRef: property), false, true))
				//    yield return instruction;
				//if (parent.Type.IsValueType)
				//    yield return Instruction.Create(OpCodes.Call, propertySetterRef);
				//else
				//    yield return Instruction.Create(OpCodes.Callvirt, propertySetterRef);
				if (null != converterType)
				{
					var converterValue = new EXamlValueConverterFromString(converterType.Resolve(), valueNode.Value as string);
					context.Values[node] = new EXamlCreateObject(converterValue, propertyType);
				}
                else
                {
					context.Values[node] = valueNode.GetBaseValue(property.PropertyType);
				}
			}
            else if (elementNode != null)
            {
                var vardef = context.Variables[elementNode];
                var implicitOperator = vardef.VariableType.GetImplicitOperatorTo(propertyType, module);
				//yield return Instruction.Create(OpCodes.Ldloc, vardef);
				if (!vardef.VariableType.InheritsFromOrImplements(propertyType) && implicitOperator != null)
				{
					//					IL_000f:  call !0 class [Tizen.NUI.Xaml.Core]Tizen.NUI.Xaml.OnPlatform`1<bool>::op_Implicit(class [Tizen.NUI.Xaml.Core]Tizen.NUI.Xaml.OnPlatform`1<!0>)
					//yield return Instruction.Create(OpCodes.Call, module.ImportReference(implicitOperator));
				}
				else if (!vardef.VariableType.IsValueType && propertyType.IsValueType)
				{
					//yield return Instruction.Create(OpCodes.Unbox_Any, module.ImportReference(propertyType));
				}
				else if (vardef.VariableType.IsValueType && propertyType.FullName == "System.Object")
				{
					//yield return Instruction.Create(OpCodes.Box, vardef.VariableType);
				}
				if (parent.Type.IsValueType)
				{
					//yield return Instruction.Create(OpCodes.Call, propertySetterRef);
				}
				else
				{
					//yield return Instruction.Create(OpCodes.Callvirt, propertySetterRef);
				}
            }

			new EXamlSetProperty(parent, localName, context.Values[node]);
		}

		static void Get(EXamlCreateObject parent, string localName, IXmlLineInfo iXmlLineInfo, EXamlContext context, out TypeReference propertyType)
		{
			var module = context.Module;
			var property = parent.GetType().GetProperty(pd => pd.Name == localName, out var declaringTypeReference);
			var propertyGetter = property.GetMethod;

			module.ImportReference(parent.GetType().ResolveCached());
			var propertyGetterRef = module.ImportReference(module.ImportReference(propertyGetter).ResolveGenericParameters(declaringTypeReference, module));
			propertyGetterRef.ImportTypes(module);
			propertyType = propertyGetterRef.ReturnType.ResolveGenericParameters(declaringTypeReference);

			//if (parent.VariableType.IsValueType)
			//	return new[] {
			//		Instruction.Create(OpCodes.Ldloca, parent),
			//		Instruction.Create(OpCodes.Call, propertyGetterRef),
			//	};
			//else
			//	return new[] {
			//		Instruction.Create(OpCodes.Ldloc, parent),
			//		Instruction.Create(OpCodes.Callvirt, propertyGetterRef),
			//	};
		}

		static bool CanAdd(EXamlCreateObject parent, XmlName propertyName, INode node, IXmlLineInfo lineInfo, EXamlContext context)
		{
			var module = context.Module;
			var localName = propertyName.LocalName;
			bool attached;
			var bpRef = GetBindablePropertyReference(parent, propertyName.NamespaceURI, ref localName, out attached, context, lineInfo);
			TypeReference propertyType;

			if (   !CanGetValue(parent, bpRef, attached, null, context, out propertyType)
				&& !CanGet(parent, localName, context, out propertyType))
				return false;

			//TODO check md.Parameters[0] type
			var adderTuple = propertyType.GetMethods(md => md.Name == "Add" && md.Parameters.Count == 1, module).FirstOrDefault();
			if (adderTuple == null)
				return false;

			return true;
		}

		static Dictionary<EXamlCreateObject, IList<string>> resourceNamesInUse = new Dictionary<EXamlCreateObject, IList<string>>();
		static bool CanAddToResourceDictionary(EXamlCreateObject parent, TypeReference collectionType, IElementNode node, IXmlLineInfo lineInfo, EXamlContext context)
		{
			if (   collectionType.FullName != "Tizen.NUI.Binding.ResourceDictionary"
				&& collectionType.ResolveCached().BaseType?.FullName != "Tizen.NUI.Binding.ResourceDictionary")
				return false;


			if (node.Properties.ContainsKey(XmlName.xKey)) {
				var key = (node.Properties[XmlName.xKey] as ValueNode).Value as string;
				if (!resourceNamesInUse.TryGetValue(parent, out var names))
					resourceNamesInUse[parent] = (names = new List<string>());
				if (names.Contains(key))
					throw new XamlParseException($"A resource with the key '{key}' is already present in the ResourceDictionary.", lineInfo);
				names.Add(key);
				return true;
			}

			//is there a RD.Add() overrides that accepts this ?
			var nodeTypeRef = context.Variables[node].VariableType;
			var module = context.Module;
			if (module.ImportMethodReference((XamlTask.bindingAssemblyName, XamlTask.bindingNameSpace, "ResourceDictionary"),
											 methodName: "Add",
											 parameterTypes: new[] { (nodeTypeRef.Scope.Name, nodeTypeRef.Namespace, nodeTypeRef.Name) }) != null)
				return true;

			throw new XamlParseException("resources in ResourceDictionary require a x:Key attribute", lineInfo);
		}

		static void Add(EXamlCreateObject parent, XmlName propertyName, INode node, IXmlLineInfo iXmlLineInfo, EXamlContext context)
		{
			//Fang: Need to deal
			//var module = context.Module;
			//var elementNode = node as IElementNode;
			//var vardef = context.Variables [elementNode];

			//TypeReference propertyType;
			//foreach (var instruction in GetPropertyValue(parent, propertyName, context, iXmlLineInfo, out propertyType))
			//	yield return instruction;

			//if (CanAddToResourceDictionary(parent, propertyType, elementNode, iXmlLineInfo, context)) {
			//	foreach (var instruction in AddToResourceDictionary(elementNode, iXmlLineInfo, context))
			//		yield return instruction;
			//	yield break;
			//}

			//var adderTuple = propertyType.GetMethods(md => md.Name == "Add" && md.Parameters.Count == 1, module).FirstOrDefault();
			//var adderRef = module.ImportReference(adderTuple.Item1);
			//adderRef = module.ImportReference(adderRef.ResolveGenericParameters(adderTuple.Item2, module));
			//var childType = GetParameterType(adderRef.Parameters[0]);
			//var implicitOperator = vardef.VariableType.GetImplicitOperatorTo(childType, module);

			//yield return Instruction.Create(OpCodes.Ldloc, vardef);
			//if (implicitOperator != null)
			//	yield return Instruction.Create(OpCodes.Call, module.ImportReference(implicitOperator));
			//if (implicitOperator == null && vardef.VariableType.IsValueType && !childType.IsValueType)
			//	yield return Instruction.Create(OpCodes.Box, vardef.VariableType);
			//yield return Instruction.Create(OpCodes.Callvirt, adderRef);
			//if (adderRef.ReturnType.FullName != "System.Void")
			//	yield return Instruction.Create(OpCodes.Pop);
		}

		static IEnumerable<Instruction> AddToResourceDictionary(IElementNode node, IXmlLineInfo lineInfo, EXamlContext context)
		{
			var module = context.Module;

			if (node.Properties.ContainsKey(XmlName.xKey)) {
//				IL_0014:  ldstr "key"
//				IL_0019:  ldstr "foo"
//				IL_001e:  callvirt instance void class [Tizen.NUI.Xaml.Core]Tizen.NUI.Xaml.ResourceDictionary::Add(string, object)
				yield return Create(Ldstr, (node.Properties[XmlName.xKey] as ValueNode).Value as string);
				var varDef = context.Variables[node];
				yield return Create(Ldloc, varDef);
				if (varDef.VariableType.IsValueType)
					yield return Create(Box, module.ImportReference(varDef.VariableType));
				yield return Create(Callvirt, module.ImportMethodReference((XamlTask.bindingAssemblyName, XamlTask.bindingNameSpace, "ResourceDictionary"),
																		   methodName: "Add",
																		   parameterTypes: new[] {
																			   ("mscorlib", "System", "String"),
																			   ("mscorlib", "System", "Object"),
																		   }));
				yield break;
			}

			var nodeTypeRef = context.Variables[node].VariableType;
			yield return Create(Ldloc, context.Variables[node]);
			yield return Create(Callvirt, module.ImportMethodReference((XamlTask.bindingAssemblyName, XamlTask.bindingNameSpace, "ResourceDictionary"),
																	   methodName: "Add",
																	   parameterTypes: new[] { (nodeTypeRef.Scope.Name, nodeTypeRef.Namespace, nodeTypeRef.Name) }));
			yield break;
		}

		public static TypeReference GetParameterType(ParameterDefinition param)
		{
			if (!param.ParameterType.IsGenericParameter)
				return param.ParameterType;
			var type = (param.Method as MethodReference).DeclaringType as GenericInstanceType;
			return type.GenericArguments [0];
		}

		static bool GetNameAndTypeRef(ref TypeReference elementType, string namespaceURI, ref string localname,
			EXamlContext context, IXmlLineInfo lineInfo)
		{
			var dotIdx = localname.IndexOf('.');
			if (dotIdx > 0)
			{
				var typename = localname.Substring(0, dotIdx);
				localname = localname.Substring(dotIdx + 1);
				elementType = new XmlType(namespaceURI, typename, null).GetTypeReference(context.Module, lineInfo);
				return true;
			}
			return false;
		}

		static void SetDataTemplate(IElementNode parentNode, ElementNode node, EXamlContext parentContext,
			IXmlLineInfo xmlLineInfo)
		{
			//Fang
			//var parentVar = parentContext.Variables[parentNode];
			////Push the DataTemplate to the stack, for setting the template
			//parentContext.IL.Emit(OpCodes.Ldloc, parentVar);

			////Create nested class
			////			.class nested private auto ansi sealed beforefieldinit '<Main>c__AnonStorey0'
			////			extends [mscorlib]System.Object


			//var module = parentContext.Module;
			//var anonType = new TypeDefinition(
			//	null,
			//	"<" + parentContext.Body.Method.Name + ">_anonXamlCDataTemplate_" + dtcount++,
			//	TypeAttributes.BeforeFieldInit |
			//	TypeAttributes.Sealed |
			//	TypeAttributes.NestedPrivate) {
			//	BaseType = module.TypeSystem.Object,
			//	CustomAttributes = {
			//		new CustomAttribute (module.ImportCtorReference(("mscorlib", "System.Runtime.CompilerServices", "CompilerGeneratedAttribute"), parameterTypes: null)),
			//	}
			//};

			//parentContext.Body.Method.DeclaringType.NestedTypes.Add(anonType);
			//var ctor = anonType.AddDefaultConstructor();

			//var loadTemplate = new MethodDefinition("LoadDataTemplate",
			//	MethodAttributes.Assembly | MethodAttributes.HideBySig,
			//	module.TypeSystem.Object);
			//loadTemplate.Body.InitLocals = true;
			//anonType.Methods.Add(loadTemplate);

			//var parentValues = new FieldDefinition("parentValues", FieldAttributes.Assembly, module.ImportArrayReference(("mscorlib", "System", "Object")));
			//anonType.Fields.Add(parentValues);

			//TypeReference rootType = null;
			//var vdefRoot = parentContext.Root as VariableDefinition;
			//if (vdefRoot != null)
			//	rootType = vdefRoot.VariableType;
			//var fdefRoot = parentContext.Root as FieldDefinition;
			//if (fdefRoot != null)
			//	rootType = fdefRoot.FieldType;

			//var root = new FieldDefinition("root", FieldAttributes.Assembly, rootType);
			//anonType.Fields.Add(root);

			////Fill the loadTemplate Body
			//var templateIl = loadTemplate.Body.GetILProcessor();
			//templateIl.Emit(OpCodes.Nop);
			//var templateContext = new ILContext(templateIl, loadTemplate.Body, module, parentValues)
			//{
			//	Root = root
			//};
			//node.Accept(new CreateObjectVisitor(templateContext), null);
			//node.Accept(new SetNamescopesAndRegisterNamesVisitor(templateContext), null);
			//node.Accept(new SetFieldVisitor(templateContext), null);
			//node.Accept(new SetResourcesVisitor(templateContext), null);
			//node.Accept(new SetPropertiesVisitor(templateContext, stopOnResourceDictionary: true), null);

			//templateIl.Emit(OpCodes.Ldloc, templateContext.Variables[node]);
			//templateIl.Emit(OpCodes.Ret);

			////Instanciate nested class
			//var parentIl = parentContext.IL;
			//parentIl.Emit(OpCodes.Newobj, ctor);

			////Copy required local vars
			//parentIl.Emit(OpCodes.Dup); //Duplicate the nestedclass instance
			//parentIl.Append(node.PushParentObjectsArray(parentContext));
			//parentIl.Emit(OpCodes.Stfld, parentValues);
			//parentIl.Emit(OpCodes.Dup); //Duplicate the nestedclass instance
			//if (parentContext.Root is VariableDefinition)
			//	parentIl.Emit(OpCodes.Ldloc, parentContext.Root as VariableDefinition);
			//else if (parentContext.Root is FieldDefinition)
			//{
			//	parentIl.Emit(OpCodes.Ldarg_0);
			//	parentIl.Emit(OpCodes.Ldfld, parentContext.Root as FieldDefinition);
			//}
			//else
			//	throw new InvalidProgramException();
			//parentIl.Emit(OpCodes.Stfld, root);

			////SetDataTemplate
			//parentIl.Emit(Ldftn, loadTemplate);
			//parentIl.Emit(Newobj, module.ImportCtorReference(("mscorlib", "System", "Func`1"),
			//												 classArguments: new[] { ("mscorlib", "System", "Object") },
			//												 paramCount: 2));

			//parentContext.IL.Emit(OpCodes.Callvirt, module.ImportPropertySetterReference((XamlTask.bindingAssemblyName, XamlTask.bindingInternalNameSpace, "IDataTemplate"), propertyName: "LoadTemplate"));

			//loadTemplate.Body.Optimize();
		}

		bool TrySetRuntimeName(XmlName propertyName, EXamlCreateObject variableDefinition, ValueNode node)
		{
			if (null == variableDefinition)
            {
				return false;
            }

			if (propertyName != XmlName.xName)
				return false;

			var attributes = variableDefinition.GetType().ResolveCached()
				.CustomAttributes.Where(attribute => attribute.AttributeType.FullName == "Tizen.NUI.Xaml.RuntimeNamePropertyAttribute").ToList();

			if (!attributes.Any())
				return false;

			var runTimeName = attributes[0].ConstructorArguments[0].Value as string;

			if (string.IsNullOrEmpty(runTimeName)) 
				return false;

			SetPropertyValue(variableDefinition, new XmlName("", runTimeName), node, Context, node);
			return true;
		}
	}
}