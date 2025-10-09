using Godot;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;

public enum GameModeType
{
    None,
    TTT,
}


public partial class GameModeManager : Node
{

    GameStateOptions options;
    public Dictionary<ulong, BasicPlayer> basicPlayers = new(); //added to when the object is created, so only make a player character once per player
    public Dictionary<ulong, Ghost> ghostPlayers = new(); //added to when the object is created, so only make a player character once per player
    public List<PackageOrderInfo> packageOrders = new();

    public double remainingRoundTime;
    private int numTraitorsAlive;
    private int numInnocentsAlive;
    private int numManagersAlive;
    private int totalPlayers;


    private int numFinishedOrders;
    private int ordersNeeded;


    public override void _Ready()
    {
        Logging.Log($"Starting Game Mode manager", "GameModeManager");
        options = Global.gameState.options;
    }
    public async void GameStartAsHost()
    {
        Logging.Log($"Starting server-side game mode init", "GameModeManager");
        await ToSignal(GetTree().CreateTimer(Global.gameState.options.newRoundDelay), SceneTreeTimer.SignalName.Timeout);
        RPCManager.RPC(this, "StartNewRound", []);

        await ToSignal(GetTree().CreateTimer(Global.gameState.options.roleAssignmentDelay), SceneTreeTimer.SignalName.Timeout);
        AssignRoles();

        GenerateOrders();
    }

    public async void TraitorsWin()
    {
        //display a UI element, play a sound or music? then start the countdown for a new round
        await ToSignal(GetTree().CreateTimer(Global.gameState.options.newRoundDelay), SceneTreeTimer.SignalName.Timeout);
        StartNewRound();
    }

    public async void InnocentsWin()
    {
        //display a UI element, play a sound or music? then start the countdown for a new round
        await ToSignal(GetTree().CreateTimer(Global.gameState.options.newRoundDelay), SceneTreeTimer.SignalName.Timeout);
        StartNewRound();
    }

    public async void EndRound()
    {
        //display a UI element, play a sound or music? then start the countdown for a new round
        await ToSignal(GetTree().CreateTimer(Global.gameState.options.newRoundDelay), SceneTreeTimer.SignalName.Timeout);
        StartNewRound();
    }

    public void StartEmergencyEvacuation()
    {

    }

    public void StartEndOfGameEvacuation()
    {

    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void StartNewRound()
    {

        //I want to call respawn on the BasicPlayer that I am the authority of, but how do I find it
        basicPlayers[Global.steamid].Respawn();

        //Move my ghost to the void
        //Move my player to the correct spot
        //take control of the player character


    }

    public void GenerateOrders()
    {
        packageOrders.Clear();
        //generate and add to packageOrders
        ordersNeeded = 4;
    }

    public void AssignRoles()
    {
        List<ulong> players = Global.Lobby.lobbyPeers.ToList();
        List<ulong> traitors = new();
        List<ulong> managers = new();

        int numPlayers = players.Count;
        int numTraitors = Math.Max(Mathf.FloorToInt(numPlayers * options.percentTraitors), 1);
        int numManagers = 0;
        if (numTraitors > 1)
        {
            numManagers = 1;
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
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void AssignRole(ulong id, Team team, Role role)
    {
        Logging.Log($"Player {id} has been assigned team:{team} and role:{role}", "GameModeManager");
        Global.gameState.PlayerCharacters[id].Assignment(team, role);
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
        if (numTraitorsAlive <= 0)
        {
            //do something maybe
        }
        else if ((numInnocentsAlive + numManagersAlive + numTraitorsAlive) / totalPlayers < 0.34f)
        {
            StartEmergencyEvacuation();
        }
    }

    public int GetNumInnocentsAlive()
    {
        return numInnocentsAlive;
    }
    public void SetNumInnocentsAlive(int numAlive)
    {
        numInnocentsAlive = numAlive;
        if (numInnocentsAlive + numManagersAlive <= 0)
        {
            TraitorsWin();
        }
        else if ((numInnocentsAlive + numManagersAlive + numTraitorsAlive) / totalPlayers < 0.34f)
        {
            StartEmergencyEvacuation();
        }
    }

    public int GetNumManagersAlive()
    {
        return numManagersAlive;
    }
    public void SetNumManagersAlive(int numAlive)
    {
        numManagersAlive = numAlive;
        if (numInnocentsAlive + numManagersAlive <= 0)
        {
            TraitorsWin();
        }
        else if ((numInnocentsAlive + numManagersAlive + numTraitorsAlive) / totalPlayers < 0.34f)
        {
            StartEmergencyEvacuation();
        }
    }

    internal void StartGameMode(string scenePath, GameModeType gameMode)
    {

        switch (gameMode)
        {
            case GameModeType.TTT:
                Global.ui.ToGameUI();
                SpawnSelf(GameObjectType.Ghost); //Spawn my spectator character and hide it somewhere
                SpawnSelf(GameObjectType.BasicPlayer); //Spawn my actual character and hide it somewhere
                ghostPlayers[Global.steamid].Respawn();
                //Finish any other init type stuff
                //TakeControl of my spectator Character
                //Teleport my spectator to a good spot
                //I want to respawn my spectator to do the above

                Global.ui.StopLoadingScreen();
                break;
            default:
                Logging.Error($"Unknown game mode - cannot start game!", "GameModeManager");
                break;
        }
    }
    public void SpawnSelf(GameObjectType pcType)
    {
        if (GameObjectLoader.LoadObjectByType(pcType) is GOBasePlayerCharacter pc)
        {
            Transform3D transform = Transform3D.Identity;
            transform.Origin = new Vector3(0, -20, 0);
            GameState.GameObjectConstructorData data = new GameState.GameObjectConstructorData();
            data.spawnTransform = transform;
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