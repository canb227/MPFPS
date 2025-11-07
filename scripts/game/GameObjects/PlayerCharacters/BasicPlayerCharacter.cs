
using Godot;
using ImGuiNET;
using MessagePack;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;

public enum CharacterState
{
    Living,
    Missing,
    Dead
}
[GlobalClass]
public partial class BasicPlayerCharacter : GOBasePlayerCharacter, IsDamagable, HasInventory
{
    public event Action<ulong> KnockedOut;
    public event Action<ulong> Killed;
    [Export] public AudioStreamPlayer3D characterSFX;
    [Export] public AudioStreamPlayer3D movementSFX;
    [Export] public AnimationTree animationTree;
    [Export] public CollisionShape3D collider;
    [Export] public ColorRect hurtColorRect;
    private ShaderMaterial hurtShaderMaterial;



    public CharacterSoundManager characterSoundManager = new();
    public int roleCredits { get; set; }
    public float maxHealth { get; set; } = 100;
    public float currentHealth { get; set; } = 100;
    public float maxStunBar { get; set; } = 100;
    public float currentStunBar { get; set; } = 100;
    public float currentTimeUntilStunRegen { get; set; } = 0;
    public float stunRegenDelaySeconds { get; set; } = 5;
    public float stunRegenRatePerSecond { get; set; } = 20;
    public Inventory inventory { get; set; } = new();
    public IsInventoryItem equipped { get; set; }
    public CharacterState state { get; set; }
    public float camXRotMax = 85;
    public float camXRotMin = -85;
    public float baseSpeed = 6;
    public float acceleration = 1;
    public float deceleration = 1;
    public float finalSpeed;
    private Vector3 jumpVelocity = new Vector3(0, 6, 0);
    private bool airbrake = false;
    //item bools
    public bool handcuffed;
    public bool knockedOut;
    private bool crouched;
    public bool onGround = true;
    private float fov = 90;


    public Dictionary<AmmoType, int> ammoStored = new() //should be all 0 for production
    {
        {AmmoType.ShotgunAmmo, 0 },
        {AmmoType.RifleAmmo, 0 },
        {AmmoType.SniperAmmo, 0 },
    };
    public Dictionary<AmmoType, int> maxAmmoStored = new()
    {
        {AmmoType.ShotgunAmmo, 24 },
        {AmmoType.RifleAmmo, 60 },
        {AmmoType.SniperAmmo, 30 },
    };

    private int currentItemSlot;
    private InventoryGroupCategory currentGroup;

    public override void _Ready()
    {
        base._Ready();
        Logging.Log($"FOVIS: {Global.Config.loadedPlayerConfig.fov}", "FOV");
        Global.ui.inGameUI.ScoreBoard.AddLivingWorkerPlayerRow(authority);

        currentGroup = InventoryGroupCategory.Hands;
        currentItemSlot = 0;
        this.CollisionLayer = 1 << 4; //5
        this.CollisionMask = (1 << 0) | (1 << 1) | (1 << 4);//1,2,5
        priority = 100;

        interactRayCast = new();
        interactRayCast.TargetPosition = new Vector3(0, 0, -4);
        interactRayCast.CollideWithBodies = true;
        interactRayCast.CollisionMask = (1 << 0) | (1 << 1) | (1 << 2) | (1 << 3); //layer 1, 2, 3, 4, world, entities, players(hitboxes), items, 
        camera.AddChild(interactRayCast);

        //we scale ourselves
        Scale = new(0.75f, 0.75f, 0.75f);

        hurtShaderMaterial = hurtColorRect.Material as ShaderMaterial;

        role = Global.gameState.PlayerData[authority].role;
        GameState.PlayerDataReceivedEvent += GameState_PlayerDataReceivedEvent;

        //weird phys bug fix, set to knocked out then back TODO
        collider.RotationDegrees = new Vector3(90, 0, 0);
        collider.Position = new Vector3(0, -0.634f, 0);
        ((CapsuleShape3D)collider.Shape).Radius = 0.186f;
        ((CapsuleShape3D)collider.Shape).Height = 2.3f;
                
        collider.RotationDegrees = new Vector3(0, 0, 0);
        collider.Position = new Vector3(0, 0.442f, 0);
        ((CapsuleShape3D)collider.Shape).Radius = 0.5f;
        ((CapsuleShape3D)collider.Shape).Height = 2.4f;

    }

    private void GameState_PlayerDataReceivedEvent(PlayerData data, ulong sender)
    {
        if (sender==authority)
        {
            role = data.role;
        }
    }

    public override bool InitFromData(GameObjectConstructorData data)
    {
        Global.gameState.gameModeManager.basicPlayers.Add(authority, this);
        Global.gameState.gameModeManager.playerStats[authority] = new PlayerRoundStats();

        base.InitFromData(data);
        return true;
    }

