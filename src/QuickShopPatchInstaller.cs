using System;
using System.Reflection;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace NoRubberBushingWear;

internal static class QuickShopPatchInstaller
{
    private const string PartScriptTypeName = "PartScript";
    private const string GameInventoryTypeName = "GameInventory";
    private const string GameManagerTypeName = "GameManager";
    private const string GlobalDataTypeName = "GlobalData";
    private const string InventoryTypeName = "Inventory";
    private const string ItemTypeName = "Item";

    private static string? hoveredPartId;
    private static bool loggedQuickBuyFailure;

    public static int Install(Harmony harmony)
    {
        int count = 0;

        Type? partScriptType = AccessTools.TypeByName(PartScriptTypeName);

        count += PatchIfFound(
            harmony,
            AccessTools.Method(partScriptType, "SetMouseOver", new[] { typeof(bool) }),
            postfix: nameof(TrackHoveredPart));

        count += PatchIfFound(
            harmony,
            AccessTools.Method(partScriptType, "ActionMount", new[] { typeof(bool) }),
            prefix: nameof(AutoBuyMissingPartBeforeMount));

        Type? inventoryType = AccessTools.TypeByName(InventoryTypeName);

        count += PatchIfFound(
            harmony,
            AccessTools.Method(inventoryType, "GetItems", new[] { typeof(string) }),
            postfix: nameof(AutoBuyIntoEmptyItemsList));

        if (count > 0)
        {
            Plugin.ModLog.LogInfo("QuickShop enabled. Missing mount parts are bought automatically before install.");
        }
        else
        {
            Plugin.ModLog.LogWarning("QuickShop mount hook point was not found.");
        }

        return count;
    }

    public static void TrackHoveredPart(object __instance, bool b)
    {
        if (!b)
        {
            string? exitingPartId = ResolvePurchasablePartId(__instance);
            if (!string.IsNullOrWhiteSpace(exitingPartId) && string.Equals(hoveredPartId, exitingPartId, StringComparison.Ordinal))
            {
                hoveredPartId = null;
            }

            return;
        }

        hoveredPartId = ResolvePurchasablePartId(__instance);
    }

