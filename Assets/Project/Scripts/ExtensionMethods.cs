using System;
using System.Reflection;
using System.ComponentModel;

public static class ExtensionMethods
{
    public static string GetDescription<T>(this T value) where T : struct
    {
        Type type = value.GetType();
        string name = Enum.GetName(type, value);

        if (!type.IsEnum)
        {
            throw new ArgumentException("Value must be of type Enum", "value");
        }

        MemberInfo[] memberInfo = type.GetMember(value.ToString());

        if (memberInfo != null && memberInfo.Length > 0)
        {
            object[] attributes = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0)
            {
                return ((DescriptionAttribute)attributes[0]).Description;
            }
        }

        return value.ToString();
    }
}
