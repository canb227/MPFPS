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
    public virtual Node3D lookRotationNode { get; set; }

    [Export]
    public virtual Node3D cameraLocationNode { get; set; }

    [Export]
    public virtual Node3D firstPersonEquipmentAttachmentPoint {  get; set; }

    [Export]
    public virtual Node3D thirdPersonEquipmentAttachmentPoint { get; set; }

    public virtual ulong controllingPlayerID { get; set; }
    public virtual Team team {  get; set; }
    public virtual Role role { get; set; }
    public virtual PlayerInputData input { get; set; }
    public override bool predict { get; set; } = true;
    protected PlayerCamera cam { get; set; }
    public RayCast3D rayCast { get; set; }
    public ActionFlags lastTickActions { get; set; }

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
        controllingPlayerID = data.authority;
        return true;
    }

    public virtual void SpawnSelf()
    {
        this.Transform = Global.gameState.GetPlayerSpawnTransform();
        ResetCharacterInfo();
        if (Global.gameState.PlayerCharacters.ContainsKey(controllingPlayerID))
        {
            Global.gameState.PlayerCharacters[controllingPlayerID].ReleaseControl();
        }
        TakeControl();
    }

    public virtual void TakeControl()
    {
        Global.gameState.PlayerCharacters[controllingPlayerID] = this;
        input = Global.gameState.PlayerInputs[controllingPlayerID];
        cam.Current = true;
    }

    public void ReleaseControl()
    {
        Global.gameState.PlayerCharacters[controllingPlayerID] = null;
        input = null;
        cam.Current = false;
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

