using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System;
using Unity.Collections;
using Unity.Entities;

namespace Bloodstone.API;

/// <summary>
/// Various extensions to make it easier to work with VRising APIs.
/// </summary>
public static class VExtensions
{
    static EntityManager EntityManager => VWorld.EntityManager;

    // for checking entity indexes by string to verify within entityManager capacity
    const string PREFIX = "Entity(";
    const int LENGTH = 7;

    /// <summary>
    /// Send the given system message to the user.
    /// </summary>
    public static void SendSystemMessage(this User user, string message)
    {
        if (!VWorld.IsServer) throw new System.Exception("SendSystemMessage can only be called on the server.");

        FixedString512Bytes fixedMessage = new(message);
        ServerChatUtils.SendSystemMessageToClient(VWorld.Server.EntityManager, user, ref fixedMessage);
    }

    /// <summary>
    /// Modify the given component on the given entity. The argument is passed
    /// as a reference, so it can be modified in place. The resulting struct
    /// is written back to the entity.
    /// </summary>
    public static void WithComponentData<T>(this Entity entity, ActionRefHandler<T> action)
        where T : struct
    {
        var component = VWorld.Game.EntityManager.GetComponentData<T>(entity);
        action(ref component);
        VWorld.Game.EntityManager.SetComponentData<T>(entity, component);
    }

    public delegate void ActionRefHandler<T>(ref T item);
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
    public unsafe static void Write<T>(this Entity entity, T componentData) where T : struct
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
        EntityManager.AddComponent(entity, new(Il2CppType.Of<T>()));
    }
    public static void Remove<T>(this Entity entity) where T : struct
    {
        EntityManager.RemoveComponent(entity, new(Il2CppType.Of<T>()));
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

        // Parse numbers
        if (!int.TryParse(span[..colon], out int index)) return false;
        if (!int.TryParse(tail[..closeRel], out _)) return false;

        // Single unsigned capacity check
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
        if (entity.Has<PlayerCharacter>())
        {
            return true;
        }

        return false;
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
}