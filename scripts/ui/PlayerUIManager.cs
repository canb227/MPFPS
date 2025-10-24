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
    [Export] public DeadPlayerScreen deadPlayerScreen;
    [Export] public MarginContainer InventoryUI;
    [Export] public Label targetPlayerName;
    [Export] public Label targetPlayerHealth;
    [Export] public Label targetPlayerRole;

    public void ShowPlayerUI(ulong characterID)
    {
        UpdateHealthUI((int)Global.gameState.gameModeManager.basicPlayers[characterID].currentHealth, (int)Global.gameState.gameModeManager.basicPlayers[characterID].maxHealth);
        Visible = true;
        PlayerInfoUI.Visible = true;
    }
    public void HidePlayerUI()
    {
        Visible = false;
        PlayerInfoUI.Visible = false;
    }

    public void UpdateTimeLeftUI(string timerString)
    {
        PlayerInfoUI.UpdateTimeLeftUI(timerString);
    }

    //player info functions
    public void UpdateRoleUI(Team newTeam)
    {
        PlayerInfoUI.UpdateRoleUI(newTeam);
    }
    public void UpdateStunUI(int newStunBarRemaning, int maxStunBar)
    {
        PlayerInfoUI.UpdateStunUI(newStunBarRemaning, maxStunBar);
    }
    public void UpdateAmmoUI(int remainingAmmo, int storedAmmo, int maxAmmo)
    {
        PlayerInfoUI.UpdateAmmoUI(remainingAmmo, storedAmmo, maxAmmo);
    }
    public void UpdateHealthUI(int newHealth, int newHealthMax)
    {
        PlayerInfoUI.UpdateHealthUI(newHealth, newHealthMax);
    }


    
}