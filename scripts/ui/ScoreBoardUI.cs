using Godot;
using Limbo.Console.Sharp;
using SteamMultiplayerPeerCSharp;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Reflection;


[GlobalClass]
public partial class ScoreBoardUI : MarginContainer
{

    [Export] public Label TimeRemainingLabel;
    [Export] public Label TimeRemainingNumber;
    [Export] public Label DeliveryStatusLabel;
    [Export] public Label DeliveryStatusNumber;
    [Export] public Label EvacuationETALabel;
    [Export] public Label EvacuationETANumber;
    [Export] public VBoxContainer LivingWorkersList;
    [Export] public VBoxContainer MissingWorkersList;
    [Export] public VBoxContainer DeadWorkersList;


    public void UpdateTimeLeftUI(string timeLeftString)
    {
        TimeRemainingNumber.Text = timeLeftString;
    }

    //Move Worker Section
    public void MovePlayerToLiving(ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNodeFromLists(playerID.ToString());
        if (playerRow != null)
        {
            playerRow.Reparent(LivingWorkersList);
        }
        else
        {
            Logging.Error($"Tried to move playerID that isn't in Scoreboard.", "ScoreBoardUI");
        }

    }
    public void MovePlayerToMissing(ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNodeFromLists(playerID.ToString());
        if (playerRow != null)
        {
            playerRow.Reparent(MissingWorkersList);
        }
        else
        {
            Logging.Error($"Tried to move playerID that isn't in Scoreboard.", "ScoreBoardUI");
        }
    }
    public void MovePlayerToDead(ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNodeFromLists(playerID.ToString());
        if (playerRow != null)
        {
            playerRow.Reparent(DeadWorkersList);
        }
        else
        {
            Logging.Error($"Tried to move playerID that isn't in Scoreboard.", "ScoreBoardUI");
        }
    }

    public void RemovePlayerRow(ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNodeFromLists(playerID.ToString());
        if (playerRow != null)
        {
            playerRow.QueueFree();
        }
        else
        {
            Logging.Error($"Tried to remove playerID that isn't in Scoreboard.", "ScoreBoardUI");
        }

    }

    public void AddLivingWorkerPlayerRow(ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNodeFromLists(playerID.ToString());
        if (playerRow == null)
        {
            ScoreBoardPlayerRow temp = ResourceLoader.Load<PackedScene>("res://scenes/ui/hud/ScoreBoardPlayerRow.tscn").Instantiate<ScoreBoardPlayerRow>();
            temp.SetPlayerID(playerID);
            LivingWorkersList.AddChild(temp);
        }
        else
        {
            Logging.Log($"Tried to add player to Living Worker list but they are already on the scoreboard somewhere, using MovePlayerToLiving instead.", "ScoreBoardUI");
            MovePlayerToLiving(playerID);
        }

    }

    public void AddMissingWorkerPlayerRow(ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNodeFromLists(playerID.ToString());
        if (playerRow == null)
        {
            ScoreBoardPlayerRow temp = ResourceLoader.Load<PackedScene>("res://scenes/ui/hud/ScoreBoardPlayerRow.tscn").Instantiate<ScoreBoardPlayerRow>();
            temp.SetPlayerID(playerID);
            MissingWorkersList.AddChild(temp);
        }
        else
        {
            Logging.Log($"Tried to add player to Missing Worker list but they are already on the scoreboard somewhere, using MovePlayerToMissing instead.", "ScoreBoardUI");
            MovePlayerToMissing(playerID);
        }
    }

    public void AddDeadWorkerPlayerRow(ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNodeFromLists(playerID.ToString());
        if (playerRow == null)
        {
            ScoreBoardPlayerRow temp = ResourceLoader.Load<PackedScene>("res://scenes/ui/hud/ScoreBoardPlayerRow.tscn").Instantiate<ScoreBoardPlayerRow>();
            temp.SetPlayerID(playerID);
            DeadWorkersList.AddChild(temp);
        }
        else
        {
            Logging.Log($"Tried to add player to Dead Worker list but they are already on the scoreboard somewhere, using MovePlayerToDead instead.", "ScoreBoardUI");
            MovePlayerToDead(playerID);
        }
    }

    public void SetPlayerIDAsTraitor(ulong playerID)
    {
        //only set the row as traitor for traitors
        if(Global.gameState.gameModeManager.basicPlayers[Global.steamid].team == Team.Traitor)
        {
            ScoreBoardPlayerRow playerRow = GetNodeFromLists(playerID.ToString());
            playerRow.SetAsTraitor();
        }
    }

    public void SetPlayerIDAsManager(ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNodeFromLists(playerID.ToString());
        playerRow.SetAsManager();
    }
    
