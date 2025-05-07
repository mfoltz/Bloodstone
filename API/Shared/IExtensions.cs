using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bloodstone.API.Shared;
public static class IExtensions
{
    public static Dictionary<TValue, TKey> Reverse<TKey, TValue>(this IDictionary<TKey, TValue> source)
    {
        var reversed = new Dictionary<TValue, TKey>();

        foreach (var kvp in source)
        {
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
    public static bool IsIndexWithinRange<T>(this IList<T> list, int index)
    {
        return index >= 0 && index < list.Count;
    }
    public static bool Start(this IEnumerator routine, out Coroutine coroutine)
    {
        coroutine = GameFrame.StartCoroutine(routine);
        return coroutine != null;
    }
    public static void Run(this IEnumerator routine)
    {
        GameFrame.StartCoroutine(routine);
    }
}
