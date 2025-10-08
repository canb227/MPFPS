using Godot;
using Limbo.Console.Sharp;
using SteamMultiplayerPeerCSharp;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Reflection;


[GlobalClass]
public partial class InGameUI : Control
{
    [Export] public PlayerUIManager PlayerUIManager;
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

        PlayerUIManager.UpdateTimeLeftUI(timerString);
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
}