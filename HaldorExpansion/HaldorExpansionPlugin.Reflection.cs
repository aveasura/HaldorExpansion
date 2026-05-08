using System;
using System.Reflection;
using UnityEngine;

namespace HaldorExpansion
{
    public partial class HaldorExpansionPlugin
    {
        private static void SetFieldIfExists(object obj, string fieldName, object value)
        {
            FieldInfo f = obj.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null)
            {
                f.SetValue(obj, value);
            }
        }

        private static void CopyFieldIfExists(object src, object dst, string fieldName)
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

        internal static float GetCharacterMaxHealth(Character character)
        {
            return InvokeFloatMethod(character, "GetMaxHealth");
        }

        internal static float GetCharacterHealth(Character character)
        {
            return InvokeFloatMethod(character, "GetHealth");
        }

        internal static void SetCharacterHealth(Character character, float health)
        {
            if (character == null)
                return;

            health = Mathf.Max(0f, health);

            Type type = character.GetType();
            while (type != null)
            {
                MethodInfo setHealthMethod = type.GetMethod(
                    "SetHealth",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(float) },
                    null);

                if (setHealthMethod != null)
                {
                    setHealthMethod.Invoke(character, new object[] { health });
                    return;
                }

                type = type.BaseType;
            }

            FieldInfo healthField = character.GetType().GetField("m_health",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (healthField != null)
            {
                healthField.SetValue(character, health);
            }
        }

        private static float InvokeFloatMethod(object target, string methodName)
        {
            if (target == null)
                return 0f;

            Type type = target.GetType();
            while (type != null)
            {
                MethodInfo method = type.GetMethod(methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (method != null && method.ReturnType == typeof(float))
                {
                    object result = method.Invoke(target, null);
                    if (result is float value)
                        return value;
                }

                type = type.BaseType;
            }

            return 0f;
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