using Bloodstone.API.Shared;
using ProjectM.Network;
using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using static Bloodstone.Network.Registry;

namespace Bloodstone.Network;
internal static class Transport
{
    static readonly ConcurrentDictionary<string, NetBuffer> _netBuffers = [];
    static readonly TimeSpan _bufferTime = TimeSpan.FromSeconds(LIFETIME);

    static readonly HMACSHA256 _hmac = new(Encoding.UTF8.GetBytes(Const.SHARED_KEY));
    static int _nextMsgId = 1;
    const int LIFETIME = 10;

    static bool _initialized = false;
    public static void SendServerPacket<T>(User user, T packet) where T : unmanaged
    {
        // VWorld.Log.LogWarning("[SendServerPacket] SendToClient(Pong)");

        Type type = typeof(T);
        uint typeId = Hash32(type.FullName!);
        var pack = Serialization.GetPacker(type);
        byte[] data = pack(packet!);
        string b64 = Convert.ToBase64String(data);

        int maxPerPart = Const.SAFE_PAYLOAD_BYTES;
        int totalParts = (int)Math.Ceiling(b64.Length / (double)maxPerPart);
        string msgGuid = Interlocked.Increment(ref _nextMsgId).ToString("X6");

        for (int part = 0; part < totalParts; part++)
        {
            int start = part * maxPerPart;
            int len = Math.Min(maxPerPart, b64.Length - start);
            string slice = b64.Substring(start, len);

            string header = $"{msgGuid}|{part}/{totalParts}|{typeId}|";
            string preHmac = header + slice;
            string tag = ComputeMac(preHmac);
            string full = Const.PREFIX + preHmac + "|" + tag;

            PacketRelay._sendServerPacket(user, full);
        }
    }
    public static void SendClientPacket<T>(User user, T packet) where T : unmanaged
    {
        // VWorld.Log.LogWarning("[SendClientPacket] SendToServer(Ping)");

        Type type = typeof(T);
        uint typeId = Hash32(type.FullName!);
        var pack = Serialization.GetPacker(type);
        byte[] data = pack(packet!);
        string b64 = Convert.ToBase64String(data);

        int maxPerPart = Const.SAFE_PAYLOAD_BYTES;
        int totalParts = (int)Math.Ceiling(b64.Length / (double)maxPerPart);
        string msgGuid = Interlocked.Increment(ref _nextMsgId).ToString("X6");

        for (int part = 0; part < totalParts; part++)
        {
            int start = part * maxPerPart;
            int len = Math.Min(maxPerPart, b64.Length - start);
            string slice = b64.Substring(start, len);

            string header = $"{msgGuid}|{part}/{totalParts}|{typeId}|";
            string preHmac = header + slice;
            string tag = ComputeMac(preHmac);
            string full = Const.PREFIX + preHmac + "|" + tag;

            PacketRelay._sendClientPacket(user, full);
        }
    }
    internal static void Bootstrap()
    {
        if (_initialized) return;
        _initialized = true;

        PacketRelay.OnPacketReceivedHandler += OnChatMessage;
    }
    static void OnChatMessage(User sender, string message)
    {
        // VWorld.Log.LogWarning($"[OnChatMessage] {(VWorld.IsServer ? "SERVER" : "CLIENT")} RX fragment/line");

        SweepBuffers();
        if (!message.StartsWith(Const.PREFIX)) return;

        string payload = message.AsSpan(Const.PREFIX.Length).ToString();
        int lastPipe = payload.LastIndexOf('|');
        if (lastPipe < 0) return;                            

        string tagHex = payload[(lastPipe + 1)..];
        string unsigned = payload[..lastPipe];
        if (!VerifyMac(unsigned, tagHex)) return;           

        // split header
        string[] parts = unsigned.Split('|', 4);
        if (parts.Length < 4) return;
        string msgGuid = parts[0];
        string partInfo = parts[1];            
        string typeIdStr = parts[2];
        string thisChunk = parts[3];

        // fragment bookkeeping
        var tuple = partInfo.Split('/');
        int idx = int.Parse(tuple[0]);
        int total = int.Parse(tuple[1]);

        NetBuffer buffer = _netBuffers.GetOrAdd(msgGuid, _ => new(total));
        if (buffer.AddPart(idx, thisChunk))
        {
            _netBuffers.TryRemove(msgGuid, out _);
            HandleCompleteMessage(
                sender,
                UInt32.Parse(typeIdStr),
                buffer.Concat());
        }
    }
    static void HandleCompleteMessage(User sender, uint typeId, string b64)
    {
        // VWorld.Log.LogWarning($"[HandleCompleteMessage] {(VWorld.IsServer ? "SERVER" : "CLIENT")} complete msg typeId=0x{typeId:X}");

        if (!TryGet(typeId, out var handler))
            return;

        bool shouldUnpack = handler.Dir switch
        {
            Direction.Serverbound => VWorld.IsServer,
            Direction.Clientbound => VWorld.IsClient,
            _ => false
        };

        if (!shouldUnpack)
            return;

        object obj = handler.Unpack(Convert.FromBase64String(b64));
        handler.Invoke(sender, obj);
    }
    static void SweepBuffers()
    {
        if (_netBuffers.IsEmpty)
            return;

        DateTime now = DateTime.UtcNow;
        foreach (var kv in _netBuffers)
        {
            if (now - kv.Value.LastSeen > _bufferTime)
                _netBuffers.TryRemove(kv.Key, out _);
        }
    }
    static string ComputeMac(string input)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hash = _hmac.ComputeHash(bytes);
        return BitConverter.ToString(hash, 0, 8).Replace("-", "");
    }
    public static bool HasPacketPrefix(string msg)
        => msg.StartsWith(Const.PREFIX, StringComparison.Ordinal);
    static bool VerifyMac(string input, string hex)
        => string.Equals(ComputeMac(input), hex, StringComparison.OrdinalIgnoreCase);
}
