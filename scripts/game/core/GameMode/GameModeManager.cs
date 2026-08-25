using Godot;
using MessagePack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

public enum GameModeType
{
    None,
    TTT,
}


public partial class GameModeManager : Node
{
    //Events
    public static event Action SwarmIncoming;
    public static event Action SwarmStarted;
    public static event Action SwarmDefeated;
    public static event Action GeneratorUnderAttack;
    public static event Action GeneratorSafe;
    public static event Action EvacuationStarted;
    public static event Action EvacuationEnded;
    public static event Action OnPackageOrdersUpdated;
    public static event Action OnPossibleAddressesUpdated;
    public static event Action OnDeliveryQueueAppended;
    public static event Action PlayInfoBeep;
    public static event Action<int> OnOrderPacked;
    public static event Action<int> OnOrderLabelled;
    public static event Action<int> OnOrderReadyToDeliver;
    public static event Action<int> OnOrderFinished;


    //

    public ItemSpawnManager itemSpawnManager = new();
    public Dictionary<ulong, BasicPlayerCharacter> basicPlayers = new(); //added to when the object is created, so only make a player character once per player
    public Dictionary<ulong, Ghost> ghostPlayers = new(); //added to when the object is created, so only make a player character once per player
    public List<ulong> deadPlayers = new();
    public Dictionary<ulong, PlayerRoundStats> playerStats = new();
    public List<PackageOrderInfo> packageOrders = new();
    public Queue<int> deliveryQueue = new();
    public Dictionary<GameObjectType, int> minimumItemTypeCount = new();
    public List<string> possibleRoundAddressNumbers = new();
    public List<string> possibleRoundAddressStreets = new();
    public List<string> possibleRoundAddressSuffixes = new();

    public SwarmManager swarmManager = new();
    public bool roundStarted;
    public bool evacuationStarted;
    public GOGenerator generator;
    public GOLabelPrinter labelPrinter;
    public GOShippingTube shippingTube;
    public GOCrusher crusher;
    public Helicopter helicopter;
    
    public List<SpotLight3D> spotLights = new();
    public List<MeshInstance3D> cases = new();


   
    /// <summary>
    /// Our current local understanding of gameState options
    /// </summary>
    public GameModeOptions options = new();

    public double remainingRoundTime;
    public double publicRemainingRoundTime;
    private int numTraitorsAlive;
    private int numInnocentsAlive;
    private int numManagersAlive;
    private int numFinishedOrders;
    private int ordersNeeded;
    public int numPlayers;
    public int numTraitors;
    public int numManagers;
    public bool lightsOn;
    public string codeWords;
    public List<string> possibleCodeWords = new List<string>
    {
        "Parcel", "Crate", "Freight", "Pallet", "Dispatch", "Courier", "Dropzone",
        "Cargo", "Tracking", "Barcode", "Label", "Packing", "Shipping", "Conveyor",
        "Warehouse", "Inventory", "Carton", "Bubblewrap", "Tape", "Forklift",
        "Express", "Priority", "Sorting", "Route", "Circuit", "Servo", "Gearshift",
        "Overclock", "Nanobot", "Alloy", "Dynamo", "Volt", "Capacitor", "Gyro",
        "Firmware", "Uplink", "C4", "Sparkplug", "Ironclad", "Sentinel",
        "Titan", "Wrench", "Pineapple", "Marshmallow", "Bumblebee", "Noodle",
        "Waffles", "Stardust", "Jellybean", "Tater", "Glitter", "Banana", "Gumdrop",
        "Sprinkles", "Whisker", "Teacup", "Pancake", "Blueberry", "Firefly",
        "Pebble", "Button", "Daydream", "Bolt", "DeliveryCo", "Neon", "Flux",
        "DartGun", "Byte", "Crank", "Echo", "Snap", "Rusty", "Jet", "Lock", "Plate",
        "Patch", "Drift", "Spark", "Moon", "Rivet", "Hawk", "Drop", "Bucket",
        "Fizz", "Bubble"
    };


    //This event fires whenever GameStateOptions change. Subscribe with GameState.GameStateOptionsReceivedEvent += MyFuncNameHere;
    public delegate void GameModeOptionsReceived(GameModeOptions options, ulong sender);
    public static event GameModeOptionsReceived GameModeOptionsReceivedEvent;

