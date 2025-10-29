using Godot;



[GlobalClass]
public partial class GOC4 : GOBaseRoleItem
{
    [Export] Node3D fps { get; set; }
    [Export] Node3D tps { get; set; }
    [Export] CollisionShape3D collider { get; set; }
    [Export] AudioStreamPlayer3D audioStreamPlayer { get; set; }
    [Export] Area3D explosionRadius { get; set; }
    private double countdown { get; set; } = 45;
    private bool planted { get; set; }
    private double beepTimer = 0.0;

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
    }

    public override void _Process(double delta)
    {
        if (planted)
        {
            countdown -= delta;
            if (audioStreamPlayer.Playing)
            {
                audioStreamPlayer.Play();
            }
            if (countdown < 0 && Global.steamid == authority) //only authority can trigger explosion
            {
                DetonateC4();
            }
        }
    }
    
    private double GetBeepInterval()
    {
        // Example: linearly map countdown (10 → 0) to interval (1.0 → 0.1)
        double maxInterval = 1.0;
        double minInterval = 0.1;
        double maxCountdown = 10.0; // adjust to your bomb timer length

        double t = Mathf.Clamp(countdown / maxCountdown, 0.0, 1.0);
        return minInterval + (maxInterval - minInterval) * t;
    }


    public void DetonateC4()
    {
        float maxDamage = 100.0f;
        float force = 50.0f; // tweak this for stronger/weaker knockback

         // Get all bodies currently inside the Area3D
        var bodies = explosionRadius.GetOverlappingBodies();

        foreach (var body in bodies)
        {
            if (body is Node3D node)
            {
                float distance = GlobalTransform.Origin.DistanceTo(node.GlobalTransform.Origin);

                // Scale damage by distance
                float radius = (explosionRadius.GetNode<CollisionShape3D>("CollisionShape3D").Shape as SphereShape3D).Radius;
                float damage = Mathf.Max(0, maxDamage * (1 - (distance / radius)));

                if (node.HasMethod("TakeDamage"))
                    node.Call("TakeDamage", damage);

                // Apply physics impulse if it's a rigid body
                if (node is RigidBody3D rb)
                {
                    Vector3 dir = (rb.GlobalTransform.Origin - GlobalTransform.Origin).Normalized();
                    float scaledForce = force * (1 - (distance / radius));
                    rb.ApplyImpulse(Vector3.Zero, dir * scaledForce);
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