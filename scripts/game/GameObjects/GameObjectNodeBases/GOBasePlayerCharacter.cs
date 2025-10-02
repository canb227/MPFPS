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


    public virtual ulong controllingPlayerID { get; set; }
    public virtual Team team {  get; set; }
    public virtual Role role { get; set; }
    public virtual PlayerInputData input { get; set; }
    public override bool predict { get; set; } = true;

    public override void _Ready()
    {

        base._Ready();
        Logging.Log($"Spawned a new player character with id:{id} and authority/controllerID: {authority}/{controllingPlayerID}.", "PlayerCharacter");
        if (controllingPlayerID == Global.steamid)
        {
            Logging.Log($"A GOBasePlayerCharacter that I am controlling just spawned! Creating camera and hooking up inputs!", "PlayerCharacter");
            SetupLocalPlayerCharacter();
        }
        else
        {
            Global.gameState.PlayerInputs.Add(controllingPlayerID, new PlayerInputData());
            Global.gameState.PlayerInputs[controllingPlayerID].playerID = controllingPlayerID;
        }
        input = Global.gameState.PlayerInputs[controllingPlayerID];
    }

    public abstract void Assignment(Team team, Role role);

    /// <summary>
    /// PlayerCharacters must implement this function such that the cameraParent object is correctly set to the object that sets the camera's position
    /// </summary>
    protected abstract void SetupLocalPlayerCharacter();

    public abstract Camera3D GetCamera();

}

