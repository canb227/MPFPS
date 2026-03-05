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
    [Export] public EscapeMenuUI EscapeMenu;
    [Export] public RoundReportUI RoundReport;

    public void UpdateTimeLeftUI()
    {
        string timerString = "";
        string hiddenTimerString = "";
        if (Global.gameState.gameModeManager.evacuationStarted)
        {
            timerString = $"{TimeSpan.FromSeconds(Global.gameState.gameModeManager.evacuationTimeLeft):mm\\:ss}";
            hiddenTimerString = $"{TimeSpan.FromSeconds(Global.gameState.gameModeManager.evacuationTimeLeft):mm\\:ss}";
        }
        else
        {
            timerString = $"{TimeSpan.FromSeconds(Global.gameState.gameModeManager.publicRemainingRoundTime):mm\\:ss}";
            hiddenTimerString = $"{TimeSpan.FromSeconds(Global.gameState.gameModeManager.remainingRoundTime):mm\\:ss}";
        }
        PlayerUIManager.UpdateTimeLeftUI(timerString, hiddenTimerString);
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

    public void ShowScoreBoard()
    {
        if (!ScoreBoard.Visible)
        {
            ScoreBoard.Visible = true;
            Input.MouseMode = Input.MouseModeEnum.Confined;
        }
    }

    public void HideScoreBoard()
    {
        if (ScoreBoard.Visible)
        {
            ScoreBoard.Visible = false;
            if(!EscapeMenu.Visible)
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }
        }
    }

    public void ShowEscapeMenu()
    {
        if (!EscapeMenu.Visible)
        {
            EscapeMenu.Visible = true;
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    public void HideEscapeMenu()
    {
        if (EscapeMenu.Visible)
        {
            EscapeMenu.Visible = false;
            if(!ScoreBoard.Visible)
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }
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