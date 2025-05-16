
using Bloodstone.API.Shared;
using Bloodstone.Network;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.UI;
using System;
using Unity.Entities;
using static Bloodstone.Network.PacketRelay;
using static Bloodstone.API.Shared.VExtensions;

namespace Bloodstone.Patches;
public static class ChatMessageSystemServerPatch
{
    /// <summary>
    /// Event emitted whenever a chat message is received by the server.
    /// </summary>
    public delegate void ChatEventHandler(VChatEvent e);
    public static event ChatEventHandler? OnChatMessageHandler;

    static Harmony? _harmony;
    public static void Initialize()
    {
        if (_harmony != null)
            throw new Exception("Detour already initialized. You don't need to call this. The Bloodstone plugin will do it for you.");

        _harmony = Harmony.CreateAndPatchAll(typeof(ChatMessageSystemServerPatch), MyPluginInfo.PLUGIN_GUID);
    }
    public static void Uninitialize()
    {
        if (_harmony == null)
            throw new Exception("Detour wasn't initialized. Are you trying to unload Bloodstone twice?");

        _harmony.UnpatchSelf();
    }

    [HarmonyPatch(typeof(ChatMessageSystem), nameof(ChatMessageSystem.OnUpdate))]
    [HarmonyPrefix]
    public static void OnUpdatePrefix(ChatMessageSystem __instance)
    {
        using NativeAccessor<Entity> entities = __instance.__query_661171423_0.ToEntityArrayAccessor();
        using NativeAccessor<ChatMessageEvent> chatMessageEvents = __instance.__query_661171423_0.ToComponentDataArrayAccessor<ChatMessageEvent>();
        using NativeAccessor<FromCharacter> fromCharacters = __instance.__query_661171423_0.ToComponentDataArrayAccessor<FromCharacter>();

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            ChatMessageEvent chatMessage = chatMessageEvents[i];
            FromCharacter fromCharacter = fromCharacters[i];

            string messageText = chatMessage.MessageText.Value;

            if (Transport.HasPacketPrefix(messageText))
            {
                OnClientPacketReceived(messageText);
                entity.Destroy(true);
                continue;
            }

            VChatEvent vChatEvent = new(fromCharacter.User, fromCharacter.Character, messageText, chatMessage.MessageType);
            BloodstonePlugin.Logger.LogInfo($"[Chat] [{vChatEvent.Type}] {vChatEvent.User.CharacterName}: {vChatEvent.Message}");

            try
            {
                OnChatMessageHandler?.Invoke(vChatEvent);
                if (vChatEvent.Cancelled)
                    entity.Destroy(true);
            }
            catch (Exception ex)
            {
                BloodstonePlugin.Logger.LogError("Error dispatching chat event:");
                BloodstonePlugin.Logger.LogError(ex);
            }
        }
    }
}
public static class ChatMessageSystemClientPatch
{
    static Harmony? _harmony;
    public static void Initialize()
    {
        if (_harmony != null)
            throw new Exception("Detour already initialized. You don't need to call this. The Bloodstone plugin will do it for you.");

        _harmony = Harmony.CreateAndPatchAll(typeof(ChatMessageSystemClientPatch), MyPluginInfo.PLUGIN_GUID);
    }
    public static void Uninitialize()
    {
        if (_harmony == null)
            throw new Exception("Detour wasn't initialized. Are you trying to unload Bloodstone twice?");

        _harmony.UnpatchSelf();
    }

    [HarmonyPatch(typeof(ClientChatSystem), nameof(ClientChatSystem.OnUpdate))]
    [HarmonyPrefix]
    public static void OnUpdatePrefix(ClientChatSystem __instance)
    {
        using NativeAccessor<Entity> entities = __instance._ReceiveChatMessagesQuery.ToEntityArrayAccessor();
        using NativeAccessor<ChatMessageServerEvent> chatMessageServerEvents = __instance._ReceiveChatMessagesQuery.ToComponentDataArrayAccessor<ChatMessageServerEvent>();

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            ChatMessageServerEvent chatMessage = chatMessageServerEvents[i];

            string messageText = chatMessage.MessageText.Value;

            if (Transport.HasPacketPrefix(messageText))
            {
                OnServerPacketReceived(messageText);
                entity.Destroy(true);
            }
        }
    }
}

/// <summary>
/// Represents a chat message sent by a user.
/// </summary>
public class VChatEvent
{
    /// <summary>
    /// The user entity of the user that sent the message. This contains the `User` component.
    /// </summary>
    public Entity SenderUserEntity { get; }
    /// <summary>
    /// The character entity of the user that sent the message. This contains the character
    /// instances, such as its position, health, etc.
    /// </summary>
    public Entity SenderCharacterEntity { get; }
    /// <summary>
    /// The message that was sent.
    /// </summary>
    public string Message { get; }
    /// <summary>
    /// The type of message that was sent.
    /// </summary>
    public ChatMessageType Type { get; }

    /// <summary>
    /// Whether this message was cancelled. Cancelled messages will not be
    /// forwarded to the normal VRising chat system and will not be sent to
    /// any other clients. Use the Cancel() function to set this flag. Note
    /// that cancelled events will still be forwarded to other plugins that
    /// have subscribed to this event.
    /// </summary>
    public bool Cancelled { get; set; } = false;

    /// <summary>
    /// The user component instance of the user that sent the message.
    /// </summary>
    public User User => VWorld.Server.EntityManager.GetComponentData<User>(SenderUserEntity);
    internal VChatEvent(Entity userEntity, Entity characterEntity, string message, ChatMessageType type)
    {
        SenderUserEntity = userEntity;
        SenderCharacterEntity = characterEntity;
        Message = message;
        Type = type;
    }

    /// <summary>
    /// Cancel this message. Cancelled messages will not be forwarded to the
    /// normal VRising chat system and will not be sent to any other clients.
    /// Note that cancelled events will still be forwarded to other plugins 
    /// that have subscribed to this event.
    /// </summary>
    public void Cancel()
    {
        Cancelled = true;
    }
}