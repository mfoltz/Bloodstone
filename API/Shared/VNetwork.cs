using Bloodstone.Network;
using System;
using static Bloodstone.Network.Registry;

namespace Bloodstone.API.Shared;
public static class VNetwork
{
    public static void RegisterServerbound<T>(Action<T> handler) where T : unmanaged
        => Register(typeof(T), Direction.Serverbound, obj => handler((T)obj));
    public static void RegisterClientbound<T>(Action<T> handler) where T : unmanaged
        => Register(typeof(T), Direction.Clientbound, obj => handler((T)obj));
    public static void RegisterBidirectional<T>(Action<T> handler) where T : unmanaged
    {
        RegisterServerbound(handler);
        RegisterClientbound(handler);
    }
    public static void SendToServer<T>(T msg) where T : unmanaged
        => Transport.Send(Direction.Serverbound, msg);
    public static void SendToClient<T>(ProjectM.Network.User user, T msg) where T : unmanaged
        => Transport.Send(Direction.Clientbound, msg, user);
    public static void BroadcastToClients<T>(T msg) where T : unmanaged
        => Transport.Broadcast(msg);
    static VNetwork()
    {
        Transport.Bootstrap();
    }
}
