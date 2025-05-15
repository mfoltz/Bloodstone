using Bloodstone.Network;
using ProjectM.Network;
using System;
using static Bloodstone.Network.Registry;

namespace Bloodstone.API.Shared;
public static class VNetwork
{
    public static void RegisterServerbound<T>(Action<T> handler) where T : unmanaged
        => Register(typeof(T), Direction.Serverbound, obj => handler((T)obj));
    public static void RegisterClientbound<T>(Action<T> handler) where T : unmanaged
        => Register(typeof(T), Direction.Clientbound, obj => handler((T)obj));
    public static void SendToServer<T>(T packet) where T : unmanaged
        => Transport.SendClientPacket(packet);
    public static void SendToClient<T>(User user, T packet) where T : unmanaged
        => Transport.SendServerPacket(user, packet);
    static VNetwork()
    {
        Transport.Bootstrap();
    }
}