    public static void AutoBuyMissingPartBeforeMount(object __instance)
    {
        try
        {
            object? inventory = GetInventory();
            if (inventory == null)
            {
                LogQuickBuyFailure("QuickShop could not resolve Inventory.");
                return;
            }

            string? resolvedPartId = ResolvePurchasablePartId(__instance);
            if (string.IsNullOrWhiteSpace(resolvedPartId))
            {
                return;
            }

            string partId = resolvedPartId!;
            if (InventoryHasPart(inventory, partId))
            {
                return;
            }

            object? partProperty = GetPartProperty(partId);
            int price = GetIntProperty(partProperty, "Price");
            if (price < 0 || GetPlayerMoney() < price)
            {
                return;
            }

            object? item = CreateItem(partId);
            if (item == null)
            {
                LogQuickBuyFailure("QuickShop could not create Item.");
                return;
            }

            AccessTools.Method(inventory.GetType(), "Add", new[] { item.GetType(), typeof(bool) })
                ?.Invoke(inventory, new[] { item, true });

            AddPlayerMoney(-price);
            Plugin.ModLog.LogInfo($"QuickShop bought one missing mount part {partId} for {price}.");
        }
        catch (Exception ex)
        {
            LogQuickBuyFailure($"QuickShop auto-buy before mount failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public static void AutoBuyIntoEmptyItemsList(object __instance, string _ID, object? __result)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_ID) || !string.Equals(_ID, hoveredPartId, StringComparison.Ordinal))
            {
                return;
            }

            if (__result == null || GetListCount(__result) > 0)
            {
                return;
            }

            object? item = TryBuyMissingPart(__instance, _ID);
            if (item == null)
            {
                return;
            }

            AccessTools.Method(__result.GetType(), "Add", new[] { AccessTools.TypeByName("BaseItem") })
                ?.Invoke(__result, new[] { item });
        }
        catch (Exception ex)
        {
            LogQuickBuyFailure($"QuickShop inventory-list auto-buy failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static string? ResolvePurchasablePartId(object partScript)
    {
        foreach (string methodName in new[] { "GetIDWithTuned", "GetID" })
        {
            string? rawPartId = AccessTools.Method(partScript.GetType(), methodName)
                ?.Invoke(partScript, Array.Empty<object>()) as string;

            if (string.IsNullOrWhiteSpace(rawPartId))
            {
                continue;
            }

            string partId = rawPartId!;
            if (CanBuyPart(partId) && GetPartProperty(partId) != null)
            {
                return partId;
            }
        }

        return null;
    }

    private static bool InventoryHasPart(object inventory, string partId)
    {
        object? existing = AccessTools.Method(inventory.GetType(), "GetItem", new[] { typeof(string) })
            ?.Invoke(inventory, new object[] { partId });
        return existing != null;
    }

    private static object? TryBuyMissingPart(object inventory, string partId)
    {
        if (InventoryHasPart(inventory, partId))
        {
            return null;
        }

        object? partProperty = GetPartProperty(partId);
        int price = GetIntProperty(partProperty, "Price");
        if (price < 0 || GetPlayerMoney() < price)
        {
            return null;
        }

        object? item = CreateItem(partId);
        if (item == null)
        {
            LogQuickBuyFailure("QuickShop could not create Item.");
            return null;
        }

        AccessTools.Method(inventory.GetType(), "Add", new[] { item.GetType(), typeof(bool) })
            ?.Invoke(inventory, new[] { item, true });

        AddPlayerMoney(-price);
        Plugin.ModLog.LogInfo($"QuickShop bought one missing mount part {partId} for {price}.");
        return item;
    }

    private static bool CanBuyPart(string partId)
    {
        Type? gameInventoryType = AccessTools.TypeByName(GameInventoryTypeName);
        MethodInfo? canAddToShopList = AccessTools.Method(gameInventoryType, "CanAddToShopList", new[] { typeof(string), typeof(bool) });
        return canAddToShopList == null || canAddToShopList.Invoke(null, new object[] { partId, true }) is true;
    }

    private static object? GetPartProperty(string partId)
    {
        object? gameInventory = AccessTools.PropertyGetter(AccessTools.TypeByName(GameInventoryTypeName), "Instance")
            ?.Invoke(null, Array.Empty<object>());

        return gameInventory == null
            ? null
            : AccessTools.Method(gameInventory.GetType(), "GetItemPropertyCached", new[] { typeof(string) })
                ?.Invoke(gameInventory, new object[] { partId });
    }

    private static object? GetInventory()
    {
        Type? gameManagerType = AccessTools.TypeByName(GameManagerTypeName);
        UnityEngine.Object? gameManager = gameManagerType == null ? null : UnityEngine.Object.FindObjectOfType(Il2CppType.From(gameManagerType));
        object? inventory = gameManager == null
            ? null
            : AccessTools.PropertyGetter(gameManagerType, "Inventory")?.Invoke(gameManager, Array.Empty<object>());

        if (inventory != null)
        {
            return inventory;
        }

        Type? inventoryType = AccessTools.TypeByName(InventoryTypeName);
        return inventoryType == null ? null : UnityEngine.Object.FindObjectOfType(Il2CppType.From(inventoryType));
    }

    private static object? CreateItem(string partId)
    {
        Type? itemType = AccessTools.TypeByName(ItemTypeName);
        return itemType == null ? null : Activator.CreateInstance(itemType, partId);
    }

    private static int GetPlayerMoney()
    {
        return AccessTools.PropertyGetter(AccessTools.TypeByName(GlobalDataTypeName), "PlayerMoney")
            ?.Invoke(null, Array.Empty<object>()) as int? ?? 0;
    }

    private static void AddPlayerMoney(int amount)
    {
        AccessTools.Method(AccessTools.TypeByName(GlobalDataTypeName), "AddPlayerMoney", new[] { typeof(int) })
            ?.Invoke(null, new object[] { amount });
    }

    private static int GetIntProperty(object? instance, string propertyName)
    {
        if (instance == null)
        {
            return -1;
        }

        object? value = AccessTools.PropertyGetter(instance.GetType(), propertyName)?.Invoke(instance, Array.Empty<object>());
        return value is int result ? result : -1;
    }

    private static int GetListCount(object list)
    {
        object? count = AccessTools.PropertyGetter(list.GetType(), "Count")?.Invoke(list, Array.Empty<object>());
        return count is int result ? result : 0;
    }

    private static int PatchIfFound(Harmony harmony, MethodBase? target, string? prefix = null, string? postfix = null)
    {
        if (target == null)
        {
            return 0;
        }

        try
        {
            HarmonyMethod? prefixPatch = prefix == null ? null : new HarmonyMethod(typeof(QuickShopPatchInstaller), prefix);
            HarmonyMethod? postfixPatch = postfix == null ? null : new HarmonyMethod(typeof(QuickShopPatchInstaller), postfix);
            harmony.Patch(target, prefixPatch, postfixPatch);
            Plugin.ModLog.LogInfo($"Patched {Describe(target)}.");
            return 1;
        }
        catch (Exception ex)
        {
            Plugin.ModLog.LogWarning($"Failed to patch {Describe(target)}: {ex.GetType().Name}: {ex.Message}");
            return 0;
        }
    }

    private static void LogQuickBuyFailure(string message)
    {
        if (loggedQuickBuyFailure)
        {
            return;
        }

        loggedQuickBuyFailure = true;
        Plugin.ModLog.LogWarning(message);
    }

    private static string Describe(MethodBase method)
    {
        return $"{method.DeclaringType?.FullName ?? "<unknown>"}.{method.Name}";
    }
}
