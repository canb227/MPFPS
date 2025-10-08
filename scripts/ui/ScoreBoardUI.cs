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

    //add/remove/move player rows
    public void AddLivingWorkerPlayerRow(ulong playerID)
    {
        ScoreBoardPlayerRow temp = new ScoreBoardPlayerRow(playerID);
    }

    public void RemoveLivingPlayerRow(ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
        playerRow.QueueFree();
    }

    public void MoveLivingPlayerToMissing(ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
        playerRow.Reparent(MissingWorkersList);
    }

    public void MoveLivingPlayerToDead(ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
        playerRow.Reparent(DeadWorkersList);
    }

    public void AddMissingWorkerPlayerRow(ulong playerID)
    {
        ScoreBoardPlayerRow temp = new ScoreBoardPlayerRow(playerID);
    }

    public void RemoveMissingPlayerRow(ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
        playerRow.QueueFree();
    }

    public void MoveMissingPlayerToLiving(ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
        playerRow.Reparent(LivingWorkersList);
    }
    public void MoveMissingPlayerToDead(ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
        playerRow.Reparent(DeadWorkersList);
    }

    public void AddDeadWorkerPlayerRow(ulong playerID)
    {
        ScoreBoardPlayerRow temp = new ScoreBoardPlayerRow(playerID);
    }

    public void RemoveDeadPlayerRow(ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
        playerRow.QueueFree();
    }

    public void MoveDeadPlayerToLiving(ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
        playerRow.Reparent(LivingWorkersList);
    }
    public void MoveDeadPlayerToMissing(ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
        playerRow.Reparent(MissingWorkersList);
    }


    //player row updates
    public void UpdatePlayerIcon(TextureRect newPlayerIcon, ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
        playerRow.UpdatePlayerIcon(newPlayerIcon);
    }

    public void UpdatePlayerName(string newPlayerName, ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
        playerRow.UpdatePlayerName(newPlayerName);
    }

    public void UpdateKarmaUI(int newKarma, ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
        playerRow.UpdateKarmaUI(newKarma);
    }

    public void UpdateScoreUI(int newScore, ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
        playerRow.UpdateScoreUI(newScore);
    }
    public void UpdateDeathsUI(int newDeaths, ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
        playerRow.UpdateDeathsUI(newDeaths);

    }
    public void UpdatePingUI(int newPing, ulong playerID)
    {
        ScoreBoardPlayerRow playerRow = GetNode<ScoreBoardPlayerRow>(playerID.ToString());
        playerRow.UpdatePingUI(newPing);
    }
}