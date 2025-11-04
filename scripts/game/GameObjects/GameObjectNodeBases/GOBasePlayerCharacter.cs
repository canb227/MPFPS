using Godot;
using Steamworks;


public abstract partial class GOBasePlayerCharacter : GOBaseCharacterBody3D
{
    [Export]
    public virtual Node3D firstPersonModel { get; set; }

    [Export]
    public virtual Node3D thirdPersonModel { get; set; }

    [Export]
    public virtual Camera3D camera { get; set; }
    [Export]
    public virtual Node3D firstPersonEquipmentAttachmentPoint {  get; set; }

    [Export]
    public virtual Node3D thirdPersonEquipmentAttachmentPoint { get; set; }

    public virtual ulong controllingPlayerID { get; set; } = 0;
    public virtual Team team {  get; set; }
    public virtual Role role { get; set; }
    public virtual PlayerInputData input { get; set; }
    public virtual PlayerInputData inputBackup { get; set; }
    public override bool predict { get; set; } = true;
    public RayCast3D interactRayCast { get; set; }
    public RayCast3D visualRayCast { get; set; }
    public RayCast3D gunRayCast { get; set; }

    public ActionFlags lastTickActions { get; set; }
    public ulong currentlySeenCharacterID { get; set; }
    public CharacterState currentlySeenCharacterState { get; set; }
    public string currentlySeenCharacterHealthString { get; set; }


    public abstract void Assignment(Team team, Role role);

    public abstract Camera3D GetCamera();
    public abstract void Pickup(IsInventoryItem item);
    public abstract void Equip(InventoryGroupCategory category, int index = 0);


    public virtual void Reset()
    {

    }

    public override void _Ready()
    {
        base._Ready();
        Logging.Log($"Spawned a new player character with id:{id} and authority: {authority}.", "PlayerCharacter");
        visualRayCast = new();
        visualRayCast.TargetPosition = new Vector3(0, 0, -12);
        visualRayCast.CollideWithBodies = true;
        visualRayCast.CollisionMask = (1 << 0) | (1 << 1) | (1 << 2) | (1 << 3); //layer 1, 2, 3, 4, world, entities, players(hitboxes), items, 
        camera.AddChild(visualRayCast);
        
        gunRayCast = new();
        gunRayCast.TargetPosition = new Vector3(0, 0, -100);
        gunRayCast.CollideWithBodies = true;
        gunRayCast.CollisionMask = (1 << 0) | (1 << 1) | (1 << 2) | (1 << 3); //layer 1, 2, 3, 4, world, entities, players(hitboxes), items, 
        camera.AddChild(gunRayCast);

        if(authority == Global.steamid)
        {
            thirdPersonModel.Visible = false;
        }
    }

    public override bool InitFromData(GameObjectConstructorData data)
    {
        GlobalTransform = data.spawnTransform;
        //paramList[0] is auth takeControl boolean
        if((bool)data.paramList[0])
        {
            rpc_TakeControl(authority);
        }
        return true;
    }

