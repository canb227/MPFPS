using Godot;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
public partial class GameModeManager : Node
{
    
    GameStateOptions options;


    public override void _Ready()
    {
        Logging.Log($"Starting Game Mode manager", "GameModeManager");
        options = Global.gameState.options;
    }
    public async void GameStartAsHost()
    {
        Logging.Log($"Starting server-side game mode init", "GameModeManager");
        await ToSignal(GetTree().CreateTimer(Global.gameState.options.roleAssignmentDelay), SceneTreeTimer.SignalName.Timeout);
        AssignRoles();
    }

    private void AssignRoles()
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
        Logging.Log($"Out of {numPlayers} players, {numTraitors} will be picked as traitors","GameModeManager");
        for (int i = 0; i < numTraitors; i++)
        {
            ulong selectedID = players[Random.Shared.Next(numPlayers)];
            players.Remove(selectedID);
            traitors.Add(selectedID);
        }

        Logging.Log($"Out of {numPlayers} players, {numManagers} will be picked as managers","GameModeManager");
        for (int i = 0; i < numManagers; i++)
        {
            ulong selectedID = players[Random.Shared.Next(numPlayers)];
            players.Remove(selectedID);
            managers.Add(selectedID);
        }

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