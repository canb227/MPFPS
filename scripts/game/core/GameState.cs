using Godot;
using ImGuiNET;
using MessagePack;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;

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

    public GameObjectConstructorData(GameObjectType type)
    {
        this.type = type;
        this.id = Global.gameState.GenerateNewID();
        this.authority = Global.steamid;
        this.paramList = new();
        this.spawnTransform = Transform3D.Identity;
    }
}

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
    /// Player data list, keyed by playerID (SteamID). Stores various tidbits about all of our peers (including ourselves)
    /// </summary>
    public Dictionary<ulong, PlayerData> PlayerData = new();

    /// <summary>
    /// 
    /// </summary>
    public Dictionary<ulong, ulong> PlayerIDToControlledCharacter = new();

    /// <summary>
    /// How many Physics frames have passed since the game started. We are co-opting Godot Physics tick rate as the networking tick rate.
    /// </summary>
    public ulong tick = 0;

    /// <summary>
    /// 
    /// </summary>
    public GameModeManager gameModeManager;

    /// <summary>
    /// 
    /// </summary>
    public AIManager AIManager;

    /// <summary>
    /// true if the game has actually started
    /// </summary>
    public bool gameStarted = false;

    /// <summary>
    /// 
    /// </summary>
    public ulong defaultAuth = 0;

    //This event fires whenever a player's data changes. Subscribe with GameState.PlayerDataReceivedEvent += MyFuncNameHere;
    public delegate void PlayerDataReceived(PlayerData data, ulong sender);
    public static event PlayerDataReceived PlayerDataReceivedEvent;

    /// <summary>
    /// 
    /// </summary>
    public Node3D GameObjectNodeParent;


    private GameObject debugTarget;
    private int numUpdatesPerFrame = 20;
    private ulong StateFreshnessThreshold { get; set; } = 60;
    private Queue<StateUpdatePacket> StateUpdatePacketBuffer = new();
    private Queue<PlayerInputData> PlayerInputPacketBuffer = new();

    //runs after GameState gets added to scenetree during Main.cs init
    public override void _Ready()
    {
        GameObjectNodeParent = new Node3D();
        GameObjectNodeParent.Name = "GameObjects";
        AddChild(GameObjectNodeParent);

        //Add a little guy that plugs in to the Godot sceneTree so it can collect our local input.
        PlayerInputHandler PIH = new PlayerInputHandler();
        PIH.Name = "LocalInput";
        AddChild(PIH);

        //Start the game paused.
        ProcessMode = ProcessModeEnum.Disabled;

        Lobby.JoinedToLobbyEvent += OnJoinedToLobby;
        Lobby.NewLobbyPeerAddedEvent += OnNewLobbyPeerAdded;

        GameModeManager gmm = new();
        gmm.Name = "Game Mode Manager";
        AddChild(gmm);
        gameModeManager = gmm;

    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void StartGame(string scenePath, GameModeType gameMode)
    {
        Logging.Log($"Starting Game as char:{GameObjectLoader.GameObjectDictionary[PlayerData[Global.steamid].selectedCharacter].type.ToString()} !", "GameState");
        //Global.ui.StartLoadingScreen();
        MapManager.LoadMap(scenePath);



        AIManager aim = new();
        aim.Name = "AI Manager";
        AddChild(aim);
        AIManager = aim;

        gameModeManager.StartGameMode(scenePath, gameMode);
        gameStarted = true;

        if (Global.Lobby.bIsLobbyHost)
        {
            gameModeManager.GameStartAsHost();
            aim.GameStartAsHost();
        }
        ProcessMode = ProcessModeEnum.Pausable;
        RPCManager.RPC(gameModeManager, "ClientReady", [Global.steamid]);
    }

    public void ResetGameState()
    {
        foreach (var go in GameObjects.Values)
        {
            (go as Node).QueueFree();
        }
        foreach (var playerID in PlayerIDToControlledCharacter.Keys)
        {
            PlayerIDToControlledCharacter[playerID] = 0;
        }
        GameObjects.Clear();
    }

    private void OnNewLobbyPeerAdded(ulong newPlayerSteamID)
    {
        if (!NetworkUtils.IsMe(newPlayerSteamID))
        {
            if(newPlayerSteamID!=Global.Lobby.LobbyHostSteamID)
            {
                PushLocalPlayerData();
            }
            PlayerInputs.Add(newPlayerSteamID, new PlayerInputData());
            PlayerInputs[newPlayerSteamID].playerID = newPlayerSteamID;
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
        if (Global.DrawDebugScreens)
        {
            GameStateDebug();
        }
        
        //update time global shader parameter
        float t = Time.GetTicksMsec() / 1000.0f;
        RenderingServer.GlobalShaderParameterSet("time", t);
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
        foreach(GameObject gameObject in GameObjects.Values.ToArray())
        {
            if (gameObject.authority==Global.steamid)
            {
                //If we're the authority, process the next tick of the object then increment its priority
                gameObject.PerTickAuth(delta);
                gameObject.priorityAccumulator += gameObject.priority;

                if (!gameObject.sleeping)
                {
                    topObjects.Add(gameObject.id, gameObject.priorityAccumulator);
                }

            }
            else
            {
                //if we're not the authority just run the local prediction and remediation code for the object
                gameObject.PerTickLocal(delta);
            }
            gameObject.PerTickShared(delta);
        }
        Global.ui.PerTick(delta);
        gameModeManager.PerTick(delta);



        var sortedDescending = topObjects.OrderByDescending(pair => pair.Value).ToList();
        if (sortedDescending.Count != 0)
        {
            bool continueUpdating = true;
            int numUpdates = 0;
            while (continueUpdating && numUpdates < numUpdatesPerFrame && numUpdates < sortedDescending.Count)
            {
                ulong objID = sortedDescending.First().Key;
                sortedDescending.RemoveAt(0);
                GameObject obj = GameObjects[objID];
                obj.priorityAccumulator = 0;
                byte[] upd = obj.GenerateStateUpdate();

                StateUpdatePacket packet = new StateUpdatePacket();
                packet.type = obj.type;
                packet.objectID = objID;
                packet.sender = Global.steamid;
                packet.data = upd;
                packet.tick = tick;
                Global.network.BroadcastData(MessagePackSerializer.Serialize(packet), Channel.GameObjectState, Global.Lobby.AllPeersExceptSelf(), NetworkUtils.k_nSteamNetworkingSend_UnreliableNoNagle);
                numUpdates++;
            }
        }

        //We're always the authority over our own input state, send that to all of our peers.
        var localInput = PlayerInputs[Global.steamid];
        byte[] data = MessagePackSerializer.Serialize(localInput);
        Global.network.BroadcastData(data, Channel.PlayerInput, Global.Lobby.AllPeersExceptSelf());

        foreach( var input in PlayerInputs)
        {
           // input.Value.actions = 0;
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
        Logging.Log($"Broadcasting RPC for Local_SpawnObject for {gameObjectType}", "GameState");
        RPCManager.RPC(this, "Local_SpawnObject", [gameObjectType, MessagePackSerializer.Serialize(data)]);
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void Local_SpawnObject(GameObjectType type, byte[] _data)
    {
        
        GameObjectConstructorData data = MessagePackSerializer.Deserialize<GameObjectConstructorData>(_data);
        GameObject newObj = GameObjectLoader.LoadObjectByType(type);
        Logging.Log($"Spawning {type} on local machine with ID: {data.id} and authority {data.authority}", "GameState");
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
                    n.Name = newObj.id.ToString();
                    GameObjectNodeParent.AddChild(n, true);
                }
            }
            else
            {
                Logging.Error($"Failed to init object from data: Type:{newObj.type} Params:{string.Join(",",data.paramList)}", "GameState");
            }
        }
    }


    //Private and internal API functions below
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public GameObject Local_RegisterExistingObject(GameObject gameObject, ulong id, ulong authority, GameObjectType type)
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
    public void PushLocalPlayerData()
    {
        byte[] payload = MessagePackSerializer.Serialize(PlayerData[Global.steamid]);
        Global.network.BroadcastData(payload, Channel.PlayerData, Global.Lobby.lobbyPeers.ToList());
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

        if (debugTarget != null)
        {
            ImGui.Begin("Targetted Object Menu");
            ImGui.Text($"Object ID: {debugTarget.id}");
            ImGui.Text($"Object Auth: {debugTarget.authority}");
            ImGui.Text($"ObjectStateDump: {debugTarget.GenerateStateString()}");
            ImGui.End();
        }

        ImGui.Begin("Player Inputs");
        foreach (var entry in PlayerInputs)
        {
            ImGui.Text($"Player ID: {entry.Key} | MvInput: {entry.Value.MovementInputVector} | LkInput: {entry.Value.LookInputVector}");
            ImGui.Text($"Actions: {entry.Value.actions.ToString()}");
        }
        ImGui.End();
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
                if (updateObj.sleeping)
                {
                    Logging.Warn($"Ignoring state update for slept object {updateObj.id}", "GameState");
                    return;
                }
                updateObj.ProcessStateUpdate(stateUpdate.data.ToArray());
            }
            else
            {
                //Logging.Error($"DESYNC! State update for unknown object {stateUpdate.objectID}! Attempting to fix!","GameState");
                //GameObject fixObj = GameObjectLoader.LoadObjectByType(stateUpdate.type);
                //Local_SpawnObject(fixObj, stateUpdate.objectID, stateUpdate.sender, stateUpdate.type);
                //fixObj.ProcessStateUpdate(stateUpdate.data);
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
    private bool dedge = false;

    public void ProcessStateUpdatePacketBytes(byte[] stateUpdatePacketBytes, ulong sender)
    {
        //Logging.Log($"State Packet: {MessagePackSerializer.ConvertToJson(stateUpdatePacketBytes)}", "StateUpdatePacket");


        StateUpdatePacket stateUpdate = new();
        try
        {
            stateUpdate = MessagePackSerializer.Deserialize<StateUpdatePacket>(stateUpdatePacketBytes);
        }
        catch(Exception e)
        {
            if (!dedge)
            {
                Logging.Error($"Exception parsing state packet (SILENCING THIS ERROR): {e.ToString()}", "GameState");
                Logging.Error($"The broken packet came from sender {sender}, has a length of {stateUpdatePacketBytes.Length}", "GameState");
                dedge = true;
            }
        }
        if (dedge)
        {
            return;
        }
        StateUpdatePacketBuffer.Enqueue(stateUpdate);
        //if ((tick-stateUpdate.tick)<=StateFreshnessThreshold)
        //{
        //    StateUpdatePacketBuffer.Enqueue(stateUpdate);
        //}
        //else
        //{
        //    Logging.Log($"Got a packet that is {tick - stateUpdate.tick} ticks old! (current tick {tick}, packet tick {stateUpdate.tick}, threshold {StateFreshnessThreshold}) Discarding...", "GameState");
        //}
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

    public GOBasePlayerCharacter GetCharacterControlledBy(ulong playerID)
    {
        if (PlayerIDToControlledCharacter.TryGetValue(playerID,out ulong charID))
        {
            if (GameObjects.TryGetValue(charID, out GameObject obj))
            {
                if (obj is GOBasePlayerCharacter pc)
                {
                    return pc;
                }
            }
            else
            {
                Logging.Error($"The requested gameObject ID {charID} was not found in the GameObject dictionary!", "GameState");
            }
        }
        else
        {
            Logging.Error($"The requested playerID {playerID} was not found in the PlayerIDToCurrentlyControlledCharacter dictionary!", "GameState");
        }
        return null;
    }

    private static float VoiceMixRate = 44100;

    internal void ProcessSteamVoiceBytes(byte[] payload, ulong sender, bool alive)
    {
        if (alive)
        {
            //if the sender is alive pipe the voice audio so it comes out of their basicPlayer head
            if (GetCharacterControlledBy(sender) is BasicPlayerCharacter pc)
            {
                //Logging.Log($"processing voice data from {sender}, piping it into their playercharacter's ({pc.id}) head", "SteamVoice");
                pc.ProcessVoiceData(payload);
            }

        }
        else
        {
            //if the sender is dead
            if (GetCharacterControlledBy(Global.steamid) is Ghost ghost)
            {
                //and if we are also dead
                ////pipe the voice audio to come out of the speaker inside our own ghosty head for us to hear
                //Logging.Log($"processing (dead) voice data from {sender}, piping it into our own ghost's ({ghost.id}) head ", "SteamVoice");
                ghost.ProcessVoiceData(payload);
            }
        }
    }
}