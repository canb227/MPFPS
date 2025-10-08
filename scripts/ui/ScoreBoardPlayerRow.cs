using Godot;
using ImGuiGodot.Internal;
using Limbo.Console.Sharp;
using SteamMultiplayerPeerCSharp;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Reflection;


[GlobalClass]
public partial class ScoreBoardPlayerRow : HBoxContainer
{
    public ulong playerID;
    [Export] public TextureRect playerIcon;
    [Export] public Label playerName;
    [Export] public Label karma;
    [Export] public Label score;
    [Export] public Label deaths;
    [Export] public Label ping;

    public ScoreBoardPlayerRow(ulong playerID)
    {
        this.playerID = playerID;
        playerName.Name = Utils.IDToName(playerID);
        playerIcon.Texture = Utils.GetSmallSteamAvatar(playerID);
    }

    public override void _Ready()
    {
        Name = playerID.ToString();
    }

    public void UpdatePlayerIcon(TextureRect newPlayerIcon)
    {
        playerIcon = newPlayerIcon;
    }

    public void UpdatePlayerName(string newPlayerName)
    {
        playerName.Text = newPlayerName;
    }

    public void UpdateKarmaUI(int newKarma)
    {
        if (newKarma > 9999)
        {
            newKarma = 9999;
        }
        karma.Text = newKarma.ToString();
    }

    public void UpdateScoreUI(int newScore)
    {
        if (newScore > 9999)
        {
            newScore = 9999;
        }
        score.Text = newScore.ToString();
    }
    public void UpdateDeathsUI(int newDeaths)
    {
        if (newDeaths > 9999)
        {
            newDeaths = 9999;
        }
        deaths.Text = newDeaths.ToString();
    }
    public void UpdatePingUI(int newPing)
    {
        if (newPing > 9999)
        {
            newPing = 9999;
        }
        ping.Text = newPing.ToString();
    }

}