﻿using System;
using System.Collections.Generic;

namespace Bloodstone.API;
public static class IExtensions
{
    public static Dictionary<TValue, TKey> Reverse<TKey, TValue>(
        this IDictionary<TKey, TValue> source)
    {
        var reversed = new Dictionary<TValue, TKey>();

        foreach (var kvp in source)
        {
            reversed[kvp.Value] = kvp.Key;
        }

        return reversed;
    }
    public static Dictionary<TValue, TKey> ReverseIl2CppDictionary<TKey, TValue>(
        this Il2CppSystem.Collections.Generic.Dictionary<TKey, TValue> source)
    {
        var reversed = new Dictionary<TValue, TKey>();

        if (source == null) return reversed;

        foreach (var kvp in source)
        {
            if (reversed.ContainsKey(kvp.Value))
            {
                continue;
            }

            reversed[kvp.Value] = kvp.Key;
        }

        return reversed;
    }
    public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
    {
        foreach (var item in collection)
        {
            action(item);
        }
    }
    public static bool IsIndexWithinRange<T>(this IList<T> list, int index)
    {
        return index >= 0 && index < list.Count;
    }
    public static bool ContainsAll(this string stringChars, List<string> strings)
    {
        foreach (string str in strings)
        {
            if (!stringChars.Contains(str, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
    public static bool ContainsAny(this string stringChars, List<string> strings)
    {
        foreach (string str in strings)
        {
            if (stringChars.Contains(str, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
