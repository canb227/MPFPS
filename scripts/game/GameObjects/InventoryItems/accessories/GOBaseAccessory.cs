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
    public override InventoryGroupCategory category { get; set; } = InventoryGroupCategory.Special;
    public override bool droppable { get; set; } = true;
    public ulong currentlyHeldBy { get; set; }
    public bool customHeldPhysics { get; set; }
    public bool snapHoldNoPhysics { get; set; }
    public float heldWeight { get; set; }
    public float heldDrag { get; set; }
    public float heldFriction { get; set; }
    private ActionFlags lastTickActions;

    public override void HandleInput(ActionFlags input)
    {
        if (!lastTickActions.HasFlag(ActionFlags.Fire) && input.HasFlag(ActionFlags.Fire))
        {
            Logging.Log($"Buh!", "GOBaseAccessory");
        }
    }

    public virtual void OnHold(ulong byID)
    {
        GravityScale = 0.1f;
        LinearDamp = 20;
        AngularDamp = 5;
    }

    public virtual void OnRelease(ulong byID)
    {
        LinearVelocity = LinearVelocity.Clamp(0, 5);
        GravityScale = 1;
        LinearDamp = ProjectSettings.GetSetting("physics/3d/default_linear_damp").AsSingle();
        AngularDamp = ProjectSettings.GetSetting("physics/3d/default_angular_damp").AsSingle();
    }
}

[MessagePackObject]
public struct GOBaseAccessoryStateUpdate
{
    [Key(0)]
    public ulong inInventoryOf;
    [Key(1)]
    public ulong equippedBySteamID;
    [Key(2)]
    public Vector3 position;
    [Key(3)]
    public Vector3 rotation;
}