    public void PlayerIsManager(ulong playerID)
    {
        SetPlayerIDAsManager(playerID);
    }

    public void PlayerIsTraitor(ulong playerID)
    {
        //local player is an innocent so we do nothing, leaving the traitor player unlabeled
        if (Global.gameState.gameModeManager.basicPlayers[Global.steamid].state == CharacterState.Living && (Global.gameState.gameModeManager.basicPlayers[Global.steamid].team == Team.Innocent || Global.gameState.gameModeManager.basicPlayers[Global.steamid].team == Team.Manager))
        {

        }
        else //local player is a traitor so they get objective truth
        {
            SetPlayerIDAsTraitor(playerID);
        }
    }

    public void PlayerDied(ulong playerID)
    {
        //local player is alive and an innocent so we do nothing, leaving the dead player as alive
        if (Global.gameState.gameModeManager.basicPlayers[Global.steamid].state == CharacterState.Living && (Global.gameState.gameModeManager.basicPlayers[Global.steamid].team == Team.Innocent || Global.gameState.gameModeManager.basicPlayers[Global.steamid].team == Team.Manager))
        {

        }
        else //local player is either dead or a traitor so they get objective truth
        {
            AddMissingWorkerPlayerRow(playerID);
        }
        //local player died so update scoreboard
        if (playerID == Global.steamid)
        {
            UpdateScoreboardForDeadPlayer();
        }
    }

    private void UpdateScoreboardForDeadPlayer()
    {
        //TODO
    }

    public void PlayerFound(ulong playerID)
    {
        AddDeadWorkerPlayerRow(playerID);
    }
    
    public ScoreBoardPlayerRow GetNodeFromLists(string playerID)
    {
        ScoreBoardPlayerRow temp = (ScoreBoardPlayerRow)LivingWorkersList.GetNodeOrNull<PanelContainer>(playerID);
        if (temp == null)
        {
            temp = (ScoreBoardPlayerRow)MissingWorkersList.GetNodeOrNull<PanelContainer>(playerID);
        }
        if (temp == null)
        {
            temp = (ScoreBoardPlayerRow)DeadWorkersList.GetNodeOrNull<PanelContainer>(playerID);
        }
        return temp;
    }

    public void NewRound()
    {
        //clear all of our visual lists
        foreach (var child in LivingWorkersList.GetChildren())
        {
            child.QueueFree();
        }
        foreach (var child in MissingWorkersList.GetChildren())
        {
            child.QueueFree();
        }
        foreach (var child in DeadWorkersList.GetChildren())
        {
            child.QueueFree();
        }

        //use the basicplayers list
        foreach(ulong basicPlayerID in Global.gameState.gameModeManager.basicPlayers.Keys)
        {
            AddLivingWorkerPlayerRow(basicPlayerID);
        }
    }

    



    //player inner-row updates
    public void UpdatePlayerIcon(TextureRect newPlayerIcon, ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
        if (playerRow != null)
        {
            playerRow.UpdatePlayerIcon(newPlayerIcon);
        }
        else
        {
            Logging.Error($"Tried to update player icon for playerID that isn't in Scoreboard.", "ScoreBoardUI");
        }
    }

    public void UpdatePlayerName(string newPlayerName, ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
        if (playerRow != null)
        {
            playerRow.UpdatePlayerName(newPlayerName);
        }
        else
        {
            Logging.Error($"Tried to update player name for playerID that isn't in Scoreboard.", "ScoreBoardUI");
        }
    }

    public void UpdateKarmaUI(int newKarma, ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
        if (playerRow != null)
        {
            playerRow.UpdateKarmaUI(newKarma);
        }
        else
        {
            Logging.Error($"Tried to update karma for playerID that isn't in Scoreboard.", "ScoreBoardUI");
        }
    }

    public void UpdateScoreUI(int newScore, ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
        if (playerRow != null)
        {
            playerRow.UpdateScoreUI(newScore);
        }
        else
        {
            Logging.Error($"Tried to update score for playerID that isn't in Scoreboard.", "ScoreBoardUI");
        }
    }
    public void UpdateDeathsUI(int newDeaths, ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
        if (playerRow != null)
        {
            playerRow.UpdateDeathsUI(newDeaths);
        }
        else
        {
            Logging.Error($"Tried to update deaths for playerID that isn't in Scoreboard.", "ScoreBoardUI");
        }

    }
    public void UpdatePingUI(int newPing, ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
        if (playerRow != null)
        {
            playerRow.UpdatePingUI(newPing);
        }
        else
        {
            Logging.Error($"Tried to update ping for playerID that isn't in Scoreboard.", "ScoreBoardUI");
        }
    }
}