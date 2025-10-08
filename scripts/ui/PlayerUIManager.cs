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

    public void UpdateTimeLeftUI(string timerString)
    {
        PlayerInfoUI.UpdateTimeLeftUI(timerString);
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


    
}