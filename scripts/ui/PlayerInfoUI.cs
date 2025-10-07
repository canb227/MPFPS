using Godot;
using Limbo.Console.Sharp;
using SteamMultiplayerPeerCSharp;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Reflection;


[GlobalClass]
public partial class PlayerInfoUI : MarginContainer
{
    [Export] public PanelContainer TeamPanel;
    [Export] public Label TeamLabel;
    [Export] public Label TimeLeftLabel;
    [Export] public ProgressBar StunBar;
    [Export] public ProgressBar HealthBar;
    [Export] public Label HealthLabel;
    [Export] public ProgressBar AmmoBar;
    [Export] public Label AmmoLabel;
    float uiTimeLeftSeconds = 600;

    public override void _PhysicsProcess(double delta)
    {
        uiTimeLeftSeconds -= (float)delta;
        int minutes = (int)Math.Floor(uiTimeLeftSeconds / 60);
        int seconds = (int)uiTimeLeftSeconds % 60;

        TimeLeftLabel.Text = $"{minutes:D2}:{seconds:D2}";
    }

    public void UpdateRoleUI(Team newTeam)
    {
        StyleBoxFlat styleBox = TeamPanel.GetThemeStylebox("panel") as StyleBoxFlat;
        if (newTeam == Team.Innocent)
        {
            styleBox.BgColor = new Godot.Color(0.028f, 0.679f, 0.009f); //green
            TeamLabel.Text = "Innocent";
        }
        else if (newTeam == Team.Traitor)
        {
            styleBox.BgColor = new Godot.Color(0.803f, 0.003f, 0.004f); //red
            TeamLabel.Text = "Traitor";
        }
        else if (newTeam == Team.Manager)
        {
            styleBox.BgColor = new Godot.Color(0.005f, 0.005f, 0.65f); //Blue
            TeamLabel.Text = "Manager";
        }
        else
        {
            styleBox.BgColor = new Godot.Color(0.333f, 0.333f, 0.333f); //grey
            TeamLabel.Text = "None?";
        }
    }
    public void UpdateTimeLeft(float timeLeftSeconds)
    {
        uiTimeLeftSeconds = timeLeftSeconds;
        int minutes = (int)Math.Floor(uiTimeLeftSeconds / 60);
        int seconds = (int)uiTimeLeftSeconds % 60;

        TimeLeftLabel.Text = $"{minutes:D2}:{seconds:D2}";
    }
    public void UpdateStunUI(int newStunBarRemaning)
    {
        StunBar.Value = newStunBarRemaning;
    }
    public void UpdateAmmoUI(int remainingAmmo, int maxAmmo)
    {
        AmmoBar.MaxValue = maxAmmo;
        AmmoBar.Value = remainingAmmo;
    }
    public void UpdateHealthUI(int newHealth, int newHealthMax)
    {
        HealthBar.MaxValue = newHealthMax;
        HealthBar.Value = newHealth;
    }
    
}