using Godot;
using Godot.Collections;
using ImGuiGodot.Internal;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum AmmoType
{
    ShotgunAmmo,
    RifleAmmo,
    SniperAmmo,
}
[GlobalClass]
public partial class BasicGun : GOBaseInventoryItem, IsHoldable
{
    //A note on the audio player, we have two audio players because otherwise we cant have the gun firing sound continue as we start our reload, making it very jarring.
    //we can have multiple sounds of the same type playing on a player at the sametime, ie the gunshots overlap, but not if they are different audio files.
    [Export] AudioStreamPlayer3D audioStreamPlayer1 { get; set; }
    [Export] AudioStreamPlayer3D audioStreamPlayer2 { get; set; }
    [Export] PackedScene shotHitParticle { get; set; }
    [Export] public double fireRate { get; set; } = 8; //number of shots per second
    [Export] public AmmoType ammoType { get; set; } = AmmoType.RifleAmmo;
    [Export] public int currentMagazineAmmo { get; set; } = 30;
    [Export] public int magazineSize { get; set; } = 30;
    [Export] public float reloadTimeSeconds { get; set; } = 2;
    [Export] public int bulletsPerShot { get; set; } = 1;
    [Export] public float spread;
    [Export] public float physDamagePerShot = 5;
    [Export] public float stunDamagePerShot = 5;
    private float reloadTimeLeft { get; set; }
    public override InventoryGroupCategory category { get; set; } = InventoryGroupCategory.Weapon;
    public override bool droppable { get; set; } = true;
    public ulong currentlyHeldBy { get; set; }
    public bool customHeldPhysics { get; set; }
    public bool snapHoldNoPhysics { get; set; }
    public float heldWeight { get; set; }
    public float heldDrag { get; set; }
    public float heldFriction { get; set; }
    private bool reloading { get; set; }
    private GOBasePlayerCharacter playerHeldBy;


    private ActionFlags lastTickActions;

    private int lastFireIndex;
    private double _timeUntilFire;
    private double _timeUntilReload;
    private string[] gunSounds;
    private string[] emptySounds;
    private PhysicsDirectSpaceState3D directSpaceStateCache;
    public override void _Ready()
    {
        base._Ready();
        gunSounds = ["res://assets/audio/weapons/basic/fire1.wav"];
        emptySounds = ["res://assets/audio/weapons/basic/ar2_empty.wav"];
        directSpaceStateCache = GetWorld3D().DirectSpaceState;
    }

