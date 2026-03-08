using Godot;
using Godot.Collections;
using ImGuiGodot.Internal;
using MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum AmmoType
{
    ShotgunAmmo,
    RifleAmmo,
    SniperAmmo,
    MediDart,
}
[GlobalClass]
public partial class BasicGun : GOBaseInventoryItem, IsHoldable
{
    //A note on the audio player, we have two audio players because otherwise we cant have the gun firing sound continue as we start our reload, making it very jarring.
    //we can have multiple sounds of the same type playing on a player at the sametime, ie the gunshots overlap, but not if they are different audio files.
    [Export] public AudioStreamPlayer3D audioStreamPlayer1 { get; set; }
    [Export] public AudioStreamPlayer3D audioStreamPlayer2 { get; set; }
    [Export] public PackedScene shotHitParticle { get; set; }
    [Export] public double fireRate { get; set; } = 8; //number of shots per second
    [Export] public AmmoType ammoType { get; set; } = AmmoType.RifleAmmo;
    [Export] public int currentMagazineAmmo { get; set; } = 30;
    [Export] public int magazineSize { get; set; } = 30;
    [Export] public float reloadTimeSeconds { get; set; } = 2;
    [Export] public int bulletsPerShot { get; set; } = 1;
    [Export] public float spread;
    [Export] public float physDamagePerShot = 5;
    [Export] public float stunDamagePerShot = 5;
    [Export] public int maxPenetrations = 2;
    [Export] public float headshotMultiplier = 1.67f;

    public float reloadTimeLeft { get; set; }
    public override InventoryGroupCategory category { get; set; } = InventoryGroupCategory.Weapon;
    public override bool droppable { get; set; } = true;
    public bool reloading { get; set; }
    public GOBasePlayerCharacter playerHeldBy;
    [Export]public string iconPath;


    public ActionFlags lastTickActions;

