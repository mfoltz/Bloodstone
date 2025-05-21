using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Mono.Cecil;
using UnityEngine;
using Bloodstone.API.Client;
using Bloodstone.Patches.Shared;

namespace Bloodstone.API.Shared;
public static class Reload
{
#nullable disable
    private static string _reloadCommand;
    private static string _reloadPluginsFolder;
    // private static ReloadBehaviour _clientBehavior;
    private static Keybinding _clientReloadKeybinding;
#nullable enable

    /// <summary>
    /// Contains the list of all plugins that are loaded and support reloading
    /// They exist outside of <see cref="IL2CPPChainloader"/>"/>
    /// </summary>
    public static List<BasePlugin> LoadedPlugins { get; } = new();
    internal static void Initialize(string reloadCommand, string reloadPluginsFolder)
    {
        _reloadCommand = reloadCommand;
        _reloadPluginsFolder = reloadPluginsFolder;

        // note: no need to remove this on unload, since we'll unload the hook itself anyway
        ChatMessageSystemServerPatch.OnChatMessageHandler += HandleReloadCommand;

        if (VWorld.IsClient)
        {
            /*
            _clientReloadKeybinding = KeybindManager.Register(new()
            {
                Id = "gg.deca.Bloodstone.reload",
                Category = "Bloodstone",
                Name = "Reload Plugins",
                DefaultKeybinding = KeyCode.F6,
            });
            */

            _clientReloadKeybinding = KeybindManager.Register("gg.deca.Bloodstone.reload", "Reload Plugins", "Bloodstone", KeyCode.F6);
            _clientReloadKeybinding.AddKeyDownListener(ReloadClientPlugins);
            // _clientBehavior = BloodstonePlugin.Instance.AddComponent<ReloadBehaviour>();
        }

        LoadPlugins();
    }
    internal static void Uninitialize()
    {
        ChatMessageSystemServerPatch.OnChatMessageHandler -= HandleReloadCommand;

        /*
        if (_clientBehavior != null)
        {
            UnityEngine.Object.Destroy(_clientBehavior);
        }
        */
    }
    private static void HandleReloadCommand(VChatEvent ev)
    {
        if (ev.Message != _reloadCommand) return;
        if (!ev.User.IsAdmin) return; // ignore non-admin reload attempts

        ev.Cancel();

        UnloadPlugins();
        var loaded = LoadPlugins();

        if (loaded.Count > 0)
        {
            ev.User.SendSystemMessage($"Reloaded {string.Join(", ", loaded)}. See console for details.");
        }
        else
        {
            ev.User.SendSystemMessage($"Did not reload any plugins because no reloadable plugins were found. Check the console for more details.");
        }
    }
    static void UnloadPlugins()
    {
        for (int i = LoadedPlugins.Count - 1; i >= 0; i--)
        {
            var plugin = LoadedPlugins[i];

            if (!plugin.Unload())
            {
                BloodstonePlugin.Logger.LogWarning($"Plugin {plugin.GetType().FullName} does not support unloading, skipping...");
            }
            else
            {
                LoadedPlugins.RemoveAt(i);
            }
        }
    }
    static List<string> LoadPlugins()
    {
        if (!Directory.Exists(_reloadPluginsFolder)) return new();

        return Directory.GetFiles(_reloadPluginsFolder, "*.dll").SelectMany(LoadPlugin).ToList();
    }
    static List<string> LoadPlugin(string path)
    {
        var defaultResolver = new DefaultAssemblyResolver();
        defaultResolver.AddSearchDirectory(_reloadPluginsFolder);
        defaultResolver.AddSearchDirectory(Paths.ManagedPath);
        defaultResolver.AddSearchDirectory(Paths.BepInExAssemblyDirectory);
        defaultResolver.AddSearchDirectory(Path.Combine(Paths.BepInExRootPath, "interop"));

        using var dll = AssemblyDefinition.ReadAssembly(path, new() { AssemblyResolver = defaultResolver });
        dll.Name.Name = $"{dll.Name.Name}-{DateTime.Now.Ticks}";

        using var ms = new MemoryStream();
        dll.Write(ms);

        var loaded = new List<string>();

        var assembly = Assembly.Load(ms.ToArray());
        foreach (var pluginType in assembly.GetTypes().Where(x => typeof(BasePlugin).IsAssignableFrom(x)))
        {
            // skip plugins not marked as reloadable
            if (!pluginType.GetCustomAttributes<ReloadableAttribute>().Any())
            {
                BloodstonePlugin.Logger.LogWarning($"Plugin {pluginType.FullName} is not marked as reloadable, skipping...");
                continue;
            }

            // skip plugins already loaded
            if (LoadedPlugins.Any(x => x.GetType() == pluginType)) continue;

            try
            {
                // we skip chainloader here and don't check dependencies. Fast n dirty.
                var plugin = (BasePlugin)Activator.CreateInstance(pluginType);
                var metadata = MetadataHelper.GetMetadata(plugin);
                LoadedPlugins.Add(plugin);
                plugin.Load();
                loaded.Add(metadata.Name);

                // ensure initialize hook runs even if we reload far after initialization is already done
                if (OnInitialize.HasInitialized && plugin is IRunOnInitialized runOnInitialized)
                {
                    runOnInitialized.OnGameInitialized();
                }

                BloodstonePlugin.Logger.LogInfo($"Loaded plugin {pluginType.FullName}");
            }
            catch (Exception ex)
            {
                BloodstonePlugin.Logger.LogError($"Plugin {pluginType.FullName} threw an exception during initialization:");
                BloodstonePlugin.Logger.LogError(ex);
            }
        }

        return loaded;
    }
    static void ReloadClientPlugins()
    {
        BloodstonePlugin.Logger.LogInfo("Reloading client plugins...");
        UnloadPlugins();
        LoadPlugins();
    }

    /*
    class ReloadBehaviour : MonoBehaviour
    {
        void Update()
        {
            // does this need to be called from MonoBehaviour? either way, self note to add listener later
            if (_clientReloadKeybinding.IsPressed)
            {
                BloodstonePlugin.Logger.LogInfo("Reloading client plugins...");
                UnloadPlugins();
                LoadPlugins();
            }
        }
    }
    */
}