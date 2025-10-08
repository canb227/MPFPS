using Godot;
using Limbo.Console.Sharp;
using SteamMultiplayerPeerCSharp;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Reflection;


[GlobalClass]
public partial class PlayerUIManager : Control
{
    [Export] public PlayerInfoUI PlayerInfoUI;
    [Export] public MarginContainer InventoryUI;
    [Export] public ScoreBoardUI ScoreBoardUI;
    float uiTimeLeftSeconds = 600;

    public override void _PhysicsProcess(double delta)
    {
        uiTimeLeftSeconds -= (float)delta;
        UpdateTimeLeftUI(uiTimeLeftSeconds);
    }

    public void UpdateTimeLeftUI(float timeLeftSeconds)
    {
        uiTimeLeftSeconds = timeLeftSeconds;
        int minutes = (int)Math.Floor(uiTimeLeftSeconds / 60);
        int seconds = (int)uiTimeLeftSeconds % 60;
        string timerString = $"{minutes:D2}:{seconds:D2}";

        PlayerInfoUI.UpdateTimeLeftUI(timerString);
        ScoreBoardUI.UpdateTimeLeftUI(timerString);
    }

    //player info functions
    public void UpdateRoleUI(Team newTeam)
    {
        PlayerInfoUI.UpdateRoleUI(newTeam);
    }
    public void UpdateStunUI(int newStunBarRemaning)
    {
        PlayerInfoUI.UpdateStunUI(newStunBarRemaning);
    }
    public void UpdateAmmoUI(int remainingAmmo, int maxAmmo)
    {
        PlayerInfoUI.UpdateAmmoUI(remainingAmmo, maxAmmo);
    }
    public void UpdateHealthUI(int newHealth, int newHealthMax)
    {
        PlayerInfoUI.UpdateHealthUI(newHealth, newHealthMax);
    }

    //scoreboard functions
    public void UpdatePlayerIcon(TextureRect newPlayerIcon, ulong playerID)
    {
        ScoreBoardUI.UpdatePlayerIcon(newPlayerIcon, playerID);
    }

    public void UpdatePlayerName(string newPlayerName, ulong playerID)
    {
        ScoreBoardUI.UpdatePlayerName(newPlayerName, playerID);
    }

    public void UpdateKarmaUI(int newKarma, ulong playerID)
    {
        ScoreBoardUI.UpdateKarmaUI(newKarma, playerID);
    }

    public void UpdateScoreUI(int newScore, ulong playerID)
    {
        ScoreBoardUI.UpdateScoreUI(newScore, playerID);
    }
    public void UpdateDeathsUI(int newDeaths, ulong playerID)
    {
        ScoreBoardUI.UpdateDeathsUI(newDeaths, playerID);

    }
    public void UpdatePingUI(int newPing, ulong playerID)
    {
        ScoreBoardUI.UpdatePingUI(newPing, playerID);
    }
    
}