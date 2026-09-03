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
public partial class GOFlashlight : GOBaseAccessory
{
    [Export] SpotLight3D spotLight3D { get; set; }
    [Export] AudioStreamPlayer3D audioStreamPlayer { get; set; }
    public override void HandleInput(ActionFlags input)
    {
        if (!lastTickActions.HasFlag(ActionFlags.Fire) && input.HasFlag(ActionFlags.Fire))
        {
            RPCManager.RPCID(id, "ToggleFlashLight", []);
        }
        base.HandleInput(input);
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void ToggleFlashLight()
    {
        spotLight3D.Visible = !spotLight3D.Visible;
        audioStreamPlayer.Play();
    }
    public override void OnDropped(ulong bySteamID)
    {
        base.OnDropped(bySteamID);
        spotLight3D.ShadowEnabled = false;
    }
    public override void OnPickup(ulong bySteamID)
    {
        base.OnPickup(bySteamID);
        spotLight3D.ShadowEnabled = true;
    }

    public override void OnUnequipped(ulong bySteamID)
    {
        base.OnUnequipped(bySteamID);
        if(spotLight3D.Visible)
        {
            spotLight3D.Visible = false;
            audioStreamPlayer.Play();
        }
    }


}