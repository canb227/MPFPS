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
    [Export] public ScoreBoardUI ScoreBoard;
    [Export] public RoundReportUI RoundReport;

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
        ScoreBoard.UpdatePlayerIcon(newPlayerIcon, playerID);
    }

    public void UpdatePlayerName(string newPlayerName, ulong playerID)
    {
        ScoreBoard.UpdatePlayerName(newPlayerName, playerID);
    }

    public void ToggleScoreBoard()
    {
        ScoreBoard.Visible = !ScoreBoard.Visible;
    }

    public void ShowScoreBoard()
    {
        if (!ScoreBoard.Visible)
        {
            ScoreBoard.Visible = true;
        }
    }

    public void HideScoreBoard()
    {
        if (ScoreBoard.Visible)
        {
            ScoreBoard.Visible = false;
        }
    }

    public void ShowRoundReport(Team winningTeam)
    {
        if (!RoundReport.Visible)
        {
            RoundReport.ShowRoundReport(winningTeam);
        }
    }

    public void HideRoundReport()
    {
        if (RoundReport.Visible)
        {
            RoundReport.Visible = false;
        }
    }
}