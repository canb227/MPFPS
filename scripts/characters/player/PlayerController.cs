using Godot;
using ImGuiNET;
using System;


public partial class PlayerController : Controller
{
    public readonly ulong userSteamID;
    public int playerNumber = -1;
    public int teamNumber = -1;
    public Character possessedCharacter;

    public Vector2 movementInput = Vector2.Zero;

    public PlayerController(ulong userSteamID)
    {
        this.userSteamID = userSteamID;

        //NetworkInputEvent += HandleInputMessage  idk some shit like this maybe
    }

    public Character GetPossessed()
    {
        return possessedCharacter;
    }

    public void Possess(Character character)
    {

        possessedCharacter = character;
        throw new NotImplementedException();
    }

    public void Possess(ulong eid)
    {
        Entity e = Global.world.FindEntity(eid);
        if (e is Character)
        {
            possessedCharacter = (Character)e;
        }
        throw new NotImplementedException();
    }



    public int GetTeam()
    {
        return teamNumber;
    }

    public void ChangeTeam(int teamNum)
    {
        teamNumber = teamNum;
    }

    public bool IsHuman()
    {
        return true;
    }

    public bool IsAI()
    {
        return false;
    }
    public void HandleNetworkInputMessage(ulong fromSteamID) //, InputMessage msg)
    {
        if (fromSteamID == this.userSteamID)
        {
            //this input is for me, apply it
        }
    }

    public void PerFrame(double delta)
    {
        if (userSteamID == Global.steamid && Global.DrawDebugScreens)
        {
            PlayerControllerDebugDraw();
        }

        movementInput = Input.GetVector("MOVE_FORWARD", "MOVE_BACKWARD", "MOVE_LEFT", "MOVE_RIGHT");


    }

    private void PlayerControllerDebugDraw()
    {
        ImGui.Begin("Local PlayerController Debug");
        ImGui.Text($"Movement Input Vector  X: {movementInput.X} |  Y: {movementInput.Y}");
        ImGui.End();
    }
}