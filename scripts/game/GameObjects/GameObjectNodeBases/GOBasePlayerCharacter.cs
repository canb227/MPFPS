using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


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
    public override bool predict { get; set; } = true;
    public RayCast3D rayCast { get; set; }
    public ActionFlags lastTickActions { get; set; }

    public bool IsMe()
    {
        return Global.steamid == controllingPlayerID;
    }

    public override void _Ready()
    {
        base._Ready();
        Logging.Log($"Spawned a new player character with id:{id} and authority/controllerID: {authority}/{controllingPlayerID}.", "PlayerCharacter");
        if (controllingPlayerID == Global.steamid)
        {
            Logging.Log($"A GOBasePlayerCharacter that I am controlling just spawned! Creating camera and hooking up inputs!", "PlayerCharacter");
            SetupLocalPlayerCharacter();
        }
    }

    public override bool InitFromData(GameState.GameObjectConstructorData data)
    {
        GlobalTransform = data.spawnTransform;
        return true;
    }

    public virtual void Respawn()
    {
        this.Transform = Global.gameState.GetPlayerSpawnTransform();
        ResetCharacterInfo();
        if (Global.gameState.PlayerCharacters.ContainsKey(controllingPlayerID))
        {
            Global.gameState.PlayerCharacters[controllingPlayerID].ReleaseControl();
        }
        TakeControl(authority);
    }

    [RPCMethod(mode=RPCMode.SendToAllPeers)]
    public virtual void TakeControl(ulong playerID)
    {
        if (controllingPlayerID != 0)
        {
            Logging.Error($"Cannot take control of player character, they are already being controlled", "PlayerCharacter");
        }
        else if (Global.gameState.PlayerCharacters[controllingPlayerID] != null)
        {
            Logging.Error($"Player {playerID} Cannot take control of player character, they are already controlling character: {Global.gameState.PlayerCharacters[controllingPlayerID].id.ToString()} ", "PlayerCharacter");
        }
        else
        {
            controllingPlayerID = playerID;
            Global.gameState.PlayerCharacters[controllingPlayerID] = this;
            input = Global.gameState.PlayerInputs[controllingPlayerID];
            if (IsMe())
            {
                camera.Current = true;
            }
        }
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void ReleaseControl()
    {
        if (controllingPlayerID == 0)
        {
            Logging.Error($"Cannot release control of player character, they are not being controlled", "PlayerCharacter");
        }
        else if (Global.gameState.PlayerCharacters[controllingPlayerID].id != id)
        {
            Logging.Error($"Player {controllingPlayerID} Cannot release control of player character {id}, they are not controlling this character.", "PlayerCharacter");
        }
        else
        {
            Global.gameState.PlayerCharacters[controllingPlayerID] = null;
            controllingPlayerID = 0;
            input = null;
            if (IsMe())
            {
                camera.Current = false;
            }
        }
    }

    public abstract void ResetCharacterInfo();

    public abstract void Assignment(Team team, Role role);

    /// <summary>
    /// PlayerCharacters must implement this function such that the cameraParent object is correctly set to the object that sets the camera's position
    /// </summary>
    protected abstract void SetupLocalPlayerCharacter();

    public abstract Camera3D GetCamera();


    public abstract void Pickup(IsInventoryItem item);
    public abstract void Equip(InventoryGroupCategory category, int index = 0);
    public override void PerTickAuth(double delta)
    { 
    
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

