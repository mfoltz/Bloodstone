using Bloodstone.API.Shared;
using Stunlock.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using UnityEngine;

namespace Bloodstone.API.Client;

[Serializable]
public abstract class MenuOption
{
    public string Name { get; set; }
    public string Description { get; set; }

    [JsonIgnore]
    public LocalizationKey NameKey;

    [JsonIgnore]
    public LocalizationKey DescKey;
    protected MenuOption() { }
    protected MenuOption(string name, string description)
    {
        Name = name;
        Description = description;
        NameKey = LocalizationKeyManager.GetLocalizationKey(name);
        DescKey = LocalizationKeyManager.GetLocalizationKey(description);
    }
    public abstract void ApplyDefault();
    public abstract void ApplySaved(MenuOption other);
}

[Serializable]
public abstract class MenuOption<T> : MenuOption
{
    public delegate void OptionChangedHandler<TValue>(TValue newValue);
    public virtual T Value { get; set; }
    public T DefaultValue { get; set; }

    public event OptionChangedHandler<T> OnOptionChangedHandler = delegate { };
    protected MenuOption() : base() { }
    protected MenuOption(string name, string description, T defaultValue)
        : base(name, description)
    {
        Value = defaultValue;
        DefaultValue = defaultValue;
    }
    public virtual void SetValue(T value)
    {
        Value = value;
        OnOptionChangedHandler(value);
    }
    public void AddListener(OptionChangedHandler<T> listener) => OnOptionChangedHandler += listener;
    public override void ApplyDefault() => SetValue(DefaultValue);
    public override void ApplySaved(MenuOption other)
    {
        if (other is MenuOption<T> typed)
        {
            VWorld.Log.LogWarning($"[MenuOption] Applying saved values - {other.Name}");
            SetValue(typed.Value);
        }
        else
        {
            VWorld.Log.LogWarning($"[MenuOption] Type mismatch loading values - {other.Name}");
        }
    }
}

[Serializable]
public class Toggle : MenuOption<bool>
{
    public Toggle() : base() { }
    public Toggle(string name, string description, bool defaultValue)
        : base(name, description, defaultValue) { }
    public override void ApplySaved(MenuOption other)
    {
        if (other is Toggle toggle)
            SetValue(toggle.Value);
    }
    public override void ApplyDefault() => SetValue(DefaultValue);
}

[Serializable]
public class Slider : MenuOption<float>
{
    public float MinValue { get; set; }
    public float MaxValue { get; set; }

    [JsonIgnore]
    public int Decimals { get; set; }

    [JsonIgnore]
    public float StepValue { get; set; }
    public override float Value
    {
        get => Mathf.Clamp(base.Value, MinValue, MaxValue);
        set => base.Value = Mathf.Clamp(value, MinValue, MaxValue);
    }
    public Slider() : base() { }
    public Slider(string name, string description, float min, float max, float defaultValue, int decimals = default, float step = default)
        : base(name, description, Mathf.Clamp(defaultValue, min, max))
    {
        MinValue = min;
        MaxValue = max;
        Decimals = decimals;
        StepValue = step;
        Value = defaultValue;
    }
    public override void SetValue(float value)
    {
        base.SetValue(Mathf.Clamp(value, MinValue, MaxValue));
    }
    public override void ApplySaved(MenuOption other)
    {
        if (other is Slider slider) SetValue(slider.Value);
    }
    public override void ApplyDefault()
    {
        SetValue(DefaultValue);
    }
}

[Serializable]
public class Dropdown : MenuOption<int>
{
    public List<string> Values { get; set; } = [];
    public Dropdown() : base() { }
    public Dropdown(string name, string description, int defaultIndex, string[] values)
        : base(name, description, defaultIndex)
    {
        Values = values?.ToList() ?? [];
    }
    public T GetEnumValue<T>(T fallback = default)
    {
        try { return (T)Enum.Parse(typeof(T), Values[Value]); }
        catch { return fallback; }
    }
    public override void ApplySaved(MenuOption other)
    {
        if (other is Dropdown dropdown)
        {
            int index = Mathf.Clamp(dropdown.Value, 0, Values.Count - 1);
            SetValue(index);
        }
    }
    public override void ApplyDefault()
    {
        SetValue(Mathf.Clamp(DefaultValue, 0, Values.Count - 1));
    }
}

