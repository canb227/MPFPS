using Godot;
using Godot.Collections;
using ImGuiNET;
using Steamworks;
using System;

public partial class PlayerInputHandler : Node
{

    public bool VoiceChatAlwaysOn = true;



    public override void _Ready()
    {
        Logging.Log($"Starting local input gathering", "LocalInput");
        Global.gameState.PlayerInputs[Global.steamid] = new PlayerInputData();
        Global.gameState.PlayerInputs[Global.steamid].playerID = Global.steamid;
        if (VoiceChatAlwaysOn)
        {
            SteamUser.StartVoiceRecording();
        }

    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionType())
        {
            Global.gameState.PlayerInputs[Global.steamid].MovementInputVector = Input.GetVector("MoveForward", "MoveBackward", "MoveLeft", "MoveRight");
            foreach (string action in Enum.GetNames(typeof(ActionFlags)))
            {
                if (@event.IsAction(action))
                {

                    if(@event.IsPressed())
                    {
                        Global.gameState.PlayerInputs[Global.steamid].actions = Global.gameState.PlayerInputs[Global.steamid].actions | InputMapManager.actionNameToActionFlagMap[action];
                    }
                    else
                    {
                        Global.gameState.PlayerInputs[Global.steamid].actions = Global.gameState.PlayerInputs[Global.steamid].actions & ~InputMapManager.actionNameToActionFlagMap[action];
                    }
                }
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion m)
        {
            Global.gameState.PlayerInputs[Global.steamid].LookInputVector = m.Relative;
        }
        else if (@event is InputEventJoypadMotion j)
        {
            //Controller not supported
        }
    }



    public override void _Process(double delta)
    {
        if (SteamUser.GetAvailableVoice(out uint numBytes) == EVoiceResult.k_EVoiceResultOK)
        {
            byte[] voiceBytes = new byte[numBytes];
            var result = SteamUser.GetVoice(true, voiceBytes, numBytes, out uint bytesWritten);

            if (bytesWritten != numBytes)
            {
                Logging.Warn($"Unexpected number of bytes in voice buffer array: (wrote {bytesWritten} but expected {numBytes})", "SteamVoice");
            }
            if (result!= EVoiceResult.k_EVoiceResultOK)
            {
                Logging.Warn($"Error collecting voice data: {result.ToString()}", "SteamVoice");
                return;
            }
            //Logging.Log($"Sending voice data of size: {voiceBytes.Length}", "SteamVoice");

            var playerChar = Global.gameState.GetCharacterControlledBy(Global.steamid);
            if (playerChar is BasicPlayerCharacter bpc)
            {
                if (bpc.knockedOut || bpc.currentHealth <= 0)
                {
                    return;
                }
                else
                {
                    Global.network.BroadcastData(voiceBytes, Channel.SteamVoice, Global.Lobby.AllPeersExceptSelf(), NetworkUtils.k_nSteamNetworkingSend_UnreliableNoDelay);
                }
            }
            else if (playerChar is Ghost ghost)
            {
                Global.network.BroadcastData(voiceBytes, Channel.SteamVoiceDead, Global.Lobby.AllPeersExceptSelf(), NetworkUtils.k_nSteamNetworkingSend_UnreliableNoDelay);
            }


            
        }
       
        
        if (Global.DrawDebugScreens)
        {
            //ImGui.Begin("input Debug");
            //ImGui.Text("InputMvVector: " + Global.gameState.PlayerInputs[Global.steamid].MovementInputVector.ToString());
            //ImGui.Text("InputLookVector: " + Global.gameState.PlayerInputs[Global.steamid].LookInputVector.ToString());
            //foreach (var actionEntry in Global.gameState.PlayerInputs[Global.steamid].actions)
            //{
            //    ImGui.Text($"{actionEntry.Key}:{actionEntry.Value}");
            //}
            //ImGui.End();
        }
    }

}

public struct SteamVoiceMessage
{
    public byte[] compressedVoiceBuffer;
    public int numBytes;
}