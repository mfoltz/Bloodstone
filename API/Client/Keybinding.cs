using ProjectM;
using Stunlock.Localization;
using System;
using System.Text.Json.Serialization;
using UnityEngine;

namespace Bloodstone.API.Client;

/// <summary>
/// Properly hooking keybinding menu in V Rising is a major pain in the ass. The
/// geniuses over at Stunlock studios decided to make the keybindings a flag enum.
/// This sounds decent, but it locks you to a whopping 64 unique keybindings. Guess
/// how many the game uses? 64 exactly.
///
/// As a result we can't just hook into the same system and add a new control, since
/// we don't actually have any free keybinding codes we could re-use. If we tried to
/// do that, it would mean that if a user used one of our keybinds, they would also
/// use at least one of the pre-configured game keybinds (since the IsKeyDown check
/// only checks whether the specific bit in the current input bitfield is set). As a
/// result we have to work around this by carefully avoiding that our custom invalid
/// keybinding flags are never serialized to the input system that V Rising uses, so
/// we have to implement quite a bit ourselves. This will probably break at some point
/// since I doubt Stunlock will be content with 64 unique input settings for the rest
/// of the game's lifetime. Good luck for who will end up needing to fix it.
/// </summary>

[Serializable]
public class Keybinding
{
    public string Name;
    public string Description;
    public string Category;

    public KeyCode Primary = KeyCode.None;
    public string PrimaryName => KeybindManager.GetLiteral(Primary);

    public delegate void KeyHandler();

    public event KeyHandler OnKeyPressedHandler = delegate { };
    public event KeyHandler OnKeyDownHandler = delegate { };
    public event KeyHandler OnKeyUpHandler = delegate { };

    [JsonIgnore]
    public LocalizationKey NameKey;

    [JsonIgnore]
    public LocalizationKey DescriptionKey;

    [JsonIgnore]
    public ButtonInputAction InputFlag;

    [JsonIgnore]
    public int AssetGuid;
    public Keybinding() { }
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
    public void AddKeyPressedListener(KeyHandler action) => OnKeyPressedHandler += action;
    public void AddKeyDownListener(KeyHandler action) => OnKeyDownHandler += action;
    public void AddKeyUpListener(KeyHandler action) => OnKeyUpHandler += action;
    public void KeyPressed() => OnKeyPressedHandler();
    public void KeyDown() => OnKeyDownHandler();
    public void KeyUp() => OnKeyUpHandler();
    public void ApplySaved(Keybinding keybind)
    {
        if (keybind == null) return;

        Primary = keybind.Primary;
    }
}