/*
internal abstract class MenuOption(string name, string description)
{
    public string Name { get; } = name;
    public string Description { get; } = description;

    [JsonIgnore]
    public LocalizationKey NameKey { get; } = LocalizationManager.GetLocalizationKey(name);

    [JsonIgnore]
    public LocalizationKey DescKey { get; } = LocalizationManager.GetLocalizationKey(description);
    public abstract void ApplyDefault();
    public abstract void ApplySaved(MenuOption other);
}
internal abstract class MenuOption<T>(string name, string description, T defaultValue) : MenuOption(name, description)
{
    public delegate void OptionChangedHandler<TValue>(TValue newValue);
    public virtual T Value { get; internal set; } = defaultValue;
    public T DefaultValue { get; } = defaultValue;
    public event OptionChangedHandler<T> OnOptionChanged = delegate { };
    public virtual void SetValue(T value)
    {
        Value = value;
        OnOptionChanged(value);
    }
    public void AddListener(OptionChangedHandler<T> listener)
    {
        OnOptionChanged += listener;
    }
    public override void ApplyDefault() => SetValue(DefaultValue);
    public override void ApplySaved(MenuOption other)
    {
        if (other is MenuOption<T> typed) SetValue(typed.Value);
    }
}
internal class Toggle : MenuOption<bool>
{
    public Toggle() : base("", "", false) { }

    [JsonConstructor]
    public Toggle(string name, string description, bool defaultValue)
        : base(name, description, defaultValue) { }
}
internal class Slider : MenuOption<float>
{
    public string NameProp => Name;
    public string DescriptionProp => Description;
    public float DefaultValueProp => DefaultValue;
    public float MinValue { get; set; }
    public float MaxValue { get; set; }
    public int Decimals { get; set; }
    public float StepValue { get; set; }
    public override float Value
    {
        get => Mathf.Clamp(base.Value, MinValue, MaxValue);
        internal set => base.Value = Mathf.Clamp(value, MinValue, MaxValue);
    }
    public Slider() : base("", "", 0) { }

    [JsonConstructor]
    public Slider(string name, string description, float minValue, float maxValue, float defaultValue, int decimals = 0, float stepValue = 0)
        : base(name, description, defaultValue)
    {
        MinValue = minValue;
        MaxValue = maxValue;
        Decimals = decimals;
        StepValue = stepValue;
        Value = Mathf.Clamp(Value, MinValue, MaxValue);
    }
    public override void SetValue(float value)
    {
        base.SetValue(Mathf.Clamp(value, MinValue, MaxValue));
    }
}
internal class Dropdown : MenuOption<int>
{
    public string NameProp => Name;                
    public string DescriptionProp => Description;    
    public int DefaultValueProp => DefaultValue;     
    public string[] ValuesRaw
    {
        get => [..Values];
        set => Values = value?.ToList() ?? [];
    }
    public List<string> Values { get; set; } = [];
    public Dropdown() : base("", "", 0) { }

    [JsonConstructor]
    public Dropdown(string name, string description, int defaultValue, string[] values)
        : base(name, description, defaultValue)
    {
        Values = [..values];
    }
    public T GetEnumValue<T>(T fallback = default)
    {
        try { return (T)Enum.Parse(typeof(T), Values[Value]); }
        catch { return fallback; }
    }
}
*/

/*
internal class Dropdown(string name, string description, int defaultValue, string[] values) : MenuOption<int>(name, description, defaultValue)
{
    public List<string> Values { get; } = [..values];
    public T GetEnumValue<T>(T fallback = default)
    {
        try { return (T)Enum.Parse(typeof(T), Values[Value]); }
        catch { return fallback; }
    }
}
*/

/*
internal class Toggle(string name, string description, bool defaultValue) : MenuOption<bool>(name, description, defaultValue)
{
}
*/

/*
internal class Slider : MenuOption<float>
{
    public float MinValue { get; }
    public float MaxValue { get; }
    public int Decimals { get; }
    public float StepValue { get; }
    public override float Value
    {
        get => Mathf.Clamp(base.Value, MinValue, MaxValue);
        internal set => base.Value = Mathf.Clamp(value, MinValue, MaxValue);
    }
    public Slider(string name, string description, float minValue, float maxValue, float defaultValue, int decimals = 0, float stepValue = 0)
        : base(name, description, defaultValue)
    {
        MinValue = minValue;
        MaxValue = maxValue;
        Decimals = decimals;
        StepValue = stepValue;
        Value = Mathf.Clamp(Value, MinValue, MaxValue);
    }
    public override void SetValue(float value)
    {
        base.SetValue(Mathf.Clamp(value, MinValue, MaxValue));
    }
}
*/