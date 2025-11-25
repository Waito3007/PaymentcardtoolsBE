using System;

namespace Paymentcardtools.Extension;

public static class Extension
{
    public static string GetDescriptionOfEnum<T>(this T enumValue) where T : Enum
    {
        var fi = enumValue.GetType().GetField(enumValue.ToString());
        var attributes = (System.ComponentModel.DescriptionAttribute[])fi.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);
        return (attributes.Length > 0) ? attributes[0].Description : enumValue.ToString();
    }
}
