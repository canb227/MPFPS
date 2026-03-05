using Godot;
using Limbo.Console.Sharp;
using SteamMultiplayerPeerCSharp;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Reflection;


[GlobalClass]
public partial class EscapeMenuUI : MarginContainer
{
    [Export] public Button ResumeGameButton;
    [Export] public Button CloseGameButton;
    public override void _Ready()
    {
        base._Ready();
        ResumeGameButton.Pressed += CloseMenu;
        CloseGameButton.Pressed += CloseGame;
    }

    public void CloseGame()
    {
        GetTree().Quit();
    }

    
    public void CloseMenu()
    {
        Global.ui.inGameUI.HideEscapeMenu();
    }


}