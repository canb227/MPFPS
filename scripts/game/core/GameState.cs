using Godot;
using ImGuiNET;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

/// <summary>
/// Struct that defines a single network message that updates the state of a single IGameObject.
/// </summary>
[MessagePackObject]
public struct StateUpdatePacket
{
    /// <summary>
    /// The ID of the object to update
    /// </summary>
    [Key(0)]
    public ulong objectID;

    /// <summary>
    /// Byte array that contains the state data for the object.
    /// </summary>
    [Key(1)]
    public byte[] data;

    /// <summary>
    /// IGameObject Type enum value - must match type of object with the given ID
    /// </summary>
    [Key(2)]
    public GameObjectType type;

    /// <summary>
    /// Auto populated with the current tick when sent.
    /// </summary>
    [Key(3)]
    public ulong tick;

    /// <summary>
    /// auto populated with the local user's steamID when sent
    /// </summary>
    [Key(4)]
    public ulong sender;

    public StateUpdatePacket(ulong id, byte[] data, GameObjectType type)
    {
        this.objectID = id;
        this.data = data;
        this.type = type;
        this.tick = Global.gameState.tick;
        this.sender = Global.steamid;
    }
}

/// <summary>
/// Class (TODO: change to record?) that holds a single player's input data. This gets sent once per frame to all peers.
/// </summary>
[MessagePackObject]
public partial class PlayerInputData
{
    [Key(0)]
    public ulong playerID;

    [Key(1)]
    public Vector2 MovementInputVector;

    [Key(2)]
    public Vector2 LookInputVector;

    [Key(3)]
    public Dictionary<string, bool> inputs = new();
}

[MessagePackObject]
public class PlayerData
{
    [Key(0)]
    public ulong playerID;

}

[MessagePackObject]
public class GameStateOptions
{
    [Key(0)]
    public string selectedMapScenePath = "res://scenes/world/debugPlatform.tscn";

    [Key(1)]
    public bool debugMode = false;
}

/// <summary>
/// Central game management singleton. Handles core game processing loop, player input routing, object spawning, network sync, and other stuff
/// </summary>
public partial class GameState : Node3D
{
    /// <summary>
    /// List of all locally tracked game objects. Keyed by object ID for quick lookup. If in object isn't in this dictionary its completely cut off from all of the logic management and networking.
    /// </summary>
    public Dictionary<ulong, IGameObject> GameObjects = new();

    /// <summary>
    /// List of references to each player's current input data. Keyed by playerID (SteamID). Our local input ends up in this dictionary under our ID.
    /// </summary>
    public Dictionary<ulong, PlayerInputData> PlayerInputs = new();

    /// <summary>
    /// List of all player characters for easy access - be sure to update this if you reroute a player's input to a different character. Keyed by playerID (SteamID).
    /// </summary>
    public Dictionary<ulong, GOBasePlayerCharacter> PlayerCharacters = new();

    /// <summary>
    /// Player data list, keyed by playerID (SteamID). Stores various tidbits about all of our peers (including ourselves)
    /// </summary>
    public Dictionary<ulong, PlayerData> PlayerData = new();


    /// <summary>
    /// State update buffer - will one day help smooth inconsistent network performance. Doesn't really do anything yet.
    /// </summary>
    public Queue<StateUpdatePacket> StateUpdatePacketBuffer = new();

    /// <summary>
    /// Player input buffer - will one day help smooth inconsistent network performance. Doesn't really do anything yet.
    /// </summary>
    public Queue<PlayerInputData> PlayerInputPacketBuffer = new();

    /// <summary>
    /// How many Physics frames have passed since the game started. We are co-opting Godot Physics tick rate as the networking tick rate.
    /// </summary>
    public ulong tick = 0;

    public GameStateOptions options = new();

    public delegate void GameStateOptionsReceived(GameStateOptions options, ulong sender);
    public static event GameStateOptionsReceived GameStateOptionsReceivedEvent;

    public delegate void PlayerDataReceived(PlayerData data, ulong sender);
    public static event PlayerDataReceived PlayerDataReceivedEvent;

