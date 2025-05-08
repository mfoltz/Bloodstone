using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Bloodstone.Network;
internal static class Registry
{
    public enum Direction : byte
    {
        Serverbound = 0,
        Clientbound = 1
    }
    public static class Const
    {
        public const int MAX_CHAT_BYTES = 512;                 
        public const string PREFIX = "\u200B#BCN:";         
        public const int PREFIX_BYTES = 8;                      
        public const int HEADER_RESERVE = 32;   
        public const int SAFE_PAYLOAD_BYTES = MAX_CHAT_BYTES - HEADER_RESERVE;
        public const string SHARED_KEY = MyPluginInfo.PLUGIN_VERSION;
    }
    public record Handler(Direction Dir, Action<object> Invoke);
    static readonly ConcurrentDictionary<uint, Handler> _handlers = new();
    public static void Register(Type t, Direction dir, Action<object> cb)
    {
        uint id = Hash32(t.FullName!);
        _handlers[id] = new Handler(dir, cb);
    }
    public static bool TryGet(uint id, out Handler handler)
        => _handlers.TryGetValue(id, out handler);
    public static IEnumerable<KeyValuePair<uint, Handler>> All => _handlers;
    public static uint Hash32(string s)
    {
        unchecked
        {
            uint hash = 0x811C9DC5;
            foreach (char c in s)
                hash = (hash ^ c) * 0x01000193;
            return hash == 0 ? 1u : hash;
        }
    }
}
