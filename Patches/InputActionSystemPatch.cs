using Bloodstone.API.Client;
using HarmonyLib;
using ProjectM;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bloodstone.Patches;
internal static class InputActionSystemPatch
{
    static Harmony? _harmony;
    public static void Initialize()
    {
        if (_harmony != null)
            throw new Exception("Detour already initialized. You don't need to call this. The Bloodstone plugin will do it for you.");

        _harmony = Harmony.CreateAndPatchAll(typeof(InputActionSystemPatch), MyPluginInfo.PLUGIN_GUID);
    }
    public static void Uninitialize()
    {
        if (_harmony == null)
            throw new Exception("Detour wasn't initialized. Are you trying to unload Bloodstone twice?");

        _harmony.UnpatchSelf();
    }

    [HarmonyPatch(typeof(InputActionSystem), nameof(InputActionSystem.OnCreate))]
    [HarmonyPostfix]
    static void OnCreatePostfix(InputActionSystem __instance)
    {
        __instance._LoadedInputActions.Disable();

        InputActionMap inputActionMap = new(MyPluginInfo.PLUGIN_NAME);
        __instance._LoadedInputActions.m_ActionMaps.AddItem(inputActionMap);

        __instance._LoadedInputActions.Enable();
    }

    [HarmonyPatch(typeof(InputActionSystem), nameof(InputActionSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix()
    {
        foreach (Keybinding keybind in KeybindManager.Keybinds.Values)
        {
            if (IsKeybindDown(keybind)) keybind.KeyDown();
            if (IsKeybindUp(keybind)) keybind.KeyUp();
            if (IsKeybindPressed(keybind)) keybind.KeyPressed();
        }
    }
    static bool IsKeybindDown(Keybinding keybind)
    {
        return Input.GetKeyDown(keybind.Primary);
    }
    static bool IsKeybindUp(Keybinding keybind)
    {
        return Input.GetKeyUp(keybind.Primary);
    }
    static bool IsKeybindPressed(Keybinding keybind)
    {
        return Input.GetKey(keybind.Primary);
    }
}