    private Node3D nodePlayers;
    private Node3D nodeGameObjects;
    private Node3D nodeStaticLevel;
    private List<Marker3D> PlayerSpawnPoints = new();
    private IGameObject debugTarget;
    public override void _Ready()
    {
        //Create some little organizers for our scenetree to help with debugging
        nodePlayers = new Node3D();
        nodePlayers.Name = "Players";
        AddChild(nodePlayers);

        nodeGameObjects = new Node3D();
        nodeGameObjects.Name = "GameObjects";
        AddChild(nodeGameObjects);

        //Add a little guy that plugs in to the Godot sceneTree so it can collect our local input.
        PlayerInputHandler PIH = new PlayerInputHandler();
        PIH.Name = "LocalInput";
        AddChild(PIH);

        //Start the game paused.
        ProcessMode = ProcessModeEnum.Disabled;
    }

    /// <summary>
    /// Only for RPC use, Do not call directly. See <see cref="RPCManager.RPC_StartGame(string)"/>
    /// </summary>
    /// <param name="scenePath"></param>
    public void StartGame(string scenePath)
    {
        Global.ui.StartLoadingScreen();
        LoadStaticLevel(scenePath);
        SpawnSelf(GameObjectType.Ghost);
        Global.ui.StopLoadingScreen();

        ProcessMode = ProcessModeEnum.Pausable;
    }

    public void LoadStaticLevel(string scenePath)
    {
        if (nodeStaticLevel != null)
        {
            nodeStaticLevel.QueueFree();
            nodeStaticLevel = null;
        }
        nodeStaticLevel = ResourceLoader.Load<PackedScene>(scenePath).Instantiate<Node3D>();
        AddChild(nodeStaticLevel);
        LoadStaticLevelMetas();
    }
    
    public void LoadStaticLevelMetas()
    {
        foreach (Marker3D marker in nodeStaticLevel.GetNode("meta/playerSpawns").GetChildren())
        {
            PlayerSpawnPoints.Add(marker);
        }
    }

    public void SpawnSelf(GameObjectType pcType)
    {
        Transform3D SpawnTransform = PlayerSpawnPoints[Random.Shared.Next(PlayerSpawnPoints.Count)].GlobalTransform;
        if (GameObjectLoader.LoadObjectByType(pcType) is GOBasePlayerCharacter pc)
        {
            pc.GlobalTransform = SpawnTransform;
            pc.controllingPlayerID = Global.steamid;
            SpawnObjectAsAuth(pc, pc.type);
        }
        else
        {
            Logging.Error($"Provided object type to spawn as player must be base player derived object", "GameState");
        }
    }

    public override void _Process(double delta)
    {
        // Every frame, execute PerFrame behaviour on all registered GameObjects based on if we're the authority for the object.
        foreach (IGameObject gameObject in GameObjects.Values)
        {
            if (gameObject.authority == Global.steamid)
            {
                gameObject.PerFrameAuth(delta);
            }
            else
            {
                gameObject.PerFrameLocal(delta);
            }
        }

        if(Global.DrawDebugScreens)
        {
            GameStateDebug();
        }
    }

    private void GameStateDebug()
    {
        ImGui.Begin("GameState Debug");
        ImGui.Text($"# Players: {PlayerData.Count}");
        ImGui.Text($"# GameObjects: {GameObjects.Count}");
        ImGui.Text($"ENTITY LIST --------------------------");
        foreach (IGameObject gameObject in GameObjects.Values)
        {
            ImGui.Text($"ID:{gameObject.id} | TYPE:{gameObject.type} | AUTHORITY:{gameObject.authority}");
        }
        ImGui.End();

        if (debugTarget != null)
        {
            ImGui.Begin("Targetted Object Menu");
            ImGui.Text($"Object ID: {debugTarget.id}");
            ImGui.Text($"Object Auth: {debugTarget.authority}");
            ImGui.Text($"ObjectStateDump: {debugTarget.GenerateStateString()}");
            ImGui.End();
        }    
    }

