using Godot;
using ImGuiNET;
using MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


/// <summary>
/// Central game management singleton. Handles core game processing loop, player input routing, object spawning, network sync, and other stuff
/// </summary>
public partial class GameState : Node3D
{
    /// <summary>
    /// List of all locally tracked game objects. Keyed by object ID for quick lookup. If in object isn't in this dictionary its completely cut off from all of the logic management and networking.
    /// </summary>
    public Dictionary<ulong, GameObject> GameObjects = new();

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

    /// <summary>
    /// Our current local understanding of gameState options
    /// </summary>
    public GameStateOptions options = new();

    /// <summary>
    /// true if the game has actually started
    /// </summary>
    public bool gameStarted = false;

    //This event fires whenever GameStateOptions change. Subscribe with GameState.GameStateOptionsReceivedEvent += MyFuncNameHere;
    public delegate void GameStateOptionsReceived(GameStateOptions options, ulong sender);
    public static event GameStateOptionsReceived GameStateOptionsReceivedEvent;

    //This event fires whenever a player's data changes. Subscribe with GameState.PlayerDataReceivedEvent += MyFuncNameHere;
    public delegate void PlayerDataReceived(PlayerData data, ulong sender);
    public static event PlayerDataReceived PlayerDataReceivedEvent;

    //public tracking vars
    private Node3D nodePlayers;
    private Node3D nodeGameObjects;
    private Node3D nodeStaticLevel;
    private List<Marker3D> PlayerSpawnPoints = new();
    private GameObject debugTarget;
    private int numUpdatesPerFrame = 10;

    //runs after GameState gets added to scenetree during Main.cs init
    public override void _Ready()
    {
        //Create some little organizers for our scenetree
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

        PlayerData[Global.steamid] = new PlayerData();
        PlayerData[Global.steamid].playerID = Global.steamid;
        //Start the game paused.
        ProcessMode = ProcessModeEnum.Disabled;
    }