    public override void ProcessStateUpdate(byte[] _update)
    {
        BasicPlayerStateUpdate update = MessagePackSerializer.Deserialize<BasicPlayerStateUpdate>(_update);
        GlobalRotation = update.Rotation;
        GlobalPosition = update.Position;
        camera.Rotation = update.CameraRotation;
        Velocity = update.Velocity;
        crouched = update.Crouched;
        onGround = update.OnGround;
    }

    public override byte[] GenerateStateUpdate()
    {
        BasicPlayerStateUpdate update = new BasicPlayerStateUpdate();
        update.Rotation = GlobalRotation;
        update.Position = GlobalPosition;
        update.CameraRotation = camera.Rotation;
        update.OnGround = IsOnFloor();
        update.Velocity = Velocity;
        update.Crouched = crouched;
        onGround = IsOnFloor();
        return MessagePackSerializer.Serialize(update);
    }

    //various input functions, PerTickAuth is the main loop
    public override void PerTickAuth(double delta)
    {
        base.PerTickAuth(delta);
        if(!knockedOut)
        {
            //we wrap each one because an input could kill the character meaning the later calls have no input anymore
            if (input != null)
            {
                HandleNonMovementInput(delta);
            }
            if (input != null)
            {
                HandleEquippedPassthruInput(delta);
            }
            if (input != null)
            {
                HandleMovementInputAndPhysics(delta);
                lastTickActions = input.actions;
            }
        }

        //stun regen
        if (currentTimeUntilStunRegen <= 0)
        {
            if (currentStunBar < maxStunBar)
            {
                currentStunBar = Math.Min(currentStunBar + (stunRegenRatePerSecond * (float)delta), maxStunBar);
                if (controllingPlayerID == Global.steamid)
                {
                    Global.ui.inGameUI.PlayerUIManager.UpdateStunUI((int)currentStunBar, (int)maxStunBar);
                }
            }
            if(currentStunBar == maxStunBar && knockedOut)
            {
                if (controllingPlayerID == Global.steamid)
                {
                    RPCManager.RPC(this, "rpc_WakeUp", []);
                }
            }
        }
        else
        {
            currentTimeUntilStunRegen -= (float)delta;
        }
    }


    public override void PerFrameShared(double delta)
    {
        if (input != null)
        {
            HandleMouseLook(delta);
        }
    }

    public override void PerTickShared(double delta)
    {
        base.PerTickShared(delta);

        //use input from local and remote players to calculate footsteps
        if (input != null && !knockedOut)
        {

            if (!lastTickActions.HasFlag(ActionFlags.Jump) && input.actions.HasFlag(ActionFlags.Jump))
            {
                if (IsOnFloor())
                {
                    characterSoundManager.PlayMovementSound(movementSFX, MovementSoundType.Generic, true);
                }
            }
            else if (IsOnFloor() && Math.Abs(Velocity.Z) + Math.Abs(Velocity.X) > 0.0f && !crouched)
            {
                characterSoundManager.PlayMovementSound(movementSFX, MovementSoundType.Generic, false);
            }
        }
        
        UpdateAnimationTree();
    }