    public override void _PhysicsProcess(double delta)
    {
        //Work thru our Queue of incoming updates first
        HandleStateUpdateQueue();
        HandleInputQueue();

        //Go to the next tick
        tick++;

        //Iterate thru all registered GameObjects based on authority.
        foreach(IGameObject gameObject in GameObjects.Values)
        {
            if (gameObject.authority==Global.steamid)
            {
                //If we're the authority, process the next tick of the object then send our peers an update on its state.
                gameObject.PerTickAuth(delta);

                //WARNING: THIS WILL SEND A NETWORK PACKET FOR EVERY SINGLE OBJECT IN THE GAME
                //TODO: Implement object priority queue here the moment there is even a whiff of performance issues

                StateUpdatePacket stateUpdate = new StateUpdatePacket(gameObject.id, gameObject.GenerateStateUpdate(), gameObject.type);
                byte[] stateData = MessagePackSerializer.Serialize(stateUpdate);
                Global.network.BroadcastData(stateData, Channel.GameObjectState, Global.Lobby.AllPeersExceptSelf());
            }
            else
            {
                //if we're not the authority just run the local prediction and remediation code for the object
                gameObject.PerTickLocal(delta);
            }
        }

        //We're always the authority over our own input state, send that to all of our peers.
        var localInput = PlayerInputs[Global.steamid];
        byte[] data = MessagePackSerializer.Serialize(localInput);
        Global.network.BroadcastData(data, Channel.PlayerInput, Global.Lobby.lobbyPeers.ToList());
    }

    /// <summary>
    /// Spawn an object with authority over it and tell all your peers about it.
    /// </summary>
    /// <param name="gameObject"></param>
    public void SpawnObjectAsAuth(IGameObject gameObject, GameObjectType type)
    {
        SpawnNewObject(gameObject,GenerateNewID(),Global.steamid,type);
        StateUpdatePacket stateUpdate = new StateUpdatePacket(gameObject.id, gameObject.GenerateStateUpdate(), gameObject.type);
        byte[] stateData = MessagePackSerializer.Serialize(stateUpdate);
        Global.network.BroadcastData(stateData, Channel.GameObjectState, Global.Lobby.AllPeersExceptSelf());
    }

    /// <summary>
    /// Spawns an object without interacting with the networking sync. Use with caution!
    /// </summary>
    /// <param name="gameObject"></param>
    /// <returns>a reference to the spawned object</returns>
    private IGameObject SpawnNewObject(IGameObject gameObject, ulong id, ulong authority, GameObjectType type)
    {
        if (GameObjects.ContainsKey(id))
        {
            Logging.Error($"Error, cannot spawn object with type {gameObject.type}, id {gameObject.id} already exists", "GameState");
            return null;
        }
        if (id==0)
        {
            Logging.Error($"Error, cannot spawn object with type {gameObject.type}, invalid ID (0)!", "GameState");
            return null;
        }
        if (authority == 0 || !Global.Lobby.lobbyPeers.Contains(authority))
        {
            Logging.Error($"Error, cannot spawn object with type {gameObject.type}, invalid authority provided: {authority}!", "GameState");
            return null;
        }

        gameObject.id = id;
        gameObject.authority = authority;
        gameObject.type = type;

        GameObjects[gameObject.id] = gameObject;
        nodeGameObjects.AddChild(gameObject as Node, true);
        if (gameObject is GOBasePlayerCharacter pc)
        {
            PlayerCharacters[pc.controllingPlayerID] = pc;
        }
        return gameObject;
    }

    

    private void HandleInputQueue()
    {
        bool processInputs =  PlayerInputPacketBuffer.Count > 0;
        while (processInputs)
        {
            PlayerInputData inputData = PlayerInputPacketBuffer.Dequeue();
            if (PlayerInputs.TryGetValue(inputData.playerID, out inputData))
            {
                PlayerInputs[inputData.playerID] = inputData;
            }
            else
            {
                PlayerInputs.Add(inputData.playerID, inputData);
            }
            if (PlayerInputPacketBuffer.Count > 0)
            {
                processInputs = true;
            }
            else
            {
                processInputs = false;
            }
        }
    }

