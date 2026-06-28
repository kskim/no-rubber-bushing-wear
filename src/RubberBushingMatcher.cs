using System;
using System.Collections.Generic;
using System.Reflection;

namespace NoRubberBushingWear;

internal static class RubberBushingMatcher
{
    private const int MaxDepth = 2;

    private static readonly HashSet<string> KnownIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "rubber bushing",
        "small rubber bushing",
        "rubber_bushing",
        "rubber-bushing",
        "bushing rubber",
        "bushing_rubber",
        "bushing-rubber",
        "tuleja 1",
        "tulejamala 1"
    };

    private static readonly string[] LikelyMemberNames =
    {
        "id",
        "name",
        "partname",
        "partid",
        "uid",
        "key",
        "displayname",
        "localizedname",
        "localizationkey"
    };

    public static bool ContainsRubberBushing(object? instance, object?[]? args)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);

        if (IsRubberBushing(instance, visited, 0))
        {
            return true;
        }

        if (args == null)
        {
            return false;
        }

        foreach (object? arg in args)
        {
            if (IsRubberBushing(arg, visited, 0))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsRubberBushing(object? value, HashSet<object> visited, int depth)
    {
        if (value == null || depth > MaxDepth)
        {
            return false;
        }

        if (value is string text)
        {
            return IsKnownId(text);
        }

        Type type = value.GetType();

        if (type.IsPrimitive || type.IsEnum)
        {
            return false;
        }

        if (!visited.Add(value))
        {
            return false;
        }

        if (IsKnownId(type.Name) || IsKnownId(type.FullName ?? string.Empty))
        {
            return true;
        }

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (FieldInfo field in type.GetFields(flags))
        {
            if (!IsLikelyIdentityMember(field.Name))
            {
                continue;
            }

            if (TryRead(field, value, out object? memberValue) && IsRubberBushing(memberValue, visited, depth + 1))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsKnownId(string value)
    {
        string normalized = Normalize(value);
        return KnownIds.Contains(normalized);
    }

    private static bool IsLikelyIdentityMember(string name)
    {
        string normalized = Normalize(name);

        foreach (string likelyName in LikelyMemberNames)
        {
            if (normalized.Contains(likelyName))
            {
                return true;
            }
        }

        return false;
    }

    private static string Normalize(string value)
    {
        return value.Trim()
            .Replace("_", " ")
            .Replace("-", " ")
            .ToLowerInvariant();
    }

    private static bool TryRead(FieldInfo field, object instance, out object? value)
    {
        try
        {
            value = field.GetValue(instance);
            return true;
        }
        catch
        {
            value = null;
            return false;
        }
    }
}
