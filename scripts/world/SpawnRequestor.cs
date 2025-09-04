using GameMessages;
using Godot;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

public static class SpawnRequestor
{

    public static void SpawnGameObject(SpawnGameObjectData data)
    {
        Logging.Log($"Requesting the authority ({data.Authority}) spawn a new GameObject of type {data.ObjName}", "SpawnRequestorGameObject");
        data.Request = true;
        data.Sender = Global.steamid;
        Global.network.SendData(data.ToByteArray(), NetType.SPAWN_GAMEOBJECT, NetworkUtils.SteamIDToIdentity(data.Authority));
    }

    public static void HandleSpawnGameObjectProto(byte[] data, ulong fromSteamID)
    {
        SpawnGameObjectData msg = SpawnGameObjectData.Parser.ParseFrom(data);

        if (msg.Request)
        {
            Logging.Log($"User {fromSteamID} just requested that we spawn GameObject {msg.ObjName}.","SpawnRequestorGameObject");
            if (!NetworkUtils.IsMe(msg.Authority))
            {
                Logging.Error($"ERROR: We are not the authority, why was a request sent to us?", "SpawnRequestorGameObject");
            }

            //Authority validation checks here

            Logging.Log($"Commanding all peers to spawn new gameObject with type: {msg.ObjName} and ID: {msg.WithID}", "SpawnRequestorGameObject");
            msg.Request = false;
            msg.Sender = Global.steamid;
            msg.WithID = Global.world.AssignNewID();
            Global.network.BroadcastData(msg.ToByteArray(), NetType.SPAWN_GAMEOBJECT, Global.Lobby.lobbyPeers.ToList());
        }
        else
        {
            Logging.Log($"User {fromSteamID} just commanded that we spawn GameObject {msg.ObjName}.", "SpawnRequestorGameObject");
            if (!msg.HasWithID)
            {
                Logging.Error($"ERROR: This spawn command is missing an ID assignment.", "SpawnRequestorGameObject");
            }

            Global.world.SpawnGameObject(msg);
            
            if (msg.RequestorAutoPossess != 0)
            {
                Global.world.controllers[msg.RequestorAutoPossess].PossessLocal(msg.WithID);
            }

        }
    }

    public static void SpawnAndPossess(SpawnGameObjectData data, ulong controllerUID)
    {

        Logging.Log($"Requesting the authority ({data.Authority}) spawn a new GameObject (character) of type {data.ObjName}, then connect a controller to it.", "SpawnRequestorGameObject");
        data.Request = true;
        data.RequestorAutoPossess = controllerUID;
        data.Sender = Global.steamid;
        Global.network.SendData(data.ToByteArray(), NetType.SPAWN_GAMEOBJECT, NetworkUtils.SteamIDToIdentity(data.Authority));
        
    }
}