    public int roundNumber = 0;
    public double evacuationTimeLeft = 95;

    public override void _Ready()
    {
        Logging.Log($"Starting Game Mode manager", "GameModeManager");
        Lobby.NewLobbyPeerAddedEvent += OnNewLobbyPeerAdded;
        Lobby.LobbyPeerRemovedEvent += OnLobbyPeerRemoved;
        AddChild(swarmManager);
    }

    public void PerTick(double delta)
    {
        if (roundStarted)
        {
            remainingRoundTime -= delta;
            publicRemainingRoundTime -= delta;
            if (evacuationStarted)
            {
                evacuationTimeLeft -= delta;
            }
            if (evacuationTimeLeft <= 0 )
            {
                if(Global.Lobby.bIsLobbyHost)
                {
                    EvacuationEnding();
                    evacuationTimeLeft = 99999;
                }
            }
            if (Global.Lobby.bIsLobbyHost && remainingRoundTime <= 0)
            {
                RPCManager.RPC(this, "StartEndOfGameEvacuation", []);
            }
            Global.ui.inGameUI.UpdateTimeLeftUI();
            if (Global.Lobby.bIsLobbyHost)
            {
                swarmManager.PerTick(delta);
            }
        }

    }

    public void ProcessGameModeOptionsPacketBytes(byte[] payload, ulong sender)
    {
        GameModeOptions opts = MessagePackSerializer.Deserialize<GameModeOptions>(payload);
        options = opts;
        GameModeOptionsReceivedEvent?.Invoke(options, sender);
    }

    private void OnNewLobbyPeerAdded(ulong newPlayerSteamID)
    {
        if (!NetworkUtils.IsMe(newPlayerSteamID))
        {
            if (Global.Lobby.bIsLobbyHost)
            {
                PushGameStateOptions();
            }
        }
    }

    private void OnLobbyPeerRemoved(ulong playerSteamID)
    {
        if(basicPlayers.Keys.Contains(playerSteamID))
        {
            Logging.Log("Force Killing Disconnected Peers Basic Player Character", "GameModeManager");
            basicPlayers[playerSteamID].KillSelf();
        }
    }

    public void PushGameStateOptions()
    {
        byte[] payload = MessagePackSerializer.Serialize(options);
        Global.network.BroadcastData(payload, Channel.GameStateOptions, Global.Lobby.lobbyPeers.ToList());
    }

    List<ulong> gameReadyClients = new();
    bool firstRun = true;
    public async void GameStartAsHost()
    {
        if(firstRun)
        {
            Logging.Log($"Waiting for all clients to be ready.", "GameModeManager");
        
            var start = Time.GetTicksMsec();
            while (Global.Lobby.AllPeersExceptSelf().Except(gameReadyClients).Any())
            {
                if (Time.GetTicksMsec() - start > 120000)
                {
                    Logging.Log("Timeout waiting for clients", "GameModeManager");
                    break;
                }
                Logging.Log("Waiting...", "GameModeManager");
                await ToSignal(GetTree().CreateTimer(1f), SceneTreeTimer.SignalName.Timeout);
            }
            firstRun = false;
        }


        Logging.Log($"Clients Ready Start Countdown to New Round.", "GameModeManager");
        await ToSignal(GetTree().CreateTimer(options.newRoundDelay), SceneTreeTimer.SignalName.Timeout);
        RPCManager.RPC(this, "StartNewRound", []);

        await ToSignal(GetTree().CreateTimer(options.roleAssignmentDelay), SceneTreeTimer.SignalName.Timeout);
        AssignRoles();
    }