    public virtual void TakeControl(ulong playerID)
    {
        RPCManager.RPC(this, "rpc_TakeControl", [playerID]);
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public virtual void rpc_TakeControl(ulong playerID)
    {
        Logging.Log($"Player {playerID} is taking control of character {id}", "GameModeManager");
        if (controllingPlayerID != 0)
        {
            Logging.Error($"Player {playerID} cannot take control of player character {id}, that character is already being controlled by player {controllingPlayerID}", "PlayerCharacter");
        }
        else if (Global.gameState.PlayerIDToControlledCharacter.TryGetValue(playerID, out ulong charID) && charID != 0)
        {
            Logging.Error($"Player {playerID} Cannot take control of player character {id}, that player is already controlling character: {Global.gameState.GetCharacterControlledBy(controllingPlayerID).id} ", "PlayerCharacter");
        }
        else
        {
            controllingPlayerID = playerID;
            Global.gameState.PlayerIDToControlledCharacter[playerID] = id;
            input = Global.gameState.PlayerInputs[controllingPlayerID];
            if (IsMe())
            {
                Logging.Log($"Local inputs are now being fed to character {id}", "GameModeManager");
                camera.Current = true;
                Input.MouseMode = Input.MouseModeEnum.Captured;
                OnControlTaken(playerID);
            }
        }
    }

    //Used to temporarly ignore user input, original for machine interaction/control
    public void LockControl()
    {
        inputBackup = input;
        input = new();
    }
    
    public void UnlockControl()
    {
        input = inputBackup;
    }

    protected abstract void OnControlTaken(ulong byID);



    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public virtual void ReleaseControl()
    {
        if (controllingPlayerID == 0)
        {
            Logging.Error($"Cannot release control of player character {id}, they are not being controlled", "PlayerCharacter");
        }
        else if (Global.gameState.GetCharacterControlledBy(controllingPlayerID).id != id)
        {
            Logging.Error($"Something has gone wrong", "PlayerCharacter");
        }
        else
        {
            OnControlReleased();
            Global.gameState.PlayerIDToControlledCharacter[controllingPlayerID] = 0;
            controllingPlayerID = 0;
            input = null;

            if (IsMe())
            {
                camera.Current = false;
                Input.MouseMode = Input.MouseModeEnum.Confined;
            }
        }
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void QueueFreeHelper()
    {
        QueueFree();
    }

    protected virtual void OnControlReleased()
    {
        if (controllingPlayerID == Global.steamid)
        {
            currentlySeenCharacterID = 0;
            Global.ui.inGameUI.PlayerUIManager.targetPlayerName.Visible = false;
            Global.ui.inGameUI.PlayerUIManager.targetPlayerHealth.Visible = false;
            Global.ui.inGameUI.PlayerUIManager.targetPlayerRole.Visible = false;
        }
    }

    public bool IsMe()
    {
        return Global.steamid == controllingPlayerID;
    }

    public override void PerTickAuth(double delta)
    {
        if (Global.gameState.PlayerIDToControlledCharacter[Global.steamid] == id)
        {
            HandleVisualRayCast(delta);
        }
    }
    
    public virtual void HandleVisualRayCast(double delta)
    {
        if (visualRayCast.GetCollider() is CollisionObject3D collider)
        {
            // Collision layers are stored as a bitmask
            uint layerMask = collider.CollisionLayer;

            // Check if layer 3 bit is set
            bool isLayer3 = (layerMask & (1 << 2)) != 0; // layer 3 → bit index 2 (since it's 1-based in the editor)

            if (isLayer3)
            {
                // Walk up to find the BasicPlayerCharacter
                Node current = collider;
                while (current != null && current is not BasicPlayerCharacter)
                    current = current.GetParent();

                if (current is BasicPlayerCharacter basicPlayerCharacter)
                {
                    if(basicPlayerCharacter.id != id)
                    {
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
                                Global.ui.inGameUI.PlayerUIManager.targetPlayerName.Text = SteamFriends.GetFriendPersonaName(new CSteamID(basicPlayerCharacter.authority));
                                Global.ui.inGameUI.PlayerUIManager.targetPlayerName.LabelSettings.FontSize = 16;
                                Global.ui.inGameUI.PlayerUIManager.targetPlayerHealth.LabelSettings.FontColor = basicPlayerCharacter.GetHealthInfo().Item1;
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
                                Global.ui.inGameUI.PlayerUIManager.targetPlayerName.Text = "Unidentified Body";
                                Global.ui.inGameUI.PlayerUIManager.targetPlayerName.LabelSettings.FontColor = Colors.Yellow;
                                Global.ui.inGameUI.PlayerUIManager.targetPlayerName.LabelSettings.FontSize = 32;
                                Global.ui.inGameUI.PlayerUIManager.targetPlayerHealth.LabelSettings.FontColor = basicPlayerCharacter.GetHealthInfo().Item1;
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
                                Global.ui.inGameUI.PlayerUIManager.targetPlayerName.Text = SteamFriends.GetFriendPersonaName(new CSteamID(basicPlayerCharacter.authority));
                                Global.ui.inGameUI.PlayerUIManager.targetPlayerName.LabelSettings.FontColor = basicPlayerCharacter.GetHealthInfo().Item1;
                                Global.ui.inGameUI.PlayerUIManager.targetPlayerName.LabelSettings.FontSize = 16;
                                Global.ui.inGameUI.PlayerUIManager.targetPlayerHealth.LabelSettings.FontColor = basicPlayerCharacter.GetHealthInfo().Item1;
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
            }
            else
            {
                if(currentlySeenCharacterID != 0)
                {
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
                currentlySeenCharacterID = 0;
                Global.ui.inGameUI.PlayerUIManager.targetPlayerName.Visible = false;
                Global.ui.inGameUI.PlayerUIManager.targetPlayerHealth.Visible = false;
                Global.ui.inGameUI.PlayerUIManager.targetPlayerRole.Visible = false;
            }
        }
    }

    public override void PerFrameAuth(double delta)
    {

    }

    public override void PerTickLocal(double delta)
    { 
    
    }

    public override void PerFrameLocal(double delta)
    { 
    
    }

    public override void PerTickShared(double delta)
    { 
    
    }

    public override void PerFrameShared(double delta)
    { 
    
    }

}

