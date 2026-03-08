using Godot;
using Limbo.Console.Sharp;
using SteamMultiplayerPeerCSharp;
using Steamworks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;


[GlobalClass]
public partial class PlayerUIManager : Control
{
    [Export] public PlayerInfoUI PlayerInfoUI;
    [Export] public DeadPlayerScreen deadPlayerScreen;
    [Export] public RoleShopScreen roleShopScreen;
    [Export] public MarginContainer InventoryUI;
    [Export] public Label targetPlayerName;
    [Export] public Label targetPlayerHealth;
    [Export] public Label targetPlayerRole;
    [Export] public TextureRect inventorySlot1;
    [Export] public TextureRect inventorySlot2;
    [Export] public TextureRect inventorySlot3;
    [Export] public TextureRect inventorySlot4;
    [Export] public PanelContainer infoBox;
    [Export] public Label infoLabel;
    [Export] public PanelContainer statusBox;
    [Export] public Label statusLabel;

    [Export] public float FadeDuration = 3f;
    public float infoDisplayTimeLeft;
    private bool _fadeStarted = false;

    public override void _Process(double delta)
    {
        if(infoDisplayTimeLeft > 0)
        {
            infoDisplayTimeLeft -= (float)delta;
        }

        if (infoDisplayTimeLeft <= 3f && !_fadeStarted)
        {
            _fadeStarted = true;
            StartFadeOut();
        }
    }

    private void StartFadeOut()
    {
        var tween = CreateTween();
        tween.TweenProperty(infoBox, "modulate:a", 0f, FadeDuration);
    }

    private void FadeIn(float duration = 0.5f)
    {
        var tween = CreateTween();
        tween.TweenProperty(infoBox, "modulate:a", 1f, duration);
    }


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

    public void UpdateTimeLeftUI(string timerString, string hiddenTimerString)
    {
        PlayerInfoUI.UpdateTimeLeftUI(timerString, hiddenTimerString);
    }

    //player info functions
    public void UpdateTeamUI(Team newTeam)
    {
        PlayerInfoUI.UpdateTeamUI(newTeam);
    }

    public void UpdateInventorySlot(int slot, string iconPath)
    {
        GD.Print("Update Slot " + slot);
        if (slot == 1)
        {
            if (iconPath == "")
            {
                inventorySlot1.Texture = null;
            }
            else
            {
                inventorySlot1.Texture = ResourceLoader.Load<Texture2D>(iconPath);
            }            
        }
        else if (slot == 2)
        {
            if (iconPath == "")
            {
                inventorySlot2.Texture = null;
            }
            else
            {
                inventorySlot2.Texture = ResourceLoader.Load<Texture2D>(iconPath);
            }          
        }
        else if (slot == 3)
        {
            if (iconPath == "")
            {
                inventorySlot3.Texture = null;
            }
            else
            {
                inventorySlot3.Texture = ResourceLoader.Load<Texture2D>(iconPath);
            }           
        }
        else if (slot == 4)
        {
            if (iconPath == "")
            {
                inventorySlot4.Texture = null;
            }
            else
            {
                inventorySlot4.Texture = ResourceLoader.Load<Texture2D>(iconPath);
            }  
        }
        else
        {
            Logging.Error("INVALID Inventory Slot Update Request", "PlayerUIManager");
        }
    }

    public void DisplayNewInfo(string infoString, float maxDisplayTime = 60f)
    {
        infoLabel.Text = infoString;
        infoDisplayTimeLeft = maxDisplayTime;
        _fadeStarted = false;
        FadeIn();
    }

    private List<string> statusStrings = new();
    public void AddNewStatus(string infoString)
    {
        if(!statusStrings.Contains(infoString))
        {
            statusStrings.Insert(0, infoString);
        }
        statusLabel.Text = infoString;
        statusBox.Visible = true;
    }

    public void EndStatus(string infoString)
    {
        if(statusStrings.Contains(infoString))
        {
            statusStrings.Remove(infoString);
        }
        if(!statusStrings.Any())
        {
            statusBox.Visible = false;
        }
        else
        {
            statusLabel.Text = statusStrings[0];
        }
    }


    public void UpdateStunUI(int newStunBarRemaning, int maxStunBar)
    {
        PlayerInfoUI.UpdateStunUI(newStunBarRemaning, maxStunBar);
    }
    public void UpdateAmmoUI(int remainingAmmo, int storedAmmo, int maxAmmo)
    {
        PlayerInfoUI.UpdateAmmoUI(remainingAmmo, storedAmmo, maxAmmo);
    }
    public void UpdateStoredAmmoUI(int storedAmmo)
    {
        PlayerInfoUI.UpdateStoredAmmoUI(storedAmmo);
    }
    public void UpdateHealthUI(int newHealth, int newHealthMax)
    {
        PlayerInfoUI.UpdateHealthUI(newHealth, newHealthMax);
    }


    
}