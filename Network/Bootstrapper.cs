using Bloodstone.API.Shared;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using System;
using Unity.Collections;
using Unity.Entities;

namespace Bloodstone.Network;
internal static class Bootstrapper
{
    static EntityManager EntityManager => VWorld.EntityManager;
    static bool _initialized;

    static readonly ComponentType[] _componentTypes =
    {
        ComponentType.ReadOnly(Il2CppType.Of<FromCharacter>()),
        ComponentType.ReadOnly(Il2CppType.Of<NetworkEventType>()),
        ComponentType.ReadOnly(Il2CppType.Of<SendNetworkEventTag>()),
        ComponentType.ReadOnly(Il2CppType.Of<ChatMessageEvent>())
    };

    static readonly NetworkEventType _eventType = new()
    {
        IsAdminEvent = false,
        EventId = NetworkEvents.EventId_ChatMessageEvent,
        IsDebugEvent = false,
    };
    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        if (VWorld.IsClient)
            ClientPacketRelay();
        else if (VWorld.IsServer)
            ServerPacketRelay();

        Transport.Bootstrap();
    }
    static void ClientPacketRelay()
    {
        PacketRelay._sendClientPacket = (user, packet) =>
        {
            if (!VWorld.LocalCharacter.Exists() || !VWorld.LocalUser.Exists())
            {
                VWorld.Log.LogWarning($"[PacketRelay] LocalCharacter or LocalUser does not exist yet!");
            }

            VWorld.Log.LogWarning($"[PacketRelay] Sending packet to server ({DateTime.Now.TimeOfDay})");

            ChatMessageEvent chatMessageEvent = new()
            {
                MessageText = new FixedString512Bytes(packet),
                MessageType = ChatMessageType.Local,
                ReceiverEntity = VWorld.LocalUser.GetNetworkId()
            };

            Entity networkEntity = EntityManager.CreateEntity(_componentTypes);
            networkEntity.Write(new FromCharacter { Character = VWorld.LocalCharacter, User = VWorld.LocalUser });
            networkEntity.Write(_eventType);
            networkEntity.Write(chatMessageEvent);
        };
    }
    static void ServerPacketRelay()
    {
        PacketRelay._sendServerPacket = (user, packet) =>
        {
            VWorld.Log.LogWarning($"[PacketRelay] Sending packet to client ({DateTime.Now.TimeOfDay})");
            FixedString512Bytes fixedPacket = new(packet);
            ServerChatUtils.SendSystemMessageToClient(EntityManager, user, ref fixedPacket);
        };
    }
}
