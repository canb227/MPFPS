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
    public static event Action OnPackageOrdersUpdated;
    public static event Action OnPossibleAddressesUpdated;
    //

    public ItemSpawnManager itemSpawnManager = new();
    public Dictionary<ulong, BasicPlayerCharacter> basicPlayers = new(); //added to when the object is created, so only make a player character once per player
    public Dictionary<ulong, Ghost> ghostPlayers = new(); //added to when the object is created, so only make a player character once per player
    public List<ulong> disconnectedPlayers = new();
    public List<PackageOrderInfo> packageOrders = new();
    
    public List<string> possibleRoundAddressNumbers = new();
    public List<string> possibleRoundAddressStreets = new();
    public List<string> possibleRoundAddressSuffixes = new();
    public Dictionary<GameObjectType, int> minimumItemTypeCount = new();


   
    /// <summary>
    /// Our current local understanding of gameState options
    /// </summary>
    public GameModeOptions options = new();

    public double remainingRoundTime;
    private int numTraitorsAlive;
    private int numInnocentsAlive;
    private int numManagersAlive;
    private int totalPlayers;
    private int numFinishedOrders;
    private int ordersNeeded;

    //This event fires whenever GameStateOptions change. Subscribe with GameState.GameStateOptionsReceivedEvent += MyFuncNameHere;
    public delegate void GameModeOptionsReceived(GameModeOptions options, ulong sender);
    public static event GameModeOptionsReceived GameModeOptionsReceivedEvent;

    public int roundNumber = 0;

    public override void _Ready()
    {
        Logging.Log($"Starting Game Mode manager", "GameModeManager");
        Lobby.NewLobbyPeerAddedEvent += OnNewLobbyPeerAdded;
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

    public void PushGameStateOptions()
    {
        byte[] payload = MessagePackSerializer.Serialize(options);
        Global.network.BroadcastData(payload, Channel.GameStateOptions, Global.Lobby.lobbyPeers.ToList());
    }

    public async void GameStartAsHost()
    {
        Logging.Log($"Starting server-side game mode init", "GameModeManager");
        await ToSignal(GetTree().CreateTimer(options.newRoundDelay), SceneTreeTimer.SignalName.Timeout);
        RPCManager.RPC(this, "StartNewRound", []);

        await ToSignal(GetTree().CreateTimer(options.roleAssignmentDelay), SceneTreeTimer.SignalName.Timeout);
        AssignRoles();


    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public async void TraitorsWin()
    {
        Logging.Log("Traitors Win As Peer", "GameModeManager");
        Global.ui.inGameUI.ShowRoundReport(Team.Traitor);
        await ToSignal(GetTree().CreateTimer(options.newRoundDelay), SceneTreeTimer.SignalName.Timeout);
        StartNewRound();
    }
    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public async void InnocentsWin()
    {
        Logging.Log("Innocents Win As Peer", "GameModeManager");
        Global.ui.inGameUI.ShowRoundReport(Team.Innocent);
        //display a UI element, play a sound or music? then start the countdown for a new round
        await ToSignal(GetTree().CreateTimer(options.newRoundDelay), SceneTreeTimer.SignalName.Timeout);
        StartNewRound();
    }
    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public async void ForceEndRound()
    {
        Logging.Log("ForceEndRound as Peer", "GameModeManager");
        Global.ui.inGameUI.ShowRoundReport(Team.None);
        //display a UI element, play a sound or music? then start the countdown for a new round
        await ToSignal(GetTree().CreateTimer(options.newRoundDelay), SceneTreeTimer.SignalName.Timeout);
        StartNewRound();
    }
    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void StartEmergencyEvacuation()
    {
        Logging.Log("Start Emergency Evacuation as Peer", "GameModeManager");
    }
    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void StartEndOfGameEvacuation()
    {
        Logging.Log("Start End of Game Evacuation as Peer", "GameModeManager");
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public async void StartNewRound()
    {
        if (roundNumber == 0)
        {
            Logging.Log("Starting First Round as Peer", "GameModeManager");
            RPCManager.RPC(Global.gameState.GetCharacterControlledBy(Global.steamid), "ReleaseControl", []);
            SpawnAndControlNewLocalPlayerCharacter(GameObjectType.BasicPlayer);
            SpawnCharacterStartingInventory(Global.gameState.GetCharacterControlledBy(Global.steamid));
        }
        else
        {
            //Logging.Log("Cleaning up previous round as Peer if needed, done as a map reload later?", "GameModeManager");
            //GOBasePlayerCharacter pc = Global.gameState.GetCharacterControlledBy(Global.steamid);
            //RPCManager.RPC(pc, "ReleaseControl", []);
            //foreach (BasicPlayerCharacter basicPlayer in basicPlayers.Values)
            //{
            //    Global.gameState.GameObjects.Remove(basicPlayer.id);
            //    basicPlayer.QueueFree();
            //}
            //basicPlayers.Clear();

            //Logging.Log("Starting New Round as Peer", "GameModeManager");
            ////RPCManager.RPC(pc, "ReleaseControl", []);
            ///
            // foreach (BasicPlayerCharacter basicPlayer in basicPlayers.Values)
            // {
            //     basicPlayer.ReleaseControl();
            // }
            basicPlayers.Clear();
            Global.gameState.ResetGameState();
            await MapManager.ResetMap();

            SpawnAndControlNewLocalPlayerCharacter(GameObjectType.BasicPlayer);
            SpawnCharacterStartingInventory(Global.gameState.GetCharacterControlledBy(Global.steamid));
        }
        roundNumber++;
        //clear the scoreboard , role assignment comes later
        Global.ui.inGameUI.RoundReport.NewRound();
        Global.ui.inGameUI.ScoreBoard.NewRound();
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
        packageOrders.Clear();

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

        //RPCManager.RPC(this, "SetPossibleRoundAddresses", new List<object> {possibleRoundAddressNumbers, possibleRoundAddressStreets, possibleRoundAddressSuffixes}); //THIS NEEDS TO BE RPC'd TODO
        OnPossibleAddressesUpdated?.Invoke(); //remove this once we fix the RPC


        ordersNeeded = 4; //determine this dynamically or via some pre-set scale (update the Take value above too)


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
            int randomizer = rand.Next(3)-1; //between 0 and 2 (-1) -1 to 1
            for (int j = 0; j < options.itemsPerPackage+randomizer; j++)
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
        RPCManager.RPC(this, "SetPackageOrders", [ packageOrders.ToList() ]); //THIS NEEDS TO BE RPC'd TODO
        //OnPackageOrdersUpdated?.Invoke(); //remove this once we fix the RPC
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

        int numPlayers = players.Count;
        int numTraitors = Mathf.FloorToInt(numPlayers * options.percentTraitors);
        int numManagers = Mathf.FloorToInt(numPlayers * options.percentManagers);
        if (options.manualOverride)
        {
            numTraitors = options.manualTraitorCount;
            numManagers = options.manualManagerCount;
        }     
        Logging.Log($"Out of {numPlayers} players, {numTraitors} will be picked as traitors", "GameModeManager");
        for (int i = 0; i < numTraitors; i++)
        {
            ulong selectedID = players[Random.Shared.Next(numPlayers)];
            players.Remove(selectedID);
            traitors.Add(selectedID);
        }
        numTraitorsAlive = numTraitors;

        Logging.Log($"Out of {numPlayers} players, {numManagers} will be picked as managers", "GameModeManager");
        for (int i = 0; i < numManagers; i++)
        {
            ulong selectedID = players[Random.Shared.Next(numPlayers)];
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
            pa.role = Role.Normal;
            byte[] data = MessagePackSerializer.Serialize(pa);
            RPCManager.RPC(this, "AssignRole", [id, Team.Traitor, Role.Normal]);
        }

        foreach (ulong id in managers)
        {
            PlayerAssignment pa = new();
            pa.id = id;
            pa.team = Team.Manager;
            pa.role = Role.Normal;
            byte[] data = MessagePackSerializer.Serialize(pa);
            RPCManager.RPC(this, "AssignRole", [id, Team.Manager, Role.Normal]);
        }

        foreach (ulong id in players)
        {
            PlayerAssignment pa = new();
            pa.id = id;
            pa.team = Team.Innocent;
            pa.role = Role.Normal;
            byte[] data = MessagePackSerializer.Serialize(pa);
            RPCManager.RPC(this, "AssignRole", [id, Team.Innocent, Role.Normal]);
        }
        if(numPlayers == 0)
        {
            RPCManager.RPC(this, "ForceEndRound", []);
        }
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void AssignRole(ulong id, Team team, Role role)
    {
        Logging.Log($"Player {id} has been assigned team:{team} and role:{role}", "GameModeManager");
        basicPlayers[id].Assignment(team, role);
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
            Global.ui.inGameUI.PlayerUIManager.UpdateRoleUI(team);
        }
    }

    public int GetNumFinishedOrders()
    {
        return numFinishedOrders;
    }
    public void SetNumFinishedOrders(int numFinished)
    {
        numFinishedOrders = numFinished;
        if (numFinishedOrders >= ordersNeeded)
        {
            StartEndOfGameEvacuation();
        }
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
            if (numTraitorsAlive <= 0)
            {
                //do something maybe
            }
            else if ((numInnocentsAlive + numManagersAlive + numTraitorsAlive) / totalPlayers < 0.34f)
            {
                RPCManager.RPC(this, "StartEmergencyEvacuation", []);
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
            else if ((numInnocentsAlive + numManagersAlive + numTraitorsAlive) / totalPlayers < 0.34f)
            {
                RPCManager.RPC(this, "StartEmergencyEvacuation", []);
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
            else if ((numInnocentsAlive + numManagersAlive + numTraitorsAlive) / totalPlayers < 0.34f)
            {
                RPCManager.RPC(this, "StartEmergencyEvacuation", []);
            }
        }
    }

    public void DecreaseNumManagersAlive()
    {
        SetNumManagersAlive(numManagersAlive - 1);
    }

    public void CharacterDied(Team team)
    {
        Logging.Log("A Character has died", "GameModeManager");
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
    }

    internal void StartGameMode(string scenePath, GameModeType gameMode)
    {

        switch (gameMode)
        {
            case GameModeType.TTT:
                Global.ui.ToGameUI();

                SpawnAndControlNewLocalPlayerCharacter(GameObjectType.Ghost);

                Global.ui.StopLoadingScreen();
                break;
            default:
                Logging.Error($"Unknown game mode - cannot start game!", "GameModeManager");
                break;
        }
    }

    public void SpawnAndControlNewLocalPlayerCharacter(GameObjectType pcType)
    {
        if (GameObjectLoader.LoadObjectByType(pcType) is GOBasePlayerCharacter sd)
        {
            GameObjectConstructorData data = new GameObjectConstructorData();
            data.spawnTransform = MapManager.GetPlayerSpawnTransform();
            data.id = Global.gameState.GenerateNewID();
            data.authority = Global.steamid;
            data.type = pcType;
            List<Object> paramList = new List<Object>();
            data.paramList = paramList;
            Global.gameState.Auth_SpawnObject(pcType, data);
        }
        else
        {
            Logging.Error($"Provided object type to spawn as player must be base player derived object", "GameState");
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
    None,
    Normal,

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