using ProjectM.Network;
using System;

namespace Bloodstone.Network;
internal static class PacketRelay
{
    public static event Action<string>? OnPacketReceivedHandler;
    public static void OnClientPacketReceived(string packet) => OnPacketReceivedHandler?.Invoke(packet);
    public static void OnServerPacketReceived(string packet) => OnPacketReceivedHandler?.Invoke(packet);

    // Packets from Client will always be from the local user
    public static Action<string> _sendClientPacket = packet => throw new InvalidOperationException("PacketRelay.SendClientPacket isn't bootstrapped, only use this from the client!");

    // Packets from Server need to be sent to specific users
    public static Action<User, string> _sendServerPacket = (user, packet) => throw new InvalidOperationException("PacketRelay.SendServerPacket isn't bootstrapped, only use this from the server!");
}
