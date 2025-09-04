using Godot;
using ImGuiNET;
using System;
using GameMessages;
using Google.Protobuf;
using System.Linq;
using System.Collections.Generic;
using Google.Protobuf.Collections;

public partial class PlayerController : Node, Controller
{
    public ulong userSteamID;
    public int playerNumber = -1;
    public int teamNumber = -1;
    
    public Character possessedCharacter;

    public Vector2 movementInput = Vector2.Zero;
    public Vector2 mouseRelativeAccumulator = Vector2.Zero;
    public MapField<string,ActionInput> inputs = new MapField<string, ActionInput>();

    public PlayerController(ulong userSteamID)
    {
        this.userSteamID = userSteamID;
        Global.world.controllers.Add(userSteamID,this);
        SteamNetwork.NetworkPCMessageEvent += OnPCMessage;
        mouseRelativeAccumulator = Vector2.Zero;

        foreach(string actionName in Global.InputMap.InputActionList.Keys)
        {
            ActionInput actionInput = new ActionInput();
            actionInput.ActionName = actionName;
            actionInput.Changed = false;
            actionInput.Pressed = false;
            actionInput.Released = false;
            actionInput.JustPressed = false;
            inputs.Add(actionName, actionInput);
        }
    }

    private void OnPCMessage(PlayerControllerMessage msg)
    {
        if (msg.Target != GetUniqueID())
        {
            return;
        }
        switch (msg.Type)
        {
            case PlayerControllerMessageType.PcError:
                break;
            case PlayerControllerMessageType.Possess:
                if (msg.Request)
                {
                    msg.Sender = Global.steamid;
                    msg.Request = false;
                    Global.network.BroadcastData(msg.ToByteArray(), NetType.PLAYERCONTROLLER, Global.Lobby.lobbyPeers.ToList());
                }
                else
                {
                    PossessLocal(msg.Other);
                }
                break;
            case PlayerControllerMessageType.InputOne:
                break;
            case PlayerControllerMessageType.InputFull:
                movementInput.X = msg.MovementVectorX;
                movementInput.Y = msg.MovementVectorY;
                mouseRelativeAccumulator.X = msg.RotateVectorAccumulatorX;
                mouseRelativeAccumulator.Y = msg.RotateVectorAccumulatorY;
                foreach(var item in msg.Inputs)
                {
                    if (item.Value.Changed)
                    {
                        item.Value.Changed = false;
                        inputs[item.Key] = item.Value;
                    }
                }
                break;
            default:
                break;
        }
    }

    public Character GetPossessed()
    {
        return possessedCharacter;
    }

    public void Possess(ulong CharacterUID)
    {
        PlayerControllerMessage msg = new();
        msg.Type = PlayerControllerMessageType.Possess;
        msg.Sender = Global.steamid;
        msg.Authority = userSteamID;
        msg.Target = userSteamID;
        msg.Other = CharacterUID;

        if (NetworkUtils.IsMe(userSteamID))
        {
            msg.Request = false;
            Global.network.BroadcastData(msg.ToByteArray(), NetType.PLAYERCONTROLLER, Global.Lobby.lobbyPeers.ToList());
        }
        else
        {
            msg.Request = true;
            Global.network.SendData(msg.ToByteArray(), NetType.PLAYERCONTROLLER, NetworkUtils.SteamIDToIdentity(userSteamID));
        }

    }

    public void Possess(Character character)
    {
        Possess(character.GetUID());
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


    public void PerFrame(double delta)
    {
        if (userSteamID == Global.steamid && Global.DrawDebugScreens)
        {
            PlayerControllerDebugDraw();
        }

        if (NetworkUtils.IsMe(userSteamID))
        {
            Vector2 tempmovementInput = Input.GetVector("MOVE_FORWARD", "MOVE_BACKWARD", "MOVE_LEFT", "MOVE_RIGHT");

            PlayerControllerMessage FrameInputMessage = new();
            FrameInputMessage.Type = PlayerControllerMessageType.InputFull;
            FrameInputMessage.Request = false;
            FrameInputMessage.Sender = Global.steamid;
            FrameInputMessage.Authority = Global.steamid;
            FrameInputMessage.Target = Global.steamid;
            FrameInputMessage.MovementVectorX = tempmovementInput.X;
            FrameInputMessage.MovementVectorY = tempmovementInput.Y;
            FrameInputMessage.RotateVectorAccumulatorX = mouseRelativeAccumulator.X;
            FrameInputMessage.RotateVectorAccumulatorY = mouseRelativeAccumulator.Y;

            foreach (string actionName in inputs.Keys)
            {
                if (inputs[actionName].Changed)
                {
                    FrameInputMessage.Inputs.Add(actionName,inputs[actionName]);
                    inputs[actionName].Changed = false;
                }
            }


            Global.network.BroadcastData(FrameInputMessage.ToByteArray(), NetType.PLAYERCONTROLLER, Global.Lobby.lobbyPeers.ToList());

            FrameInputMessage = new();
        }

    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseEvent && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            mouseRelativeAccumulator += mouseEvent.Relative;
        }

        foreach (string actionName in Global.InputMap.InputActionList.Keys)
        {
            if (@event.IsAction(actionName))
            {
                inputs[actionName].Pressed = @event.IsActionPressed(actionName,true);
                inputs[actionName].JustPressed = @event.IsActionPressed(actionName, false);
                inputs[actionName].Released = @event.IsActionReleased(actionName);
                inputs[actionName].Changed = true;
            }

        }
    }

    private void PlayerControllerDebugDraw()
    {
        ImGui.Begin("Local PlayerController Debug");
        ImGui.Text($"Movement Input Vector  X: {movementInput.X} |  Y: {movementInput.Y}");
        ImGui.Text($"Mouse Input Vector  X: {mouseRelativeAccumulator.X} |  Y: {mouseRelativeAccumulator.Y}");
        foreach(string actionName in inputs.Keys)
        {
            ImGui.Text($"InputAction: {actionName} |  justPressed?:{inputs[actionName].JustPressed} | pressed?:{inputs[actionName].Pressed} | released?:{inputs[actionName].Released}");
        }
        ImGui.End();

        ImGui.Begin("Local PlayerController Controlled Char Debug");
        ImGui.Text($"Name:  {possessedCharacter.Name}");
        if (possessedCharacter.Name.Equals("PlayerSpectator"))
        {
            SpectatorCharacter sc = (SpectatorCharacter)possessedCharacter;
            ImGui.Text($"Velocity:  {sc.body.Velocity}");
            ImGui.Text($"Local Velocity:  {sc.GetLocalVelocity()}");
        }

        ImGui.End();
    }

    public ulong GetUniqueID()
    {
        return userSteamID;
    }

    public void PossessLocal(ulong ChracterUID)
    {
        possessedCharacter = (Character)Global.world.entities[ChracterUID];
        possessedCharacter.controller = this;
        
        if (NetworkUtils.IsMe(userSteamID))
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }

    }

    public void Tick(double delta)
    {
        //throw new NotImplementedException();
    }
}