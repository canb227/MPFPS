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
    private RayCast3D gunRayCast;


    private ActionFlags lastTickActions;

    private int lastFireIndex;
    private double _timeUntilFire;
    private string[] gunSounds;
    private string[] emptySounds;
    public override void _Ready()
    {
        base._Ready();
        gunSounds = ["res://assets/audio/weapons/basic/fire1.wav"];
        emptySounds = ["res://assets/audio/weapons/basic/ar2_empty.wav"];
    }

    public override void PerTickShared(double delta)
    {
        base.PerTickShared(delta);
        if (_timeUntilFire > 0)
        {
            _timeUntilFire -= delta;
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
    private async void Reload()
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
                    //play reload sound
                    audioStreamPlayer2.Stream = GD.Load<AudioStream>("res://assets/audio/weapons/basic/ar2_reload.wav");
                    audioStreamPlayer2.Play();
                    //play reload animation
                    animationPlayer.SpeedScale = 1.0f;
                    animationPlayer.Play("reload");
                }
                else
                {
                    if(!audioStreamPlayer2.Playing)
                    {
                        audioStreamPlayer2.Stream = GD.Load<AudioStream>("res://assets/audio/weapons/basic/ar2_empty.wav");
                        audioStreamPlayer2.Play();
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
                    //shoot the gun
                    Logging.Log($"Pew!", "BasicGun");
                    if (gunRayCast.IsColliding())
                    {
                        var hit = gunRayCast.GetCollider();

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
                    currentMagazineAmmo--;
                    UpdateUI();

                    PlayShotSound();

                    //play firing animation
                    animationPlayer.SpeedScale = (float)fireRate;
                    animationPlayer.Play("fire");
                }
                else
                {
                    //gun is empty
                    Logging.Log($"Gun Empty!", "BasicGun");

                    PlayEmptySound();
                }
            }
        }
    }

    public override void OnEquipped(ulong bySteamID)
    {
        base.OnEquipped(bySteamID);

        if (GetHeldBy() is BasicPlayerCharacter basicPlayerCharacter)
        {
            gunRayCast = basicPlayerCharacter.gunRayCast;
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
        gunRayCast = null;
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

    private void PlayEmptySound()
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