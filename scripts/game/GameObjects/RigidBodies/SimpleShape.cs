using System.Numerics;
using Godot;
using MessagePack;

[GlobalClass]
public partial class SimpleShape : GOBaseRigidBody, IsHoldable
{

    Godot.Vector3 desiredPosition;
    Godot.Vector3 desiredRotation;

    public ulong currentlyHeldBy { get; set; }
    public bool customHeldPhysics { get; set; }
    public bool snapHoldNoPhysics { get; set; } = true;
    public float heldWeight { get; set; }
    public float heldDrag { get; set; }
    public float heldFriction { get; set; }

    public override void _Ready()
    {
        base._Ready();
        if (authority != Global.steamid)
        {
            SetPhysicsProcess(false);
        }
    }

    public override void ProcessStateUpdate(byte[] update)
    {
        SimpleShapeStateUpdate sssu = MessagePackSerializer.Deserialize<SimpleShapeStateUpdate>(update);
        LinearVelocity = sssu.velocity;
        desiredPosition = sssu.position;
        desiredRotation = sssu.rotation;
    }

    public override byte[] GenerateStateUpdate()
    {
        SimpleShapeStateUpdate sssu = new();
        sssu.velocity = LinearVelocity;
        sssu.position = GlobalPosition;
        sssu.rotation = Rotation;
        return MessagePackSerializer.Serialize(sssu);
    }

    public override void PerTickAuth(double delta)
    {

    }

    public override void PerTickLocal(double delta)
    {
        Position = Position.Lerp(desiredPosition, (float)(delta/0.1f)); //lerp over .1 seconds
        Rotation = Rotation.Lerp(desiredRotation, (float)(delta/0.1f)); //lerp over .1 seconds
    }

    public override void PerFrameLocal(double delta)
    {

    }

    public override void PerFrameAuth(double delta)
    {

    }
    public override string GenerateStateString()
    {
        return MessagePackSerializer.ConvertToJson(GenerateStateUpdate());
    }

    public override void PerTickShared(double delta)
    {
    }

    public override void PerFrameShared(double delta)
    {
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
public struct SimpleShapeStateUpdate
{
    [Key(0)]
    public Godot.Vector3 position;
    [Key(1)]
    public Godot.Vector3 velocity;
    [Key(2)]
    public Godot.Vector3 rotation;
}