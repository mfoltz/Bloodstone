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
    static readonly ConcurrentDictionary<string, NetBuffer> _buffers = [];

    static readonly HMACSHA256 _hmac = new(Encoding.UTF8.GetBytes(Const.SHARED_KEY));
    static int _nextMsgId = 1;
    internal static void Send<T>(Direction dir, T message, ProjectM.Network.User? targetUser = null) where T : unmanaged
    {
        Type t = typeof(T);
        uint typeId = Hash32(t.FullName!);
        var pack = Serialization.GetPacker(t);
        byte[] data = pack(message!);
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

            if (dir == Direction.Serverbound)
                PacketRelay._clientSend(full);                       
            else if (targetUser is null)
                PacketRelay._serverBroadcast(full);                 
            else
                PacketRelay._serverSendToUser(targetUser.Value, full);
        }
    }
    internal static void Broadcast<T>(T msg) where T : unmanaged
        => Send(Direction.Clientbound, msg, null);
    internal static void Bootstrap()
    {
        if (_bootstrapped) return;
        _bootstrapped = true;

        PacketRelay.OnPacketReceivedHandler += OnChatMessage;  // wire managed callback
    }

    static bool _bootstrapped = false;
    static void OnChatMessage(ProjectM.Network.User sender, string message, bool isServerSide)
    {
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

        var buffer = _buffers.GetOrAdd(msgGuid, _ => new NetBuffer(msgGuid, total));
        if (buffer.AddPart(idx, thisChunk))
        {
            _buffers.TryRemove(msgGuid, out _);
            HandleCompleteMessage(
                sender,
                UInt32.Parse(typeIdStr),
                buffer.Concat(),
                isServerSide ? Direction.Serverbound : Direction.Clientbound);
        }
    }
    static void HandleCompleteMessage(User sender, uint typeId, string b64, Direction dir)
    {
        if (!TryGet(typeId, out var handler) || handler.Dir != dir)
            return;

        byte[] data = Convert.FromBase64String(b64);
        var unpack = Serialization.GetUnpacker(handler.Invoke.Method.GetParameters()[0].ParameterType);
        object obj = unpack(data);
        handler.Invoke(obj);
    }
    static string ComputeMac(string input)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hash = _hmac.ComputeHash(bytes);
        return BitConverter.ToString(hash, 0, 8).Replace("-", "");
    }
    static bool VerifyMac(string input, string hex)
        => string.Equals(ComputeMac(input), hex, StringComparison.OrdinalIgnoreCase);
}
