using System.Reflection;
using System;

namespace HaldorExpansion.Compatibility
{
    internal static class ReflectionAccess
    {
        internal static void SetFieldIfExists(object obj, string fieldName, object value)
        {
            FieldInfo f = obj.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null)
            {
                f.SetValue(obj, value);
            }
        }

        internal static void CopyFieldIfExists(object src, object dst, string fieldName)
        {
            FieldInfo fs = src.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo fd = dst.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (fs != null && fd != null)
            {
                object v = fs.GetValue(src);
                fd.SetValue(dst, v);
            }
        }
        
        private static object GetFieldValueObject(object instance, string fieldName)
        {
            if (instance == null || string.IsNullOrEmpty(fieldName))
                return null;

            Type type = instance.GetType();

            while (type != null)
            {
                FieldInfo field = type.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (field != null)
                    return field.GetValue(instance);

                type = type.BaseType;
            }

            return null;
        }

        private static T GetFieldValue<T>(object instance, string fieldName)
        {
            object value = GetFieldValueObject(instance, fieldName);

            if (value == null)
                return default(T);

            if (value is T)
                return (T)value;

            return default(T);
        }
    }
}
