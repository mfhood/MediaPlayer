using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using System.Linq;

namespace MediaPlayer
{
    public static class EnumExtension
    {
        public static string GetDescription(this Enum value)
        {
            string name = value.ToString();
            FieldInfo fieldInfo = value.GetType().GetField(name);
            DescriptionAttribute descriptionAttribute = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
            if (descriptionAttribute != null)
            {
                return descriptionAttribute.Description;
            }
            return string.Empty;
        }
    }
}
