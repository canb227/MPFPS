
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
    [Export] public AudioStreamPlayer3D ourVoiceSpeaker;
    [Export] public AudioStreamPlayer3D characterSFX;
    [Export] public AudioStreamPlayer3D movementSFX;
    public CharacterSoundManager characterSoundManager = new();
    public float maxHealth { get; set; } = 100;
    public float currentHealth { get; set; } = 100;
    public float maxStunBar { get; set; } = 100;
    public float currentStunBar { get; set; } = 100;
    public float currentTimeUntilStunRegen { get; set; } = 0;
    public float stunRegenDelaySeconds { get; set; } = 3;
    public float stunRegenRatePerSecond { get; set; } = 5;
    public Inventory inventory { get; set; } = new();
    public IsInventoryItem equipped { get; set; }
    public CharacterState state { get; set; }
    public ulong currentlySeenCharacterID { get; set; }
    public CharacterState currentlySeenCharacterState { get; set; }
    public string currentlySeenCharacterHealthString { get; set; }
    public float camXRotMax = 85;
    public float camXRotMin = -85;
    public float baseSpeed = 6;
    public float acceleration = 1;
    public float deceleration = 1;
    public float finalSpeed;
    private Vector3 jumpVelocity = new Vector3(0, 6, 0);
    private bool airbrake = false;

    public Dictionary<AmmoType, int> ammoStored = new() //should be all 0 for production
    {
        {AmmoType.ShotgunAmmo, 8 },
        {AmmoType.RifleAmmo, 30 },
        {AmmoType.SniperAmmo, 10 },
    };
    public Dictionary<AmmoType, int> maxAmmoStored = new()
    {
        {AmmoType.ShotgunAmmo, 24 },
        {AmmoType.RifleAmmo, 90 },
        {AmmoType.SniperAmmo, 30 },
    };

    private int currentItemSlot;
    private InventoryGroupCategory currentGroup;

    public override void _Ready()
    {
        base._Ready();
        if(Global.gameState.gameModeManager != null && Global.gameState.gameModeManager.basicPlayers != null)
        {
            Global.gameState.gameModeManager.basicPlayers.Add(authority, this);
        }
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
    }
    public override void ProcessStateUpdate(byte[] _update)
    {
        BasicPlayerStateUpdate update = MessagePackSerializer.Deserialize<BasicPlayerStateUpdate>(_update);
        GlobalRotation = update.Rotation;
        GlobalPosition = update.Position;
    }

    public override byte[] GenerateStateUpdate()
    {
        BasicPlayerStateUpdate update = new BasicPlayerStateUpdate();
        update.Rotation = GlobalRotation;
        update.Position = GlobalPosition;
        return MessagePackSerializer.Serialize(update);
    }

    //various input functions, PerTickAuth is the main loop
    public override void PerTickAuth(double delta)
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
        }
        else
        {
            currentTimeUntilStunRegen -= (float)delta;
        }
        if (Global.gameState.PlayerIDToControlledCharacter[Global.steamid] == id)
        {
            HandleVisualRayCast(delta);
        }
    }


    public void HandleVisualRayCast(double delta)
    {
        if (visualRayCast.GetCollider() is CollisionObject3D collider)
        {
            // Collision layers are stored as a bitmask
            uint layerMask = collider.CollisionLayer;

            // Check if layer 3 bit is set
            bool isLayer3 = (layerMask & (1 << 2)) != 0; // layer 3 → bit index 2 (since it's 1-based in the editor)
            GD.Print(collider.GetType() + " " + collider.Name + layerMask + " " + isLayer3);

            if (isLayer3)
            {
                // Walk up to find the BasicPlayerCharacter
                Node current = collider;
                while (current != null && current is not BasicPlayerCharacter)
                    current = current.GetParent();

                if (current is BasicPlayerCharacter basicPlayerCharacter)
                {
                    GD.Print(basicPlayerCharacter.id + " " + basicPlayerCharacter.state + " " + currentlySeenCharacterID);
                    if (basicPlayerCharacter.state == CharacterState.Living)
                    {
                        (Color, string) healthInfo = basicPlayerCharacter.GetHealthInfo();
                        if (basicPlayerCharacter.id != currentlySeenCharacterID || basicPlayerCharacter.state != currentlySeenCharacterState || healthInfo.Item2 != currentlySeenCharacterHealthString)
                        {
                            currentlySeenCharacterID = basicPlayerCharacter.id;
                            currentlySeenCharacterState = basicPlayerCharacter.state;
                            currentlySeenCharacterHealthString = healthInfo.Item2;
                            Global.ui.inGameUI.PlayerUIManager.targetPlayerName.Visible = true;
                            Global.ui.inGameUI.PlayerUIManager.targetPlayerHealth.Visible = true;
                            Global.ui.inGameUI.PlayerUIManager.targetPlayerRole.Visible = true;
                            Logging.Log("We see a new living basicPlayerCharacter: " + currentlySeenCharacterID, "BasicPlayerCharacter");
                            Global.ui.inGameUI.PlayerUIManager.targetPlayerName.Text = SteamFriends.GetFriendPersonaName(new CSteamID(basicPlayerCharacter.authority));
                            Global.ui.inGameUI.PlayerUIManager.targetPlayerHealth.AddThemeColorOverride("font_color", basicPlayerCharacter.GetHealthInfo().Item1);
                            Global.ui.inGameUI.PlayerUIManager.targetPlayerHealth.Text = basicPlayerCharacter.GetHealthInfo().Item2;
                            Global.ui.inGameUI.PlayerUIManager.targetPlayerRole.Text = basicPlayerCharacter.role.ToString();
                        }
                    }
                    else if (basicPlayerCharacter.state == CharacterState.Missing)
                    {
                        if (basicPlayerCharacter.id != currentlySeenCharacterID || basicPlayerCharacter.state != currentlySeenCharacterState)
                        {
                            currentlySeenCharacterID = basicPlayerCharacter.id;
                            currentlySeenCharacterState = basicPlayerCharacter.state;
                            Global.ui.inGameUI.PlayerUIManager.targetPlayerName.Visible = true;
                            Global.ui.inGameUI.PlayerUIManager.targetPlayerHealth.Visible = true;
                            Global.ui.inGameUI.PlayerUIManager.targetPlayerRole.Visible = true;
                            Logging.Log("We see a new missing basicPlayerCharacter: " + currentlySeenCharacterID, "BasicPlayerCharacter");
                            Global.ui.inGameUI.PlayerUIManager.targetPlayerName.Text = "Unidentified Body";
                            Global.ui.inGameUI.PlayerUIManager.targetPlayerName.AddThemeColorOverride("font_color", Colors.Yellow);
                            Global.ui.inGameUI.PlayerUIManager.targetPlayerHealth.AddThemeColorOverride("font_color", Colors.LightGray);
                            Global.ui.inGameUI.PlayerUIManager.targetPlayerHealth.Text = "Corpse";
                            Global.ui.inGameUI.PlayerUIManager.targetPlayerRole.Text = "Press F to search and identify";
                        }
                    }
                    else if (basicPlayerCharacter.state == CharacterState.Dead)
                    {
                        if (basicPlayerCharacter.id != currentlySeenCharacterID || basicPlayerCharacter.state != currentlySeenCharacterState)
                        {
                            currentlySeenCharacterID = basicPlayerCharacter.id;
                            currentlySeenCharacterState = basicPlayerCharacter.state;
                            Global.ui.inGameUI.PlayerUIManager.targetPlayerName.Visible = true;
                            Global.ui.inGameUI.PlayerUIManager.targetPlayerHealth.Visible = true;
                            Global.ui.inGameUI.PlayerUIManager.targetPlayerRole.Visible = true;
                            Logging.Log("We see a new missing basicPlayerCharacter: " + currentlySeenCharacterID, "BasicPlayerCharacter");
                            Global.ui.inGameUI.PlayerUIManager.targetPlayerName.Text = SteamFriends.GetFriendPersonaName(new CSteamID(basicPlayerCharacter.authority));
                            Global.ui.inGameUI.PlayerUIManager.targetPlayerName.RemoveThemeColorOverride("font_color");
                            Global.ui.inGameUI.PlayerUIManager.targetPlayerHealth.AddThemeColorOverride("font_color", Colors.LightGray);
                            Global.ui.inGameUI.PlayerUIManager.targetPlayerHealth.Text = "Corpse";
                            Global.ui.inGameUI.PlayerUIManager.targetPlayerRole.Text = "Press F to search";
                        }
                    }
                    else
                    {
                        Logging.Error("Invalid Player State in Visual Check", "BasicPlayerCharacter");
                    }
                }
            }
            else
            {
                if(currentlySeenCharacterID != 0)
                {
                    GD.Print("Reset Inner");
                    currentlySeenCharacterID = 0;
                    Global.ui.inGameUI.PlayerUIManager.targetPlayerName.Visible = false;
                    Global.ui.inGameUI.PlayerUIManager.targetPlayerHealth.Visible = false;
                    Global.ui.inGameUI.PlayerUIManager.targetPlayerRole.Visible = false;
                }
            }
        }
        else
        {
            if(currentlySeenCharacterID != 0)
            {
                GD.Print("Reset Outer");
                currentlySeenCharacterID = 0;
                Global.ui.inGameUI.PlayerUIManager.targetPlayerName.Visible = false;
                Global.ui.inGameUI.PlayerUIManager.targetPlayerHealth.Visible = false;
                Global.ui.inGameUI.PlayerUIManager.targetPlayerRole.Visible = false;
            }
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
        //use input from local and remote players to calculate footsteps
        if (input != null)
        {
            if (input.actions.HasFlag(ActionFlags.Jump))
            {
                if (IsOnFloor())
                {
                    characterSoundManager.PlayMovementSound(movementSFX, MovementSoundType.Generic, true);
                }
            }
            else if (IsOnFloor() && Math.Abs(Velocity.Z) + Math.Abs(Velocity.X) > 0.0f)
            {
                characterSoundManager.PlayMovementSound(movementSFX, MovementSoundType.Generic, false);
            }
        }
        
    }


    private void HandleNonMovementInput(double delta)
    {
        if (!lastTickActions.HasFlag(ActionFlags.Use) && input.actions.HasFlag(ActionFlags.Use))
        {
            if (interactRayCast.IsColliding())
            {
                var temp = interactRayCast.GetCollider();
                if (interactRayCast.GetCollider() is IsInventoryItem s)
                {
                    Logging.Log("Calling Pickup!", "BasicPlayerCharacter");
                    Pickup(s);
                }
                else if (interactRayCast.GetCollider() is IsInteractable i)
                {
                    i.Local_OnInteract(id);
                }
                else if (interactRayCast.GetCollider() is BasicPlayerCharacter basicPlayerCharacter)
                {
                    switch (basicPlayerCharacter.state)
                    {
                        case CharacterState.Missing:
                            basicPlayerCharacter.OnFound();
                            goto case CharacterState.Dead;

                        case CharacterState.Dead:
                            Global.ui.inGameUI.PlayerUIManager.deadPlayerScreen.OpenDeadPlayerScreen(basicPlayerCharacter); //show dead player ui stuff
                            break;
                    }

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
    }
    
    private void EquipNextFromSlot(InventoryGroupCategory category)
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

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public override void Pickup(IsInventoryItem item)
    {
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
                    //auto-equip weapons
                    if(group.category == InventoryGroupCategory.Weapon)
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
        if(equipped != null && equipped.droppable)
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
        if (equipped != null)
        {
            equipped.HandleInput(input.actions);
        }

    }

    private void HandleMovementInputAndPhysics(double delta)
    {
        Velocity = HandleYAxis(Velocity, delta);

        Vector3 localVelocity = CalculateLocalVelocity();

        Velocity = PCUtils.GlobalizeVector(this, localVelocity);
        PushAwayRigidBodies();
        MoveAndSlide();
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
            globalVelocity.Y -= ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle() * (float)delta;
        }

        if (input.actions.HasFlag(ActionFlags.Jump))
        {
            if (IsOnFloor())
            {
                globalVelocity += jumpVelocity;
            }
        }
        return globalVelocity;
    }

    private Vector3 CalculateLocalVelocity()
    {
        Vector3 localVelocity = PCUtils.LocalizeVector(this, Velocity);

        finalSpeed = baseSpeed;
        if (input.actions.HasFlag(ActionFlags.Sprint))
        {
            finalSpeed = baseSpeed * 2;
        }

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
        if (Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            float mouseX = input.LookInputVector.X * 5 * ((float)delta);
            float mouseY = input.LookInputVector.Y * 5 * ((float)delta);

            float newXRot = camera.RotationDegrees.X - mouseY;
            float newYRot = RotationDegrees.Y - mouseX;

            if (newXRot > camXRotMax) { newXRot = camXRotMax; }
            if (newXRot < camXRotMin) { newXRot = camXRotMin; }

            camera.RotationDegrees = new Vector3(newXRot, camera.RotationDegrees.Y, camera.RotationDegrees.Z);
            RotationDegrees = new Vector3(RotationDegrees.X, newYRot, RotationDegrees.Z);
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

    public void TakeStunDamage(float damage, ulong byID, PainSoundType soundType)
    {
        RPCManager.RPC(this, "rpc_TakeStunDamage", [damage,byID,soundType]);
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void rpc_TakeStunDamage(float damage, ulong byID, PainSoundType soundType)
    {
        if (state == CharacterState.Living)
        {
            currentStunBar -= damage;
            currentTimeUntilStunRegen = stunRegenDelaySeconds;
            characterSoundManager.PlayDamageSound(characterSFX, soundType);
            Logging.Log($"{damage} Stun Taken, {currentStunBar} Stun Bar Remains", "BasicPlayerCharacter");
            if (controllingPlayerID == Global.steamid)
            {
                Global.ui.inGameUI.PlayerUIManager.UpdateStunUI((int)currentStunBar, (int)maxStunBar); ;
            }
            if (currentStunBar <= 0)
            {
                rpc_OnKnockedOut();
            }
        }
        else
        {
            Logging.Log("Tried to deal damage to already dead character: " + authority, "BasicPlayerCharacter");
        }
    }

    public void OnKnockedOut()
    {
        RPCManager.RPC(this, "rpc_OnKnockedOut", []);
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void rpc_OnKnockedOut()
    {
        Logging.Log($"{authority} PlayerCharacter has been knocked out", "BasicPlayerCharacter");
        KnockedOut?.Invoke(authority);
        //characterSoundManager.PlayerKnockoutSound(characterSFX);
        inventory.DropHeldItem();
        currentStunBar = 0;
        //ragdoll and other stuff
    }

    public void TakeDamage(float damage, ulong byID, PainSoundType soundType)
    {
        RPCManager.RPC(this, "rpc_TakeDamage", [damage,byID,soundType]);
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void rpc_TakeDamage(float damage, ulong byID, PainSoundType soundType)
    {
        TakeStunDamage(damage*2, byID, PainSoundType.None);
        if (state == CharacterState.Living)
        {
            currentHealth -= damage;
            characterSoundManager.PlayDamageSound(characterSFX, soundType);
            Logging.Log($"{damage} Damage Taken, {currentHealth} Health Remains", "BasicPlayerCharacter");
            if (controllingPlayerID == Global.steamid)
            {
                Global.ui.inGameUI.PlayerUIManager.UpdateHealthUI((int)currentHealth, (int)maxHealth); ;
            }
            if (currentHealth <= 0)
            {
                Logging.Log($"{authority} PlayerCharacter has died", "BasicPlayerCharacter");
                rpc_OnDeath();
            }
        }
        else
        {
            Logging.Log("Tried to deal damage to already dead character: " + authority, "BasicPlayerCharacter");
        }
    }

    public void OnDeath()
    {
        RPCManager.RPC(this, "rpc_OnDeath", []);
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void rpc_OnDeath()
    {
        Killed?.Invoke(authority);
        characterSoundManager.PlayDeathSound(characterSFX);
        inventory.DropAllItems();
        state = CharacterState.Missing;
        currentHealth = 0;
        Global.ui.inGameUI.ScoreBoard.PlayerDied(authority);
        Global.gameState.gameModeManager.CharacterDied(team);
        ulong tempControllingPlayerID = controllingPlayerID;
        ReleaseControl();
        Global.gameState.gameModeManager.ghostPlayers[tempControllingPlayerID].TakeControl(tempControllingPlayerID);
    }



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
        Global.ui.inGameUI.PlayerUIManager.UpdateStunUI((int)currentStunBar, (int)maxStunBar);
        Global.ui.inGameUI.PlayerUIManager.UpdateHealthUI((int)currentHealth, (int)maxHealth);
        Global.ui.inGameUI.PlayerUIManager.UpdateRoleUI(team);
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
        if (controllingPlayerID == Global.steamid)
        {
            Logging.Log("Disabling Player UI " + controllingPlayerID, "BasicPlayerCharacter");
            Global.ui.inGameUI.PlayerUIManager.HidePlayerUI();
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

}