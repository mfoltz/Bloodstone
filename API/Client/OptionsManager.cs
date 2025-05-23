using Bloodstone.API.Shared;
using Stunlock.Localization;
using System;
using System.Collections.Generic;

namespace Bloodstone.API.Client;
internal static class OptionsManager
{
    public enum OptionItemType 
    { 
        Toggle, 
        Slider, 
        Dropdown, 
        Divider 
    }
    public class OptionEntry(OptionItemType type, string key)
    {
        public OptionItemType Type { get; } = type;
        public string Key { get; } = key;
    }
    public static IReadOnlyDictionary<LocalizationKey, List<OptionEntry>> CategoryEntries => _categoryEntries;
    static readonly Dictionary<LocalizationKey, List<OptionEntry>> _categoryEntries = [];
    public static IReadOnlyDictionary<string, MenuOption> Options => _options;
    static readonly Dictionary<string, MenuOption> _options = [];
    public static IReadOnlyDictionary<string, LocalizationKey> CategoryKeys => _categoryKeys;
    static readonly Dictionary<string, LocalizationKey> _categoryKeys = [];
    static readonly HashSet<string> _categoryHeaders = [];
    public static IReadOnlyList<OptionEntry> OrderedEntries => _orderedEntries;
    static readonly List<OptionEntry> _orderedEntries = [];
    public static Toggle AddToggle(string name, string description, bool defaultValue)
    {
        var toggle = new Toggle(name, description, defaultValue);
        _options[name] = toggle;
        _orderedEntries.Add(new OptionEntry(OptionItemType.Toggle, name));
        return toggle;
    }
    public static Slider AddSlider(string name, string description, float min, float max, float defaultVal, int decimals = 0, float step = 0)
    {
        var slider = new Slider(name, description, min, max, defaultVal, decimals, step);
        _options[name] = slider;
        _orderedEntries.Add(new OptionEntry(OptionItemType.Slider, name));
        return slider;
    }
    public static Dropdown AddDropdown(string name, string description, int defaultIndex, string[] values)
    {
        var dropdown = new Dropdown(name, description, defaultIndex, values);
        _options[name] = dropdown;
        _orderedEntries.Add(new OptionEntry(OptionItemType.Dropdown, name));
        return dropdown;
    }
    public static void AddDivider(string label)
    {
        _orderedEntries.Add(new(OptionItemType.Divider, label));
    }
    static void RegisterOption(string category, string name, MenuOption option, OptionItemType type)
    {
        var localizationKey = LocalizeOptionHeader(category);
        _options[name] = option;
        _categoryEntries[localizationKey].Add(new OptionEntry(type, name));
    }
    static LocalizationKey LocalizeOptionHeader(string category)
    {
        if (!_categoryHeaders.Contains(category) && !_categoryKeys.TryGetValue(category, out var localizationKey))
        {
            localizationKey = LocalizationKeyManager.GetLocalizationKey(category);
            _categoryKeys[category] = localizationKey;
            _categoryHeaders.Add(category);
            _categoryEntries[localizationKey] = [];
        }
        else
        {
            localizationKey = _categoryKeys[category];
        }

        return localizationKey;
    }
    public static bool TryGetOption(OptionEntry entry, out MenuOption? option)
    {
        option = null;

        if (!_options.TryGetValue(entry.Key, out var raw))
        {
            VWorld.Log.LogWarning($"[OptionsManager] Key not found: {entry.Key}");
            return false;
        }

        var expectedType = GetValueType(entry.Type);
        if (expectedType == null)
        {
            VWorld.Log.LogWarning($"[OptionsManager] Unsupported type for: {entry.Key} ({entry.Type})");
            return false;
        }

        var menuOptionType = typeof(MenuOption<>).MakeGenericType(expectedType);
        if (!menuOptionType.IsInstanceOfType(raw))
        {
            VWorld.Log.LogWarning($"[OptionsManager] Type mismatch: {entry.Key} (expected: {menuOptionType.Name}, actual: {raw.GetType().Name})");
            return false;
        }

        option = raw;
        return true;
    }
    static Type? GetValueType(OptionItemType type) => type switch
    {
        OptionItemType.Toggle => typeof(bool),
        OptionItemType.Slider => typeof(float),
        OptionItemType.Dropdown => typeof(int),
        _ => null
    };
}