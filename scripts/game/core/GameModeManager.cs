using Godot;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
public partial class GameModeManager : Node, GameObject
{
    
    GameStateOptions options;

    public ulong id { get; set; }
    public float priority { get; set; }
    public float priorityAccumulator { get; set; }
    public ulong authority { get; set; }
    public GameObjectType type { get; set; }
    public bool dirty { get; set; }
    public bool sleeping { get; set; }
    public bool destroyed { get; set; }
    public bool predict { get; set; }

    public override void _Ready()
    {
        Logging.Log($"Starting Game Mode manager", "GameModeManager");
        options = Global.gameState.options;
        authority = Global.Lobby.LobbyHostSteamID;
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

        int numPlayers = players.Count;
        int numTraitors = Math.Max(Mathf.FloorToInt(numPlayers * options.percentTraitors), 1);
        Logging.Log($"Out of {numPlayers} players, {numTraitors} will be picked as traitors","GameModeManager");
        for (int i = 0; i < numTraitors; i++)
        {
            ulong selectedID = players[Random.Shared.Next(numPlayers)];
            players.Remove(selectedID);
            traitors.Add(selectedID);
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

        foreach (ulong id in players)
        {
            PlayerAssignment pa = new();
            pa.id = id;
            pa.team = Team.Innocent;
            pa.role = Role.Normal;
            byte[] data = MessagePackSerializer.Serialize(pa);
            RPCManager.RPC(this, "AssignRole", [id,Team.Innocent,Role.Normal]);
        }
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void AssignRole(ulong id, Team team, Role role)
    {
        Logging.Log($"Player {id} has been assigned team:{team} and role:{role}", "GameModeManager");
        Global.gameState.PlayerCharacters[id].Assignment(team, role);
    }

    public void PerTickAuth(double delta)
    {
        throw new NotImplementedException();
    }

    public void PerFrameAuth(double delta)
    {
        throw new NotImplementedException();
    }

    public void PerTickLocal(double delta)
    {
        throw new NotImplementedException();
    }

    public void PerFrameLocal(double delta)
    {
        throw new NotImplementedException();
    }

    public void PerTickShared(double delta)
    {
        throw new NotImplementedException();
    }

    public void PerFrameShared(double delta)
    {
        throw new NotImplementedException();
    }

    public void ProcessStateUpdate(byte[] update)
    {
        throw new NotImplementedException();
    }

    public byte[] GenerateStateUpdate()
    {
        throw new NotImplementedException();
    }

    public string GenerateStateString()
    {
        throw new NotImplementedException();
    }
}

public enum Team
{
    None,
    Innocent,
    Traitor
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