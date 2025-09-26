using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public abstract partial class GOBasePlayerCharacter : GOBaseCharacterBody3D
{
    public abstract ulong controllingPlayerID { get; set; }
    public abstract int team {  get; set; }
    public abstract PlayerInputData input { get; set; }
    public abstract Node3D cameraParent { get; set; }
    public override bool predict { get; set; } = true;
    public override void _Ready()
    {
        base._Ready();
        Logging.Log($"Spawned a new player character with id:{id} , controllingPlayer: {controllingPlayerID} , team: {team}","PlayerCharacter");
        if (controllingPlayerID == Global.steamid)
        {
            Logging.Log($"A GOBasePlayerCharacter that I am controlling just spawned! Creating camera and hooking up inputs!", "PlayerCharacter");
            CreateAndConfigureCamera();
            ConfigureInput();
        }
        else
        {

        }
    }

    /// <summary>
    /// PlayerCharacters must implement this function such that the input object is correctly set afterwards.
    /// </summary>
    protected abstract void ConfigureInput();

    /// <summary>
    /// PlayerCharacters must implement this function such that the cameraParent object is correctly set to the object that sets the camera's position
    /// </summary>
    protected abstract void CreateAndConfigureCamera();

    public abstract Camera3D GetCamera();
}

