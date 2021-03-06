using System;
using Tizen.NUI.Binding.Internals;
using Tizen.NUI.Binding;
using Tizen.NUI.EXaml;
using Mono.Cecil;

namespace Tizen.NUI.Xaml
{
    [ContentProperty("Path")]
    [AcceptEmptyServiceProvider]
    internal sealed class BindingExtension : IMarkupExtension<BindingBase>
    {
        public string Path { get; set; } = Tizen.NUI.Binding.Binding.SelfPath;
        public BindingMode Mode { get; set; } = BindingMode.Default;

        public EXamlCreateObject ModeInEXaml { get; set; } = null;

        public object Converter { get; set; }

        public object ConverterParameter { get; set; }

        public string StringFormat { get; set; }

        public object Source { get; set; }

        public string UpdateSourceEventName { get; set; }

        public object TargetNullValue { get; set; }

        public object FallbackValue { get; set; }

        public TypedBindingBase TypedBinding { get; set; }

        public EXamlCreateObject ProvideValue(ModuleDefinition module)
        {
            if (TypedBinding == null)
            {
                var newTypeRef = module.ImportReference(typeof(Tizen.NUI.Binding.Binding));
                return new EXamlCreateObject(null, newTypeRef, new object[] { Path, ModeInEXaml, Converter, ConverterParameter, StringFormat, Source });
            }
            else
            {
                throw new Exception("TypedBinding should not be not null");
                //TypedBinding.Mode = Mode;
                //TypedBinding.Converter = Converter;
                //TypedBinding.ConverterParameter = ConverterParameter;
                //TypedBinding.StringFormat = StringFormat;
                //TypedBinding.Source = Source;
                //TypedBinding.UpdateSourceEventName = UpdateSourceEventName;
                //TypedBinding.FallbackValue = FallbackValue;
                //TypedBinding.TargetNullValue = TargetNullValue;
                //return TypedBinding;
            }
        }

        BindingBase IMarkupExtension<BindingBase>.ProvideValue(IServiceProvider serviceProvider)
        {
            if (TypedBinding == null)
                return new Tizen.NUI.Binding.Binding(Path, Mode, Converter as IValueConverter, ConverterParameter, StringFormat, Source)
                {
                    UpdateSourceEventName = UpdateSourceEventName,
                    FallbackValue = FallbackValue,
                    TargetNullValue = TargetNullValue,
                };

            TypedBinding.Mode = Mode;
            TypedBinding.Converter = Converter as IValueConverter;
            TypedBinding.ConverterParameter = ConverterParameter;
            TypedBinding.StringFormat = StringFormat;
            TypedBinding.Source = Source;
            TypedBinding.UpdateSourceEventName = UpdateSourceEventName;
            TypedBinding.FallbackValue = FallbackValue;
            TypedBinding.TargetNullValue = TargetNullValue;
            return TypedBinding;
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        {
            return (this as IMarkupExtension<BindingBase>).ProvideValue(serviceProvider);
        }
    }
}