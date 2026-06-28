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

    private static object? hoveredPartScript;
    private static int lastHandledFrame = -1;
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
            AccessTools.Method(partScriptType, "OnDisable"),
            postfix: nameof(ClearHoveredPart));

        count += PatchIfFound(
            harmony,
            AccessTools.Method(partScriptType, "OnDestroy"),
            postfix: nameof(ClearHoveredPart));

        count += PatchIfFound(
            harmony,
            AccessTools.Method(partScriptType, "Update"),
            postfix: nameof(HandleRepairQuickShopInput));

        if (count > 0)
        {
            Plugin.ModLog.LogInfo("QuickShop enabled. Press B while hovering a repair-screen part to buy one replacement.");
        }
        else
        {
            Plugin.ModLog.LogWarning("QuickShop repair-screen hook points were not found.");
        }

        return count;
    }

    public static void TrackHoveredPart(object __instance, bool b)
    {
        if (b)
        {
            hoveredPartScript = __instance;
        }
        else if (ReferenceEquals(hoveredPartScript, __instance))
        {
            hoveredPartScript = null;
        }
    }

    public static void ClearHoveredPart(object __instance)
    {
        if (ReferenceEquals(hoveredPartScript, __instance))
        {
            hoveredPartScript = null;
        }
    }

    public static void HandleRepairQuickShopInput()
    {
        if (hoveredPartScript == null || !Input.GetKeyDown(KeyCode.B) || lastHandledFrame == Time.frameCount)
        {
            return;
        }

        lastHandledFrame = Time.frameCount;
        TryBuyHoveredPart();
    }

    private static void TryBuyHoveredPart()
    {
        try
        {
            string? rawPartId = GetHoveredPartId(hoveredPartScript!);
            if (string.IsNullOrWhiteSpace(rawPartId))
            {
                return;
            }

            string partId = rawPartId!;
            if (!CanBuyPart(partId))
            {
                return;
            }

            object? partProperty = GetPartProperty(partId);
            int price = GetIntProperty(partProperty, "Price");
            if (price < 0 || GetPlayerMoney() < price)
            {
                return;
            }

            object? inventory = GetInventory();
            object? item = CreateItem(partId);
            if (inventory == null || item == null)
            {
                LogQuickBuyFailure("QuickShop could not resolve Inventory or Item.");
                return;
            }

            AccessTools.Method(inventory.GetType(), "Add", new[] { item.GetType(), typeof(bool) })
                ?.Invoke(inventory, new[] { item, true });

            AddPlayerMoney(-price);
            Plugin.ModLog.LogInfo($"QuickShop bought one {partId} for {price}.");
        }
        catch (Exception ex)
        {
            LogQuickBuyFailure($"QuickShop repair-screen buy failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static string? GetHoveredPartId(object partScript)
    {
        MethodInfo? getIdWithTuned = AccessTools.Method(partScript.GetType(), "GetIDWithTuned");
        string? tunedId = getIdWithTuned?.Invoke(partScript, Array.Empty<object>()) as string;
        if (!string.IsNullOrWhiteSpace(tunedId))
        {
            return tunedId;
        }

        MethodInfo? getId = AccessTools.Method(partScript.GetType(), "GetID");
        return getId?.Invoke(partScript, Array.Empty<object>()) as string;
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
