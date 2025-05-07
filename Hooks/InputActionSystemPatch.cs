using Bloodstone.API.Client;
using HarmonyLib;
using ProjectM;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bloodstone.Hooks;

[HarmonyPatch]
internal static class InputActionSystemPatch
{

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
        return Input.GetKeyDown(keybind.Primary) || Input.GetKeyDown(keybind.Secondary);
    }
    static bool IsKeybindUp(Keybinding keybind)
    {
        return Input.GetKeyUp(keybind.Primary) || Input.GetKeyUp(keybind.Secondary);
    }
    static bool IsKeybindPressed(Keybinding keybind)
    {
        return Input.GetKey(keybind.Primary) || Input.GetKey(keybind.Secondary);
    }
}
