using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Tizen.NUI.XamlBinding;
using Tizen.NUI.Xaml;
using Tizen.NUI.Xaml.Build.Tasks;

namespace Tizen.NUI.Xaml.Core.XamlC
{
    internal class PositionTypeConverter : ICompiledTypeConverter
    {
        IEnumerable<Instruction> GenerateIL(ModuleDefinition module, params double[] args)
        {
            foreach (var d in args)
                yield return Instruction.Create(OpCodes.Ldc_R8, d);

            yield return Instruction.Create(OpCodes.Newobj, module.ImportCtorReference((XamlCTask.nuiAssemblyName, XamlCTask.nuiNameSpace, "Position"),
                parameterTypes: args.Select(a => ("mscorlib", "System", "Single")).ToArray()));
        }

        public IEnumerable<Instruction> ConvertFromString(string value, ILContext context, BaseNode node)
        {
            var module = context.Body.Method.Module;

            if (!string.IsNullOrEmpty(value))
            {
                string[] parts = value.Split('.');
                if (parts.Length == 1 || (parts.Length == 2 && (parts[0].Trim() == "ParentOrigin" || parts[0].Trim() == "PivotPoint")))
                {
                    string position = parts[parts.Length - 1].Trim();

                    switch (position)
                    {
                        case "Top":
                            return GenerateIL(module, 0.0);
                        case "Bottom":
                            return GenerateIL(module, 1.0);
                        case "Left":
                            return GenerateIL(module, 0.0);
                        case "Right":
                            return GenerateIL(module, 1.0);
                        case "Middle":
                            return GenerateIL(module, 0.5);
                        case "TopLeft":
                            return GenerateIL(module, 0.0, 0.0, 0.5);
                        case "TopCenter":
                            return GenerateIL(module, 0.5, 0.0, 0.5);
                        case "TopRight":
                            return GenerateIL(module, 1.0, 0.0, 0.5);
                        case "CenterLeft":
                            return GenerateIL(module, 0.0, 0.5, 0.5);
                        case "Center":
                            return GenerateIL(module, 0.5, 0.5, 0.5);
                        case "CenterRight":
                            return GenerateIL(module, 1.0, 0.5, 0.5);
                        case "BottomLeft":
                            return GenerateIL(module, 0.0, 1.0, 0.5);
                        case "BottomCenter":
                            return GenerateIL(module, 0.5, 1.0, 0.5);
                        case "BottomRight":
                            return GenerateIL(module, 1.0, 1.0, 0.5);
                    }
                }
                else
                {
                    double x, y, z;
                    var thickness = value.Split(',');

                    if (double.TryParse(thickness[0], NumberStyles.Number, CultureInfo.InvariantCulture, out x) &&
                                double.TryParse(thickness[1], NumberStyles.Number, CultureInfo.InvariantCulture, out y) &&
                                double.TryParse(thickness[2], NumberStyles.Number, CultureInfo.InvariantCulture, out z))
                        return GenerateIL(module, x, y, z);
                }
            }

            throw new XamlParseException($"Cannot convert \"{value}\" into Position", node);
        }
    }

    internal class Position2DTypeConverter : ICompiledTypeConverter
    {
        IEnumerable<Instruction> GenerateIL(ModuleDefinition module, params int[] args)
        {
            foreach (var d in args)
                yield return Instruction.Create(OpCodes.Ldc_I4, d);

            yield return Instruction.Create(OpCodes.Newobj, module.ImportCtorReference((XamlCTask.nuiAssemblyName, XamlCTask.nuiNameSpace, "Position2D"),
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

            throw new XamlParseException($"Cannot convert \"{value}\" into Position2D", node);
        }
    }
}
