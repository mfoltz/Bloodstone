using Bloodstone.Util;
using ProjectM;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Bloodstone.API.Client;
public static class KeybindManager
{
    public static IReadOnlyDictionary<string, Keybinding> Keybinds => _keybinds;
    static readonly Dictionary<string, Keybinding> _keybinds = [];

    const ulong HASH_LONG = 14695981039346656037UL;
    const uint HASH_INT = 2166136261U;

    static readonly Dictionary<KeyCode, string> _keyLiterals = new()
    {
        { KeyCode.Space, " " },
        { KeyCode.BackQuote, "`" },
        { KeyCode.Minus, "-" },
        { KeyCode.Equals, "=" },
        { KeyCode.LeftBracket, "[" },
        { KeyCode.RightBracket, "]" },
        { KeyCode.Backslash, "\\" },
        { KeyCode.Semicolon, ";" },
        { KeyCode.Quote, "'" },
        { KeyCode.Comma, "," },
        { KeyCode.Period, "." },
        { KeyCode.Slash, "/" },
        { KeyCode.Alpha0, "0" },
        { KeyCode.Alpha1, "1" },
        { KeyCode.Alpha2, "2" },
        { KeyCode.Alpha3, "3" },
        { KeyCode.Alpha4, "4" },
        { KeyCode.Alpha5, "5" },
        { KeyCode.Alpha6, "6" },
        { KeyCode.Alpha7, "7" },
        { KeyCode.Alpha8, "8" },
        { KeyCode.Alpha9, "9" },
        { KeyCode.A, "A" },
        { KeyCode.B, "B" },
        { KeyCode.C, "C" },
        { KeyCode.D, "D" },
        { KeyCode.E, "E" },
        { KeyCode.F, "F" },
        { KeyCode.G, "G" },
        { KeyCode.H, "H" },
        { KeyCode.I, "I" },
        { KeyCode.J, "J" },
        { KeyCode.K, "K" },
        { KeyCode.L, "L" },
        { KeyCode.M, "M" },
        { KeyCode.N, "N" },
        { KeyCode.O, "O" },
        { KeyCode.P, "P" },
        { KeyCode.Q, "Q" },
        { KeyCode.R, "R" },
        { KeyCode.S, "S" },
        { KeyCode.T, "T" },
        { KeyCode.U, "U" },
        { KeyCode.V, "V" },
        { KeyCode.W, "W" },
        { KeyCode.X, "X" },
        { KeyCode.Y, "Y" },
        { KeyCode.Z, "Z" },
        { KeyCode.UpArrow, "↑" },
        { KeyCode.DownArrow, "↓" },
        { KeyCode.LeftArrow, "←" },
        { KeyCode.RightArrow, "→" }
    };
    public static Keybinding Register(string name, string description, string category, KeyCode defaultKey)
    {
        if (_keybinds.TryGetValue(name, out var existing))
        {
            // Core.Log.LogInfo($"[KeybindsManager] Skipped duplicate keybind registration: {name}");
            return existing;
        }

        var keybind = new Keybinding(name, description, category, defaultKey);
        _keybinds[name] = keybind;

        return keybind;
    }
    public static void Rebind(Keybinding keybind, KeyCode newKey)
    {
        keybind.Primary = newKey;
        // KeybindManager.SaveKeybinds();
        Persistence.SaveKeybinds();
    }
    public static ButtonInputAction ComputeInputFlag(string descriptionId)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(descriptionId);
        ulong num = Hash64(bytes);
        bool flag = false;

        do
        {
            foreach (ButtonInputAction buttonInputAction in Enum.GetValues<ButtonInputAction>())
            {
                if (num == (ulong)buttonInputAction)
                {
                    flag = true;
                    num--;
                }
            }
        } while (flag);

        return (ButtonInputAction)num;
    }
    public static int ComputeAssetGuid(string descriptionId)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(descriptionId);
        return (int)Hash32(bytes);
    }
    public static string GetLiteral(KeyCode key)
    {
        return _keyLiterals.TryGetValue(key, out var literal) ? literal : key.ToString();
    }
    static ulong Hash64(byte[] data)
    {
        ulong hash = HASH_LONG;

        foreach (var b in data)
        {
            hash ^= b;
            hash *= 1099511628211UL;
        }

        return hash;
    }
    static uint Hash32(byte[] data)
    {
        uint hash = HASH_INT;

        foreach (var b in data)
        {
            hash ^= b;
            hash *= 16777619U;
        }

        return hash;
    }
}
