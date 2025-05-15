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
    public static PackDelHandler GetPacker(Type type) => _packers.GetOrAdd(type, CreatePacker);
    public static UnpackDelHandler GetUnpacker(Type type) => _unpackers.GetOrAdd(type, CreateUnpacker);

    static readonly ConcurrentDictionary<Type, PackDelHandler> _packers = new();
    static readonly ConcurrentDictionary<Type, UnpackDelHandler> _unpackers = new();
    static PackDelHandler CreatePacker(Type type)
    {
        if (IsBlittable(type))
        {
            int size = Marshal.SizeOf(type);

            return obj =>
            {
                int size = Marshal.SizeOf(type);
                byte[] bytes = new byte[size];              

                unsafe
                {
                    fixed (void* dest = bytes)
                        Unsafe.Copy(dest, ref obj!);
                }

                return bytes;                           
            };
        }

        return obj => JsonSerializer.SerializeToUtf8Bytes(obj, type, _jsonOptions);
    }
    static UnpackDelHandler CreateUnpacker(Type type)
    {
        if (IsBlittable(type))
        {
            int size = Marshal.SizeOf(type);

            return data =>
            {
                object obj = Activator.CreateInstance(type)!;

                unsafe
                {
                    fixed (byte* src = data)
                        Unsafe.Copy(ref obj, src);
                }

                return obj;
            };
        }

        return data => JsonSerializer.Deserialize(data, type, _jsonOptions)!;
    }
    static bool IsBlittable(Type t)
        => t.IsValueType && !t.IsEnum && !t.ContainsGenericParameters;
}