    //runs once per frame
    public override void _Process(double delta)
    {
        // Every frame, execute PerFrame behaviour on all registered GameObjects based on if we're the authority for the object.
        foreach (GameObject gameObject in GameObjects.Values)
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

        // Draw ImGUI debug screens if they are on
        if(Global.DrawDebugScreens)
        {
            GameStateDebug();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Dictionary<ulong, float> topObjects = new();
 
        //Work thru our Queue of incoming updates first
        HandleStateUpdateQueue();
        HandleInputQueue();

        //Go to the next tick
        tick++;

        //Iterate thru all registered GameObjects based on authority.
        foreach(GameObject gameObject in GameObjects.Values)
        {
            if (gameObject.authority==Global.steamid)
            {
                //If we're the authority, process the next tick of the object then increment its priority
                gameObject.PerTickAuth(delta);
                gameObject.priorityAccumulator += gameObject.priority;
                topObjects.Add(gameObject.id, gameObject.priorityAccumulator);
            }
            else
            {
                //if we're not the authority just run the local prediction and remediation code for the object
                gameObject.PerTickLocal(delta);
            }
        }

        var sortedDescending = topObjects.OrderByDescending(pair => pair.Value).ToList();
        bool continueUpdating = true;
        int numUpdates = 0;
        while (continueUpdating && numUpdates<numUpdatesPerFrame && numUpdates<topObjects.Count)
        {
            ulong objID = sortedDescending.First().Key;
            sortedDescending.RemoveAt(0);
            GameObjects[objID].priorityAccumulator = 0;
            byte[] upd = GameObjects[objID].GenerateStateUpdate();
            Global.network.BroadcastData(upd, Channel.GameObjectState, Global.Lobby.AllPeersExceptSelf());
            numUpdates++;
        }

        //We're always the authority over our own input state, send that to all of our peers.
        var localInput = PlayerInputs[Global.steamid];
        byte[] data = MessagePackSerializer.Serialize(localInput);
        Global.network.BroadcastData(data, Channel.PlayerInput, Global.Lobby.AllPeersExceptSelf());
    }

    /// <summary>
    /// Loads a Scene from the file system that holds a static level. Basic processing is done to fetch various nodes we expect to see in the level <see cref="LoadStaticLevelMetas"/>
    /// </summary>
    /// <param name="scenePath"></param>
    public void LoadStaticLevel(string scenePath)
    {
        Logging.Log($"Loading static level from scene at path: {scenePath}", "GameStateLevel");
        if (nodeStaticLevel != null)
        {
            nodeStaticLevel.QueueFree();
            nodeStaticLevel = null;
        }
        nodeStaticLevel = ResourceLoader.Load<PackedScene>(scenePath).Instantiate<Node3D>();
        AddChild(nodeStaticLevel);
        LoadStaticLevelMetas();
        LoadStaticLevelGameObjects();
    }

    private void LoadStaticLevelGameObjects()
    {
        if (Global.steamid == Global.Lobby.LobbyHostSteamID)
        {
            foreach (Node node in nodeStaticLevel.GetNode("GameObjects").GetChildren())
            {
                if (node is GameObject obj)
                {
                    RegisterNewObject(obj, GenerateNewID(), Global.steamid, obj.type);
                }
            }
        }
    }

    /// <summary>
    /// Parse the loaded static level and try to find some useful stuff that may or may not be there.
    /// </summary>
    public void LoadStaticLevelMetas()
    {
        //TODO: Establish a static level meta contract for expected nodes
        Logging.Log($"Attempting to find meta nodes in static level...", "GameStateLevel");
        Node meta = nodeStaticLevel.GetNode("meta");
        if (meta==null)
        {
            Logging.Warn("Static level has no top-level \"meta\" node! Skipping meta node init","GameStateLevel");
            return;
        }

        if (meta.GetNode("playerSpawns")!=null)
        {

            foreach (Marker3D marker in nodeStaticLevel.GetNode("meta/playerSpawns").GetChildren())
            {
                PlayerSpawnPoints.Add(marker);
            }
            Logging.Log($"Loaded {PlayerSpawnPoints.Count} player spawn points.", "GameStateLevel");
        }
        else
        {
            Logging.Warn("Static level meta has no \"playerSpawns\" node! Skipping player spawn init", "GameStateLevel");
        }

    }

    /// <summary>
    /// Spawn the local player as a character with the given type 
    /// </summary>
    /// <param name="pcType"></param>
    public void SpawnSelf(GameObjectType pcType)
    {
        Transform3D SpawnTransform = GetPlayerSpawnTransform();
        if (GameObjectLoader.LoadObjectByType(pcType) is GOBasePlayerCharacter pc)
        {
            pc.GlobalTransform = SpawnTransform;
            pc.controllingPlayerID = Global.steamid;
            SpawnNewObject(pc, GenerateNewID(), Global.steamid, pcType);
            StateUpdatePacket stateUpdate = new StateUpdatePacket(pc.id, pc.GenerateStateUpdate(), pc.type, StateUpdateFlag.SpawnPlayer);
            byte[] stateData = MessagePackSerializer.Serialize(stateUpdate);
            Global.network.BroadcastData(stateData, Channel.GameObjectState, Global.Lobby.AllPeersExceptSelf());
        }
        else
        {
            Logging.Error($"Provided object type to spawn as player must be base player derived object", "GameState");
        }
    }

  

    /// <summary>
    /// Spawn an object with authority over it and tell all your peers about it.
    /// </summary>
    /// <param name="gameObject"></param>
    public void SpawnObjectAsAuth(GameObject gameObject, GameObjectType type)
    {
        SpawnNewObject(gameObject,GenerateNewID(),Global.steamid,type);
        StateUpdatePacket stateUpdate = new StateUpdatePacket(gameObject.id, gameObject.GenerateStateUpdate(), gameObject.type, StateUpdateFlag.Spawn);
        byte[] stateData = MessagePackSerializer.Serialize(stateUpdate);
        Global.network.BroadcastData(stateData, Channel.GameObjectState, Global.Lobby.AllPeersExceptSelf());
    }

    /// <summary>
    /// havent decided if this is gonna be a thing yet
    /// </summary>
    /// <param name="id"></param>
    public void DestroyAsAuth(ulong id)
    {
        Logging.Warn("auth destruction not yet networked", "GameState");
        if (GameObjects.TryGetValue(id, out GameObject obj))
        {
            obj.destroyed = true;
            (obj as Node).ProcessMode = ProcessModeEnum.Disabled;
            (obj as Node3D).Visible = false;
        }
    }

    public GOBasePlayerCharacter GetLocalPlayerCharacter()
    {
        return PlayerCharacters[Global.steamid];
    }

    public GOBasePlayerCharacter GetPlayerCharacter(ulong id)
    {
        return PlayerCharacters[id];
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

    /// <summary>
    /// elite collision-proof id generation scheme - patent pending
    /// </summary>
    /// <returns></returns>
    public ulong GenerateNewID()
    {
        ulong id = (ulong)Random.Shared.NextInt64();
        while (GameObjects.ContainsKey(id))
        {
            id = (ulong)Random.Shared.NextInt64();
        }
        return id;
    }

    //Private and internal API functions below
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Spawns an object without interacting with the networking sync. Use with caution!
    /// </summary>
    /// <param name="gameObject"></param>
    /// <returns>a reference to the spawned object</returns>
    private GameObject SpawnNewObject(GameObject gameObject, ulong id, ulong authority, GameObjectType type)
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

    private GameObject RegisterNewObject(GameObject gameObject, ulong id, ulong authority, GameObjectType type)
    {
        if (GameObjects.ContainsKey(id))
        {
            Logging.Error($"Error, cannot spawn object with type {gameObject.type}, id {gameObject.id} already exists", "GameState");
            return null;
        }
        if (id == 0)
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
        return gameObject;
    }

    private void GameStateDebug()
    {
        ImGui.Begin("GameState Debug");
        ImGui.Text($"# Players: {PlayerData.Count}");
        ImGui.Text($"# GameObjects: {GameObjects.Count}");
        ImGui.Text($"ENTITY LIST --------------------------");
        foreach (GameObject gameObject in GameObjects.Values)
        {
            if (gameObject.authority == Global.steamid)
            {
                ImGui.Text($"ID:{gameObject.id} | TYPE:{gameObject.type} | AUTHORITY:ME | PRIORITY:{gameObject.priorityAccumulator}");
            }
            else
            {
                ImGui.Text($"ID:{gameObject.id} | TYPE:{gameObject.type} | AUTHORITY:{gameObject.authority}");
            }
        }
        ImGui.End();

        ImGui.Begin("Peer Input Debug");
        ImGui.Text($"Number of peer inputs: {PlayerInputs.Count}");
        //foreach (var input in PlayerInputs)
        //{
        //    int count = 0;
        //    foreach(var action in input.Value.actions)
        //    {
        //        if (action.Value == true) count++;

        //    }
        //    ImGui.Text($"Peer: {input.Key} is pressing {count} actions");
        //}
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

    private void HandleInputQueue()
    {
        bool processInputs =  PlayerInputPacketBuffer.Count > 0;
        while (processInputs)
        {
            PlayerInputData newInputData = PlayerInputPacketBuffer.Dequeue();
            if (PlayerInputs.TryGetValue(newInputData.playerID, out PlayerInputData currentInputData))
            {
                PlayerInputs[newInputData.playerID] = newInputData;
            }
            else
            {
                PlayerInputs.Add(newInputData.playerID, newInputData);
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
            switch (stateUpdate.flag)
            {
                case StateUpdateFlag.Update:
                    if (GameObjects.TryGetValue(stateUpdate.objectID, out GameObject updateObj))
                    {
                        if (updateObj.authority != stateUpdate.sender)
                        {
                            Logging.Error($"Peer: {stateUpdate.sender} is making claims on an object ({updateObj.id}) they are not authority of!", "GameState");
                            return;
                        }
                        if (updateObj.type != updateObj.type)
                        {
                            Logging.Error($"Peer: {stateUpdate.sender} sent a state update with type mismatch on object {updateObj.id} (obj type: {updateObj.type}, packet type: {stateUpdate.type})", "GameState");
                            return;
                        }
                        updateObj.ProcessStateUpdate(stateUpdate.data);
                    }
                    else
                    {
                        Logging.Error($"DESYNC! State update for unknown object {stateUpdate.objectID}! Attempting to fix!","GameState");
                        GameObject fixObj = GameObjectLoader.LoadObjectByType(stateUpdate.type);
                        SpawnNewObject(fixObj, stateUpdate.objectID, stateUpdate.sender, stateUpdate.type);
                        fixObj.ProcessStateUpdate(stateUpdate.data);
                    }
                    break;
                case StateUpdateFlag.Spawn:
                    Logging.Log($"Auth spawn request from peer.", "GameState");
                    GameObject newObj = GameObjectLoader.LoadObjectByType(stateUpdate.type);
                    SpawnNewObject(newObj, stateUpdate.objectID, stateUpdate.sender, stateUpdate.type);
                    newObj.ProcessStateUpdate(stateUpdate.data);
                    break;
                case StateUpdateFlag.SpawnPlayer:
                    Logging.Log($"Player spawn request from peer.", "GameState");
                    GOBasePlayerCharacter pcObj = (GOBasePlayerCharacter)GameObjectLoader.LoadObjectByType(stateUpdate.type);
                    pcObj.controllingPlayerID = stateUpdate.sender;
                    PlayerCharacters[pcObj.id] = pcObj;
                    SpawnNewObject(pcObj, stateUpdate.objectID, stateUpdate.sender, stateUpdate.type);
                    pcObj.ProcessStateUpdate(stateUpdate.data);
                    break;
                case StateUpdateFlag.Destroy:
                    break;
                default:
                    break;
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

    public void ProcessGameStateOptionsPacketBytes(byte[] payload, ulong sender)
    {
        GameStateOptions opts = MessagePackSerializer.Deserialize<GameStateOptions>(payload);
        options = opts;
        GameStateOptionsReceivedEvent?.Invoke(options, sender);
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

    public void ProcessPlayerDataPacketBytes(byte[] payload, ulong sender)
    {

        PlayerData data = MessagePackSerializer.Deserialize<PlayerData>(payload);
        if (sender != data.playerID)
        {
            Logging.Error($"Peer {sender} attempted to change PlayerData of non-self! ({data.playerID})", "GameState");
        }
        PlayerData[data.playerID] = data;
        PlayerDataReceivedEvent?.Invoke(PlayerData[data.playerID], sender);
    }

    public void SetDebugTarget(GameObject go)
    {
        debugTarget = go;
    }

    private Transform3D GetPlayerSpawnTransform()
    {
        return PlayerSpawnPoints[Random.Shared.Next(PlayerSpawnPoints.Count)].GlobalTransform;
    }

    /// <summary>
    /// Only for RPC use, Do not call directly. See <see cref="RPCManager.NetCommand_StartGame(string)"/>
    /// </summary>
    /// <param name="scenePath"></param>
    public void StartGame(string scenePath)
    {
        Logging.Log($"Starting Game as char:{GameObjectLoader.GameObjectDictionary[PlayerData[Global.steamid].selectedCharacter].type.ToString()} !", "GameState");
        Global.ui.StartLoadingScreen();
        LoadStaticLevel(scenePath);
        SpawnSelf(GameObjectLoader.GameObjectDictionary[PlayerData[Global.steamid].selectedCharacter].type);
        Global.ui.StopLoadingScreen();
        gameStarted = true;
        ProcessMode = ProcessModeEnum.Pausable;
        GameModeManager gmm = new();
        gmm.Name = "Game Mode Manager";
        AddChild(gmm);
        AIManager aim = new();
        aim.Name = "AI Manager";
        AddChild(aim);
        if (Global.Lobby.bIsLobbyHost)
        {
            gmm.GameStartAsHost();
            aim.GameStartAsHost();
        }
    }
}

