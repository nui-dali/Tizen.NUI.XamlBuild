using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Tizen.NUI.Binding;
using Tizen.NUI.Xaml;
using Tizen.NUI.Xaml.Build.Tasks;

namespace Tizen.NUI.Xaml.Core.XamlC
{
    internal class SizeTypeConverter : ICompiledTypeConverter
    {
        IEnumerable<Instruction> GenerateIL(ModuleDefinition module, params float[] args)
        {
            foreach (var d in args)
                yield return Instruction.Create(OpCodes.Ldc_R4, d);

            yield return Instruction.Create(OpCodes.Newobj, module.ImportCtorReference((XamlTask.nuiAssemblyName, XamlTask.nuiNameSpace, "Size"),
                parameterTypes: args.Select(a => ("mscorlib", "System", "Single")).ToArray()));
        }

        public IEnumerable<Instruction> ConvertFromString(string value, ILContext context, BaseNode node)
        {
            var module = context.Body.Method.Module;

            if (!string.IsNullOrEmpty(value))
            {
                var thickness = value.Split(',');

                if (3 == thickness.Length)
                {
                    float x, y, z;

                    if (float.TryParse(thickness[0], NumberStyles.Number, CultureInfo.InvariantCulture, out x) &&
                                float.TryParse(thickness[1], NumberStyles.Number, CultureInfo.InvariantCulture, out y) &&
                                float.TryParse(thickness[2], NumberStyles.Number, CultureInfo.InvariantCulture, out z))
                        return GenerateIL(module, x, y, z);
                }
                else if (2 == thickness.Length)
                {
                    float x, y;

                    if (float.TryParse(thickness[0], NumberStyles.Number, CultureInfo.InvariantCulture, out x) &&
                                float.TryParse(thickness[1], NumberStyles.Number, CultureInfo.InvariantCulture, out y))
                        return GenerateIL(module, x, y, 0);
                }
            }

            throw new XamlParseException($"Cannot convert \"{value}\" into Size", node);
        }
    }

    internal class Size2DTypeConverter : ICompiledTypeConverter
    {
        IEnumerable<Instruction> GenerateIL(ModuleDefinition module, params int[] args)
        {
            foreach (var d in args)
                yield return Instruction.Create(OpCodes.Ldc_I4, d);

            yield return Instruction.Create(OpCodes.Newobj, module.ImportCtorReference((XamlTask.nuiAssemblyName, XamlTask.nuiNameSpace, "Size2D"),
                parameterTypes: args.Select(a => ("mscorlib", "System", "Int32")).ToArray()));
        }

        public IEnumerable<Instruction> ConvertFromString(string value, ILContext context, BaseNode node)
        {
            var module = context.Body.Method.Module;

            if (!string.IsNullOrEmpty(value))
            {
                int x, y;
                var thickness = value.Split(',');
                if (int.TryParse(thickness[0], NumberStyles.Number, CultureInfo.InvariantCulture, out x) &&
                    int.TryParse(thickness[1], NumberStyles.Number, CultureInfo.InvariantCulture, out y))
                    return GenerateIL(module, x, y);
            }

            throw new XamlParseException($"Cannot convert \"{value}\" into Size2D", node);
        }
    }
}
