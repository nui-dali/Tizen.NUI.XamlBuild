using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;

using Tizen.NUI;
using Tizen.NUI.Xaml.Build.Tasks;

namespace Tizen.NUI.Xaml.Core.XamlC
{
    internal class ExtentsTypeConverter : ICompiledTypeConverter
    {
        IEnumerable<Instruction> GenerateIL(ModuleDefinition module, params double[] args)
        {
            foreach (var d in args)
                yield return Instruction.Create(OpCodes.Ldc_R8, d);

            yield return Instruction.Create(OpCodes.Newobj, module.ImportCtorReference((XamlTask.nuiAssemblyName, XamlTask.nuiNameSpace, "Extents"),
                parameterTypes: args.Select(a => ("mscorlib", "System", "UInt16")).ToArray()));
        }

        public IEnumerable<Instruction> ConvertFromString(string value, ILContext context, BaseNode node)
        {
            var module = context.Body.Method.Module;

            if (!string.IsNullOrEmpty(value))
            {
                var thickness = value.Split(',');

                if (4 == thickness.Length)
                {
                    double start, end, top, bottom;

                    if (double.TryParse(thickness[0], NumberStyles.Number, CultureInfo.InvariantCulture, out start) &&
                        double.TryParse(thickness[1], NumberStyles.Number, CultureInfo.InvariantCulture, out end) &&
                        double.TryParse(thickness[2], NumberStyles.Number, CultureInfo.InvariantCulture, out top) &&
                        double.TryParse(thickness[2], NumberStyles.Number, CultureInfo.InvariantCulture, out bottom))

                        return GenerateIL(module, start, end, top, bottom);
                }
            }

            throw new XamlParseException($"Cannot convert \"{value}\" into Position", node);
        }
    }
}
