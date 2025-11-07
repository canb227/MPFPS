using Godot;
using Godot.Collections;
using ImGuiGodot.Internal;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


[GlobalClass]
public partial class GOPlayerRadar : GOBaseRoleItem
{
    [Export] AudioStreamPlayer3D audioStreamPlayer { get; set; }
    [Export] double scanCooldown = 5;
    private double currentScanCooldown = 5;

    private PackedScene markerScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/ingame/packageMarker.tscn");
    public override void HandleInput(ActionFlags input)
    {
        base.HandleInput(input);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        currentScanCooldown -= delta;
        if(currentScanCooldown <= 0)
        {
            currentScanCooldown = scanCooldown;
            if(Global.steamid == inInventoryOf || Global.steamid == equippedBySteamID)
            {
                foreach (var node in GetTree().GetNodesInGroup("players"))
                {
                    if (node is BasicPlayerCharacter playerCharacter)
                    {
                        var marker = markerScene.Instantiate<RadarMarker>();
                        marker.Init(this, playerCharacter, new Godot.Color(0f,0.505f,0f), 5);
                        playerCharacter.AddChild(marker);
                    }
                }
            }
            //audioStreamPlayer.Play();
        }

    }
    public override void OnDropped(ulong bySteamID)
    {
        base.OnDropped(bySteamID);
    }
    public override void OnPickup(ulong bySteamID)
    {
        base.OnPickup(bySteamID);
    }

}