    private void HandleNonMovementInput(double delta)
    {
        if (!handcuffed)
        {
            if (!lastTickActions.HasFlag(ActionFlags.Use) && input.actions.HasFlag(ActionFlags.Use))
            {
                if (interactRayCast.IsColliding())
                {
                    var hit = interactRayCast.GetCollider();
                    if (hit is IsInventoryItem s)
                    {
                        if (s.pickupable)
                        {
                            Logging.Log("Calling Pickup!", "BasicPlayerCharacter");
                            Pickup(s);
                        }

                    }
                    else if (hit is IsInteractable i)
                    {
                        i.Local_OnInteract(id);
                    }
                    else if (hit is GOAmmoBox ammoBox)
                    {
                        if(ammoStored[ammoBox.ammoType] < maxAmmoStored[ammoBox.ammoType])
                        {
                            RPCManager.RPC(ammoBox, "PickupAmmo", []);
                            ammoStored[ammoBox.ammoType] = Math.Min(ammoBox.ammoAmount + ammoStored[ammoBox.ammoType], maxAmmoStored[ammoBox.ammoType]);
                            if(equipped is BasicGun basicGun)
                            {
                                if(basicGun.ammoType == ammoBox.ammoType)
                                {
                                    Global.ui.inGameUI.PlayerUIManager.UpdateStoredAmmoUI(ammoStored[ammoBox.ammoType]);
                                }
                            }
                        }

                    }
                    else
                    {
                        Node current = (Node)hit;
                        while (current != null && current is not BasicPlayerCharacter)
                            current = current.GetParent();

                        if (current is BasicPlayerCharacter basicPlayerCharacter)
                        {
                            switch (basicPlayerCharacter.state)
                            {
                                case CharacterState.Living:
                                    if (basicPlayerCharacter.handcuffed)
                                    {
                                        DropEquipped();
                                    }
                                    break;

                                case CharacterState.Missing:
                                    RPCManager.RPC(basicPlayerCharacter, "OnFound", []);
                                    Global.ui.inGameUI.PlayerUIManager.deadPlayerScreen.OpenDeadPlayerScreen(basicPlayerCharacter); //show dead player ui stuff
                                    break;

                                case CharacterState.Dead:
                                    Global.ui.inGameUI.PlayerUIManager.deadPlayerScreen.OpenDeadPlayerScreen(basicPlayerCharacter); //show dead player ui stuff
                                    break;
                            }

                        }
                    }

                }
            }
            if (Global.steamid == authority && !lastTickActions.HasFlag(ActionFlags.OpenShop) && input.actions.HasFlag(ActionFlags.OpenShop))
            {
                if (team == Team.Traitor || team == Team.Manager)
                {
                    if (!Global.ui.inGameUI.PlayerUIManager.roleShopScreen.Visible)
                    {
                        Global.ui.inGameUI.PlayerUIManager.roleShopScreen.OpenRoleShopScreen();
                    }
                    else
                    {
                        Global.ui.inGameUI.PlayerUIManager.roleShopScreen.CloseRoleShopScreen();
                    }
                }
            }
            if (!lastTickActions.HasFlag(ActionFlags.DropItem) && input.actions.HasFlag(ActionFlags.DropItem))
            {
                DropEquipped();
            }
            if (!lastTickActions.HasFlag(ActionFlags.InventorySlot1) && input.actions.HasFlag(ActionFlags.InventorySlot1))
            {
                EquipNextFromSlot(InventoryGroupCategory.Hands);
            }
            else if (!lastTickActions.HasFlag(ActionFlags.InventorySlot2) && input.actions.HasFlag(ActionFlags.InventorySlot2))
            {
                EquipNextFromSlot(InventoryGroupCategory.Weapon);
            }
            else if (!lastTickActions.HasFlag(ActionFlags.InventorySlot3) && input.actions.HasFlag(ActionFlags.InventorySlot3))
            {
                EquipNextFromSlot(InventoryGroupCategory.Accessory);
            }
            else if (!lastTickActions.HasFlag(ActionFlags.InventorySlot4) && input.actions.HasFlag(ActionFlags.InventorySlot4))
            {
                EquipNextFromSlot(InventoryGroupCategory.Role);
            }
            if (input.actions.HasFlag(ActionFlags.Aim))
            {
                camera.Fov = Global.Config.loadedPlayerConfig.fov/2;
            }
            else
            {
                camera.Fov = Global.Config.loadedPlayerConfig.fov;
            }
        }
    }

