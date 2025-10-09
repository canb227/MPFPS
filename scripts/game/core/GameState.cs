using Godot;
using ImGuiNET;
using MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Lobby;


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

    public GameModeManager gameModeManager;

    public AIManager AIManager;

    /// <summary>
    /// true if the game has actually started
    /// </summary>
    public bool gameStarted = false;

    public ulong defaultAuth = 0;

    //This event fires whenever GameStateOptions change. Subscribe with GameState.GameStateOptionsReceivedEvent += MyFuncNameHere;
    public delegate void GameStateOptionsReceived(GameStateOptions options, ulong sender);
    public static event GameStateOptionsReceived GameStateOptionsReceivedEvent;

    //This event fires whenever a player's data changes. Subscribe with GameState.PlayerDataReceivedEvent += MyFuncNameHere;
    public delegate void PlayerDataReceived(PlayerData data, ulong sender);
    public static event PlayerDataReceived PlayerDataReceivedEvent;

    //public tracking vars
    private Node3D nodePlayers;
    public Node3D nodeGameObjects;
    private Node3D nodeStaticLevel;
    private List<Marker3D> PlayerSpawnPoints = new();
    private GameObject debugTarget;
    private int numUpdatesPerFrame = 20;
    private ulong staticIDCounter = 1;

    public ulong StateFreshnessThreshold { get; private set; } = 60;

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

        //Start the game paused.
        ProcessMode = ProcessModeEnum.Disabled;

        Lobby.JoinedToLobbyEvent += OnJoinedToLobby;
        Lobby.NewLobbyPeerAddedEvent += OnNewLobbyPeerAdded;

        
    }


    private void OnNewLobbyPeerAdded(ulong newPlayerSteamID)
    {
        if (!NetworkUtils.IsMe(newPlayerSteamID))
        {
            if (Global.Lobby.bIsLobbyHost)
            {
                PushGameStateOptions();
            }
            if(newPlayerSteamID!=Global.Lobby.LobbyHostSteamID)
            {
                PushLocalPlayerData();
            }
        }
    }

    private void OnJoinedToLobby(ulong hostSteamID)
    {
        defaultAuth = hostSteamID;
        PlayerData localPlayerData = new PlayerData();

        localPlayerData.playerID = Global.steamid;
        //TODO: Configure starting playerData values based on stored settings or smth
        PlayerData[Global.steamid] = localPlayerData;
        PushLocalPlayerData();
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
            gameObject.PerFrameShared(delta);
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
            gameObject.PerTickShared(delta);
        }

        //Jeffrey added this :) so the UI can process local input in the same way as other input idk
        Global.ui.PerTick(delta);

        var sortedDescending = topObjects.OrderByDescending(pair => pair.Value).ToList();
        bool continueUpdating = true;
        int numUpdates = 0;
        while (continueUpdating && numUpdates<numUpdatesPerFrame && numUpdates<topObjects.Count)
        {
            ulong objID = sortedDescending.First().Key;
            sortedDescending.RemoveAt(0);
            GameObjects[objID].priorityAccumulator = 0;
            byte[] upd = GameObjects[objID].GenerateStateUpdate();
            Global.network.BroadcastData(upd, Channel.GameObjectState, Global.Lobby.AllPeersExceptSelf(), NetworkUtils.k_nSteamNetworkingSend_UnreliableNoNagle);
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
        Global.ui.SetLoadingScreenDescription("Loading map...");
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
        Global.ui.SetLoadingScreenDescription("Loading map gameObjects...");
        foreach (Node node in Utils.GetChildrenRecursive(nodeStaticLevel, new()))
        {
            if (node is GameObject obj)
            {
                Local_RegisterExistingObject(obj, staticIDCounter++, defaultAuth, obj.type);
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

        if (GameObjectLoader.LoadObjectByType(pcType) is GOBasePlayerCharacter pc)
        {
            Transform3D SpawnTransform = GetPlayerSpawnTransform();
            GameObjectConstructorData data = new GameObjectConstructorData();
            data.spawnTransform = SpawnTransform;
            data.id = GenerateNewID();
            data.authority = Global.steamid;
            data.type = pcType;
            List<Object> paramList = new List<Object>();
            data.paramList = paramList;
            Auth_SpawnObject(pcType, data);
        }
        else
        {
            Logging.Error($"Provided object type to spawn as player must be base player derived object", "GameState");
        }
    }

    [MessagePackObject]
    public struct GameObjectConstructorData
    {
        [Key(0)]
        public ulong id;
        [Key(1)]
        public ulong authority;
        [Key(2)]
        public GameObjectType type;
        [Key(3)]
        public Transform3D spawnTransform;
        [Key(4)]
        public List<object> paramList;

        public GameObjectConstructorData(ulong id, ulong authority, GameObjectType type)
        {
            this.id = id;
            this.authority = authority;
            this.type = type;
            this.paramList = new();
            this.spawnTransform = Transform3D.Identity;
        }

        public GameObjectConstructorData (GameObjectType type)
        {
            this.type = type;
            this.id = Global.gameState.GenerateNewID();
            this.authority = Global.steamid;
            this.paramList = new();
            this.spawnTransform = Transform3D.Identity;
        }
    }

    [MessagePackObject]
    public struct GameObject2DConstructorData
    {
        [Key(0)]
        public ulong id;
        [Key(1)]
        public ulong authority;
        [Key(2)]
        public GameObjectType type;
        [Key(3)]
        public Transform2D spawnTransform;
        [Key(4)]
        public List<object> paramList;

        public GameObject2DConstructorData(ulong id, ulong authority, GameObjectType type)
        {
            this.id = id;
            this.authority = authority;
            this.type = type;
            this.paramList = new();
            this.spawnTransform = Transform2D.Identity;
        }

        public GameObject2DConstructorData (GameObjectType type)
        {
            this.type = type;
            this.id = Global.gameState.GenerateNewID();
            this.authority = Global.steamid;
            this.paramList = new();
            this.spawnTransform = Transform2D.Identity;
        }
    }


    /// <summary>
    /// Commands all clients to construct a GameObject from the given information 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="authority"></param>
    /// <param name="gameObjectType"></param>
    /// <param name="constructorParameters"></param>
    public void Auth_SpawnObject(GameObjectType gameObjectType, GameObjectConstructorData data)
    {
        if (GameObjects.ContainsKey(data.id))
        {
            Logging.Error($"Error, cannot spawn object with type {data.type}, id {data.id} already exists", "GameState");
            return;
        }
        if (data.id == 0)
        {
            Logging.Error($"Error, cannot spawn object with type {data.type}, invalid ID (0)!", "GameState");
            return;
        }
        if (data.authority == 0 || !Global.Lobby.lobbyPeers.Contains(data.authority))
        {
            Logging.Error($"Error, cannot spawn object with type {data.type}, invalid authority provided: {data.authority}!", "GameState");
            return;
        }
        RPCManager.RPC(this, "Local_SpawnObject", [gameObjectType, MessagePackSerializer.Serialize(data)]);
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void Local_SpawnObject(GameObjectType type, byte[] _data)
    {
        GameObjectConstructorData data = MessagePackSerializer.Deserialize<GameObjectConstructorData>(_data);
        GameObject newObj = GameObjectLoader.LoadObjectByType(type);
        if (newObj != null)
        {
            newObj.id = data.id;
            newObj.authority = data.authority;
            newObj.type = data.type;
            if (newObj.InitFromData(data))
            {
                if (newObj is Node n)
                {
                    GameObjects[newObj.id] = newObj;
                    nodeGameObjects.AddChild(n, true);
                }
            }
            else
            {
                Logging.Error($"Failed to init object from data: Type:{newObj.type} Params:{string.Join(",",data.paramList)}", "GameState");
            }
        }
    }

    /// <summary>
    /// havent decided if this is gonna be a thing yet
    /// </summary>
    /// <param name="id"></param>
    public void DestroyAsAuth(ulong id)
    {
        throw new NotImplementedException();
        Logging.Warn("auth destruction not yet networked", "GameState");
        if (GameObjects.TryGetValue(id, out GameObject obj))
        {
            obj.destroyed = true;
            (obj as Node).ProcessMode = ProcessModeEnum.Disabled;
            (obj as Node3D).Visible = false;
        }
    }


    //Private and internal API functions below
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private GameObject Local_RegisterExistingObject(GameObject gameObject, ulong id, ulong authority, GameObjectType type)
    {
        if (GameObjects.ContainsKey(id))
        {
            Logging.Error($"Error, cannot register object with type {gameObject.type}, id {gameObject.id} already exists", "GameState");
            return null;
        }
        if (id == 0)
        {
            Logging.Error($"Error, cannot register object with type {gameObject.type}, invalid ID (0)!", "GameState");
            return null;
        }
        if (authority == 0 || !Global.Lobby.lobbyPeers.Contains(authority))
        {
            Logging.Error($"Error, cannot register object with type {gameObject.type}, invalid authority provided: {authority}!", "GameState");
            return null;
        }
        if (gameObject is Node n)
        {
            if (!n.IsInsideTree())
            {
                Logging.Error($"Error, cannot register object that is not inside the scene tree", "GameState");
                return null;
            }
        }
        else
        {
            Logging.Error($"Error, cannot register object that is not Node", "GameState");
            return null;
        }

        gameObject.id = id;
        gameObject.authority = authority;
        gameObject.type = type;
        GameObjects[gameObject.id] = gameObject;
        return gameObject;
    }

    // stuf ==========================================================================

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

        GameModeManager gmm = new();
        gmm.Name = "Game Mode Manager";
        AddChild(gmm);
        gameModeManager = gmm;
        AIManager aim = new();
        aim.Name = "AI Manager";
        AddChild(aim);
        AIManager = aim;
        if (Global.Lobby.bIsLobbyHost)
        {
            gmm.GameStartAsHost();
            aim.GameStartAsHost();
        }
        ProcessMode = ProcessModeEnum.Pausable;
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
                        //GameObject fixObj = GameObjectLoader.LoadObjectByType(stateUpdate.type);
                        //Local_SpawnObject(fixObj, stateUpdate.objectID, stateUpdate.sender, stateUpdate.type);
                        //fixObj.ProcessStateUpdate(stateUpdate.data);
                    }
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


    //Incoming Network Message Processors ----------------------------------------------------------------------------------
    public void ProcessGameStateOptionsPacketBytes(byte[] payload, ulong sender)
    {
        GameStateOptions opts = MessagePackSerializer.Deserialize<GameStateOptions>(payload);
        options = opts;
        GameStateOptionsReceivedEvent?.Invoke(options, sender);
    }

    public void ProcessStateUpdatePacketBytes(byte[] stateUpdatePacketBytes, ulong sender)
    {
        StateUpdatePacket stateUpdate = MessagePackSerializer.Deserialize<StateUpdatePacket>(stateUpdatePacketBytes);
        if (tick-stateUpdate.tick>StateFreshnessThreshold)
        {
            StateUpdatePacketBuffer.Enqueue(stateUpdate);
        }
        else
        {
            Logging.Log($"Got a packet that is {tick - stateUpdate.tick} ticks old! Discarding...", "GameState");
        }
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


    //Helper Functions -----------------------------------------------------------------------
    public void SetDebugTarget(GameObject go)
    {
        debugTarget = go;
    }

    private Transform3D GetPlayerSpawnTransform()
    {
        return PlayerSpawnPoints[Random.Shared.Next(PlayerSpawnPoints.Count)].GlobalTransform;
    }

    public GOBasePlayerCharacter GetLocalPlayerCharacter()
    {
        return PlayerCharacters[Global.steamid];
    }

    public GOBasePlayerCharacter GetPlayerCharacter(ulong id)
    {
        return PlayerCharacters[id];
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
}

