using System;
using System.Windows.Markup;

namespace JapaneseLookup.GUI
{
    // https://brianlagunas.com/a-better-way-to-data-bind-enums-in-wpf/
    public class EnumBindingSourceExtension : MarkupExtension
    {
        public readonly Type EnumType;

        public EnumBindingSourceExtension(Type enumType)
        {
            if (enumType is null || !enumType.IsEnum)
                throw new Exception("Invalid Enum");


            EnumType = enumType;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Enum.GetValues(EnumType);
        }
    }
}