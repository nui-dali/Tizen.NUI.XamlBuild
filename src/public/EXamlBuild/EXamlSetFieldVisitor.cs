using System.Linq;
using Mono.Cecil.Cil;
using Tizen.NUI.Xaml;
using Tizen.NUI.Xaml.Build.Tasks;

namespace Tizen.NUI.EXaml.Build.Tasks
{
	class EXamlSetFieldVisitor : IXamlNodeVisitor
	{
		public EXamlSetFieldVisitor(EXamlContext context)
		{
			Context = context;
		}

		public EXamlContext Context { get; }

		public TreeVisitingMode VisitingMode => TreeVisitingMode.TopDown;
		public bool StopOnDataTemplate => true;
		public bool StopOnResourceDictionary => false;
		public bool VisitNodeOnDataTemplate => false;
		public bool SkipChildren(INode node, INode parentNode) => false;

		public bool IsResourceDictionary(ElementNode node)
		{
			var parentVar = Context.Variables[(IElementNode)node];
			return parentVar.VariableType.FullName == "Tizen.NUI.Binding.ResourceDictionary"
                || parentVar.VariableType.Resolve().BaseType?.FullName == "Tizen.NUI.Binding.ResourceDictionary";
		}

		public void Visit(ValueNode node, INode parentNode)
		{
            //Fang: Need to deal set field
			//if (!IsXNameProperty(node, parentNode))
			//	return;
			//var field = Context.Body.Method.DeclaringType.Fields.SingleOrDefault(fd => fd.Name == (string)node.Value);
			//if (field == null)
			//	return;
			//Context.IL.Emit(OpCodes.Ldarg_0);
			//Context.IL.Emit(OpCodes.Ldloc, Context.Variables[(IElementNode)parentNode]);
			//Context.IL.Emit(OpCodes.Stfld, field);
		}

		public void Visit(MarkupNode node, INode parentNode)
		{
		}

		public void Visit(ElementNode node, INode parentNode)
		{
		}

		public void Visit(RootNode node, INode parentNode)
		{
		}

		public void Visit(ListNode node, INode parentNode)
		{
		}

		static bool IsXNameProperty(ValueNode node, INode parentNode)
		{
			var parentElement = parentNode as IElementNode;
			INode xNameNode;
			if (parentElement != null && parentElement.Properties.TryGetValue(XmlName.xName, out xNameNode) && xNameNode == node)
				return true;
			return false;
		}
	}
}