using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using System;
using Unity.Collections;
using Unity.Entities;

namespace Bloodstone.API.Shared;

/// <summary>
/// Various extensions to make it easier to work with VRising APIs.
/// </summary>
public static class VExtensions
{
    static EntityManager EntityManager => VWorld.EntityManager;

    /// <summary>
    /// For validating entity index against EntityManager capacity without touching the entity.
    /// </summary>    
    const string PREFIX = "Entity(";
    const int LENGTH = 7;

    /// <summary>
    /// Send the given system message to the user.
    /// </summary>
    public static void SendSystemMessage(this User user, string message)
    {
        if (!VWorld.IsServer) throw new Exception("SendSystemMessage can only be called on the server.");

        FixedString512Bytes fixedMessage = new(message);
        ServerChatUtils.SendSystemMessageToClient(VWorld.Server.EntityManager, user, ref fixedMessage);
    }

    public delegate void ActionRefHandler<T>(ref T item);

    /// <summary>
    /// Modify the given component on the given entity. The argument is passed
    /// as a reference, so it can be modified in place. The resulting struct
    /// is written back to the entity.
    /// </summary>
    static void With<T>(this Entity entity, ActionRefHandler<T> action) where T : struct
    {
        T item = entity.Read<T>();
        action(ref item);

        EntityManager.SetComponentData(entity, item);
    }
    public static void AddWith<T>(this Entity entity, ActionRefHandler<T> action) where T : struct
    {
        if (!entity.Has<T>())
        {
            entity.Add<T>();
        }

        entity.With(action);
    }
    public static void HasWith<T>(this Entity entity, ActionRefHandler<T> action) where T : struct
    {
        if (entity.Has<T>())
        {
            entity.With(action);
        }
    }
    public static void Write<T>(this Entity entity, T componentData) where T : struct
    {
        EntityManager.SetComponentData(entity, componentData);
    }
    public static T Read<T>(this Entity entity) where T : struct
    {
        return EntityManager.GetComponentData<T>(entity);
    }
    public static DynamicBuffer<T> ReadBuffer<T>(this Entity entity) where T : struct
    {
        return EntityManager.GetBuffer<T>(entity);
    }
    public static DynamicBuffer<T> AddBuffer<T>(this Entity entity) where T : struct
    {
        return EntityManager.AddBuffer<T>(entity);
    }
    public static bool TryGetComponent<T>(this Entity entity, out T componentData) where T : struct
    {
        componentData = default;

        if (entity.Has<T>())
        {
            componentData = entity.Read<T>();
            return true;
        }

        return false;
    }
    public static bool TryGetComponent<T>(this Entity entity) where T : struct
    {
        if (entity.Has<T>())
        {
            entity.Remove<T>();

            return true;
        }

        return false;
    }
    public static bool Has<T>(this Entity entity) where T : struct
    {
        return EntityManager.HasComponent(entity, new(Il2CppType.Of<T>()));
    }
    public static void TryAdd<T>(this Entity entity) where T : struct
    {
        if (!entity.Has<T>()) entity.Add<T>();
    }
    public static void Add<T>(this Entity entity) where T : struct
    {
        if (!entity.Has<T>()) EntityManager.AddComponent(entity, new(Il2CppType.Of<T>()));
    }
    public static void Remove<T>(this Entity entity) where T : struct
    {
        if (entity.Has<T>()) EntityManager.RemoveComponent(entity, new(Il2CppType.Of<T>()));
    }
    public static void Destroy(this Entity entity, bool immediate = false)
    {
        if (!entity.Exists()) return;

        if (immediate)
        {
            EntityManager.DestroyEntity(entity);
        }
        else
        {
            DestroyUtility.Destroy(EntityManager, entity);
        }
    }
    public static bool Exists(this Entity entity)
    {
        return entity.HasValue() && entity.IndexWithinCapacity() && EntityManager.Exists(entity);
    }
    public static bool HasValue(this Entity entity)
    {
        return entity != Entity.Null;
    }
    public static bool IndexWithinCapacity(this Entity entity)
    {
        string entityStr = entity.ToString();
        ReadOnlySpan<char> span = entityStr.AsSpan();

        if (!span.StartsWith(PREFIX)) return false;
        span = span[LENGTH..];

        int colon = span.IndexOf(':');
        if (colon <= 0) return false;

        ReadOnlySpan<char> tail = span[(colon + 1)..];

        int closeRel = tail.IndexOf(')');
        if (closeRel <= 0) return false;

        if (!int.TryParse(span[..colon], out int index)) return false;
        if (!int.TryParse(tail[..closeRel], out _)) return false;

        int capacity = EntityManager.EntityCapacity;
        bool isValid = (uint)index < (uint)capacity;

        return isValid;
    }
    public static bool IsDisabled(this Entity entity)
    {
        return entity.Has<Disabled>();
    }
    public static bool IsPlayer(this Entity entity)
    {
        return entity.Has<PlayerCharacter>();
    }
    public static bool IsVBlood(this Entity entity)
    {
        return entity.Has<VBloodConsumeSource>();
    }
    public static bool IsGateBoss(this Entity entity)
    {
        return entity.Has<VBloodUnit>() && !entity.Has<VBloodConsumeSource>();
    }
    public static bool IsVBloodOrGateBoss(this Entity entity)
    {
        return entity.Has<VBloodUnit>();
    }
    public static User GetUser(this Entity entity)
    {
        if (entity.TryGetComponent(out User user)) return user;
        else if (entity.TryGetComponent(out PlayerCharacter playerCharacter) && playerCharacter.UserEntity.TryGetComponent(out user)) return user;

        return User.Empty;
    }
    public static NetworkId GetNetworkId(this Entity entity)
    {
        if (entity.TryGetComponent(out NetworkId networkId))
        {
            return networkId;
        }

        return NetworkId.Empty;
    }
    public static NativeAccessor<Entity> ToEntityArrayAccessor(this EntityQuery entityQuery, Allocator allocator = Allocator.Temp)
    {
        NativeArray<Entity> entities = entityQuery.ToEntityArray(allocator);
        return new(entities);
    }
    public static NativeAccessor<T> ToComponentDataArrayAccessor<T>(this EntityQuery entityQuery, Allocator allocator = Allocator.Temp) where T : unmanaged
    {
        NativeArray<T> components = entityQuery.ToComponentDataArray<T>(allocator);
        return new(components);
    }
    public readonly struct NativeAccessor<T> : IDisposable where T : unmanaged
    {
        static NativeArray<T> _array;
        public NativeAccessor(NativeArray<T> array)
        {
            _array = array;
        }
        public T this[int index]
        {
            get => _array[index];
            set => _array[index] = value;
        }
        public int Length => _array.Length;
        public NativeArray<T>.Enumerator GetEnumerator() => _array.GetEnumerator();
        public void Dispose() => _array.Dispose();
    }
}