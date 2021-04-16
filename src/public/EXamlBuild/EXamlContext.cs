using System;
using System.Collections.Generic;

using Mono.Cecil;
using Mono.Cecil.Cil;

using Tizen.NUI.Xaml;

namespace Tizen.NUI.EXaml.Build.Tasks
{
	class EXamlContext
	{
		public EXamlContext(TypeDefinition type, FieldDefinition parentContextValues = null)
		{
			Values = new Dictionary<INode, object>();
			Variables = new Dictionary<IElementNode, VariableDefinition>();
			Scopes = new Dictionary<INode, Tuple<VariableDefinition, IList<string>>>();
			TypeExtensions = new Dictionary<INode, TypeReference>();
			ParentContextValues = parentContextValues;
			Type = type;
			Module = type.Module;
		}

		public Dictionary<INode, object> Values { get; private set; }

		public Dictionary<IElementNode, VariableDefinition> Variables { get; private set; }

		public Dictionary<INode, Tuple<VariableDefinition, IList<string>>> Scopes { get; private set; }

		public Dictionary<INode, TypeReference> TypeExtensions { get; }

		public FieldDefinition ParentContextValues { get; private set; }

		public object Root { get; set; } //FieldDefinition or VariableDefinition

		public INode RootNode { get; set; }

		public TypeDefinition Type;

		public ModuleDefinition Module { get; private set; }
	}
}