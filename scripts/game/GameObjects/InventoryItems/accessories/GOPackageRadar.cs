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
public partial class GOPackageRadar : GOBaseAccessory
{
    [Export] AudioStreamPlayer3D audioStreamPlayer { get; set; }
    [Export] double scanCooldown = 30;
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
                foreach (var node in GetTree().GetNodesInGroup("PackageBoxes"))
                {
                    if (node is GOPackageBox box)
                    {
                        var marker = markerScene.Instantiate<RadarMarker>();
                        marker.Init(this, box, new Godot.Color(0.722f,0.405f,0f), 10);
                        box.AddChild(marker);
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