using Godot;
using ImGuiNET;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


[GlobalClass]
public partial class SwarmRobot : GOBaseNPC
{


    [Export]
    public SwarmRobotState state = SwarmRobotState.NONE;

    public override void _Ready()
    {
        base._Ready();


        navAgent.PathDesiredDistance = 0.5f;
        navAgent.TargetDesiredDistance = 0.5f;
        Logging.Log($"Spawned new SwarmRobot with initial state: {state} and target: {MovementTarget.Name}", "SwarmRobot");

    }

    public override void _Process(double delta)
    {
        if (Global.DrawDebugScreens)
        {
            ImGui.Begin("path");
            ImGui.Text($"Pathfinding Debug for: {Name}");
            ImGui.Text($"Target: {MovementTarget.Name}");
            ImGui.Text($"Target Pos: {MovementTarget.GlobalPosition}");
            ImGui.Text($"Self Pos: {GlobalPosition}");
            ImGui.Text($"Pathfind Next Target: {navAgent.GetNextPathPosition()}");
            ImGui.Text($"");
            ImGui.End();
        }
    }

    public override void PerTickAuth(double delta)
    {
        base._PhysicsProcess(delta);
        switch (state)
        {
            case SwarmRobotState.NONE:
                break;
            case SwarmRobotState.IDLE:
                break;
            case SwarmRobotState.WANDER:
                break;
            case SwarmRobotState.SIMPLECHASE:
                if (MovementTarget != null)
                {
                    Vector3 velocity = Velocity;
                    navAgent.TargetPosition = MovementTarget.GlobalPosition;

                    if (!IsOnFloor())
                    {
                        velocity.Y -= (float)(10 * delta);
                    }
                    else
                    {
                        if (navAgent.IsNavigationFinished())
                        {
                            if (IsOnFloor())
                            {
                                velocity.Y = 10;
                            }
                            Vector3 currentAgentPosition = GlobalTransform.Origin;
                            Vector3 newVel = currentAgentPosition.DirectionTo(MovementTarget.GlobalPosition) * 2;
                            velocity.X = newVel.X;
                            velocity.Z = newVel.Z;
                        }
                        else
                        {
                            Vector3 currentAgentPosition = GlobalTransform.Origin;
                            Vector3 nextPathPosition = navAgent.GetNextPathPosition();
                            Vector3 newVel = currentAgentPosition.DirectionTo(nextPathPosition) * 2;
                            velocity.X = newVel.X;
                            velocity.Z = newVel.Z;
                        }
                    }

                    Velocity = velocity;
                    MoveAndSlide();
                    return;
                }

                    break;
            default:
                break;
        }
    }
    
    private double retargetTimer = 0;
    private double retargetInterval = 3.0; // will randomize a bit

    private void UpdateTarget(double delta)
    {
        retargetTimer -= delta;
        if (retargetTimer <= 0)
        {
            retargetTimer = 3.0 + GD.RandRange(-1.0, 1.0); // 2–4 seconds

            // Find closest player
            Node3D closest = null;
            float closestDist = float.MaxValue;

            foreach (var player in GetTree().GetNodesInGroup("players")) // put your GOBasePlayer in "players" group
            {
                if (player is Node3D p)
                {
                    float dist = GlobalTransform.Origin.DistanceTo(p.GlobalTransform.Origin);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = p;
                    }
                }
            }

            if (closest != null)
                MovementTarget = closest;
        }
    }


    public override bool InitFromData(GameObjectConstructorData data)
    {
        base.InitFromData(data);
        this.MovementTarget = Global.instance.GetNode<Node3D>((String)data.paramList[0]);
        navAgent.TargetPosition = MovementTarget.GlobalPosition;
        this.state = (SwarmRobotState)data.paramList[1];
        return true;
    }

    public override byte[] GenerateStateUpdate()
    {
        SwarmRobotStateMessage message = new SwarmRobotStateMessage();
        message.transform = this.GlobalTransform;
        message.velocity = this.Velocity;
        message.targetNodePath = Global.instance.GetPathTo(MovementTarget);
        message.state = this.state;

        return MessagePackSerializer.Serialize(message);   
    }

    public override void ProcessStateUpdate(byte[] update)
    {
        SwarmRobotStateMessage message = MessagePackSerializer.Deserialize<SwarmRobotStateMessage>(update);
        this.Transform = message.transform;
        this.Velocity = message.velocity;
        this.MovementTarget = Global.instance.GetNode<Node3D>(message.targetNodePath);
        this.state = message.state;
    }
}

[MessagePackObject]
public struct SwarmRobotStateMessage
{
    [Key(0)]
    public Transform3D transform;

    [Key(1)]
    public Vector3 velocity;

    [Key(2)]
    public string targetNodePath;

    [Key(3)]
    public SwarmRobotState state;

}


public enum SwarmRobotState
{
    NONE,
    IDLE,
    WANDER,
    SIMPLECHASE,
}