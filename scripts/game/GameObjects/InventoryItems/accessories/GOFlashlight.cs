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
    [Export] OmniLight3D omniLight3D { get; set; }
    public override void HandleInput(ActionFlags input)
    {
        if (!lastTickActions.HasFlag(ActionFlags.Fire) && input.HasFlag(ActionFlags.Fire))
        {
            spotLight3D.Visible = !spotLight3D.Visible;
            omniLight3D.Visible = !omniLight3D.Visible;
        }
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

}