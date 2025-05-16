using Bloodstone.Services;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using static Bloodstone.Services.PlayerService;

namespace Bloodstone.API.Shared;
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
        public event EventHandler<T>? EventHandler;
        protected void Raise(T args)
        {
            EventHandler?.Invoke(this, args);
        }
        public void Subscribe(EventHandler<T> handler) => EventHandler += handler;
        public void Unsubscribe(EventHandler<T> handler) => EventHandler -= handler;
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
        public class UserConnectedModule : GameEvent<UserConnected>
        {
            static Harmony? _harmony;
            public UserConnectedModule()
            {
                ModuleRegistry.Register(this);
            }
            public override void Initialize()
            {
                _harmony = Harmony.CreateAndPatchAll(typeof(Patch), MyPluginInfo.PLUGIN_GUID);
            }
            public override void Uninitialize() => _harmony?.UnpatchSelf();
            static UserConnectedModule? Instance => ModuleRegistry.TryGet<UserConnected>(out var module) ? module as UserConnectedModule : null;
            static class Patch
            {
                [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserConnected))]
                [HarmonyPostfix]
                static void OnUserConnected(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
                {
                    if (!__instance._NetEndPointToApprovedUserIndex.TryGetValue(netConnectionId, out var userIndex)) return;
                    var client = __instance._ApprovedUsersLookup[userIndex];
                    if (!client.HasPlayerInfo(out var playerInfo)) return;

                    Instance?.Raise(new UserConnected { PlayerInfo = playerInfo });
                }
            }
        }
        public class UserDisconnectedModule : GameEvent<UserDisconnected>
        {
            static Harmony? _harmony;
            public UserDisconnectedModule()
            {
                ModuleRegistry.Register(this);
            }
            public override void Initialize()
            {
                _harmony = Harmony.CreateAndPatchAll(typeof(Patch), MyPluginInfo.PLUGIN_GUID);
            }
            public override void Uninitialize() => _harmony?.UnpatchSelf();
            static UserDisconnectedModule? Instance => ModuleRegistry.TryGet<UserDisconnected>(out var module) ? module as UserDisconnectedModule : null;
            static class Patch
            {
                [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserDisconnected))]
                [HarmonyPrefix]
                static void OnUserDisconnected(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
                {
                    if (!__instance._NetEndPointToApprovedUserIndex.TryGetValue(netConnectionId, out var userIndex)) return;
                    var client = __instance._ApprovedUsersLookup[userIndex];
                    if (!client.HasPlayerInfo(out var playerInfo)) return;

                    Instance?.Raise(new UserDisconnected { PlayerInfo = playerInfo });
                }
            }
        }
        public class CharacterCreatedModule : GameEvent<CharacterCreated>
        {
            static Harmony? _harmony;
            public CharacterCreatedModule()
            {
                ModuleRegistry.Register(this);
            }
            public override void Initialize()
            {
                _harmony = Harmony.CreateAndPatchAll(typeof(Patch), MyPluginInfo.PLUGIN_GUID);
            }
            public override void Uninitialize() => _harmony?.UnpatchSelf();
            static CharacterCreatedModule? Instance => ModuleRegistry.TryGet<CharacterCreated>(out var module) ? module as CharacterCreatedModule : null;
            static class Patch
            {
                [HarmonyPatch(typeof(HandleCreateCharacterEventSystem), nameof(HandleCreateCharacterEventSystem.CreateFadeToBlackEntity))]
                [HarmonyPostfix]
                static void OnCharacterCreated(EntityManager entityManager, FromCharacter fromCharacter)
                {
                    var userEntity = fromCharacter.User;
                    var user = userEntity.GetUser();
                    var playerInfo = CreatePlayerInfo(userEntity, user);

                    Instance?.Raise(new CharacterCreated { PlayerInfo = playerInfo });
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
        }
        public static void Subscribe(Type eventType, Delegate handler)
        {
            if (!_modules.TryGetValue(eventType, out var module)) return;

            var subscribeMethod = module.GetType().GetMethod("Subscribe");
            subscribeMethod?.Invoke(module, [handler]);
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
    static class EventRouter
    {
        static void RegisterHandlers(object? targetInstance = null)
        {
            foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()))
            {
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!method.IsDefined(typeof(EventHandlerAttribute), inherit: true)) continue;

                    var parameters = method.GetParameters();
                    if (parameters.Length != 1) continue;

                    var paramType = parameters[0].ParameterType;
                    if (!typeof(IGameEvent).IsAssignableFrom(paramType)) continue;

                    var handlerType = typeof(EventHandler<>).MakeGenericType(paramType);
                    var methodDelegate = method.IsStatic
                        ? Delegate.CreateDelegate(handlerType, method)
                        : Delegate.CreateDelegate(handlerType, targetInstance ?? Activator.CreateInstance(method.DeclaringType!), method);

                    ModuleRegistry.Subscribe(paramType, methodDelegate);
                }
            }
        }
        static void RegisterAllModules()
        {
            var gameEventType = typeof(GameEvent<>);

            foreach (var type in AppDomain.CurrentDomain.GetAssemblies()
                     .SelectMany(asm => asm.GetTypes())
                     .Where(t => t.IsClass && !t.IsAbstract))
            {
                var baseType = type.BaseType;

                if (baseType == null || !baseType.IsGenericType) continue;

                var genericDef = baseType.GetGenericTypeDefinition();
                if (genericDef != gameEventType) continue;

                try
                {
                    Activator.CreateInstance(type); // constructor should auto-register
                }
                catch (Exception ex)
                {
                    VWorld.Log.LogWarning($"[EventRouter] Failed to instantiate module: {type.Name} - {ex}");
                }
            }
        }
        internal static void RegisterHandlesAndModules()
        {
            RegisterHandlers();
            RegisterAllModules();
        }
    }

    static bool _initialized = false;
    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        EventRouter.RegisterHandlesAndModules();
    }
}
