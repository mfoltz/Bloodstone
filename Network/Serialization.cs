using Bloodstone.API.Shared;
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
        VWorld.Log.LogWarning($"[CreatePacker] Creating packer ({type.Name})");

        if (IsBlittable(type))
        {
            int size = Marshal.SizeOf(type);

            return obj =>
            {
                byte[] bytes = new byte[size];
                IntPtr ptr = Marshal.AllocHGlobal(size);

                try
                {
                    Marshal.StructureToPtr(obj, ptr, false);     // write struct -> native
                    Marshal.Copy(ptr, bytes, 0, size);           // copy native → managed
                }
                finally { Marshal.FreeHGlobal(ptr); }

                return bytes;
            };
        }

        return obj => JsonSerializer.SerializeToUtf8Bytes(obj, type, _jsonOptions);
    }
    static UnpackDelHandler CreateUnpacker(Type type)
    {
        VWorld.Log.LogWarning($"[CreateUnpacker] Creating unpacker ({type.Name})");

        if (IsBlittable(type))
        {
            int size = Marshal.SizeOf(type);

            return dataSpan =>
            {
                byte[] buffer = dataSpan.ToArray();
                IntPtr ptr = Marshal.AllocHGlobal(size);

                try
                {
                    Marshal.Copy(buffer, 0, ptr, size);        
                    return Marshal.PtrToStructure(ptr, type)!;
                }
                finally { Marshal.FreeHGlobal(ptr); }
            };
        }

        return data => JsonSerializer.Deserialize(data, type, _jsonOptions)!;
    }
    static bool IsBlittable(Type t)
        => t.IsValueType && !t.IsEnum && !t.ContainsGenericParameters;
}