    public void EquipNextFromSlot(InventoryGroupCategory category)
    {
        RPCManager.RPC(this, "rpc_EquipNextFromSlot", [category]);
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void rpc_EquipNextFromSlot(InventoryGroupCategory category)
    {
        if (inventory.GetGroup(category).items.Any())
        {
            Logging.Log($"Equip Next {category}!", "BasicPlayerCharacter");
            if (currentGroup != category)
            {
                currentItemSlot = -1;
            }
            currentGroup = category;
            InventoryGroup group = inventory.GetGroup(category);
            if (group.items.Count - 1 > currentItemSlot)
            {
                currentItemSlot++;
                Equip(category, currentItemSlot);
            }
        }
    }

    //Equipment Functions

    public override void Pickup(IsInventoryItem item)
    {
        if (item is GameObject gameObject)
        {
            RPCManager.RPCID(id, "rpc_Pickup", [gameObject.id]);
        }
        else
        {
            Logging.Error("IsInventoryItem isn't a GameObject??", "BasicPlayerCharacter");
        }
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void rpc_Pickup(ulong itemID)
    {
        IsInventoryItem item = (IsInventoryItem)Global.gameState.GameObjects[itemID];
        if (item is GOBaseInventoryItem GOItem)
        {
            if (inventory.HasGroup(GOItem.category))
            {
                InventoryGroup group = inventory.GetGroup(GOItem.category);
                if (group.CanStoreItem(item))
                {
                    group.StoreItem(item);
                    if (IsMe())
                    {
                        GOItem.AttachToPlayer(firstPersonEquipmentAttachmentPoint);
                    }
                    else
                    {
                        GOItem.AttachToPlayer(thirdPersonEquipmentAttachmentPoint);
                    }
                    GOItem.OnPickup(controllingPlayerID);
                    //auto-equip weapons and accessories
                    if (group.category == InventoryGroupCategory.Weapon || group.category == InventoryGroupCategory.Accessory)
                    {
                        Equip(group.category, group.items.Count - 1);
                    }
                }
            }
        }

    }
    
    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void PickupReplace(IsInventoryItem item)
    {
        if (item is GOBaseInventoryItem GOItem)
        {
            if (inventory.HasGroup(GOItem.category))
            {
                InventoryGroup group = inventory.GetGroup(GOItem.category);
                if (group.CanStoreItem(item))
                {
                    group.StoreOrReplaceItem(item, out IsInventoryItem replaced);
                    if(replaced != null)
                    {
                        replaced.OnDropped(authority);
                    }
                    if (IsMe())
                    {
                        GOItem.AttachToPlayer(firstPersonEquipmentAttachmentPoint); 
                    }
                    else
                    {
                        GOItem.AttachToPlayer(thirdPersonEquipmentAttachmentPoint); 
                    }
                    GOItem.OnPickup(controllingPlayerID);
                    //auto-equip weapons and accessories
                    if(group.category == InventoryGroupCategory.Weapon || group.category == InventoryGroupCategory.Accessory)
                    {
                        Equip(group.category, group.items.Count-1);
                    }
                }
            }
        }

    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public override void Equip(InventoryGroupCategory category, int index = 0)
    {
        currentGroup = category;
        currentItemSlot = index;
        if (inventory.GetGroup(category) == null || inventory.GetGroup(category).GetItem() == null)
        {
            Logging.Error($"Cannot equip item!", "BasicPlayer");
            return;
        }
        if (equipped != null)
        {
            equipped.OnUnequipped(controllingPlayerID);
            equipped = null;
        }
        IsInventoryItem item = inventory.GetGroup(category).GetItemAt(index);
        if (item is GOBaseInventoryItem i)
        {
            equipped = i;
            i.OnEquipped(controllingPlayerID);
        }
    }
    
    public void DropEquipped()
    {
        if (Global.steamid == authority)
        {
            RPCManager.RPC(this, "rpc_DropEquipped", []);
        }
    }
    
    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void rpc_DropEquipped()
    {
        if (equipped != null && equipped.droppable)
        {
            equipped.OnUnequipped(authority);
            equipped.OnDropped(authority);
            inventory.GetGroup(equipped.category).items.Remove(equipped);
            equipped = null;
            Equip(InventoryGroupCategory.Hands);
        }
    }

    private void HandleEquippedPassthruInput(double delta)
    {
        if (equipped != null && equipped is Hands hands)
        {
            hands.HandleHandInput(input, delta);
        }
        else if (equipped != null)
        {
            equipped.HandleInput(input.actions);
        }

    }


    private float crouchMoveSpeedMultiplier = 0.7f;
    private float standHeight = 2.4f;
    private float crouchHeight = 1.75f;
    private float crouchSpeed = 12.0f;
    private Vector3 standingCameraOffset = new Vector3(0, -0.24f, -0.08f);
    private Vector3 crouchingCameraOffset = new Vector3(0, -0.75f, -0.08f);
    private float cameraLerpSpeed = 12.0f;
    private float cantUncrouchSticky = 0f;
    private void HandleMovementInputAndPhysics(double delta)
    {
        Velocity = HandleYAxis(Velocity, delta);

        // Handle crouch input
        bool wantsToCrouch = input.actions.HasFlag(ActionFlags.Crouch);

        //GD.Print(wantsToCrouch + " " + crouched + " " + " " + CanUncrouch() + ((CapsuleShape3D)collider.Shape).Height);
        //we we are crouched and can't uncrouch then we must continue crouching
        // if (!CanUncrouch())
        // {
        //     wantsToCrouch = true;
        // }
        //cantUncrouchSticky -= (float)delta;
        
        // Smoothly interpolate collider height
        var capsule = collider.Shape as CapsuleShape3D;
        if (capsule != null)
        {
            float currentHeight = capsule.Height;
            float targetHeight = wantsToCrouch ? crouchHeight : standHeight;
            capsule.Height = Mathf.Lerp(currentHeight, targetHeight, (float)delta * crouchSpeed);
            crouched = capsule.Height < (standHeight + crouchHeight) / 2;
        }
        Vector3 targetOffset = crouched ? crouchingCameraOffset : standingCameraOffset;
        camera.Position = camera.Position.Lerp(targetOffset, (float)delta * cameraLerpSpeed);

        // Adjust movement speed
        Vector3 localVelocity = CalculateLocalVelocity();
        if (crouched)
        {
            localVelocity.X *= crouchMoveSpeedMultiplier;
            localVelocity.Z *= crouchMoveSpeedMultiplier;
        }
        else if (handcuffed)
        {
            localVelocity.X *= crouchMoveSpeedMultiplier;
            localVelocity.Z *= crouchMoveSpeedMultiplier;
        }
        Velocity = PCUtils.GlobalizeVector(this, localVelocity);
        PushAwayRigidBodies();
        MoveAndSlide();


        //GD.Print(IsOnFloor();
    }
    
    bool CanUncrouch()
    {
        var capsule = collider.Shape as CapsuleShape3D;
        if (capsule == null) return false;

        // Temporarily increase height
        float originalHeight = capsule.Height;
        capsule.Height = standHeight;

        // Test if the taller shape would collide
        bool blocked = TestMove(Transform, Vector3.Zero);

        // Restore original height
        capsule.Height = originalHeight;

        return !blocked;
    }


    private float PushForceScalar = 1.0f;
    private float Mass = 80.0f;
    private void PushAwayRigidBodies()
    {
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            KinematicCollision3D CollisionData = GetSlideCollision(i);

            GodotObject UnkObj = CollisionData.GetCollider();

            if (UnkObj is RigidBody3D)
            {
                RigidBody3D Obj = UnkObj as RigidBody3D;

                // Objects with more mass than us should be harder to push.
                // But doesn't really make sense to push faster than we are going
                float MassRatio = Mathf.Min(1.0f, Mass / Obj.Mass);

                // Optional add: Don't push object at all if it's 4x heavier or more
                if (MassRatio < 0.25f) continue;

                Vector3 PushDir = -CollisionData.GetNormal();
                PushDir.Y = 0;

                // How much velocity the object needs to increase to match player velocity in the push direction
                float VelocityDiffInPushDir = Velocity.Dot(PushDir) - Obj.LinearVelocity.Dot(PushDir);

                // Only count velocity towards push dir, away from character
                VelocityDiffInPushDir = Mathf.Max(0.0f, VelocityDiffInPushDir);

                PushDir.Y = 0; // Don't push object from above/below

                float PushForce = MassRatio * PushForceScalar;
                Obj.ApplyImpulse(PushDir * VelocityDiffInPushDir * PushForce, CollisionData.GetPosition() - Obj.GlobalPosition);
            }
        }
    }

    private float GetDeceleratedVelocity(float vel, float decel)
    {
        return vel > 0 ? Math.Max(vel - decel, 0) : Math.Min(vel + decel, 0);
    }

    private float GetClampedVelocity(float vel, float move, float accel, float max)
    {
        return Math.Clamp(vel + (move > 0 ? accel : -accel), -max, max);
    }

    private Vector3 HandleYAxis(Vector3 globalVelocity, double delta)
    {

        if (!IsOnFloor())
        {
            globalVelocity.Y -= ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle() * (float)delta * 1.5f;
        }
        if (input.actions.HasFlag(ActionFlags.Jump))
        {

            if (IsOnFloor())
            {
                globalVelocity += jumpVelocity;
            }
        }

        //fall damage calculation

        if (!IsOnFloor() && globalVelocity.Y < 0)
        {
            fallTime += (float)delta;
        }
        else if (IsOnFloor())
        {
            if (fallTime > safeFallTime)
            {
                float damage = (fallTime - safeFallTime) * fallingDamagePerSecond;
                TakeDamage(damage, authority, PainSoundType.Falling, ScaleDamageToVolume(damage));
            }
            if(fallTime > safeStunFallTime)
            {
                float stunDamage = (fallTime - safeStunFallTime) * fallingDamagePerSecond * 2;
                TakeStunDamage(stunDamage, authority, PainSoundType.Falling, ScaleDamageToVolume(stunDamage));
            }
            fallTime = 0f;
        }



        return globalVelocity;
    }

    //fall damage values
    private float fallTime = 0f;
    private float safeFallTime = 0.7f;
    private float safeStunFallTime = 0.6f;
    private float fallingDamagePerSecond = 50f;
    private bool wasOnFloor;

    private int ScaleDamageToVolume(float damage)
    {
        damage = Mathf.Clamp(damage, 1f, 100f);

        float inMin = 1f, inMax = 100f;
        float outMin = -6f, outMax = 6f;

        return (int)(outMin + (damage - inMin) / (inMax - inMin) * (outMax - outMin));
    }

    private Vector3 CalculateLocalVelocity()
    {
        Vector3 localVelocity = PCUtils.LocalizeVector(this, Velocity);

        finalSpeed = baseSpeed;
        // if (input != null && input.actions.HasFlag(ActionFlags.Sprint))
        // {
        //     finalSpeed = baseSpeed * 2;
        // }

        //get input vectors
        Vector2 normalizedInput = input.MovementInputVector.Normalized();
        float moveZ = normalizedInput.X;
        float moveX = normalizedInput.Y;

        // whether user z value is in opposite direction of current velocity
        bool antiInput = (localVelocity.Z > 1 && moveZ < 0) || (localVelocity.Z < -1 && moveZ > 0);

        //airbrake prevents further air movement once youve cancelled your Z movement

        if (!IsOnFloor() && (antiInput || airbrake))
        {
            airbrake = true;
            localVelocity.X = 0;
            localVelocity.Z = 0;
        }
        else
        {
            //reset airbrake when on ground
            airbrake = false;

            //accelerate directions
            if (moveZ != 0)
            {
                localVelocity.Z = GetClampedVelocity(localVelocity.Z, moveZ, acceleration, finalSpeed);
            }
            if (moveX != 0)
            {
                localVelocity.X = GetClampedVelocity(localVelocity.X, moveX, acceleration, finalSpeed);
            }
        }

        //apply deceleration

        if (IsOnFloor())
        {
            if (moveZ == 0)
            {
                localVelocity.Z = GetDeceleratedVelocity(localVelocity.Z, deceleration);
            }
            if (moveX == 0)
            {
                localVelocity.X = GetDeceleratedVelocity(localVelocity.X, deceleration);
            }

        }
        return localVelocity;
    }

    private void HandleMouseLook(double delta)
    {
        bool lockLook = false;
        if (equipped is Hands hands)
        {
            lockLook = hands.rotateMode;
        }
        if (Input.MouseMode == Input.MouseModeEnum.Captured && !lockLook)
        {
            float mouseX = input.LookInputVector.X * Global.Config.loadedPlayerConfig.mouseSensX * ((float)delta);
            float mouseY = input.LookInputVector.Y * Global.Config.loadedPlayerConfig.mouseSensY * ((float)delta);
            if(knockedOut)
            {
                mouseX = 0;
                mouseY = 0;
            }
            float newXRot = camera.RotationDegrees.X - mouseY;
            float newYRot = RotationDegrees.Y - mouseX;

            if (newXRot > camXRotMax) { newXRot = camXRotMax; }
            if (newXRot < camXRotMin) { newXRot = camXRotMin; }

            camera.RotationDegrees = new Vector3(newXRot, camera.RotationDegrees.Y, camera.RotationDegrees.Z);
            if(!knockedOut)
            {
                RotationDegrees = new Vector3(RotationDegrees.X, newYRot, RotationDegrees.Z);
            }
        }
        input.LookInputVector = Vector2.Zero; // Reset the mouse relative accumulator after applying it to the rotation
    }
    

    public override void PerFrameAuth(double delta)
    {
        if (Global.DrawDebugScreens)
        {
            ImGui.Begin("PC Debug");
            ImGui.Text("InputMvVector: " + input.MovementInputVector.ToString());
            ImGui.Text("InputLookVector: " + input.LookInputVector.ToString());
            ImGui.Text($"Actions flag: {input.actions}");
            ImGui.End();
        }
        if (Global.steamid == authority)
        {
            if (hurtVisualIntensity > 0.0f)
            {
                hurtVisualIntensity -= (float)delta * 1.0f;
                hurtVisualIntensity = Mathf.Max(hurtVisualIntensity, 0.0f);
                hurtShaderMaterial.SetShaderParameter("vignette_intensity", hurtVisualIntensity);
            }
        }
    }

    public override Camera3D GetCamera()
    {
        return camera;
    }

    public (Color, string) GetHealthInfo()
    {
        if (currentHealth == maxHealth)
        {
            return (Colors.Green, "Healthy");
        }
        else if (currentHealth / maxHealth >= 0.75f)
        {
            return (Colors.GreenYellow, "Hurt");
        }
        else if (currentHealth / maxHealth >= 0.5f)
        {
            return (Colors.Yellow, "Wounded");
        }
        else if (currentHealth / maxHealth >= 0.25f)
        {
            return (Colors.Orange, "Badly Wounded");
        }
        else if (currentHealth / maxHealth >= 0.0f)
        {
            return (Colors.Red, "Near Death");
        }
        else
        {
            return (Colors.DimGray, "Dead?");
        }
    }

    public override string GenerateStateString()
    {
        return MessagePackSerializer.ConvertToJson(GenerateStateUpdate());
    }



    //Character State Functions (Health, Stun, Death, etc) \\

    public void TakeStunDamage(float damage, ulong byID, PainSoundType soundType, int VolumeDb = 0)
    {
        //only the authority can tell people they took damage
        if (Global.steamid == authority)
        {
            RPCManager.RPC(this, "rpc_TakeStunDamage", [damage, byID, soundType, VolumeDb]);
        }
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void rpc_TakeStunDamage(float damage, ulong byID, PainSoundType soundType, int VolumeDb = 0)
    {
        if (state == CharacterState.Living)
        {
            currentStunBar -= damage;
            if(!knockedOut)
            {
                currentTimeUntilStunRegen = stunRegenDelaySeconds;
            }
            characterSoundManager.PlayDamageSound(characterSFX, soundType, VolumeDb);
            //Logging.Log($"{damage} Stun Taken, {currentStunBar} Stun Bar Remains", "BasicPlayerCharacter");
            if (controllingPlayerID == Global.steamid)
            {
                Global.ui.inGameUI.PlayerUIManager.UpdateStunUI((int)currentStunBar, (int)maxStunBar); ;
            }
            if (currentStunBar <= 0)
            {
                OnKnockedOut();
            }
        }
        else
        {
            //Logging.Log("Tried to deal damage to already dead character: " + authority, "BasicPlayerCharacter");
        }
    }

    public void OnKnockedOut()
    {
        if (Global.steamid == authority)
        {
            RPCManager.RPC(this, "rpc_OnKnockedOut", []);
        }
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void rpc_OnKnockedOut()
    {
        Logging.Log($"{authority} PlayerCharacter has been knocked out", "BasicPlayerCharacter");
        KnockedOut?.Invoke(id);
        knockedOut = true;
        DropEquipped();
        currentStunBar = 0;
        //adjust collider
        collider.RotationDegrees = new Vector3(90, 0, 0);
        collider.Position = new Vector3(0, -0.634f, 0);
        ((CapsuleShape3D)collider.Shape).Radius = 0.186f;

        //adjust camera
        camera.Position = new Vector3(0, -2.259f, 1.01f);
        camera.RotationDegrees = new Vector3(90, 0, 0);
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void rpc_WakeUp()
    {
        Logging.Log("Waking Up: " + id + " " + authority, "BasicPlayerCharacter");
        knockedOut = false;
        collider.RotationDegrees = new Vector3(0, 0, 0);
        collider.Position = new Vector3(0, 0.442f, 0);
        ((CapsuleShape3D)collider.Shape).Radius = 0.5f;

        camera.Position = new Vector3(0, -0.259f, -0.08f);
        camera.RotationDegrees = new Vector3(0, 0, 0);
    }

    public void TakeDamage(float damage, ulong byID, PainSoundType soundType, int VolumeDb = 0)
    {
        //only the authority can tell people they took damage
        //Logging.Log("Take Damage: " + Global.steamid + " " + authority, "BasicPlayerCharacter");
        if (Global.steamid == authority)
        {
            RPCManager.RPC(this, "rpc_TakeDamage", [damage, byID, soundType, VolumeDb]);
        }
    }

    private float hurtVisualIntensity;

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void rpc_TakeDamage(float damage, ulong byID, PainSoundType soundType, int VolumeDb = 0)
    {
        if (state == CharacterState.Living)
        {
            currentHealth -= damage;
            hurtVisualIntensity = 0.5f;
            if(Global.steamid == authority)
            {
                hurtShaderMaterial.SetShaderParameter("vignette_intensity", hurtVisualIntensity);
            }
            characterSoundManager.PlayDamageSound(characterSFX, soundType, VolumeDb);
            //Logging.Log($"{damage} Damage Taken, {currentHealth} Health Remains", "BasicPlayerCharacter");
            if (controllingPlayerID == Global.steamid)
            {
                Global.ui.inGameUI.PlayerUIManager.UpdateHealthUI((int)currentHealth, (int)maxHealth); ;
            }
            if (currentHealth <= 0 && Global.steamid == authority)
            {
                Logging.Log($"{authority} PlayerCharacter has died", "BasicPlayerCharacter");
                rpc_OnDeath();
            }
        }
        else
        {
            //Logging.Log("Tried to deal damage to already dead character: " + authority, "BasicPlayerCharacter");
        }
    }

    public void OnDeath()
    {
        //only the authority can tell people they died
        if (Global.steamid == authority)
        {
            RPCManager.RPC(this, "rpc_OnDeath", []);
        }
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void rpc_OnDeath()
    {
        Killed?.Invoke(id);
        characterSoundManager.PlayDeathSound(characterSFX);
        inventory.DropAllItems(authority);
        state = CharacterState.Missing;
        currentHealth = 0;
        rpc_OnKnockedOut();
        Global.ui.inGameUI.ScoreBoard.PlayerDied(authority);
        Global.gameState.gameModeManager.CharacterDied(authority, team);
        DelayDeathRelease();
    }

    public async void DelayDeathRelease()
    {
        await ToSignal(GetTree().CreateTimer(3), SceneTreeTimer.SignalName.Timeout);
        ulong tempControllingPlayerID = controllingPlayerID;
        ReleaseControl();
        Global.gameState.gameModeManager.ghostPlayers[tempControllingPlayerID].TakeControl(tempControllingPlayerID);
    }

    public void KillSelf()
    {
        currentHealth = 0;
        RPCManager.RPC(this, "rpc_OnDeath", []);
    }


    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void OnFound()
    {
        if (state != CharacterState.Dead)
        {
            state = CharacterState.Dead;
            Global.ui.inGameUI.ScoreBoard.PlayerFound(authority);
        }

    }
    
    public void AddToAmmoStored(AmmoType ammoType, int ammoAmount)
    {
        ammoStored[ammoType] += ammoAmount;
    }

    public override void Assignment(Team team, Role role)
    {
        this.team = team;
        this.role = role;
    }

    public override void TakeControl(ulong playerID)
    {
        base.TakeControl(playerID);
        if(Global.steamid == playerID)
        {
            GD.Print("UPDATE BOTTOM RIGHT UI: " + currentStunBar + " " + currentHealth + " " + team);
            Global.ui.inGameUI.PlayerUIManager.UpdateStunUI((int)currentStunBar, (int)maxStunBar);
            Global.ui.inGameUI.PlayerUIManager.UpdateHealthUI((int)currentHealth, (int)maxHealth);
            Global.ui.inGameUI.PlayerUIManager.UpdateTeamUI(team);
        }
    }

    public void Handcuff(GOHandcuffs handcuffs)
    {
        PickupReplace(handcuffs);
        handcuffed = true;
    }

    public void RemoveHandcuffs()
    {
        handcuffed = false;
    }



    //Control
    protected override void OnControlTaken(ulong byID)
    {
        if (byID == Global.steamid)
        {
            Logging.Log("Enabling Player UI " + byID, "BasicPlayerCharacter");
            Global.ui.inGameUI.PlayerUIManager.ShowPlayerUI(authority);
        }
    }

    protected override void OnControlReleased()
    {
        base.OnControlReleased();
        if (controllingPlayerID == Global.steamid)
        {
            Logging.Log("Disabling Player UI " + controllingPlayerID, "BasicPlayerCharacter");
            //Global.ui.inGameUI.PlayerUIManager.HidePlayerUI();
        }
    }

    public override void PerTickLocal(double delta)
    {
        // Vector3 globalVelocity = Velocity;
        // if (!IsOnFloor())
        // {
        //     globalVelocity.Y -= ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle() * (float)delta * 1.5f;
        // }
        // Velocity = globalVelocity;
        // MoveAndSlide();
    }
    private void UpdateAnimationTree()
    {
        if (knockedOut)
        {
            animationTree.Set("parameters/GroundedTransition/transition_request", "dead");
            //reseting upper blend just incase
            animationTree.Set("parameters/UpperBodyBlend2/blend_amount", 0);
            return;
        }

        // Update Movement Animation
        Vector3 localVel = PCUtils.LocalizeVector(this, Velocity);
        animationTree.Set("parameters/WalkRunBlend/blend_position", new Vector2(localVel.X, -1 * localVel.Z));
        animationTree.Set("parameters/CrouchBlend/blend_position", new Vector2(localVel.X, -1 * localVel.Z));

        if (crouched)
        {
            animationTree.Set("parameters/StandTransition/transition_request", "crouched");
        }
        else
        {
            animationTree.Set("parameters/StandTransition/transition_request", "standing");
        }

        if (handcuffed)
        {
            animationTree.Set("parameters/UpperBodyBlend2/blend_amount", 1);
            animationTree.Set("parameters/UpperBodyTransition/transition_request", "cuffed");
        }
        else
        {
            if (equipped is Hands hands)
            {
                animationTree.Set("parameters/UpperBodyBlend2/blend_amount", 0);
            }
            else
            {
                animationTree.Set("parameters/UpperBodyBlend2/blend_amount", 1);
                animationTree.Set("parameters/UpperBodyTransition/transition_request", "rifle");
            }
        }

        if (onGround)
        {
            animationTree.Set("parameters/GroundedTransition/transition_request", "grounded");
        }
        else
        {
            animationTree.Set("parameters/GroundedTransition/transition_request", "air");
        }
    }
}


[MessagePackObject]
public struct BasicPlayerStateUpdate
{
    [Key(0)]
    public Vector3 Position;

    [Key(1)]
    public Vector3 Rotation;
    [Key(2)]
    public Vector3 CameraRotation;

    [Key(3)]
    public Vector3 Velocity;

    [Key(4)]
    public bool OnGround;

    [Key(5)]
    public bool Crouched;
}