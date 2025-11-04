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
public partial class GOBaseAccessory : GOBaseInventoryItem, IsHoldable
{
    public override InventoryGroupCategory category { get; set; } = InventoryGroupCategory.Accessory;
    //[Export] AudioStreamPlayer3D audioStreamPlayer { get; set; }
    public override bool droppable { get; set; } = true;
    protected ActionFlags lastTickActions;
    protected RayCast3D interactRayCast;

    public override void HandleInput(ActionFlags input)
    {
        lastTickActions = input;
    }

    public override void OnEquipped(ulong bySteamID)
    {
        base.OnEquipped(bySteamID);
        if (GetHeldBy() is BasicPlayerCharacter basicPlayerCharacter)
        {
            interactRayCast = basicPlayerCharacter.interactRayCast;
        }
    }


    public override void OnUnequipped(ulong bySteamID)
    {
        base.OnUnequipped(bySteamID);
        interactRayCast = null;
        //audioStreamPlayer.Stop();
        //animationPlayer.Play("RESET");
    }
    
    public override void OnDropped(ulong bySteamID)
    {
        base.OnDropped(bySteamID);
        //audioStreamPlayer.Stop();
        //animationPlayer.Play("RESET");
    }
}