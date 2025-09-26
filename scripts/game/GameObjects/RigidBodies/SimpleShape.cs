using Godot;
using MessagePack;

[GlobalClass]
public partial class SimpleShape : GOBaseRigidBody
{
    public override void _Ready()
    {
        base._Ready();
        if (authority!=Global.steamid)
        {
            SetPhysicsProcess(false);
        }
    }

    public override void ProcessStateUpdate(byte[] update)
    {
        SimpleShapeStateUpdate sssu = MessagePackSerializer.Deserialize<SimpleShapeStateUpdate>(update);
        LinearVelocity = sssu.velocity;
        Position = Position.Slerp(sssu.position,.5f);
    }

    public override byte[] GenerateStateUpdate()
    {
        SimpleShapeStateUpdate sssu = new();
        sssu.velocity = LinearVelocity;
        sssu.position = GlobalPosition;
        return MessagePackSerializer.Serialize(sssu);
    }

    public override void PerTickAuth(double delta)
    {

    }

    public override void PerTickLocal(double delta)
    {

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
}

[MessagePackObject]
public struct SimpleShapeStateUpdate
{
    [Key(0)]
    public Vector3 position;
    [Key(1)]
    public Vector3 velocity;
}