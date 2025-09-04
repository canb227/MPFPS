using GameMessages;
using Godot;
using ImGuiGodot.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class SpectatorCharacter : PlayerCharacter
{
    public Vector3 desiredVelocity = new();

    public override void _Ready()
    {
        this.body = GetNode<CharacterBody3D>("body");
        this.cam = GetNode<Camera3D>("cam");
        this.Name = "PlayerSpectator";
    }

    public override void _PhysicsProcess(double delta)
    {


        Vector2 inputMv = ((PlayerController)controller).movementInput;
        float forwardInput = inputMv.X *.1f; // negative is left, positive is right
        float sideInput = inputMv.Y * .1f ; // negative is forward, positive is backward
        Vector3 localVel = GetLocalVelocity();

        int Hspeed = 75;
        int Vspeed = 5;
        if (((PlayerController)controller).inputs.TryGetValue("SPRINT", out ActionInput sprint))
        {
            if (sprint.Pressed)
            {
                Hspeed = 200;
                Vspeed = 10;
            }
            else
            {
                Hspeed = 75;
                Vspeed = 5;
            }
        }

        if (forwardInput==0)
        {
            localVel.Z = 0;
        }
        else
        {

            localVel.Z = forwardInput * Hspeed;
        }

        if (sideInput == 0)
        {
            localVel.X = 0;
        }
        else
        {
            localVel.X = sideInput * Hspeed;
        }

        localVel.Y = 0;
        if (((PlayerController)controller).inputs.TryGetValue("CROUCH", out ActionInput crouch))
        {
            if (crouch.Pressed)
            {
                localVel.Y = -Vspeed;
            }

        }
        if (((PlayerController)controller).inputs.TryGetValue("JUMP", out ActionInput jump))
        {
            if (jump.Pressed)
            {
                localVel.Y = Vspeed;
            }
        }

        desiredVelocity = GlobalizeVelocity(localVel);


        UpdateVelocity(desiredVelocity);
        body.MoveAndSlide();
    }

    private void UpdateVelocity(Vector3 vector3)
    {
        body.Velocity = vector3;
    }

    public override void _Process(double delta)
    {
        cam.GlobalPosition = body.GetGlobalTransformInterpolated().Origin;
        float mouseY = ((PlayerController)controller).mouseRelativeAccumulator.Y * Global.Config.loadedPlayerConfig.mouseSensY;
        float mouseX = ((PlayerController)controller).mouseRelativeAccumulator.X * Global.Config.loadedPlayerConfig.mouseSensX;

        float newXRot = body.RotationDegrees.X - mouseY;
        float newYRot = body.RotationDegrees.Y - mouseX;

        if (newXRot > 85) { newXRot = 85; }
        if (newXRot < -85) { newXRot = -85; }

        UpdateRotation(new Vector3(newXRot, newYRot, 0));
        //body.RotationDegrees = new Vector3(newXRot, newYRot, body.RotationDegrees.Z);
        ((PlayerController)controller).mouseRelativeAccumulator = Vector2.Zero; // Reset the mouse relative accumulator after applying it to the rotation

    }

    private void UpdateRotation(Vector3 vector3)
    {
        body.RotationDegrees = vector3;
        cam.RotationDegrees = vector3;
    }

    public Vector3 GetLocalVelocity()
    {
        return body.Velocity.Rotated(Vector3.Up, -body.Rotation.Y);
    }

    private Vector3 GlobalizeVelocity(Vector3 localVel)
    {
        return localVel.Rotated(Vector3.Up, body.Rotation.Y);
    }
}

