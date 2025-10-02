using Godot;
using MessagePack;

[GlobalClass]
public partial class SimpleShape : GOBaseRigidBody
{

    Vector3 desiredPosition;

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
        desiredPosition = sssu.position;
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
        Position = Position.Lerp(desiredPosition, (float)(0.1f * delta));
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
        throw new System.NotImplementedException();
    }

    public override void PerFrameShared(double delta)
    {
        throw new System.NotImplementedException();
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