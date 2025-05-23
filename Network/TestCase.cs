using Bloodstone.API.Shared;
using System;
using System.Collections;
using Unity.Entities;
using UnityEngine;

namespace Bloodstone.Network;
internal readonly struct Ping(long ticks)
{
    public readonly long ClientTicks = ticks;
}
internal readonly struct Pong(long cTicks, long sTicks)
{
    public readonly long ClientTicks = cTicks;
    public readonly long ServerTicks = sTicks;
}
internal static class NetworkTesting
{
    public static void PingPong()
    {
        if (VWorld.IsClient)
        {
            DelayedPing().Run();
        }
        else if (VWorld.IsServer)
        {
            VWorld.Log.LogWarning("[PingPong.Server] Registering -> SendToClient(Pong)");
            VNetwork.RegisterServerbound<Ping>((sender, ping) =>
            {
                VWorld.Log.LogWarning($"[ClientPacketReceived] Received ping from {sender.PlatformId}");
                VNetwork.SendToClient(sender, new Pong(ping.ClientTicks, DateTime.UtcNow.Ticks));
            });
        }
    }

    const float DELAY = 60f;
    static readonly WaitForSeconds _delay = new(DELAY);
    public static bool _ready = false;
    static IEnumerator DelayedPing()
    {
        while (!_ready)
        {
            yield return null;
        }

        // yield return _delay; // lazy way to do this but just wanted to test real quick >_>

        VNetwork.RegisterClientbound<Pong>((sender, pong) =>
        {
            long rttTicks = DateTime.UtcNow.Ticks - pong.ClientTicks;
            double ms = TimeSpan.FromTicks(rttTicks).TotalMilliseconds;
            VWorld.Log.LogWarning($"[ServerPacketReceived] RTT ≈ {ms:F1} ms (server responded in "
                         + $"{TimeSpan.FromTicks(pong.ServerTicks - pong.ClientTicks).TotalMilliseconds:F1} ms)");
            VNetwork.SendToServer(new Ping(DateTime.UtcNow.Ticks));
        });

        VNetwork.SendToServer(new Ping(DateTime.UtcNow.Ticks));
    }
}

