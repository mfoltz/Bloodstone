using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Bloodstone.Network;
internal static class Serialization
{
    public delegate byte[] PackDelHandler(object obj);
    public delegate object UnpackDelHandler(ReadOnlySpan<byte> data);

    static readonly JsonSerializerOptions _jsonOptions = new()
    {
        IncludeFields = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    public static PackDelHandler GetPacker(Type t) => _packers.GetOrAdd(t, CreatePacker);
    public static UnpackDelHandler GetUnpacker(Type t) => _unpackers.GetOrAdd(t, CreateUnpacker);

    static readonly ConcurrentDictionary<Type, PackDelHandler> _packers = new();
    static readonly ConcurrentDictionary<Type, UnpackDelHandler> _unpackers = new();
    static PackDelHandler CreatePacker(Type t)
    {
        if (IsBlittable(t))
        {
            int size = Marshal.SizeOf(t);

            return obj =>
            {
                byte[] arr = ArrayPool<byte>.Shared.Rent(size);
                unsafe
                {
                    fixed (byte* b = arr)
                        Unsafe.Copy(b, ref obj!);
                }

                return arr.AsSpan(0, size).ToArray();
            };
        }

        return obj => JsonSerializer.SerializeToUtf8Bytes(obj, t, _jsonOptions);
    }
    static UnpackDelHandler CreateUnpacker(Type t)
    {
        if (IsBlittable(t))
        {
            int size = Marshal.SizeOf(t);

            return data =>
            {
                object obj = Activator.CreateInstance(t)!;
                unsafe
                {
                    fixed (byte* src = data)
                        Unsafe.Copy(ref obj, src);
                }

                return obj;
            };
        }

        return data => JsonSerializer.Deserialize(data, t, _jsonOptions)!;
    }
    static bool IsBlittable(Type t)
        => t.IsValueType && !t.IsEnum && !t.ContainsGenericParameters;
}