    [RPCMethod(mode = RPCMode.OnlySendToAuth)]
    public void ClientReady(ulong clientID)
    {
        gameReadyClients.Add(clientID);
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public async void TraitorsWin()
    {
        Logging.Log("Traitors Win As Peer", "GameModeManager");
        roundStarted = false;
        Global.ui.inGameUI.ShowRoundReport(Team.Traitor);
        if(Global.Lobby.bIsLobbyHost)
        {
            GameStartAsHost();
        }
    }
    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public async void InnocentsWin()
    {
        Logging.Log("Innocents Win As Peer", "GameModeManager");
        roundStarted = false;
        Global.ui.inGameUI.ShowRoundReport(Team.Innocent);
        if(Global.Lobby.bIsLobbyHost)
        {
            GameStartAsHost();
        }
    }
    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public async void ForceEndRound()
    {
        Logging.Log("ForceEndRound as Peer", "GameModeManager");
        roundStarted = false;
        Global.ui.inGameUI.ShowRoundReport(Team.None);
        if (Global.Lobby.bIsLobbyHost)
        {
            GameStartAsHost();
        }
    }
    
    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void StartEmergencyEvacuation() //not used rn
    {
        Logging.Log("Start Emergency Evacuation as Peer", "GameModeManager");
    }
    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void StartEndOfGameEvacuation()
    {
        remainingRoundTime = 99999;
        publicRemainingRoundTime = 99999;
        //switch round timers everywhere to a 95 second countdown TDOD
        evacuationStarted = true;
        evacuationTimeLeft = 95;
        EvacuationStarted?.Invoke();
        if (Global.Lobby.bIsLobbyHost)
        {
            foreach(BasicPlayerCharacter bpc in basicPlayers.Values)
            {
                if(bpc.team == Team.Traitor && bpc.state == CharacterState.Living)
                {
                    bpc.TakeDamage(999, 0, PainSoundType.Generic, 0);
                }
            }
            //swarmManager.EvacuationStarted();
            if(Global.gameState.gameModeManager.options.hordeRobots)
            {
                Global.gameState.AIManager.EvacuationStarted();
            }
        }
        Logging.Log("Start End of Game Evacuation as Peer", "GameModeManager");
    }


    public void EvacuationEnding()
    {
        Logging.Log("End Evacuation as Host", "GameModeManager");
        EvacuationEnded?.Invoke();
    }

    public void EvacuationLeft(List<BasicPlayerCharacter> basicPlayerCharacters)
    {
        //determine who was on board and who wins as lobby host
        if (Global.Lobby.bIsLobbyHost)
        {
            bool traitorOnBoard = false;
            bool anybodyOnBoard = false;
            foreach (BasicPlayerCharacter basicPlayerCharacter in basicPlayerCharacters)
            {
                if (basicPlayerCharacter.team != Team.Traitor)
                {
                    anybodyOnBoard = true;
                }
                Logging.Log(basicPlayerCharacter.Name + " " + basicPlayerCharacter.id + " is Onboard", "GameModeManager");
                if (basicPlayerCharacter.team == Team.Traitor)
                {
                    traitorOnBoard = true;
                }
            }
            if (anybodyOnBoard) //anybody that isnt a traitor
            {
                RPCManager.RPC(this, "InnocentsWin", []);
            }
            else
            {
                RPCManager.RPC(this, "TraitorsWin", []);
            }
        }
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void TriggerGeneratorUnderAttack()
    {
        GeneratorUnderAttack?.Invoke();
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void TurnOffAllSpotLights()
    {
        lightsOn = false;
        foreach(var light in spotLights)
        {
            if(IsInstanceValid(light))
            {
                light.Visible = false;
            }
        }
        DisableEmission(cases);
        lightsOn = false;
    }
    

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void TurnOnAllSpotLights()
    {
        lightsOn = true;
        foreach(var light in spotLights)
        {
            if(IsInstanceValid(light))
            {
                light.Visible = true;
            }
        }
        EnableEmission(cases);
        lightsOn = true;
    }

    public void LocalPlayInfoBeep()
    {
        PlayInfoBeep?.Invoke();
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void TriggerGeneratorSafe()
    {
        GeneratorSafe?.Invoke();
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void StartNewRound()
    {
        if (roundNumber == 0)
        {
            Logging.Log("Starting First Round as Peer", "GameModeManager");
            if(Global.gameState.gameModeManager.options.hordeRobots)
            {
                Global.gameState.AIManager.NewRound();
            }
            RPCManager.RPC(Global.gameState.GetCharacterControlledBy(Global.steamid), "ReleaseControl", []);
            SpawnAndControlNewLocalPlayerCharacter(GameObjectType.BasicPlayer);
            SpawnCharacterStartingInventory(Global.gameState.GetCharacterControlledBy(Global.steamid));
        }
        else
        {
            Logging.Log("Starting New Round as Peer", "GameModeManager");
            Global.ui.inGameUI.RoundReport.NewRound();
            Global.ui.inGameUI.ScoreBoard.NewRound();
            Global.ui.inGameUI.PlayerUIManager.ClearAllInfoStrings();
            basicPlayers.Clear();
            ghostPlayers.Clear();
            playerStats.Clear();
            deadPlayers.Clear();
            packageOrders.Clear();
            
            

            minimumItemTypeCount.Clear();
            Global.gameState.ResetGameState();
            MapManager.ResetMap();
            TurnOnAllSpotLights();
            if(Global.gameState.gameModeManager.options.hordeRobots)
            {
                Global.gameState.AIManager.NewRound();
            }
            SpawnNewLocalPlayerCharacter(GameObjectType.Ghost);
            if(Global.gameState.GetCharacterControlledBy(Global.steamid) != null)
            {
                RPCManager.RPC(Global.gameState.GetCharacterControlledBy(Global.steamid), "ReleaseControl", []);
            }
            SpawnAndControlNewLocalPlayerCharacter(GameObjectType.BasicPlayer);
            SpawnCharacterStartingInventory(Global.gameState.GetCharacterControlledBy(Global.steamid));
        }
        roundNumber++;
        roundStarted = true;
        remainingRoundTime = options.roundTime;
        publicRemainingRoundTime = options.roundTime;
        evacuationStarted = false;
        evacuationTimeLeft = 9999999;
        Global.ui.inGameUI.PlayerUIManager.ClearAllInfoStrings();
        Global.ui.inGameUI.PlayerUIManager.ClearAllStatusStrings();
        Global.ui.inGameUI.PlayerUIManager.HideGeneratorHealthBar();
        Global.ui.inGameUI.PlayerUIManager.ClearInventoryUI();
        Global.ui.inGameUI.PlayerUIManager.UpdateTeamUI(Team.None, "");


        //GET THE MAP FROM MAP MANAGER OR GAMESTATE TODO RIGHT HERE BUDFindSpotLights
        spotLights = FindSpotLights(Global.gameState);
        cases = FindCases(Global.gameState);
        //clear the scoreboard , role assignment comes later
        if (Global.Lobby.bIsLobbyHost)
        {
            GenerateOrders();
            itemSpawnManager.GenerateItems(minimumItemTypeCount);
        }
    }



    private void SpawnCharacterStartingInventory(GOBasePlayerCharacter pc)
    {
        GameObjectConstructorData data = new(GameObjectType.Hands);
        data.paramList.Add(pc.id);
        Global.gameState.Auth_SpawnObject(GameObjectType.Hands, data);
    }

    public void GenerateOrders()
    {
        ordersNeeded = 8; //max 8 per round
        // if (options.usePackageOverride)
        // {
        //     ordersNeeded = options.numPackages;
        // }
        // else
        // {
        //     ordersNeeded = 8; //8 max
        // }

        //determine our possible address details
        Random rand = new();
        possibleRoundAddressNumbers = possibleAddressNumbersSuperSet
            .OrderBy(x => rand.Next())
            .Take(4)
            .ToList();

        possibleRoundAddressStreets = possibleAddressStreetsSuperSet
            .OrderBy(x => rand.Next())
            .Take(4)
            .ToList();

        possibleRoundAddressSuffixes = possibleAddressSuffixesSuperSet
            .OrderBy(x => rand.Next())
            .Take(4)
            .ToList();

        RPCManager.RPC(this, "SetPossibleRoundAddresses", [possibleRoundAddressNumbers, possibleRoundAddressStreets, possibleRoundAddressSuffixes]);
        //OnPossibleAddressesUpdated?.Invoke(); //remove this once we fix the RPC




        // we create duplicates so we keep the possibles for other uses, monitors etc
        List<string> numbers = possibleRoundAddressNumbers.ToList();
        List<string> streets = possibleRoundAddressStreets.ToList();
        List<string> suffixes = possibleRoundAddressSuffixes.ToList();
        for (int i = 0; i < ordersNeeded; i++)
        {
            if (numbers.Count == 0 || streets.Count == 0 || suffixes.Count == 0)
                break; // stop if we run out of unique options

            // Pick random index from each list
            int numIndex = rand.Next(numbers.Count);
            int streetIndex = rand.Next(streets.Count);
            int suffixIndex = rand.Next(suffixes.Count);

            string number = numbers[numIndex];
            string street = streets[streetIndex];
            string suffix = suffixes[suffixIndex];

            // Remove the used options so they can't be reused
            numbers.RemoveAt(numIndex);
            streets.RemoveAt(streetIndex);
            suffixes.RemoveAt(suffixIndex);

            //pick some random item enums
            List<GameObjectType> allPossibleTypes = GameObjectLoader.GetAllObjectsOfType(typeof(GOPackageItem));
            List<GameObjectType> randomTypes = new();
            int randomizer = 1;//rand.Next(3) - 1; //between 0 and 2 (-1) -1 to 1
            for (int j = 0; j < options.itemsPerPackage + randomizer; j++)
            {
                GameObjectType randomType = allPossibleTypes[rand.Next(allPossibleTypes.Count)];
                randomTypes.Add(randomType);

                if (minimumItemTypeCount.ContainsKey(randomType))
                    minimumItemTypeCount[randomType]++;
                else
                    minimumItemTypeCount[randomType] = 1;
            }
            // Construct your order with the chosen values
            packageOrders.Add(new PackageOrderInfo(number, street, suffix, randomTypes));
        }
        RPCManager.RPC(this, "SetPackageOrders", [ packageOrders.ToList() ]);
    }

    
    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void SetPossibleRoundAddresses(List<string> possibleRoundAddressNumbersArray, List<string> possibleRoundAddressStreetsArray, List<string> possibleRoundAddressSuffixesArray)
    {
        possibleRoundAddressNumbers = possibleRoundAddressNumbersArray;
        possibleRoundAddressStreets = possibleRoundAddressStreetsArray;
        possibleRoundAddressSuffixes = possibleRoundAddressSuffixesArray;
        OnPossibleAddressesUpdated?.Invoke();
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void SetPackageOrders(List<PackageOrderInfo> packageOrdersArray)
    {
        packageOrders = packageOrdersArray;
        OnPackageOrdersUpdated?.Invoke();
    }

    Random rng = new Random();

    string GetTwoRandomCodewords(List<string> words)
    {
        int firstIndex = rng.Next(words.Count);
        int secondIndex = rng.Next(words.Count);

        // Ensure they aren't the same
        while (secondIndex == firstIndex)
            secondIndex = rng.Next(words.Count);

        return $"{words[firstIndex]}, {words[secondIndex]}";
    }

    public void AssignRoles()
    {
        //only assign roles to living players, in case somebody dies pre-round.
        List<ulong> players = new();
        foreach(var player in basicPlayers)
        {
            if(player.Value.state == CharacterState.Living)
            {
                players.Add(player.Key);
            }
        }
        List<ulong> traitors = new();
        List<ulong> managers = new();

        codeWords = GetTwoRandomCodewords(possibleCodeWords);

        numPlayers = players.Count;
        numTraitors = Mathf.FloorToInt(numPlayers * options.percentTraitors);
        numManagers = Mathf.FloorToInt(numPlayers * options.percentManagers);
        if (options.manualTeamOverride)
        {
            numTraitors = options.manualTraitorCount;
            numManagers = options.manualManagerCount;
        }     
        Logging.Log($"Out of {numPlayers} players, {numTraitors} will be picked as traitors", "GameModeManager");
        for (int i = 0; i < numTraitors; i++)
        {
            ulong selectedID = players[Random.Shared.Next(players.Count)];
            players.Remove(selectedID);
            traitors.Add(selectedID);
        }
        numTraitorsAlive = numTraitors;

        Logging.Log($"Out of {numPlayers} players, {numManagers} will be picked as managers", "GameModeManager");
        for (int i = 0; i < numManagers; i++)
        {
            ulong selectedID = players[Random.Shared.Next(players.Count)];
            players.Remove(selectedID);
            managers.Add(selectedID);
        }
        numManagersAlive = numManagers;

        numInnocentsAlive = numPlayers - numManagers - numTraitors;

        foreach (ulong id in traitors)
        {
            PlayerAssignment pa = new();
            pa.id = id;
            pa.team = Team.Traitor;
            byte[] data = MessagePackSerializer.Serialize(pa);
            RPCManager.RPC(this, "AssignRole", [id, pa.team, pa.role, codeWords]);
        }

        foreach (ulong id in managers)
        {
            PlayerAssignment pa = new();
            pa.id = id;
            pa.team = Team.Manager;
            byte[] data = MessagePackSerializer.Serialize(pa);
            RPCManager.RPC(this, "AssignRole", [id, pa.team, pa.role, codeWords]);
        }

        foreach (ulong id in players)
        {
            PlayerAssignment pa = new();
            pa.id = id;
            pa.team = Team.Innocent;
            byte[] data = MessagePackSerializer.Serialize(pa);
            RPCManager.RPC(this, "AssignRole", [id, pa.team, pa.role, codeWords]);
        }
        if (numPlayers == 0)
        {
            RPCManager.RPC(this, "ForceEndRound", []);
        }
        //prepare the swarm manager given the roles
        swarmManager.PrepareRound(numPlayers);
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void AssignRole(ulong id, Team team, Role role, string currentCodeWords)
    {
        Logging.Log($"Player {id} has been assigned team:{team} and role:{role}", "GameModeManager");
        basicPlayers[id].Assignment(team, role);
        if (team == Team.Traitor || team == Team.Manager)
        {
            basicPlayers[id].roleCredits++;
        }
        
        if (team == Team.Traitor)
        {
            Global.ui.inGameUI.ScoreBoard.PlayerIsTraitor(id);
        }
        if (team == Team.Manager)
        {
            Global.ui.inGameUI.ScoreBoard.PlayerIsManager(id);
        }
        if (id == Global.steamid)
        {
            Global.ui.inGameUI.PlayerUIManager.UpdateTeamUI(team, currentCodeWords);
        }
    }

    public int GetNumFinishedOrders()
    {
        return numFinishedOrders;
    }
    public void SetNumFinishedOrders(int numFinished)
    {
        numFinishedOrders = numFinished;
        if(Global.Lobby.bIsLobbyHost)
        {
            RPCManager.RPC(this, "SetRoundTime", [(float)(remainingRoundTime-120), (float)(publicRemainingRoundTime-120)]);
        }
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void SetRoundTime(float roundTime, float publicRoundTime)
    {
        remainingRoundTime = roundTime;
        publicRemainingRoundTime = publicRoundTime;
    }
    public int GetNumTraitorsAlive()
    {
        return numTraitorsAlive;
    }
    public void SetNumTraitorsAlive(int numAlive)
    {
        numTraitorsAlive = numAlive;
        if (Global.Lobby.bIsLobbyHost)
        {
            Logging.Log("Checking Game Status in GameModeManager as Host", "GameModeManager");
            if (numTraitorsAlive <= 0 && !evacuationStarted)
            {
                RPCManager.RPC(this, "InnocentsWin", []);
            }
            else if ((numInnocentsAlive + numManagersAlive + numTraitorsAlive) / numPlayers < 0.34f)
            {
                //RPCManager.RPC(this, "StartEmergencyEvacuation", []);
            }
        }
    }

    public void DecreaseNumTraitorsAlive()
    {
        SetNumTraitorsAlive(numTraitorsAlive - 1);
    }

    public int GetNumInnocentsAlive()
    {
        return numInnocentsAlive;
    }
    public void SetNumInnocentsAlive(int numAlive)
    {
        numInnocentsAlive = numAlive;
        if (Global.Lobby.bIsLobbyHost)
        {
            Logging.Log("Checking Game Status in GameModeManager as Host", "GameModeManager");
            if (numInnocentsAlive + numManagersAlive <= 0)
            {
                RPCManager.RPC(this, "TraitorsWin", []);
            }
            else if ((numInnocentsAlive + numManagersAlive + numTraitorsAlive) / numPlayers < 0.34f)
            {
                //RPCManager.RPC(this, "StartEmergencyEvacuation", []);
            }
        }
    }
    
    public void DecreaseNumInnocentsAlive()
    {
        SetNumInnocentsAlive(numInnocentsAlive - 1);
    }

    public int GetNumManagersAlive()
    {
        return numManagersAlive;
    }
    public void SetNumManagersAlive(int numAlive)
    {
        numManagersAlive = numAlive;
        if (Global.Lobby.bIsLobbyHost)
        {
            Logging.Log("Checking Game Status in GameModeManager as Host", "GameModeManager");
            if (numInnocentsAlive + numManagersAlive <= 0)
            {
                RPCManager.RPC(this, "TraitorsWin", []);
            }
            else if ((numInnocentsAlive + numManagersAlive + numTraitorsAlive) / numPlayers < 0.34f)
            {
                //RPCManager.RPC(this, "StartEmergencyEvacuation", []);
            }
        }
    }

    public void DecreaseNumManagersAlive()
    {
        SetNumManagersAlive(numManagersAlive - 1);
    }

    public void CharacterDied(ulong steamID, Team team)
    {
        Logging.Log("A Character has died", "GameModeManager");
        deadPlayers.Add(steamID);
        if (team == Team.Innocent)
        {
            DecreaseNumInnocentsAlive();
        }
        else if (team == Team.Manager)
        {
            DecreaseNumManagersAlive();
        }
        else if (team == Team.Traitor)
        {
            DecreaseNumTraitorsAlive();
        }
        if(!evacuationStarted)
        {
            remainingRoundTime += options.timePerKillEdit;
        }
    }

    public void PlayerFound(ulong steamID)
    {
        publicRemainingRoundTime += options.timePerKillEdit;
    }


    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void OrderPacked(int orderNumber)
    {
        OnOrderPacked?.Invoke(orderNumber);
        packageOrders[orderNumber].isPacked = true;
    }
    
    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void OrderLabelled(int orderNumber)
    {
        OnOrderLabelled?.Invoke(orderNumber);
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void OrderReadyToShip(int orderNumber)
    {
        packageOrders[orderNumber].waitingForDelivery = true;
        deliveryQueue.Enqueue(orderNumber);
        OnDeliveryQueueAppended?.Invoke();
        OnOrderReadyToDeliver?.Invoke(orderNumber);
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void OrderFinished(int orderNumber)
    {
        packageOrders[orderNumber].OrderFinished();
        OnOrderFinished?.Invoke(orderNumber);
    }

    internal void StartGameMode(string scenePath, GameModeType gameMode)
    {
        switch (gameMode)
        {
            case GameModeType.TTT:
                Global.ui.ToGameUI();
                //await ToSignal(GetTree().CreateTimer(5), SceneTreeTimer.SignalName.Timeout);
                SpawnAndControlNewLocalPlayerCharacter(GameObjectType.Ghost);

                //Global.ui.StopLoadingScreen();
                break;
            default:
                Logging.Error($"Unknown game mode - cannot start game!", "GameModeManager");
                break;
        }
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void TriggerSwarmIncomingEvent()
    {
        SwarmIncoming?.Invoke();
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void TriggerSwarmStartedEvent()
    {
        GD.Print("RPC swarm started trigger");
        SwarmStarted?.Invoke();
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void TriggerSwarmDefeatedEvent()
    {
        SwarmDefeated?.Invoke();
    }

    public void SpawnNewLocalPlayerCharacter(GameObjectType pcType)
    {
        Logging.Log($"Spawning local player character of type: {pcType.ToString()} without attempting to take control", "GameModeManager");
        if (GameObjectLoader.LoadObjectByType(pcType) is GOBasePlayerCharacter sd)
        {
            GameObjectConstructorData data = new GameObjectConstructorData(pcType);
            data.spawnTransform = MapManager.GetPlayerSpawnTransform();
            data.paramList.Add(false);
            Global.gameState.Auth_SpawnObject(pcType, data);
        }
        else
        {
            Logging.Error($"Provided object type to spawn as player must be base player derived object", "GameState");
        }
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void SpawnAndControlNewLocalPlayerCharacter(GameObjectType pcType)
    {
        Logging.Log($"Spawning local player character of type: {pcType.ToString()} AND attempting to take control", "GameModeManager");
        if (GameObjectLoader.LoadObjectByType(pcType) is GOBasePlayerCharacter sd)
        {
            GameObjectConstructorData data = new GameObjectConstructorData(pcType);
            
            if(sd is BasicPlayerCharacter basicPlayerCharacter)
            {
                if (Global.gameState.PlayerData[Global.steamid].role == Role.OfficeWorker)
                {
                    data.spawnTransform = MapManager.GetOfficeWorkerSpawnTransform();
                }
                else if (Global.gameState.PlayerData[Global.steamid].role == Role.WarehouseWorker)
                {
                    data.spawnTransform = MapManager.GetWarehouseWorkerSpawnTransform();
                }
                else if (Global.gameState.PlayerData[Global.steamid].role == Role.Security)
                {
                    data.spawnTransform = MapManager.GetSecuritySpawnTransform();
                }
                else
                {
                    data.spawnTransform = MapManager.GetPlayerSpawnTransform();
                }
            }
            data.paramList.Add(true);
            Global.gameState.Auth_SpawnObject(pcType, data);
        }
        else
        {
            Logging.Error($"Provided object type to spawn as player must be base player derived object", "GameState");
        }
    }

    private List<SpotLight3D> FindSpotLights(Node root)
    {
        var result = new List<SpotLight3D>();

        foreach (Node child in root.GetChildren())
        {
            if (child is SpotLight3D spot && child.Name.ToString().Contains("spot", StringComparison.OrdinalIgnoreCase))
                result.Add(spot);

            result.AddRange(FindSpotLights(child));
        }

        return result;
    }

    private List<MeshInstance3D> FindCases(Node root)
    {
        var result = new List<MeshInstance3D>();

        foreach (Node child in root.GetChildren())
        {
            if (child is MeshInstance3D mesh && child.Name.ToString().Contains("case", StringComparison.OrdinalIgnoreCase))
                result.Add(mesh);

            result.AddRange(FindCases(child));
        }
        return result;
    }

    private void EnableEmission(List<MeshInstance3D> cases)
    {
        foreach (var mesh in cases)
        {
            if(IsInstanceValid(mesh))
            {
                if (mesh.Mesh == null || mesh.Mesh.GetSurfaceCount() <= 1)
                    continue;

                var mat = mesh.GetSurfaceOverrideMaterial(1) as StandardMaterial3D;
                if (mat == null)
                    continue;

                //mat.EmissionEnabled = true;
                mat.EmissionEnergyMultiplier = 2.5f;
                mat.Emission = new Color(1,1,1);
            }
        }
    }



    private void DisableEmission(List<MeshInstance3D> cases)
    {
        foreach (var mesh in cases)
        {
            if(IsInstanceValid(mesh))
            {
                if (mesh.Mesh == null || mesh.Mesh.GetSurfaceCount() <= 1)
                    continue;

                var mat = mesh.GetSurfaceOverrideMaterial(1) as StandardMaterial3D;
                if (mat == null)
                    continue;

                //mat.EmissionEnabled = false;
                mat.EmissionEnergyMultiplier = 0.3f;
                mat.Emission = new Color(.4f,0,0);
            }
        }
    }




    public readonly static List<string> possibleAddressNumbersSuperSet = new()
    {
        "101", "123", "145", "200", "256",
        "300", "325", "400", "450", "500",
        "612", "700", "742", "800", "850",
        "900", "950", "1000", "1105", "1200", 
    };

    public readonly static List<string> possibleAddressStreetsSuperSet = new()
    {
        "Main", "Oak", "Pine", "Maple", "Cedar",
        "Elm", "Walnut", "Chestnut", "Birch", "Willow",
        "Highland", "Riverside", "Park", "Hillcrest", "Sunset",
        "Valley", "Forest", "Lakeview", "Broadway", "Washington"
    };

    public readonly static List<string> possibleAddressSuffixesSuperSet = new()
    {
        "Street", "Avenue", "Road", "Boulevard", "Lane",
        "Drive", "Court", "Circle", "Terrace", "Place",
        "Way", "Trail", "Parkway", "Square", "Loop",
        "Crescent", "Highway", "Row", "Alley", "Commons"
    };

}

public enum Team
{
    None,
    Innocent,
    Traitor,
    Manager
}

public enum Role
{
    Security,
    OfficeWorker,
    WarehouseWorker,
}
[MessagePackObject]
public struct PlayerAssignment
{
    [Key(0)]
    public ulong id;

    [Key(1)]
    public Team team;

    [Key(2)]
    public Role role;

}