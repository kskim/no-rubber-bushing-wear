using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace NoRubberBushingWear;

internal static class RuntimePatchInstaller
{
    private static readonly Type[] NumericConditionTypes =
    {
        typeof(float),
        typeof(double),
        typeof(int)
    };

    public static int Install(Harmony harmony)
    {
        int count = 0;

        foreach (MethodBase method in FindCandidateMethods())
        {
            try
            {
                HarmonyMethod? prefix = CreatePrefix(method);
                HarmonyMethod? postfix = CreatePostfix(method);

                if (prefix == null && postfix == null)
                {
                    continue;
                }

                harmony.Patch(method, prefix, postfix);
                count++;
                Plugin.ModLog.LogInfo($"Patched {Describe(method)}.");
            }
            catch (Exception ex)
            {
                Plugin.ModLog.LogWarning($"Failed to patch {Describe(method)}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        return count;
    }

    private static IEnumerable<MethodBase> FindCandidateMethods()
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!IsGameAssembly(assembly))
            {
                continue;
            }

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(type => type != null).Cast<Type>().ToArray();
            }

            foreach (Type type in types)
            {
                foreach (MethodBase method in GetDeclaredMethods(type))
                {
                    if (method.IsAbstract || method.ContainsGenericParameters)
                    {
                        continue;
                    }

                    if (LooksLikeConditionRead(method) || LooksLikeFaultRead(method) || LooksLikeDamageWrite(method))
                    {
                        yield return method;
                    }
                }
            }
        }
    }

    private static IEnumerable<MethodBase> GetDeclaredMethods(Type type)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        foreach (MethodInfo method in type.GetMethods(flags))
        {
            yield return method;
        }

        foreach (ConstructorInfo constructor in type.GetConstructors(flags))
        {
            yield return constructor;
        }
    }

    private static bool IsGameAssembly(Assembly assembly)
    {
        string name = assembly.GetName().Name ?? string.Empty;
        return name.Equals("Assembly-CSharp", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("Assembly-CSharp-", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeConditionRead(MethodBase method)
    {
        if (method is not MethodInfo info || !NumericConditionTypes.Contains(info.ReturnType))
        {
            return false;
        }

        string name = NormalizeName(method.Name);
        return name.Contains("condition")
            || name.Contains("quality")
            || name.Contains("durability")
            || name.Contains("wear");
    }

    private static bool LooksLikeFaultRead(MethodBase method)
    {
        if (method is not MethodInfo info || info.ReturnType != typeof(bool))
        {
            return false;
        }

        string name = NormalizeName(method.Name);
        return name.Contains("fault")
            || name.Contains("broken")
            || name.Contains("damage")
            || name.Contains("diagnostic")
            || name.Contains("repair")
            || name.Contains("complete");
    }

    private static bool LooksLikeDamageWrite(MethodBase method)
    {
        if (method is MethodInfo info && info.ReturnType != typeof(void))
        {
            return false;
        }

        string name = NormalizeName(method.Name);
        return name.Contains("damage")
            || name.Contains("wear")
            || name.Contains("condition")
            || name.Contains("durability");
    }

    private static HarmonyMethod? CreatePrefix(MethodBase method)
    {
        if (!LooksLikeDamageWrite(method))
        {
            return null;
        }

        return new HarmonyMethod(typeof(RuntimePatches), nameof(RuntimePatches.SkipRubberBushingDamage));
    }

    private static HarmonyMethod? CreatePostfix(MethodBase method)
    {
            if (LooksLikeFaultRead(method))
            {
                return new HarmonyMethod(typeof(RuntimePatches), nameof(RuntimePatches.ClearRubberBushingFault));
            }

        if (method is MethodInfo info && LooksLikeConditionRead(method))
        {
            if (info.ReturnType == typeof(float))
            {
                return new HarmonyMethod(typeof(RuntimePatches), nameof(RuntimePatches.ForceFloatCondition));
            }

            if (info.ReturnType == typeof(double))
            {
                return new HarmonyMethod(typeof(RuntimePatches), nameof(RuntimePatches.ForceDoubleCondition));
            }

            if (info.ReturnType == typeof(int))
            {
                return new HarmonyMethod(typeof(RuntimePatches), nameof(RuntimePatches.ForceIntCondition));
            }
        }

        return null;
    }

    private static string NormalizeName(string value)
    {
        return value.Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
    }

    private static string Describe(MethodBase method)
    {
        return $"{method.DeclaringType?.FullName ?? "<unknown>"}.{method.Name}";
    }
}
