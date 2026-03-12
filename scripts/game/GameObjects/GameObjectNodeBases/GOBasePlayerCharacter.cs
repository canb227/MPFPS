using Godot;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Reflection;


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
    [Export] public CollisionShape3D[] bodyColliders;

    [Export] public OmniLight3D localPlayerLight;


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



    [Export] private AudioStreamGeneratorPlayback playback;
    private AudioStreamGenerator generator;



    private const int MaxVoiceBufferSize = 20000;
    private readonly List<AudioStreamWav> voiceDataQueue = new();
    private readonly Timer playbackTimer;
    [Export] private AudioStreamPlayer3D voicePlayer;
    
    public GOBasePlayerCharacter()
    {
        
    }


    public virtual void Reset()
    {

    }

    public override void _Ready()
    {
        base._Ready();
        Logging.Log($"Spawned a new player character with id:{id} and authority: {authority}. Help", "PlayerCharacter");
        visualRayCast = new();
        visualRayCast.TargetPosition = new Vector3(0, 0, -20);
        visualRayCast.CollideWithBodies = true;
        visualRayCast.CollideWithAreas = true;
        visualRayCast.CollisionMask = (1 << 0) | (1 << 1) | (1 << 2) | (1 << 3); //layer 1, 2, 3, 4, world, entities, players(hitboxes), items, 
        camera.AddChild(visualRayCast);

        gunRayCast = new();
        gunRayCast.TargetPosition = new Vector3(0, 0, -100);
        gunRayCast.CollideWithBodies = true;
        gunRayCast.CollisionMask = (1 << 0) | (1 << 1) | (1 << 2) | (1 << 3); //layer 1, 2, 3, 4, world, entities, players(hitboxes), items, 
        camera.AddChild(gunRayCast);

        generator = new AudioStreamGenerator
        {
            MixRate = (int)SteamUser.GetVoiceOptimalSampleRate(),
            BufferLength = 0.5f // Half a second buffer
        };
        // voicePlayer.Stream = generator;
        // voicePlayer.Play();
        // playback = voicePlayer.GetStreamPlayback() as AudioStreamGeneratorPlayback;

        if (IsMe())
        {
            GD.Print("CHANGING TARGETS OF AUDIO TO: " + id + " " + Name);
            thirdPersonModel.Visible = false;
            //we tell everybodies occluder audio voices to target our new camera for occlusion on our local machine
            var targetNodes = GetTree().GetNodesInGroup("AudioOccluder");
            foreach(var audioOccluder in targetNodes)
            {
                audioOccluder.Call("changeTarget", camera);
            }
        }

        //package tip setup
    }

    private Dictionary<int, string> packageTips = new(); //the key is what step and the value is the string
    //then we need to track what tip we should be displaying
    public void AddPackageTip(GOPackageBox box)
    {
        GD.Print(Global.steamid + " " + authority);
        //highlight and write messages if we are the local player
        if(Global.steamid == authority)
        {
            GD.Print("yeah thats me");
            if(box.labelApplied)
            {
                Global.ui.inGameUI.PlayerUIManager.AddNewInfoLowPriority("This package needs shipped out, find the shipping tube!"); //need to add info based on package state
                Global.gameState.gameModeManager.shippingTube.SetHighlighted(true);
                Global.gameState.gameModeManager.LocalPlayInfoBeep(); 
            }
            else
            {
                Global.ui.inGameUI.PlayerUIManager.AddNewInfoLowPriority("This package needs labelled,\nUse the label printer and stamp machine in Labelling!"); //need to add info based on package state
                Global.gameState.gameModeManager.labelPrinter.SetHighlighted(true);
                Global.gameState.gameModeManager.crusher.SetHighlighted(true);

                Global.gameState.gameModeManager.LocalPlayInfoBeep(); 
            }
        }
    }

    public void RemovePackageTip(GOPackageBox box)
    {
        //disable all highlights and remove the tip if we are the local player
        if(Global.steamid == authority)
        {
            if(box.labelApplied)
            {
                Global.ui.inGameUI.PlayerUIManager.RemoveInfo("This package needs shipped out, find the shipping tube!"); //need to add info based on package state
            }
            else
            {
                Global.ui.inGameUI.PlayerUIManager.RemoveInfo("This package needs labelled,\nUse the label printer and stamp machine in Labelling!"); //need to add info based on package state
            }
            Global.gameState.gameModeManager.shippingTube.SetHighlighted(false);
            Global.gameState.gameModeManager.labelPrinter.SetHighlighted(false);
            Global.gameState.gameModeManager.crusher.SetHighlighted(false);
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
        if(playerID == Global.steamid)
        {
            GD.Print("disable colliders");
            if(localPlayerLight != null) localPlayerLight.Visible = true;
            if(authority == Global.steamid && bodyColliders != null)
            {
                foreach(var collider in bodyColliders)
                {
                    collider.Disabled = true;
                }
            }   
        }
        if(playerID == Global.steamid && Global.gameState.gameModeManager.options.hordeRobots)
        {
            Global.gameState.AIManager.UpdateLocalPlayer(this);
        }
        if (controllingPlayerID != 0)
        {
            Logging.Error($"Player {playerID} cannot take control of player character {id}, that character is already being controlled by player {controllingPlayerID}", "PlayerCharacter");
        }
        else if (Global.gameState.PlayerIDToControlledCharacter.TryGetValue(playerID, out ulong charID) && charID != 0)
        {
            Logging.Error($"Player {playerID} Cannot take control of player character {id}, that player is already controlling character: {Global.gameState.GetCharacterControlledBy(playerID).id} ", "PlayerCharacter");
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
                Node sal = ResourceLoader.Load<PackedScene>("res://scenes/GameObjects/player/SteamAudioListener.tscn").Instantiate();
                sal.Name = "SteamAudioListener";
                camera.AddChild(sal);
                Input.MouseMode = Input.MouseModeEnum.Captured;
                OnControlTaken(playerID);
                thirdPersonModel.Visible = false;
                firstPersonModel.Visible = true;
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
            if (IsMe())
            {
                try
                {
                    camera.GetNode("SteamAudioListener").QueueFree();
                }
                catch
                {
                    
                }
                camera.Current = false;
                Input.MouseMode = Input.MouseModeEnum.Confined;
            }
            if(localPlayerLight != null) localPlayerLight.Visible = false;
            Global.gameState.PlayerIDToControlledCharacter[controllingPlayerID] = 0;
            controllingPlayerID = 0;
            input = null;
            thirdPersonModel.Visible = true;
            firstPersonModel.Visible = false;
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



    public void ProcessVoiceData(byte[] compressedVoiceData)
    {
        byte[] decompressedBytes = new byte[20000];
        var result = SteamUser.DecompressVoice(
            compressedVoiceData,
            (uint)compressedVoiceData.Length,
            decompressedBytes,
            (uint)decompressedBytes.Length,
            out uint bytesWritten,
            (uint)generator.MixRate
        );

        if (result != EVoiceResult.k_EVoiceResultOK || bytesWritten == 0)
            return;

        // Convert byte[] to float samples (16-bit PCM mono)
        for (int i = 0; i < bytesWritten; i += 2)
        {
            short sample = BitConverter.ToInt16(decompressedBytes, i);
            float normalized = sample / 32768f;
            playback.PushFrame(new(normalized, normalized));
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
                                if (basicPlayerCharacter.team == Team.Manager)
                                {
                                    Global.ui.inGameUI.PlayerUIManager.targetPlayerName.LabelSettings.FontColor = Colors.Blue;
                                }
                                else
                                {
                                    Global.ui.inGameUI.PlayerUIManager.targetPlayerName.LabelSettings.FontColor = Colors.White;
                                }                                
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

