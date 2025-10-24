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
    [Export] AudioStreamPlayer3D audioStreamPlayer3D { get; set; }
    [Export] public double fireRate { get; set; } = 8; //number of shots per second
    [Export] public AmmoType ammoType { get; set; } = AmmoType.RifleAmmo;
    [Export] public int currentMagazineAmmo { get; set; } = 30;
    [Export] public int magazineSize { get; set; } = 30;
    [Export] public float reloadTimeSeconds { get; set; } = 2;
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



    private ActionFlags lastTickActions;

    private int lastFireIndex;
    private double _timeUntilFire;
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
            Global.ui.inGameUI.PlayerUIManager.UpdateAmmoUI(currentMagazineAmmo, basicPlayerCharacter.ammoStored[ammoType], magazineSize);
        }
    }
    public override void HandleInput(ActionFlags input)
    {
        if (currentMagazineAmmo != magazineSize && !lastTickActions.HasFlag(ActionFlags.Reload) && input.HasFlag(ActionFlags.Reload))
        {
            if (Global.gameState.GameObjects[Global.gameState.PlayerIDToControlledCharacter[equippedBySteamID]] is BasicPlayerCharacter basicPlayerCharacter)
            {
                if (basicPlayerCharacter.ammoStored[ammoType] > 0)
                {
                    reloading = true;
                    reloadTimeLeft = reloadTimeSeconds;
                    //play reload sound
                    audioStreamPlayer3D.Stream = GD.Load<AudioStream>("res://assets/audio/weapons/basic/ar2_reload.wav");
                    audioStreamPlayer3D.Play();
                    //play reload animation
                    //animationPlayer.Play("fire");
                }
                else
                {
                    audioStreamPlayer3D.Stream = GD.Load<AudioStream>("res://assets/audio/weapons/basic/ar2_empty.wav");
                    audioStreamPlayer3D.Play();
                }
            }
            else
            {
                Logging.Error("Non-BasicPlayerCharacter trying to reload a gun, add an implementation here if needed, undefined behavior currently", "BasicGun");
            }
        }
        else if (input.HasFlag(ActionFlags.Fire) && !reloading)
        {
            double cooldown = 1.0 / fireRate;

            if (_timeUntilFire <= 0)
            {
                _timeUntilFire = 1.0 / fireRate;
                if (currentMagazineAmmo > 0)
                {
                    //shoot the gun
                    Logging.Log($"Pew!", "BasicGun");
                    currentMagazineAmmo--;
                    if (Global.gameState.GameObjects[Global.gameState.PlayerIDToControlledCharacter[equippedBySteamID]] is BasicPlayerCharacter basicPlayerCharacter)
                    {
                        Global.ui.inGameUI.PlayerUIManager.UpdateAmmoUI(currentMagazineAmmo, basicPlayerCharacter.ammoStored[ammoType], magazineSize);
                    }
                    else
                    {
                        Logging.Warn("Non-BasicPlayer using a gun, undefined behavior", "BasicGun");
                    }

                    //play gunshot sound
                    string[] gunSounds =
                    {
                        "res://assets/audio/weapons/basic/fire1.wav",
                    };
                    Random rand = new();
                    int index;
                    do
                    {
                        index = rand.Next(gunSounds.Length);
                    } while (index == lastFireIndex && gunSounds.Length > 1);
                    audioStreamPlayer3D.Stream = GD.Load<AudioStream>(gunSounds[index]);
                    //randomize pitch ±2%
                    float pitchVariation = (float)(0.98 + rand.NextDouble() * 0.04);
                    audioStreamPlayer3D.PitchScale = pitchVariation;

                    audioStreamPlayer3D.Play();

                    //play firing animation
                    //animationPlayer.Play("fire");
                }
                else
                {
                    //gun is empty
                    Logging.Log($"Gun Empty!", "BasicGun");
                    //play gunshot sound
                    string[] gunSounds =
                    {
                        "res://assets/audio/weapons/basic/ar2_empty.wav",
                    };
                    Random rand = new();
                    int index;
                    do
                    {
                        index = rand.Next(gunSounds.Length);
                    } while (index == lastFireIndex && gunSounds.Length > 1);
                    audioStreamPlayer3D.Stream = GD.Load<AudioStream>(gunSounds[index]);
                    //randomize pitch ±5%
                    float pitchVariation = (float)(0.98 + rand.NextDouble() * 0.04);
                    audioStreamPlayer3D.PitchScale = pitchVariation;
                    audioStreamPlayer3D.Play();

                    //play firing animation
                    //animationPlayer.Play("fire");
                }
            }
        }
    }

    public override void OnEquipped(ulong bySteamID)
    {
        base.OnEquipped(bySteamID);
        if (Global.gameState.GameObjects[Global.gameState.PlayerIDToControlledCharacter[equippedBySteamID]] is BasicPlayerCharacter basicPlayerCharacter)
        {
            Global.ui.inGameUI.PlayerUIManager.UpdateAmmoUI(currentMagazineAmmo, basicPlayerCharacter.ammoStored[ammoType], magazineSize);
        }
        else
        {
            Logging.Warn("Non-BasicPlayer using a gun, undefined behavior", "BasicGun");
        }
    }


    public override void OnUnequipped(ulong bySteamID)
    {
        base.OnUnequipped(bySteamID);
        //reset reload progress
        reloading = false;
        reloadTimeLeft = reloadTimeSeconds;
        Global.ui.inGameUI.PlayerUIManager.UpdateAmmoUI(0, 0, 0);
        audioStreamPlayer3D.Stop();
    }
    
    public override void OnDropped(ulong bySteamID)
    {
        base.OnDropped(bySteamID);
        //reset reload progress
        reloading = false;
        reloadTimeLeft = reloadTimeSeconds;
        Global.ui.inGameUI.PlayerUIManager.UpdateAmmoUI(0, 0, 0);
        audioStreamPlayer3D.Stop();
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