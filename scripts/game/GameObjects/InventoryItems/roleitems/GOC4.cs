using System;
using Godot;



[GlobalClass]
public partial class GOC4 : GOBaseRoleItem
{
    [Export] Node3D fps { get; set; }
    [Export] Node3D tps { get; set; }
    [Export] CollisionShape3D collider { get; set; }
    [Export] AudioStreamPlayer3D audioStreamPlayer { get; set; }
    [Export] AudioStreamPlayer3D explosionAudioStream { get; set; }
    [Export] Area3D explosionRadius { get; set; }
    [Export] Label timeLeftLabel { get; set; }
    private double countdownMax { get; set; } = 60; //45
    private double countdown;
    private double timeSinceLastPlay = 0.0;     
    private bool planted { get; set; }
    public override bool pickupable { get; set; } = true;


    public override void HandleInput(ActionFlags input)
    {
        if (!lastTickActions.HasFlag(ActionFlags.Fire) && input.HasFlag(ActionFlags.Fire))
        {
            RPCManager.RPC(this, "PlaceC4", []);
        }
        base.HandleInput(input);
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void PlaceC4()
    {
        if (GetHeldBy() is BasicPlayerCharacter basicPlayerCharacter)
        {
            basicPlayerCharacter.DropEquipped();
        }
        planted = true;
        countdown = countdownMax;
        pickupable = false;
    }

    public override void _Process(double delta)
    {
        if (planted)
        {
            countdown -= delta;
            timeLeftLabel.Text = $"{TimeSpan.FromSeconds(countdown):mm\\:ss}";
            // Update timer
            timeSinceLastPlay += delta;

            // Scale interval: at countdownMax = 5s, at 0 = 0s
            double interval = 5.0 * (countdown / countdownMax);

            // Clamp so it never goes below e.g. 0.2s
            interval = Math.Max(interval, 0.2);

            if (timeSinceLastPlay >= interval)
            {
                audioStreamPlayer.Play();
                timeSinceLastPlay = 0.0;
            }
            if (countdown < 0 && Global.steamid == authority) //only authority can trigger explosion
            {
                DetonateC4();
            }
        }
    }


    public void DetonateC4()
    {
        float maxDamage = 100.0f;
        explosionAudioStream.Play();
         // Get all bodies currently inside the Area3D
        var bodies = explosionRadius.GetOverlappingBodies();

        foreach (var body in bodies)
        {
            if (body is Node3D node)
            {
                float distance = GlobalTransform.Origin.DistanceTo(node.GlobalTransform.Origin);

                // Scale damage by distance
                float radius = (explosionRadius.GetNode<CollisionShape3D>("CollisionShape3D").Shape as SphereShape3D).Radius;
                float t = distance / radius;
                float falloff = 1 - Mathf.SmoothStep(0, 1, t);
                float damage = Mathf.Max(0, maxDamage * falloff);


                if (node is IsDamagable d)
                {
                    if (distance < 15)
                    {
                        d.TakeDamage(100, 0, PainSoundType.Fire, 0);
                    }
                    else
                    {
                        d.TakeDamage(damage, 0, PainSoundType.Fire, 0);
                        d.TakeStunDamage(damage * 2, 0, PainSoundType.None, 0);
                    }                    
                }
            }
        }

        //cleanup
        planted = false;
        fps.Visible = false;
        tps.Visible = false;
        collider.Disabled = true;
        explosionRadius.Monitorable = false;
        explosionRadius.Monitoring = false;
    }
    

    public override void OnDropped(ulong bySteamID)
    {
        base.OnDropped(bySteamID);
    }
    public override void OnPickup(ulong bySteamID)
    {
        base.OnPickup(bySteamID);
    }

}