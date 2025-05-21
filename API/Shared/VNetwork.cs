using Bloodstone.Network;
using ProjectM.Network;
using System;
using static Bloodstone.Network.Registry;

namespace Bloodstone.API.Shared;
public static class VNetwork
{
    // register/unregister blittable structs
    public static void RegisterServerboundStruct<T>(Action<User, T> handler) where T : unmanaged
        => RegisterServerbound(handler);                   
    public static void RegisterClientboundStruct<T>(Action<User, T> handler) where T : unmanaged
        => RegisterClientbound(handler);
    public static void RegisterBiDirectionalStruct<T>(
        Action<User, T> serverHandler,
        Action<User, T> clientHandler) where T : unmanaged
    {
        RegisterServerboundStruct(serverHandler);
        RegisterClientboundStruct(clientHandler);
    }
    public static void Unregister<T>() => Registry.Unregister<T>();

    // client/server senders
    public static void SendToServer<T>(T packet) where T : unmanaged
        => Transport.SendClientPacket(VWorld.LocalUser.Read<User>(), packet);
    public static void SendToClient<T>(User target, T packet) where T : unmanaged
        => Transport.SendServerPacket(target, packet);

    // internals
    internal static void RegisterServerbound<T>(
        Action<User, T> handler)
        => Register<T>(Direction.Serverbound,
                       (sender, obj) => handler(sender, (T)obj));
    internal static void RegisterClientbound<T>(
        Action<User, T> handler)
        => Register<T>(Direction.Clientbound,
                       (sender, obj) => handler(sender, (T)obj));
    internal static void Register<T>(
        Direction dir,
        Action<User, object> boxedHandler)
    {
        Registry.Register<T>(dir, boxedHandler);
    }
}

/*
public static class VNetwork
{
    public static void RegisterServerbound<T>(Action<User, T> handler)
        => Register<T>(Direction.Serverbound,
            (sender, obj) => handler(sender, (T)obj));
    public static void RegisterClientbound<T>(Action<User, T> handler)
        => Register<T>(Direction.Clientbound,
            (sender, obj) => handler(sender, (T)obj));
    public static void SendToServer<T>(User user, T packet) where T : unmanaged
        => Transport.SendClientPacket(user, packet);
    public static void SendToClient<T>(User user, T packet) where T : unmanaged
        => Transport.SendServerPacket(user, packet);
    public static void RegisterServerboundStruct<T>(Action<User, T> serverHandler) where T : unmanaged
        => RegisterServerbound(serverHandler);
    public static void RegisterClientboundStruct<T>(Action<User, T> clientHandler) where T : unmanaged
        => RegisterClientbound(clientHandler);
    public static void RegisterBiDirectionalStruct<T>(
            Action<User, T> serverHandler,
            Action<User, T> clientHandler) where T : unmanaged
    {
        RegisterServerboundStruct(serverHandler);
        RegisterClientboundStruct(clientHandler);
    }
    public static void Unregister<T>() => Registry.Unregister<T>();
}
*/