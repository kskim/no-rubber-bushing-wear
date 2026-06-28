namespace NoRubberBushingWear;

internal static class RuntimePatches
{
    public static bool SkipRubberBushingDamage(object? __instance, object?[]? __args)
    {
        return !RubberBushingMatcher.ContainsRubberBushing(__instance, __args);
    }

    public static void ClearRubberBushingFault(object? __instance, object?[]? __args, ref bool __result)
    {
        if (__result && RubberBushingMatcher.ContainsRubberBushing(__instance, __args))
        {
            __result = false;
        }
    }

    public static void ForceFloatCondition(object? __instance, object?[]? __args, ref float __result)
    {
        if (__result < 100f && RubberBushingMatcher.ContainsRubberBushing(__instance, __args))
        {
            __result = 100f;
        }
    }

    public static void ForceDoubleCondition(object? __instance, object?[]? __args, ref double __result)
    {
        if (__result < 100d && RubberBushingMatcher.ContainsRubberBushing(__instance, __args))
        {
            __result = 100d;
        }
    }

    public static void ForceIntCondition(object? __instance, object?[]? __args, ref int __result)
    {
        if (__result < 100 && RubberBushingMatcher.ContainsRubberBushing(__instance, __args))
        {
            __result = 100;
        }
    }
}
