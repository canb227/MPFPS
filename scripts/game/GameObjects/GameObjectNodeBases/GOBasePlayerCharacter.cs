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
    }

    public override bool InitFromData(GameObjectConstructorData data)
    {
        GlobalTransform = data.spawnTransform;
        TakeControl(authority);
        return true;
    }

    [RPCMethod(mode=RPCMode.SendToAllPeers)]
    public virtual void TakeControl(ulong playerID)
    {
        if (controllingPlayerID != 0)
        {
            Logging.Error($"Player {playerID} cannot take control of player character {id}, that character is already being controlled by player {controllingPlayerID}", "PlayerCharacter");
        }
        else if (Global.gameState.PlayerIDToControlledCharacter.TryGetValue(playerID, out ulong charID) && charID!=0)
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
                camera.Current = true;
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }

            OnControlTaken(playerID);
        }
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
            Global.gameState.PlayerIDToControlledCharacter[controllingPlayerID] = 0;
            controllingPlayerID = 0;
            input = null;

            if (IsMe())
            {
                camera.Current = false;
                Input.MouseMode = Input.MouseModeEnum.Confined;
            }

            OnControlReleased();
        }
    }

    protected abstract void OnControlReleased();

    public bool IsMe()
    {
        return Global.steamid == controllingPlayerID;
    }

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

