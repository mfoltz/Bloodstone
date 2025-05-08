using System;

namespace Bloodstone.Network;
internal static class PacketRelay
{
    public static event Action<ProjectM.Network.User, string, bool>? OnPacketReceivedHandler;
    public static void RaiseRecv(ProjectM.Network.User sender, string msg, bool isServerSide)
        => OnPacketReceivedHandler?.Invoke(sender, msg, isServerSide);

    public static Action<string> _clientSend = _ => throw new InvalidOperationException("PacketRelay.ClientSend not wired!");
    public static Action<string> _serverBroadcast = _ => throw new InvalidOperationException("PacketRelay.ServerBroadcast not wired!");
    public static Action<ProjectM.Network.User, string> _serverSendToUser = (_, _) => throw new InvalidOperationException("PacketRelay.ServerSendToUser not wired!");
}