    public int lastFireIndex;
    public double _timeUntilFire;
    public double _timeUntilReload;
    public string[] gunSounds;
    public string[] emptySounds;
    public bool waitingToFire;
    public override void _Ready()
    {
        base._Ready();
        if (ammoType == AmmoType.ShotgunAmmo)
        {
            gunSounds = ["res://assets/audio/weapons/basic/shotgun_fire6.wav"];
        }
        else if (ammoType == AmmoType.RifleAmmo)
        {
            gunSounds = ["res://assets/audio/weapons/basic/fire1.wav"];
        }
        else
        {
            gunSounds = ["res://assets/audio/weapons/basic/357_fire2.wav"];
        }        
        emptySounds = ["res://assets/audio/weapons/basic/ar2_empty.wav"];

        // Warm up the hit particle scene
        var warmup = shotHitParticle.Instantiate() as Node3D;
        warmup.Visible = false;
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
        if(waitingToFire)
        {
            FireGun();
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
        // audioStreamPlayer2.Stream = GD.Load<AudioStream>("res://assets/audio/weapons/basic/ar2_reload.wav");
        // audioStreamPlayer2.Play();
        PlaySound(audioStreamPlayer2, "res://assets/audio/weapons/basic/ar2_reload.wav", 1f);
        //play reload animation
        animationPlayer.SpeedScale = 1/(reloadTimeSeconds/2);
        animationPlayer.Play("reload");
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void EmptyAudio()
    {
        PlaySound(audioStreamPlayer2, "res://assets/audio/weapons/basic/ar2_empty.wav", 1f);
        // audioStreamPlayer2.Stream = GD.Load<AudioStream>("res://assets/audio/weapons/basic/ar2_empty.wav");
        // audioStreamPlayer2.Play();
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
                    RPCManager.RPCID(id, "ReloadAnimation", []);
                }
                else
                {
                    if (_timeUntilReload <= 0)
                    {
                        _timeUntilReload = 1.0 / 8;
                        if (!audioStreamPlayer2.Playing)
                        {
                            RPCManager.RPCID(id, "EmptyAudio", []);
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
                    RPCManager.RPCID(id, "TryFireGun", []);
                }
                else
                {
                    //gun is empty
                    RPCManager.RPCID(id, "PlayEmptySound", []);
                }
            }
        }
        if (input.HasFlag(ActionFlags.Aim))
        {
            if (NetworkUtils.IsMe(equippedBySteamID))
            {
                Global.gameState.GetCharacterControlledBy(equippedBySteamID).camera.Fov = Global.Config.loadedPlayerConfig.fov / 2;
            }
        }
        else
        {
            Global.gameState.GetCharacterControlledBy(equippedBySteamID).camera.Fov = Global.Config.loadedPlayerConfig.fov;
        }
    }
    
    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void TryFireGun()
    {
        waitingToFire = true;
    }

    public void FireGun()
    {
        Random rand = new();
        waitingToFire = false;
        float currentPhysDamage = physDamagePerShot;
        float currentStunDamage = stunDamagePerShot;
        bool headshot = false;
        for (int i = 0; i < bulletsPerShot; i++)
        {
            var exclude = new Array<Rid>();
            int penetrations = 0;

            var spaceState = GetWorld3D().DirectSpaceState;
            PhysicsRayQueryParameters3D ray = new PhysicsRayQueryParameters3D();
            Vector3 camPos = playerHeldBy.camera.GlobalTransform.Origin;
            Vector3 camForward = -playerHeldBy.camera.GlobalTransform.Basis.Z;
            Vector3 rayOrigin = camPos + camForward * 0.5f;
            Vector3 rayEnd = playerHeldBy.camera.ToGlobal(GetRandomBulletDirection(rand));

            while (true)
            {
                bool particlesSpawned = false;
                var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
                query.CollisionMask = (1 << 0) | (1 << 1) | (1 << 2) | (1 << 3);
                query.CollideWithBodies = true;
                query.CollideWithAreas = true;
                query.Exclude = exclude;

                var hitResult = spaceState.IntersectRay(query);
                if (hitResult.Count == 0)
                    break; // nothing else hit

                var collider = (Node)hitResult["collider"] as CollisionObject3D;
                Vector3 hitPos = (Vector3)hitResult["position"];
                Vector3 hitNormal = (Vector3)hitResult["normal"];

                // spawn particles, etc.


                // climb up to IsDamagable
                Node current = collider;
                while (current != null && current is not IsDamagable)
                    current = current.GetParent();

                if (current is IsDamagable target)
                {
                    if (target is BasicPlayerCharacter bpc && !particlesSpawned)
                    {
                        particlesSpawned = true;
                        var hitParticle = bpc.shotParticle.Instantiate() as Node3D;
                        GetTree().Root.AddChild(hitParticle);
                        hitParticle.GlobalPosition = (Vector3)hitResult["position"];

                        //janky solution to avoid the error using dot product
                        Vector3 direction = hitParticle.GlobalPosition - (Vector3)hitResult["normal"];
                        Vector3 up = Math.Abs(direction.Dot(Vector3.Up)) > 0.99f ? Vector3.Right : Vector3.Up;
                        hitParticle.LookAt(direction, up);

                        //add a hit effect based on target
                    }
                    if(Global.steamid == GetHeldBy().authority)
                    {
                        int shapeIndex = (int)hitResult["shape"];
                        CollisionShape3D shapeNode = collider.GetChild(shapeIndex) as CollisionShape3D;
                        if (shapeNode != null && shapeNode.IsInGroup("headshotcollider"))
                        {
                            headshot = true;
                            RPCManager.RPC((Node)target, "rpc_TakeDamage", new object[] { currentPhysDamage*headshotMultiplier, equippedBySteamID, PainSoundType.Bullet, 0 });
                            RPCManager.RPC((Node)target, "rpc_TakeStunDamage", new object[] { currentStunDamage *headshotMultiplier, equippedBySteamID, PainSoundType.Bullet, 0 });
                            //rifle does 33.4 stun (3 to knock) 15.03 dmg (7 to kill)
                            //sniper does 133.6 stun (1 to knock) 58.45 (2 to kill)
                            //shotgun :) does 13.36 per bullet total 106.88 stun (1 to knock) | does 6.68 per pullet total 53.44 dmg (2 to kill)
                        }
                        else
                        {
                            headshot = false;
                            RPCManager.RPC((Node)target, "rpc_TakeDamage", new object[] { currentPhysDamage, equippedBySteamID, PainSoundType.Bullet, 0 });
                            RPCManager.RPC((Node)target, "rpc_TakeStunDamage", new object[] { currentStunDamage, equippedBySteamID, PainSoundType.Bullet, 0 });
                            //rifle does 20 stun (5 to knock) 9 dmg (12 to kill)
                            //sniper does 80 stun (2 to knock) 35 dmg (3 to kill)
                            //shotgun :) does 8 per bullet total 64 stun (2 to knock) | does 4 per pullet total 32 dmg (4 to kill)
                        }

                    }

                    // check if it's a SwarmRobot
                    if ((current is SwarmRobot || current is HordeAgent) && penetrations < maxPenetrations)
                    {
                        penetrations++;

                        exclude.Add(collider.GetRid()); // skip this collider next time
                        rayOrigin = hitPos + camForward * 0.05f; // continue just past hit
                        continue; // loop again
                    }
                    else if(current is SwarmRobot sr)
                    {
                        //we have run out of penetration so we will reduce our damage by how much we dealt and if we have remaining damage continue
                        float targetHealthRemaining = sr.currentHealth;
                        if(headshot)
                        {
                            float effectiveHealth = targetHealthRemaining / headshotMultiplier;
                            if(effectiveHealth <= currentPhysDamage)
                            {
                                currentPhysDamage -= effectiveHealth;
                            }
                            else
                            {
                                effectiveHealth -= currentPhysDamage;
                                currentPhysDamage = 0;
                            }

                            if(effectiveHealth <= currentStunDamage)
                            {
                                currentStunDamage -= effectiveHealth;
                            }
                            else
                            {
                                currentStunDamage = 0;
                            }
                        }
                        else
                        {
                            if(targetHealthRemaining <= currentPhysDamage )
                            {
                                currentPhysDamage -= targetHealthRemaining;
                            }
                            else
                            {
                                targetHealthRemaining -= currentPhysDamage;
                                currentPhysDamage = 0;
                            }

                            if(targetHealthRemaining <= currentStunDamage)
                            {
                                currentStunDamage -= targetHealthRemaining;
                            }
                            else
                            {
                                currentStunDamage = 0;
                            }
                        }
                        if(currentPhysDamage + currentStunDamage > 0)
                        {
                            exclude.Add(collider.GetRid()); // skip this collider next time
                            rayOrigin = hitPos + camForward * 0.05f; // continue just past hit
                            continue; // loop again
                        }
                    }
                    else if(current is HordeAgent ha)
                    {
                        
                    }
                }


                if (shotHitParticle != null && !particlesSpawned)
                {
                    particlesSpawned = true;
                    var hitParticle = shotHitParticle.Instantiate() as Node3D;
                    GetTree().Root.AddChild(hitParticle);
                    hitParticle.GlobalPosition = (Vector3)hitResult["position"];

                    //janky solution to avoid the error using dot product
                    Vector3 direction = hitParticle.GlobalPosition - (Vector3)hitResult["normal"];
                    Vector3 up = Math.Abs(direction.Dot(Vector3.Up)) > 0.99f ? Vector3.Right : Vector3.Up;
                    hitParticle.LookAt(direction, up);
                }
                break; // stop if not SwarmRobot or no penetrations left
            }
        }
        
        currentMagazineAmmo--;
        UpdateUI();

        PlayShotSound();

        //play firing animation
        animationPlayer.SpeedScale = 7.5f;
        animationPlayer.Play("fire");
    }
        
    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void SpawnParticles()
    {

    }

    public override void OnEquipped(ulong bySteamID)
    {
        base.OnEquipped(bySteamID);

        if (GetHeldBy() is BasicPlayerCharacter basicPlayerCharacter)
        {
            playerHeldBy = basicPlayerCharacter;
            if(basicPlayerCharacter.authority == Global.steamid)
            {
                UpdateUI(basicPlayerCharacter);
            }
        }
        else
        {
            Logging.Warn("Non-BasicPlayer using a gun, undefined behavior", "BasicGun");
        }
    }


    public override void OnUnequipped(ulong bySteamID)
    {
        if (equippedBySteamID == Global.steamid)
        {
            Global.ui.inGameUI.PlayerUIManager.UpdateAmmoUI(0, 0, 0);
        }
        base.OnUnequipped(bySteamID);
        playerHeldBy = null;
        //reset reload progress
        reloading = false;
        reloadTimeLeft = reloadTimeSeconds;

        audioStreamPlayer1.Stop();
        audioStreamPlayer2.Stop();
        animationPlayer.Play("RESET");
    }

    public override void OnPickup(ulong bySteamID)
    {
        base.OnPickup(bySteamID);
        if(inInventoryOf == Global.steamid)
        {
            Global.ui.inGameUI.PlayerUIManager.UpdateInventorySlot(2,iconPath);
        }
    }

    
    public override void OnDropped(ulong bySteamID)
    {
        if(inInventoryOf == Global.steamid)
        {
            Global.ui.inGameUI.PlayerUIManager.UpdateInventorySlot(2,"");
            Global.ui.inGameUI.PlayerUIManager.UpdateAmmoUI(0, 0, 0);
        }
        base.OnDropped(bySteamID);
        //reset reload progress
        reloading = false;
        reloadTimeLeft = reloadTimeSeconds;
        audioStreamPlayer1.Stop();
        audioStreamPlayer2.Stop();
        animationPlayer.Play("RESET");
    }

    private void UpdateUI(BasicPlayerCharacter basicPlayer = null)
    {
        var player = basicPlayer != null ? basicPlayer : GetHeldBy();

        if (player is BasicPlayerCharacter basicPlayerCharacter)
        {
            if (equippedBySteamID == Global.steamid)
            {
                Global.ui.inGameUI.PlayerUIManager.UpdateAmmoUI(
                    currentMagazineAmmo,
                    basicPlayerCharacter.ammoStored[ammoType],
                    magazineSize
                );
            }
        }
        else
        {
            Logging.Warn("Non-BasicPlayer using a gun, undefined behavior", "BasicGun");
        }
    }

    private void PlaySound(AudioStreamPlayer3D audioPlayer, string soundResource, float pitchVariation)
    {
        if(equippedBySteamID == Global.steamid)
        {
            audioPlayer.Stream = GD.Load<AudioStream>(soundResource);
            audioPlayer.PitchScale = pitchVariation;
            audioPlayer.Play();
        }
        else
        {
            audioPlayer.Stream = GD.Load<AudioStream>(soundResource);
            audioPlayer.PitchScale = pitchVariation;
            audioPlayer.Play();
        }
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

        PlaySound(audioStreamPlayer1, gunSounds[index], pitchVariation);
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

        PlaySound(audioStreamPlayer1, emptySounds[index], pitchVariation);
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
