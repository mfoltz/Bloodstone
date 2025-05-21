using Bloodstone.API.Shared;
using Bloodstone.Services;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Network;
using System;
using System.Collections.Generic;
using Unity.Entities;
using static Bloodstone.API.Shared.VExtensions;
using static Bloodstone.Services.PlayerService;

namespace Bloodstone.API.Server;
public class VEvents
{
    public interface IGameEvent { } 
    public abstract class DynamicGameEvent : EventArgs, IGameEvent
    {
        public Entity Source { get; set; }
        public Entity? Target { get; set; }

        readonly Dictionary<Type, object> _components = [];
        public void AddComponent<T>(T component) where T : struct => _components[typeof(T)] = component;
        public bool TryGetComponent<T>(out T component) where T : struct
        {
            if (_components.TryGetValue(typeof(T), out var boxed) && boxed is T cast)
            {
                component = cast;
                return true;
            }

            component = default;
            return false;
        }
    }
    public abstract class GameEvent<T> where T : IGameEvent, new()
    {
        public delegate void EventModuleHandler(T args);
        public event EventModuleHandler? EventHandler;
        protected void Raise(T args)
        {
            EventHandler?.Invoke(args);
        }
        public void Subscribe(EventModuleHandler handler) => EventHandler += handler;
        public void Unsubscribe(EventModuleHandler handler) => EventHandler -= handler;
        public abstract void Initialize();
        public abstract void Uninitialize();
    }
    public static class ConnectionEventModules
    {
        public class UserConnected : IGameEvent
        {
            public PlayerInfo PlayerInfo { get; set; }
        }
        public class UserDisconnected : IGameEvent
        {
            public PlayerInfo PlayerInfo { get; set; }
        }
        public class CharacterCreated : IGameEvent
        {
            public PlayerInfo PlayerInfo { get; set; }
        }
        public class UserKicked : IGameEvent
        {
            public PlayerInfo PlayerInfo { get; set; }
        }
        public class UserConnectedModule : GameEvent<UserConnected>
        {
            static UserConnectedModule? _instance;
            static Harmony? _harmony;
            public override void Initialize()
            {
                _harmony = Harmony.CreateAndPatchAll(typeof(Patch), MyPluginInfo.PLUGIN_GUID);
            }
            public override void Uninitialize() => _harmony?.UnpatchSelf();
            public UserConnectedModule()
            {
                _instance = this;
                ModuleRegistry.Register(_instance);
            }
            static class Patch
            {
                [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserConnected))]
                [HarmonyPostfix]
                static void OnUserConnected(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
                {
                    if (!__instance._NetEndPointToApprovedUserIndex.TryGetValue(netConnectionId, out var userIndex)) return;
                    var client = __instance._ApprovedUsersLookup[userIndex];

                    if (!client.HasPlayerInfo(out var playerInfo)) return;
                    _instance?.Raise(new UserConnected { PlayerInfo = playerInfo });
                }
            }
        }
        public class UserDisconnectedModule : GameEvent<UserDisconnected>
        {
            static UserDisconnectedModule? _instance;
            static Harmony? _harmony;
            public override void Initialize()
            {
                _harmony = Harmony.CreateAndPatchAll(typeof(Patch), MyPluginInfo.PLUGIN_GUID);
            }
            public override void Uninitialize() => _harmony?.UnpatchSelf();
            public UserDisconnectedModule()
            {
                _instance = this;
                ModuleRegistry.Register(_instance);
            }
            static class Patch
            {
                [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserDisconnected))]
                [HarmonyPrefix]
                static void OnUserDisconnected(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
                {
                    if (!__instance._NetEndPointToApprovedUserIndex.TryGetValue(netConnectionId, out var userIndex)) return;
                    var client = __instance._ApprovedUsersLookup[userIndex];

                    if (!client.HasPlayerInfo(out var playerInfo)) return;
                    _instance?.Raise(new UserDisconnected { PlayerInfo = playerInfo });
                }
            }
        }
        public class CharacterCreatedModule : GameEvent<CharacterCreated>
        {
            static CharacterCreatedModule? _instance;
            static Harmony? _harmony;
            public override void Initialize()
            {
                _harmony = Harmony.CreateAndPatchAll(typeof(Patch), MyPluginInfo.PLUGIN_GUID);
            }
            public override void Uninitialize() => _harmony?.UnpatchSelf();
            public CharacterCreatedModule()
            {
                _instance = this;
                ModuleRegistry.Register(_instance);
            }
            static class Patch
            {
                [HarmonyPatch(typeof(HandleCreateCharacterEventSystem), nameof(HandleCreateCharacterEventSystem.CreateFadeToBlackEntity))]
                [HarmonyPostfix]
                static void OnCharacterCreated(EntityManager entityManager, FromCharacter fromCharacter)
                {
                    Entity userEntity = fromCharacter.User;
                    User user = userEntity.GetUser();

                    PlayerInfo playerInfo = CreatePlayerInfo(userEntity, user);
                    _instance?.Raise(new CharacterCreated { PlayerInfo = playerInfo });
                }
            }
        }
        public class UserKickedModule : GameEvent<UserKicked>
        {
            static UserKickedModule? _instance;
            static Harmony? _harmony;
            public override void Initialize()
            {
                _harmony = Harmony.CreateAndPatchAll(typeof(Patch), MyPluginInfo.PLUGIN_GUID);
            }
            public override void Uninitialize() => _harmony?.UnpatchSelf();
            public UserKickedModule()
            {
                _instance = this;
                ModuleRegistry.Register(_instance);
            }
            static class Patch
            {
                [HarmonyPatch(typeof(KickBanSystem_Server), nameof(KickBanSystem_Server.OnUpdate))]
                [HarmonyPrefix]
                static void OnUpdatePrefix(KickBanSystem_Server __instance)
                {
                    using NativeAccessor<KickEvent> kickEvents = __instance._KickQuery.ToComponentDataArrayAccessor<KickEvent>();

                    try
                    {
                        for (int i = 0; i < kickEvents.Length; i++)
                        {
                            KickEvent kickEvent = kickEvents[i];
                            ulong steamId = kickEvent.PlatformId;

                            if (!steamId.TryGetPlayerInfo(out PlayerInfo playerInfo)) continue;
                            _instance?.Raise(new UserKicked { PlayerInfo = playerInfo });
                        }
                    }
                    catch (Exception ex)
                    {
                        VWorld.Log.LogError($"[KickBanSystem_Server] Exception in OnUpdatePrefix: {ex}");
                    }

                }
            }
        }
    }
    public static class ModuleRegistry
    {
        static readonly Dictionary<Type, object> _modules = [];
        public static void Register<T>(GameEvent<T> module) where T : IGameEvent, new()
        {
            module.Initialize();
            _modules[typeof(T)] = module;
            // VWorld.Log.LogInfo($"[Register] Registered module for event type: {typeof(T).Name}");
        }
        public static void Subscribe<T>(Action<T> handler) where T : IGameEvent, new()
        {
            if (_modules.TryGetValue(typeof(T), out var module))
            {
                ((GameEvent<T>)module).Subscribe(handler.Invoke);
            }
            else
            {
                VWorld.Log.LogWarning($"[Subscribe] No registered module for event type! ({typeof(T).Name})");
            }
        }
        public static bool TryGet<T>(out GameEvent<T>? module) where T : IGameEvent, new()
        {
            if (_modules.TryGetValue(typeof(T), out var result))
            {
                module = (GameEvent<T>)result;
                return true;
            }

            module = default;
            return false;
        }
    }

    static bool _initialized = false;
    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        _ = new ConnectionEventModules.UserConnectedModule();
        _ = new ConnectionEventModules.UserDisconnectedModule();
        _ = new ConnectionEventModules.CharacterCreatedModule();
        _ = new ConnectionEventModules.UserKickedModule();
    }
}
