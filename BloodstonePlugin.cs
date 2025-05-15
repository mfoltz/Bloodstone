using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Bloodstone.API.Shared;
using Bloodstone.Network;
using Bloodstone.Patches;
using Bloodstone.Util;

namespace Bloodstone
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("gg.deca.VampireCommandFramework", BepInDependency.DependencyFlags.SoftDependency)]
    public class BloodstonePlugin : BasePlugin
    {
#nullable disable
        public static ManualLogSource Logger { get; set; }
        internal static BloodstonePlugin Instance { get; set; }
#nullable enable

        private ConfigEntry<bool> _enableReloadCommand;
        private ConfigEntry<string> _reloadCommand;
        private ConfigEntry<string> _reloadPluginsFolder;
        public BloodstonePlugin() : base()
        {
            Logger = Log;
            Instance = this;

            _enableReloadCommand = Config.Bind("General", "EnableReloading", true, "Whether to enable the reloading feature (both client and server).");
            _reloadCommand = Config.Bind("General", "ReloadCommand", "!reload", "Server text command to reload plugins. User must be an admin.");
            _reloadPluginsFolder = Config.Bind("General", "ReloadablePluginsFolder", "BepInEx/BloodstonePlugins", "The folder to (re)load plugins from, relative to the game directory.");
        }
        public override void Load()
        {
            // Hooks
            if (VWorld.IsServer)
            {
                ChatMessageSystemServerPatch.Initialize();
            }

            if (VWorld.IsClient)
            {
                Persistence.LoadKeybinds();
                ChatMessageSystemClientPatch.Initialize();
                InputActionSystemPatch.Initialize();
                OptionsMenuPatches.Initialize();
            }

            OnInitialize.Initialize();
            Patches.GameFrame.Initialize();
            Bootstrapper.Initialize();

            Logger.LogInfo($"Bloodstone v{MyPluginInfo.PLUGIN_VERSION} loaded.");

            // NOTE: MUST BE LAST. This initializes plugins that depend on our state being set up.
            if (VWorld.IsClient || _enableReloadCommand.Value)
            {
                Reload.Initialize(_reloadCommand.Value, _reloadPluginsFolder.Value);
            }
        }
        public override bool Unload()
        {
            // Hooks
            if (VWorld.IsServer)
            {
                ChatMessageSystemServerPatch.Uninitialize();
            }

            if (VWorld.IsClient)
            {
                Persistence.SaveKeybinds();
                ChatMessageSystemClientPatch.Uninitialize();
                InputActionSystemPatch.Uninitialize();
                OptionsMenuPatches.Uninitialize();
            }

            OnInitialize.Uninitialize();
            Patches.GameFrame.Uninitialize();

            return true;
        }
    }
}