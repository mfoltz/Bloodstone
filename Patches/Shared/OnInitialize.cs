using BepInEx.Unity.IL2CPP;
using Bloodstone.API.Server;
using Bloodstone.API.Shared;
using Bloodstone.Network;
using Bloodstone.Services;
using HarmonyLib;
using ProjectM;
using Stunlock.Core;

namespace Bloodstone.Patches.Shared;

/// <summary>
/// Hook responsible for handling calls to IRunOnInitialized.
/// </summary>
static class OnInitialize
{
#nullable disable
    static Harmony _harmony;
#nullable enable
    public static bool HasInitialized { get; set; } = false;
    public static void Initialize()
    {
        _harmony = Harmony.CreateAndPatchAll(VWorld.IsServer ? typeof(ServerDetours) : typeof(ClientDetours));
    }
    public static void Uninitialize()
    {
        _harmony.UnpatchSelf();
    }
    static void InvokePlugins()
    {
        BloodstonePlugin.Logger.LogInfo("Game has bootstrapped. Worlds and systems now exist.");

        if (HasInitialized) return;
        HasInitialized = true;

        foreach (var (name, info) in IL2CPPChainloader.Instance.Plugins)
        {
            if (info.Instance is IRunOnInitialized runOnInitialized)
            {
                runOnInitialized.OnGameInitialized();
            }
        }

        foreach (var plugin in Reload.LoadedPlugins)
        {
            if (plugin is IRunOnInitialized runOnInitialized)
            {
                runOnInitialized.OnGameInitialized();
            }
        }
    }

    // these are intentionally different classes, even if their bodies _currently_ are the same
    static class ServerDetours
    {
        [HarmonyPatch(typeof(GameBootstrap), nameof(GameBootstrap.Start))]
        [HarmonyPostfix]
        public static void Initialize()
        {
            Bootstrapper.Initialize();
            VEvents.Initialize();
            PlayerService.Initialize();
            // NetworkTesting.PingPong();

            InvokePlugins();
        }
    }
    static class ClientDetours
    {
        [HarmonyPatch(typeof(WorldBootstrapUtilities), nameof(WorldBootstrapUtilities.AddSystemsToWorld))]
        [HarmonyPostfix]
        public static void Initialize()
        {
            Bootstrapper.Initialize();
            // NetworkTesting.PingPong();
            // NetworkTesting._ready = true;

            InvokePlugins();
        }
    }
}