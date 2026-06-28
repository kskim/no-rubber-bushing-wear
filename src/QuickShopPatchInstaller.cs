using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace NoRubberBushingWear;

internal static class QuickShopPatchInstaller
{
    private static string? hoveredPartId;
    private static int mountLookupDepth;
    private static bool isBuyingPart;
    private static bool loggedQuickBuyFailure;

    public static int Install(Harmony harmony)
    {
        int count = 0;

        count += PatchIfFound(
            harmony,
            AccessTools.Method(typeof(PartScript), nameof(PartScript.SetMouseOver), new[] { typeof(bool) }),
            postfix: nameof(TrackHoveredPart));

        count += PatchIfFound(
            harmony,
            AccessTools.Method(typeof(PartScript), nameof(PartScript.ActionMount), new[] { typeof(bool) }),
            prefix: nameof(BeginMountLookup),
            postfix: nameof(EndMountLookup));

        count += PatchIfFound(
            harmony,
            AccessTools.Method(typeof(Inventory), nameof(Inventory.GetItem), new[] { typeof(string) }),
            postfix: nameof(AutoBuyIntoMissingItemLookup));

        count += PatchIfFound(
            harmony,
            AccessTools.Method(typeof(Inventory), nameof(Inventory.GetItems), new[] { typeof(string) }),
            postfix: nameof(AutoBuyIntoEmptyItemsList));

        if (count > 0)
        {
            Plugin.ModLog.LogInfo("QuickShop enabled. Missing mount parts are bought automatically before install.");
        }
        else
        {
            Plugin.ModLog.LogWarning("QuickShop mount hook points were not found.");
        }

        return count;
    }

    public static void TrackHoveredPart(PartScript __instance, bool b)
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

    public static void BeginMountLookup(PartScript __instance)
    {
        mountLookupDepth++;

        try
        {
            Inventory? inventory = GetInventory();
            string? resolvedPartId = ResolvePurchasablePartId(__instance);
            if (inventory == null || string.IsNullOrWhiteSpace(resolvedPartId))
            {
                return;
            }

            string partId = resolvedPartId!;
            if (inventory.GetItem(partId) != null)
            {
                return;
            }

            TryBuyMissingPart(inventory, partId);
        }
        catch (Exception ex)
        {
            LogQuickBuyFailure($"QuickShop auto-buy before mount failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public static void EndMountLookup()
    {
        mountLookupDepth = Math.Max(0, mountLookupDepth - 1);
    }

    public static void AutoBuyIntoMissingItemLookup(Inventory __instance, string _ID, ref Item __result)
    {
        try
        {
            if (__result != null || !ShouldAutoBuyLookup(_ID))
            {
                return;
            }

            Item? item = TryBuyMissingPart(__instance, _ID);
            if (item != null)
            {
                __result = item;
            }
        }
        catch (Exception ex)
        {
            LogQuickBuyFailure($"QuickShop item lookup auto-buy failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public static void AutoBuyIntoEmptyItemsList(Inventory __instance, string _ID, ref Il2CppSystem.Collections.Generic.List<BaseItem> __result)
    {
        try
        {
            if (!ShouldAutoBuyLookup(_ID))
            {
                return;
            }

            if (__result == null || __result.Count > 0)
            {
                return;
            }

            Item? item = TryBuyMissingPart(__instance, _ID);
            if (item != null)
            {
                __result.Add(item);
            }
        }
        catch (Exception ex)
        {
            LogQuickBuyFailure($"QuickShop inventory-list auto-buy failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static string? ResolvePurchasablePartId(PartScript partScript)
    {
        string[] candidates =
        {
            partScript.GetIDWithTuned(),
            partScript.GetID()
        };

        foreach (string? rawPartId in candidates)
        {
            if (string.IsNullOrWhiteSpace(rawPartId))
            {
                continue;
            }

            string partId = rawPartId!;
            if (CanBuyPart(partId) && GameInventory.Instance.GetItemPropertyCached(partId) != null)
            {
                return partId;
            }
        }

        return null;
    }

    private static Item? TryBuyMissingPart(Inventory inventory, string partId)
    {
        if (isBuyingPart || inventory.GetItem(partId) != null)
        {
            return null;
        }

        PartProperty partProperty = GameInventory.Instance.GetItemPropertyCached(partId);
        int price = partProperty?.Price ?? -1;
        if (price < 0 || GlobalData.PlayerMoney < price)
        {
            return null;
        }

        try
        {
            isBuyingPart = true;
            Item item = new(partId);
            inventory.Add(item, true);
            GlobalData.AddPlayerMoney(-price);
            Plugin.ModLog.LogInfo($"QuickShop bought one missing mount part {partId} for {price}.");
            return item;
        }
        finally
        {
            isBuyingPart = false;
        }
    }

    private static bool CanBuyPart(string partId)
    {
        return GameInventory.CanAddToShopList(partId, true);
    }

    private static bool ShouldAutoBuyLookup(string partId)
    {
        if (isBuyingPart || string.IsNullOrWhiteSpace(partId))
        {
            return false;
        }

        bool isCurrentHoveredPart = string.Equals(partId, hoveredPartId, StringComparison.Ordinal);
        if (!isCurrentHoveredPart && mountLookupDepth <= 0)
        {
            return false;
        }

        return CanBuyPart(partId) && GameInventory.Instance.GetItemPropertyCached(partId) != null;
    }

    private static Inventory? GetInventory()
    {
        GameManager? gameManager = UnityEngine.Object.FindObjectOfType<GameManager>();
        if (gameManager?.Inventory != null)
        {
            return gameManager.Inventory;
        }

        return UnityEngine.Object.FindObjectOfType<Inventory>();
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
