using ProjectM;
using Stunlock.Localization;
using System;
using System.Text.Json.Serialization;
using UnityEngine;

namespace Bloodstone.API.Client;

[Serializable]
public class Keybinding
{
    public string Name;
    public string Description;
    public string Category;

    public KeyCode Primary = KeyCode.None;
    public KeyCode Secondary = KeyCode.None;
    public string PrimaryName => KeybindManager.GetLiteral(Primary);
    public string SecondaryName => KeybindManager.GetLiteral(Secondary);

    public delegate void KeyHandler();

    public event KeyHandler OnKeyPressed = delegate { };
    public event KeyHandler OnKeyDown = delegate { };
    public event KeyHandler OnKeyUp = delegate { };

    [JsonIgnore]
    public LocalizationKey NameKey;

    [JsonIgnore]
    public LocalizationKey DescriptionKey;

    [JsonIgnore]
    public ButtonInputAction InputFlag;

    [JsonIgnore]
    public int AssetGuid;

    // public Keybinding() { }
    public Keybinding(string name, string description, string category, KeyCode defaultKey)
    {
        Name = name;
        Description = description;
        Category = category;
        Primary = defaultKey;
        NameKey = LocalizationKeyManager.GetLocalizationKey(name);
        DescriptionKey = LocalizationKeyManager.GetLocalizationKey(description);
        InputFlag = KeybindManager.ComputeInputFlag(name);
        AssetGuid = KeybindManager.ComputeAssetGuid(name);
    }
    public void AddKeyPressedListener(KeyHandler action) => OnKeyPressed += action;
    public void AddKeyDownListener(KeyHandler action) => OnKeyDown += action;
    public void AddKeyUpListener(KeyHandler action) => OnKeyUp += action;
    public void KeyPressed() => OnKeyPressed();
    public void KeyDown() => OnKeyDown();
    public void KeyUp() => OnKeyUp();
    public void ApplySaved(Keybinding keybind)
    {
        if (keybind == null) return;

        Primary = keybind.Primary;
        Secondary = keybind.Secondary;
    }
}
