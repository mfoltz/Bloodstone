using BepInEx.Logging;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using UnityEngine;

namespace Bloodstone.API.Shared;

/// <summary>
/// Various utilities for interacting with the Unity ECS world.
/// </summary>
public static class VWorld
{
    public static EntityManager EntityManager => Game.EntityManager;

    private static World? _clientWorld;
    private static World? _serverWorld;

    /// <summary>
    /// Return the Unity ECS World instance used on the server build of VRising.
    /// </summary>
    public static World Server
    {
        get
        {
            if (_serverWorld != null && _serverWorld.IsCreated)
                return _serverWorld;

            _serverWorld = GetWorld("Server")
                ?? throw new System.Exception("There is no Server world (yet). Did you install a server mod on the client?");
            return _serverWorld;
        }
    }

    /// <summary>
    /// Return the Unity ECS World instance used on the client build of VRising.
    /// </summary>
    public static World Client
    {
        get
        {
            if (_clientWorld != null && _clientWorld.IsCreated)
                return _clientWorld;

            _clientWorld = GetWorld("Client_0")
                ?? throw new System.Exception("There is no Client world (yet). Did you install a client mod on the server?");
            return _clientWorld;
        }
    }

    /// <summary>
    /// Return the default Unity ECS World instance. Both client and server use this
    /// to store some "global" systems, like the InputSystem.
    /// </summary>
    public static World Default => World.DefaultGameObjectInjectionWorld;

    /// <summary>
    /// Returns the "game" ECS world for the current instance. This will return either
    /// VWorld.Client or VWorld.Server, depending on what instance of VRising is running.
    /// </summary>
    public static World Game => IsClient ? Client : Server;

    /// <summary>
    /// Return whether we're currently running on the server build of VRising.
    /// </summary>
    public static bool IsServer => Application.productName == "VRisingServer";

    /// <summary>
    /// Return whether we're currently running on the client build of VRising.
    /// </summary>
    public static bool IsClient => Application.productName == "VRising";
    public static ManualLogSource Log => BloodstonePlugin.Logger;

    /// <summary>
    /// Local character and user entities when running on the client build of VRising. Will return Entity.Null on the server build of VRising.
    /// </summary>
    static Entity _localCharacter = Entity.Null;
    static Entity _localUser = Entity.Null;
    public static Entity LocalCharacter =>
        IsClient
        ? (_localCharacter != Entity.Null
            ? _localCharacter
            : (ConsoleShared.TryGetLocalCharacterInCurrentWorld(out _localCharacter, _clientWorld)
                ? _localCharacter
                : Entity.Null))
        : Entity.Null;
    public static Entity LocalUser =>
        IsClient
        ? (_localUser != Entity.Null
            ? _localUser
            : (ConsoleShared.TryGetLocalUserInCurrentWorld(out _localUser, _clientWorld)
                ? _localUser
                : Entity.Null))
        : Entity.Null;
    public static NetworkId LocalNetworkId => LocalUser.GetNetworkId();
    static World? GetWorld(string name)
    {
        foreach (var world in World.s_AllWorlds)
        {
            if (world.Name == name)
            {
                _serverWorld = world;
                return world;
            }
        }

        return null;
    }
}