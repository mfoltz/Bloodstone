using ProjectM.Network;
using System;

namespace Bloodstone.Network;
internal static class PacketRelay
{
    public static event Action<User, string>? OnPacketReceivedHandler;
    public static void OnClientPacketReceived(User sender, string packet) => OnPacketReceivedHandler?.Invoke(sender, packet);
    public static void OnServerPacketReceived(User sender, string packet) => OnPacketReceivedHandler?.Invoke(sender, packet);

    public static Action<User, string> _sendClientPacket = (_, _) => throw new InvalidOperationException("PacketRelay.SendClientPacket isn't bootstrapped, only use this from the client!");
    public static Action<User, string> _sendServerPacket = (_, _) => throw new InvalidOperationException("PacketRelay.SendServerPacket isn't bootstrapped, only use this from the server!");
}
