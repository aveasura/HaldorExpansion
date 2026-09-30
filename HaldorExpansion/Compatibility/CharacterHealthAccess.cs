using System.Reflection;
using System;
using UnityEngine;

namespace HaldorExpansion.Compatibility
{
    internal static class CharacterHealthAccess
    {

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
    }
}
