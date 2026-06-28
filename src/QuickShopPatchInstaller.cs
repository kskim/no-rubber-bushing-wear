using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace NoRubberBushingWear;

internal static class QuickShopPatchInstaller
{
    private const string ShopItemTypeName = "CMS.UI.Logic.Shop.ShopItem";
    private const string PartsShopPageTypeName = "CMS.UI.Logic.Shop.PartsShopPage";
    private const string ShopBuyWindowTypeName = "CMS.UI.Windows.ShopBuyWindow";

    private static object? hoveredShopItem;
    private static MethodInfo? submitItemMethod;
    private static MethodInfo? buyItemMethod;
    private static bool quickBuyPending;
    private static bool loggedQuickBuyFailure;

    public static int Install(Harmony harmony)
    {
        int count = 0;

        Type? shopItemType = AccessTools.TypeByName(ShopItemTypeName);
        Type? partsShopPageType = AccessTools.TypeByName(PartsShopPageTypeName);
        Type? shopBuyWindowType = AccessTools.TypeByName(ShopBuyWindowTypeName);

        count += PatchIfFound(
            harmony,
            AccessTools.Method(shopItemType, "OnPointerEnter"),
            postfix: nameof(RememberHoveredShopItem));

        count += PatchIfFound(
            harmony,
            AccessTools.Method(shopItemType, "OnPointerExit"),
            postfix: nameof(ClearHoveredShopItem));

        count += PatchIfFound(
            harmony,
            AccessTools.Method(shopItemType, "Deselect"),
            postfix: nameof(ClearHoveredShopItem));

        count += PatchIfFound(
            harmony,
            AccessTools.Method(partsShopPageType, "Close"),
            postfix: nameof(ClearAnyHoveredShopItem));

        count += PatchIfFound(
            harmony,
            AccessTools.Method(partsShopPageType, "HandleInput"),
            postfix: nameof(HandlePartsShopInput));

        MethodInfo? prepareForItem = AccessTools.Method(shopBuyWindowType, "PrepareForItem");
        count += PatchIfFound(harmony, prepareForItem, postfix: nameof(BuyPreparedItem));

        submitItemMethod = AccessTools.Method(partsShopPageType, "SubmitItem");
        buyItemMethod = AccessTools.Method(shopBuyWindowType, "BuyItem");

        if (count > 0)
        {
            Plugin.ModLog.LogInfo($"QuickShop enabled. Press B while hovering a part in the parts shop to buy one immediately.");
        }
        else
        {
            Plugin.ModLog.LogWarning("QuickShop hook points were not found.");
        }

        return count;
    }

    public static void RememberHoveredShopItem(object __instance)
    {
        hoveredShopItem = __instance;
    }

    public static void ClearHoveredShopItem(object __instance)
    {
        if (ReferenceEquals(hoveredShopItem, __instance))
        {
            hoveredShopItem = null;
        }
    }

    public static void ClearAnyHoveredShopItem()
    {
        hoveredShopItem = null;
    }

    public static void HandlePartsShopInput(object __instance)
    {
        if (!Input.GetKeyDown(KeyCode.B) || hoveredShopItem == null || IsInputFieldFocused(__instance))
        {
            return;
        }

        try
        {
            quickBuyPending = true;
            submitItemMethod ??= AccessTools.Method(__instance.GetType(), "SubmitItem");
            submitItemMethod?.Invoke(__instance, new[] { hoveredShopItem });

            if (submitItemMethod == null)
            {
                LogQuickBuyFailure("PartsShopPage.SubmitItem could not be resolved.");
            }
        }
        catch (Exception ex)
        {
            quickBuyPending = false;
            LogQuickBuyFailure($"QuickShop submit failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public static void BuyPreparedItem(object __instance)
    {
        if (!quickBuyPending)
        {
            return;
        }

        quickBuyPending = false;

        try
        {
            buyItemMethod ??= AccessTools.Method(__instance.GetType(), "BuyItem");
            buyItemMethod?.Invoke(__instance, Array.Empty<object>());

            if (buyItemMethod == null)
            {
                LogQuickBuyFailure("ShopBuyWindow.BuyItem could not be resolved.");
            }
        }
        catch (Exception ex)
        {
            LogQuickBuyFailure($"QuickShop buy failed: {ex.GetType().Name}: {ex.Message}");
        }
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

    private static bool IsInputFieldFocused(object page)
    {
        try
        {
            PropertyInfo? property = AccessTools.Property(page.GetType(), "inputFieldHasFocus");
            return property != null && property.GetValue(page) is bool focused && focused;
        }
        catch
        {
            return false;
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
