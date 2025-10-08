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
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
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
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
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
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
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
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
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
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
        if (playerRow == null)
        {
            ScoreBoardPlayerRow temp = new ScoreBoardPlayerRow(playerID);
            LivingWorkersList.AddChild(temp);
        }
        else
        {
            Logging.Error($"Tried to add player to Living Worker list but they are already on the scoreboard somewhere, use MovePlayerToLiving instead.", "ScoreBoardUI");
        }

    }

    public void AddMissingWorkerPlayerRow(ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
        if (playerRow == null)
        {
            ScoreBoardPlayerRow temp = new ScoreBoardPlayerRow(playerID);
            MissingWorkersList.AddChild(temp);
        }
        else
        {
            Logging.Error($"Tried to add player to Missing Worker list but they are already on the scoreboard somewhere, use MovePlayerToMissing instead.", "ScoreBoardUI");
        }
    }

    public void AddDeadWorkerPlayerRow(ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
        if (playerRow == null)
        {
            ScoreBoardPlayerRow temp = new ScoreBoardPlayerRow(playerID);
            DeadWorkersList.AddChild(temp);
        }
        else
        {
            Logging.Error($"Tried to add player to Dead Worker list but they are already on the scoreboard somewhere, use MovePlayerToDead instead.", "ScoreBoardUI");
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