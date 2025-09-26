using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class Player : CharacterBody3D
{

    [Export]
    private PlayerInput input;

    [Export]
    private MeshInstance3D mesh;

    [Export]
    private CollisionShape3D collider;

    [Export]
    private Camera3D cam;

    private int _playerID;

    [Export]
    public int playerID { get { return _playerID; } set { _playerID = value; SetMultiplayerAuthority(playerID); } }

    [Export]
    public float speed = 5;

    public float sens = 0.005f;

    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    public override void _Ready()
    {
        if (playerID == Multiplayer.GetUniqueId())
        {
            cam.Current = true;
            Input.MouseMode = Input.MouseModeEnum.Captured;

        }
        else
        {
            cam.Current = false;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsMultiplayerAuthority())
        {
            Vector3 velocity = Velocity;
            if (!IsOnFloor())
            {
                velocity.Y -= gravity * (float)delta;
            }

            Vector3 direction = Transform.Basis * new Vector3(input.movementInputVector.X, 0, input.movementInputVector.Y);
            direction = direction.Normalized();

            if (!direction.IsEqualApprox(Vector3.Zero))
            {
                velocity.X = direction.X * speed;
                velocity.Z = direction.Z * speed;
            }
            else
            {
                velocity.X = 0;
                velocity.Z = 0;
            }
            Velocity = velocity;

            RotateY((float)(-input.mouseInputAccumulator.X * sens));
            cam.RotateX((float)(-input.mouseInputAccumulator.Y * sens));
            input.mouseInputAccumulator = Vector2.Zero;
        }
        else
        {

        }
        MoveAndSlide();
    }

    public override void _Process(double delta)
    {

    }
}