    public override void PerTickShared(double delta)
    {
        base.PerTickShared(delta);
        if (_timeUntilFire > 0)
        {
            _timeUntilFire -= delta;
        }
        if (_timeUntilReload > 0)
        {
            _timeUntilReload -= delta;
        }
        if (reloadTimeLeft > 0 && reloading)
        {
            reloadTimeLeft -= (float)delta;
        }
        else if (reloading)
        {
            Reload();
        }
    }
    private void Reload()
    {
        if (reloading && Global.gameState.GameObjects[Global.gameState.PlayerIDToControlledCharacter[equippedBySteamID]] is BasicPlayerCharacter basicPlayerCharacter)
        {
            int availableAmmo = basicPlayerCharacter.ammoStored[ammoType];
            int spaceLeft = magazineSize - currentMagazineAmmo;
            int ammoToLoad = Math.Min(spaceLeft, availableAmmo);

            currentMagazineAmmo += ammoToLoad;
            basicPlayerCharacter.AddToAmmoStored(ammoType, -ammoToLoad);
            reloading = false;
            UpdateUI(basicPlayerCharacter);
        }
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void ReloadAnimation()
    {
        //play reload sound
        audioStreamPlayer2.Stream = GD.Load<AudioStream>("res://assets/audio/weapons/basic/ar2_reload.wav");
        audioStreamPlayer2.Play();
        //play reload animation
        animationPlayer.SpeedScale = 1.0f;
        animationPlayer.Play("reload");
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void EmptyAudio()
    {
        audioStreamPlayer2.Stream = GD.Load<AudioStream>("res://assets/audio/weapons/basic/ar2_empty.wav");
        audioStreamPlayer2.Play();
    }

    public override void HandleInput(ActionFlags input)
    {
        if (currentMagazineAmmo != magazineSize && !lastTickActions.HasFlag(ActionFlags.Reload) && input.HasFlag(ActionFlags.Reload))
        {
            if (GetHeldBy() is BasicPlayerCharacter basicPlayerCharacter)
            {
                if (basicPlayerCharacter.ammoStored[ammoType] > 0)
                {
                    reloading = true;
                    reloadTimeLeft = reloadTimeSeconds;
                    RPCManager.RPC(this, "ReloadAnimation", []);
                }
                else
                {
                    if (_timeUntilReload <= 0)
                    {
                        _timeUntilReload = 1.0 / 8;
                        if (!audioStreamPlayer2.Playing)
                        {
                            RPCManager.RPC(this, "EmptyAudio", []);
                        }
                    }
                }
            }
            else
            {
                Logging.Error("Non-BasicPlayerCharacter trying to reload a gun, add an implementation here if needed, undefined behavior currently", "BasicGun");
            }
        }
        else if (input.HasFlag(ActionFlags.Fire) && !reloading)
        {
            if (_timeUntilFire <= 0)
            {
                _timeUntilFire = 1.0 / fireRate;
                if (currentMagazineAmmo > 0)
                {
                    RPCManager.RPC(this, "FireGun", []);
                }
                else
                {
                    //gun is empty
                    RPCManager.RPC(this, "PlayEmptySound" ,[]);
                }
            }
        }
    }
        
    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void FireGun()
    {
        Random rand = new();
        for (int i = 0; i < bulletsPerShot; i++)
        {
            // randomly modify weapon spread using temporary ray
            var spaceState = directSpaceStateCache;
            if(spaceState == null)
            {
                GD.Print("spaceState is null");
            }
            PhysicsRayQueryParameters3D ray = new PhysicsRayQueryParameters3D();
            ray.From = playerHeldBy.camera.GlobalPosition;
            ray.To = playerHeldBy.camera.ToGlobal(GetRandomBulletDirection(rand));
            ray.CollisionMask = (1 << 0) | (1 << 1) | (1 << 2) | (1 << 3);
            ray.CollideWithBodies = true;
            if (ray == null)
            {
                GD.Print("ray is null");
            }
            if(spaceState.IntersectRay(ray) == null)
            {
                GD.Print("spaceStateIntersectRay is null because of playerHeldBy: " + playerHeldBy);
            }
            var hitResult = spaceState.IntersectRay(ray);

            //shoot the gun
            if (hitResult.ContainsKey("collider"))
            {
                //spawn hit particle
                if (shotHitParticle != null)
                {
                    var hitParticle = shotHitParticle.Instantiate() as Node3D;
                    GetTree().Root.AddChild(hitParticle);
                    hitParticle.GlobalPosition = (Vector3)hitResult["position"];
                    //#pragma warning disable
                    //hitParticle.LookAt(hitParticle.GlobalPosition - (Vector3)hitResult["normal"]);

                    //janky solution to avoid the error using dot product
                    Vector3 direction = hitParticle.GlobalPosition - (Vector3)hitResult["normal"];
                    Vector3 up = Math.Abs(direction.Dot(Vector3.Up)) > 0.99f ? Vector3.Right : Vector3.Up;
                    hitParticle.LookAt(direction, up);
                }

                var hit = (Node)hitResult["collider"];

                //we have to like climb up the scene tree to look for the actual object, because players have static bodies to represent just their head and body hitbox
                //we do this so they are on their own layers for precision hitboxes on layer 3 and phys capsules on layer 5. not opposed to redesigning that eventually
                Node current = (Node)hit;
                while (current != null && current is not IsDamagable)
                    current = current.GetParent();

                if (current is IsDamagable target)
                {
                    Logging.Log($"Hit a IsDamagable object", "BasicGun");
                    //BasicPlayerCharacter takes stun damage = damage * 4, so 5 damage knocks out in 5 shots since 5(damage)*4(stun multipler)*5(num shots) = 100
                    target.TakeDamage(physDamagePerShot, equippedBySteamID, PainSoundType.Bullet);
                    target.TakeStunDamage(stunDamagePerShot, equippedBySteamID, PainSoundType.Bullet);
                }
            }
        }
        currentMagazineAmmo--;
        UpdateUI();

        PlayShotSound();

        //play firing animation
        animationPlayer.SpeedScale = (float)fireRate;
        animationPlayer.Play("fire");
    }

    public override void OnEquipped(ulong bySteamID)
    {
        base.OnEquipped(bySteamID);

        if (GetHeldBy() is BasicPlayerCharacter basicPlayerCharacter)
        {
            playerHeldBy = basicPlayerCharacter;
            UpdateUI(basicPlayerCharacter);
        }
        else
        {
            Logging.Warn("Non-BasicPlayer using a gun, undefined behavior", "BasicGun");
        }
    }


    public override void OnUnequipped(ulong bySteamID)
    {
        base.OnUnequipped(bySteamID);
        playerHeldBy = null;
        //reset reload progress
        reloading = false;
        reloadTimeLeft = reloadTimeSeconds;
        Global.ui.inGameUI.PlayerUIManager.UpdateAmmoUI(0, 0, 0);
        audioStreamPlayer1.Stop();
        audioStreamPlayer2.Stop();
        animationPlayer.Play("RESET");
    }
    
    public override void OnDropped(ulong bySteamID)
    {
        base.OnDropped(bySteamID);
        //reset reload progress
        reloading = false;
        reloadTimeLeft = reloadTimeSeconds;
        Global.ui.inGameUI.PlayerUIManager.UpdateAmmoUI(0, 0, 0);
        audioStreamPlayer1.Stop();
        audioStreamPlayer2.Stop();
        animationPlayer.Play("RESET");
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

    private void UpdateUI(BasicPlayerCharacter basicPlayer = null)
    {
        var player = basicPlayer != null ? basicPlayer : GetHeldBy();

        if (player is BasicPlayerCharacter basicPlayerCharacter)
        {
            Global.ui.inGameUI.PlayerUIManager.UpdateAmmoUI(
                currentMagazineAmmo,
                basicPlayerCharacter.ammoStored[ammoType],
                magazineSize
            );
        }
        else
        {
            Logging.Warn("Non-BasicPlayer using a gun, undefined behavior", "BasicGun");
        }
    }

    private void PlaySound(string soundResource, float pitchVariation)
    {
        audioStreamPlayer1.Stream = GD.Load<AudioStream>(soundResource);
        audioStreamPlayer1.PitchScale = pitchVariation;
        audioStreamPlayer1.Play();
    }

    private void PlayShotSound()
    {
        //play gunshot sound
        Random rand = new();
        int index;
        do
        {
            index = rand.Next(gunSounds.Length);
        } while (index == lastFireIndex && gunSounds.Length > 1);

        float pitchVariation = (float)(0.98 + rand.NextDouble() * 0.04);

        PlaySound(gunSounds[index], pitchVariation);
    }
    
    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void PlayEmptySound()
    {
        //play gunshot sound
        Random rand = new();
        int index;
        do
        {
            index = rand.Next(gunSounds.Length);
        } while (index == lastFireIndex && gunSounds.Length > 1);

        float pitchVariation = (float)(0.98 + rand.NextDouble() * 0.04);

        PlaySound(emptySounds[index], pitchVariation);
    }
    
    private Vector3 GetRandomBulletDirection(Random rand)
    {
        double randX = spread - (rand.NextDouble() * spread * 2);
        double randY = spread - (rand.NextDouble() * spread * 2);

        //lazy way right now. currently doing a square projection we will want to switch this to circle or sphere projection instead
        Vector2 randomPos = new Vector2((float)randX, (float)randY);

        return new Vector3(randomPos.X, randomPos.Y, -1) * 100;
    }
}

[MessagePackObject]
public struct BasicGunStateUpdate
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