    private void HandleStateUpdateQueue()
    {
        bool processStateUpdates = StateUpdatePacketBuffer.Count > 0;
        while (processStateUpdates)
        {
            StateUpdatePacket stateUpdate = StateUpdatePacketBuffer.Dequeue();
            if (GameObjects.TryGetValue(stateUpdate.objectID, out IGameObject obj))
            {
                if (obj.authority != stateUpdate.sender)
                {
                    Logging.Error($"Peer: {stateUpdate.sender} is making claims on an object ({obj.id}) they are not authority of!", "GameState");
                    return;
                }
                if (obj.type != obj.type)
                {
                    Logging.Error($"Peer: {stateUpdate.sender} sent a state update with type mismatch on object {obj.id} (obj type: {obj.type}, packet type: {stateUpdate.type})", "GameState");
                    return;
                }
                obj.ProcessStateUpdate(stateUpdate.data);
            }
            else
            {
                IGameObject newObj = GameObjectLoader.LoadObjectByType(stateUpdate.type);
                SpawnNewObject(newObj, stateUpdate.objectID, stateUpdate.sender, stateUpdate.type);
                newObj.ProcessStateUpdate(stateUpdate.data);
            }
            if (StateUpdatePacketBuffer.Count>0)
            {
                processStateUpdates = true;
            }
            else
            {
                processStateUpdates = false;
            }
        }
    }

    public void ProcessStateUpdatePacketBytes(byte[] stateUpdatePacketBytes, ulong sender)
    {
        StateUpdatePacket stateUpdate = MessagePackSerializer.Deserialize<StateUpdatePacket>(stateUpdatePacketBytes);
        StateUpdatePacketBuffer.Enqueue(stateUpdate);
    }

    public void ProcessPlayerInputPacketBytes(byte[] playerInputBytes, ulong sender)
    {
        PlayerInputData inputData = MessagePackSerializer.Deserialize<PlayerInputData>(playerInputBytes);
        PlayerInputPacketBuffer.Enqueue(inputData);
    }

    public GOBasePlayerCharacter GetLocalPlayerCharacter()
    {
        return PlayerCharacters[Global.steamid];
    }

    /// <summary>
    /// elite collision-proof id generation scheme - patent pending
    /// </summary>
    /// <returns></returns>
    internal ulong GenerateNewID()
    {
        ulong id = (ulong)Random.Shared.NextInt64();
        while (GameObjects.ContainsKey(id))
        {
            id = (ulong)Random.Shared.NextInt64();
        }
        return id;
    }

    internal void ProcessGameStateOptionsPacketBytes(byte[] payload, ulong sender)
    {
        GameStateOptions opts = MessagePackSerializer.Deserialize<GameStateOptions>(payload);
        options = opts;
        GameStateOptionsReceivedEvent?.Invoke(options, sender);
    }

    internal void ProcessPlayerDataPacketBytes(byte[] payload, ulong sender)
    {

        PlayerData data = MessagePackSerializer.Deserialize<PlayerData>(payload);
        if (sender != data.playerID)
        {
            Logging.Error($"Peer {sender} attempted to change PlayerData of non-self! ({data.playerID})", "GameState");
        }
        PlayerData[data.playerID] = data;
        PlayerDataReceivedEvent?.Invoke(PlayerData[data.playerID], sender);
    }

    public void PushGameStateOptions()
    {
        byte[] payload = MessagePackSerializer.Serialize(options);
        Global.network.BroadcastData(payload, Channel.GameStateOptions, Global.Lobby.lobbyPeers.ToList());
    }

    public void PushLocalPlayerData()
    {
        byte[] payload = MessagePackSerializer.Serialize(PlayerData[Global.steamid]);
        Global.network.BroadcastData(payload, Channel.PlayerData, Global.Lobby.lobbyPeers.ToList());
    }

    internal void SetDebugTarget(IGameObject go)
    {
        debugTarget = go;
    }

    internal void DestroyAsAuth(ulong id)
    {
        if (GameObjects.TryGetValue(id,out IGameObject obj))
        {
            obj.destroyed = true;
            (obj as Node).ProcessMode = ProcessModeEnum.Disabled;
            (obj as Node3D).Visible = false;    
        }
    }
}

