using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
#if BEPINEX_IL2CPP
using BepInEx.Unity.IL2CPP;
#endif

namespace NoRubberBushingWear;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
#if BEPINEX_IL2CPP
public sealed class Plugin : BasePlugin
#else
public sealed class Plugin : BaseUnityPlugin
#endif
{
    public const string PluginGuid = "com.local.norubberbushingwear";
    public const string PluginName = "No Rubber Bushing Wear";
    public const string PluginVersion = "1.1.0";

    private Harmony? harmony;

    internal static ManualLogSource ModLog { get; private set; } = null!;

#if BEPINEX_IL2CPP
    public override void Load()
    {
        ModLog = base.Log;
        ApplyPatches();
    }
#else
    private void Awake()
    {
        ModLog = Logger;
        ApplyPatches();
    }
#endif

    private void ApplyPatches()
    {
        harmony = new Harmony(PluginGuid);

        int patched = RuntimePatchInstaller.Install(harmony);
        patched += QuickShopPatchInstaller.Install(harmony);
        ModLog.LogInfo($"{PluginName} loaded. Harmony patch points applied: {patched}.");

        if (patched == 0)
        {
            ModLog.LogWarning("No compatible CMS 2021 hook points were found. For IL2CPP builds, use an IL2CPP-capable BepInEx install and run once so interop assemblies are generated.");
        }
    }

#if !BEPINEX_IL2CPP
    private void OnDestroy()
    {
        harmony?.UnpatchSelf();
    }
#endif
}
