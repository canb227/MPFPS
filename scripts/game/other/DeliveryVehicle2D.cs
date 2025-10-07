using System;
using Godot;

public partial class DeliveryVehicle2D : CharacterBody2D
{
    [Export] public float RotationSpeed = 3.0f;   // radians per second
    [Export] public float Acceleration = 800.0f;  // pixels per second^2
    [Export] public float MaxSpeed = 300.0f;      // pixels per second
    [Export] public float Friction = 600.0f;      // pixels per second^2

    [Export] public Area2D finishArea;
    [Export] public GODeliveryGameMonitor deliveryGameMonitor;
    public override void _Ready()
    {
        base._Ready();
        finishArea.BodyEntered  += OnFinishBodyEntered;
    }





    //called from GODeliveryGameMonitor PerTickAuth
    public void PerTick(PlayerInputData input, double delta)
    {
        float dt = (float)delta;

        // Rotation input
        float turn = 0f;
        if (input.actions.HasFlag(ActionFlags.MoveLeft))
        {
            turn -= 1f;
        }
        if (input.actions.HasFlag(ActionFlags.MoveRight))
        {
            turn += 1f;
        }
        Rotation += turn * RotationSpeed * dt;

        // Thrust input
        float thrustInput = 0f;
        if (input.actions.HasFlag(ActionFlags.MoveBackward))
        {
            thrustInput -= 1f;
        }
        if (input.actions.HasFlag(ActionFlags.MoveForward))
        {
            thrustInput += 1f;
        }

        // Local forward vector (the tank's facing direction is +X in local space)
        Vector2 forward = Vector2.Right.Rotated(Rotation);

        // Apply acceleration as force along local forward/backward
        if (Mathf.Abs(thrustInput) > 0f)
        {
            Velocity += forward * (thrustInput * Acceleration * dt);
        }
        else
        {
            // Apply friction when no thrust
            Velocity = Velocity.MoveToward(Vector2.Zero, Friction * dt);
        }

        // Clamp max speed
        if (Velocity.Length() > MaxSpeed)
            Velocity = Velocity.Normalized() * MaxSpeed;

        // Move with physics
        MoveAndSlide();
    }

    private void OnFinishBodyEntered(Node2D body)
    {
        if (body == this)
        {
            deliveryGameMonitor.MiniGameWon();
        }
    }
}