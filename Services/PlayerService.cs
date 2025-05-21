using Bloodstone.API.Shared;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using static Bloodstone.API.Server.VEvents;
using static Bloodstone.API.Server.VEvents.ConnectionEventModules;
using static Bloodstone.Util.EntityQueries;

namespace Bloodstone.Services;
public static class PlayerService
{
    static EntityManager EntityManager => VWorld.EntityManager;
    public static IReadOnlyDictionary<ulong, PlayerInfo> SteamIdPlayerInfoCache => _steamIdPlayerInfoCache;
    static readonly Dictionary<ulong, PlayerInfo> _steamIdPlayerInfoCache = [];
    public static IReadOnlyDictionary<ulong, PlayerInfo> SteamIdOnlinePlayerInfoCache => _steamIdOnlinePlayerInfoCache;
    static readonly Dictionary<ulong, PlayerInfo> _steamIdOnlinePlayerInfoCache = [];
    public static IReadOnlyDictionary<string, PlayerInfo> CharacterNamePlayerInfoCache => _characterNamePlayerInfoCache;
    static readonly Dictionary<string, PlayerInfo> _characterNamePlayerInfoCache = [];
    public static IReadOnlyDictionary<string, PlayerInfo> CharacterNameOnlinePlayerInfoCache => _characterNameOnlinePlayerInfoCache;
    static readonly Dictionary<string, PlayerInfo> _characterNameOnlinePlayerInfoCache = [];

    static bool _initialized = false;
    public struct PlayerInfo(ulong steamId = default, Entity userEntity = default, Entity characterEntity = default, User user = default)
    {
        public ulong SteamId { get; set; } = steamId;
        public readonly string Name => User.CharacterName.Value;
        public readonly bool IsAdmin => User.IsAdmin;
        public readonly bool IsConnected => User.IsConnected;
        public Entity UserEntity { get; set; } = userEntity;
        public Entity CharacterEntity { get; set; } = characterEntity;
        public User User { get; set; } = user;
    }
    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        ComponentType[] userAllComponents =
        [
            ComponentType.ReadOnly(Il2CppType.Of<User>())
        ];

        QueryDesc userQueryDesc = EntityManager.CreateQueryDesc(
            allTypes: userAllComponents,
            options: EntityQueryOptions.IncludeDisabled
        );

        BuildPlayerInfoCache(userQueryDesc).Run();
        
        // subscribing to connection events
        ModuleRegistry.Subscribe<UserConnected>(OnConnect); 
        ModuleRegistry.Subscribe<UserDisconnected>(OnDisconnect);
        ModuleRegistry.Subscribe<CharacterCreated>(OnCreate);
        ModuleRegistry.Subscribe<UserKicked>(OnKick);
    }
    static IEnumerator BuildPlayerInfoCache(QueryDesc userQueryDesc)
    {
        yield return QueryResultStreamAsync(
            userQueryDesc,
            stream =>
            {
                try
                {
                    using (stream)
                    {
                        foreach (QueryResult result in stream.GetResults())
                        {
                            Entity userEntity = result.Entity;
                            User user = result.ResolveComponentData<User>();

                            PlayerInfo playerInfo = CreatePlayerInfo(userEntity, user);
                            AddOnlinePlayerInfo(playerInfo);
                        }
                    }
                }
                catch (Exception ex)
                {
                    VWorld.Log.LogWarning($"[PlayerService] BuildPlayerInfoCaches() - {ex}");
                }
            }
        );
    }
    internal static PlayerInfo CreatePlayerInfo(Entity userEntity, User user)
    {
        Entity characterEntity = user.LocalCharacter.GetEntityOnServer();
        ulong steamId = user.PlatformId;

        return new(steamId, userEntity, characterEntity, user);
    }
    internal static bool HasPlayerInfo(this ServerBootstrapSystem.ServerClient serverClient, out PlayerInfo playerInfo)
    {
        Entity userEntity = serverClient.UserEntity;
        User user = userEntity.GetUser();
        ulong steamId = user.PlatformId;
        return SteamIdPlayerInfoCache.TryGetValue(steamId, out playerInfo);
    }
    static void AddPlayerInfo(PlayerInfo playerInfo)
    {
        _steamIdPlayerInfoCache[playerInfo.SteamId] = playerInfo;
        _characterNamePlayerInfoCache[playerInfo.Name] = playerInfo;
    }
    static void AddOnlinePlayerInfo(PlayerInfo playerInfo)
    {
        _steamIdOnlinePlayerInfoCache[playerInfo.SteamId] = playerInfo;
        _characterNameOnlinePlayerInfoCache[playerInfo.Name] = playerInfo;
    }
    static void RemoveOnlinePlayerInfo(PlayerInfo playerInfo)
    {
        _steamIdOnlinePlayerInfoCache.Remove(playerInfo.SteamId);
        _characterNameOnlinePlayerInfoCache.Remove(playerInfo.Name);
    }
    public static void OnConnect(UserConnected userConnected)
    {
        AddPlayerInfo(userConnected.PlayerInfo);
        AddOnlinePlayerInfo(userConnected.PlayerInfo);
    }
    public static void OnCreate(CharacterCreated characterCreated)
    {
        AddPlayerInfo(characterCreated.PlayerInfo);
        AddOnlinePlayerInfo(characterCreated.PlayerInfo);
    }
    public static void OnDisconnect(UserDisconnected userDisconnected)
    {
        RemoveOnlinePlayerInfo(userDisconnected.PlayerInfo);
    }
    public static void OnKick(UserKicked userKicked)
    {
        RemoveOnlinePlayerInfo(userKicked.PlayerInfo);
    }
    public static bool TryGetPlayerInfo(this ulong steamId, out PlayerInfo playerInfo)
    {
        return SteamIdPlayerInfoCache.TryGetValue(steamId, out playerInfo);
    }
    public static bool TryGetPlayerInfo(this string characterName, out PlayerInfo playerInfo)
    {
        return CharacterNamePlayerInfoCache.TryGetValue(characterName, out playerInfo);
    }
}
