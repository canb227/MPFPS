using Godot;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


public enum RPCType
{
    ERROR,
    Chat,
    StartGame,

}

[MessagePackObject]
public class RPCPacket
{
    [Key(0)]
    public RPCType type;
    
    [Key(1)]
    public List<ulong> numericalParams;

    [Key(2)]
    public List<string> stringParams;
}

public enum RPCMode
{
    OnlySendToAuth,
    SendToAllPeers,
}

[System.AttributeUsage(AttributeTargets.Method,Inherited = true ,AllowMultiple = false)]
public class RPCMethodAttribute : Attribute
{
    public RPCMode mode;

    public RPCMethodAttribute(RPCMode mode = RPCMode.SendToAllPeers)
    {
        this.mode = mode;
    }

}


public static class RPCManager
{
    public delegate void ChatReceived(string msg, ulong sender);
    public static event ChatReceived ChatReceivedEvent;


    public static void DiscoverRPCMethods()
    {
        Logging.Log($"Searching for RPC methods!", "RPCManager");
        Assembly assembly = Assembly.GetExecutingAssembly();
        foreach (Type type in assembly.GetTypes())
        {
            MethodInfo[] methods = type.GetMethods();
            foreach (MethodInfo method in methods)
            {
                RPCMethodAttribute rpc = method.GetCustomAttribute<RPCMethodAttribute>();
                if (rpc != null)
                {
                    string rpcMethodName = method.Name;
                    ParameterInfo[] parameters = method.GetParameters();
                    Logging.Log($"\nFound RPC method: {rpcMethodName} with {parameters.Length} Parameters", "RPCManager");
                    if (parameters.Length > 0)
                    {
                        Logging.Log($"Param List:", "RPCManager");
                        foreach (ParameterInfo parameter in parameters)
                        {
                            Logging.Log($"Name: {parameter.Name}, Type: {parameter.ParameterType}","RPCManager");
                        }
                    }
                }
            }
        }
    }

    public static void ProcessNetCommandBytes(byte[] payload, ulong sender)
    {
        Logging.Log($"RCV: {MessagePackSerializer.ConvertToJson(payload)}", "RPCManager");
        RPCPacket packet = MessagePackSerializer.Deserialize<RPCPacket>(payload);
        Logging.Log($"RPC RCV | type:{packet.type}", "RPCManager");
        if (packet == null)
        {
            Logging.Error($"RPC Deserializer failure", "RPCManager");
        }
        switch (packet.type)
        {
            case RPCType.ERROR:
                Logging.Error($"Invalid network command received from {sender}!", "RPCManager");
                break;
            case RPCType.StartGame:
                Logging.Log($"Network command from {sender} to start game on map {packet.stringParams[0]}!", "RPCManager");
                Global.gameState.StartGame(packet.stringParams[0]);
                break;
            case RPCType.Chat:
                Logging.Log($"Network chat from {sender}!", "RPCManager");
                ChatReceivedEvent?.Invoke(packet.stringParams[0], sender);
                break;
            default:
                break;
        }
    }

    public static void NetCommand_Chat(string msg)
    {
        RPCPacket packet = new();
        packet.type = RPCType.Chat;
        packet.stringParams = new();
        packet.stringParams.Add(msg);
        byte[] payload = MessagePackSerializer.Serialize(packet);
        Logging.Log($"RPC SENT: Type=Chat, data={MessagePackSerializer.ConvertToJson(payload)}", "RPCManager");
        Global.network.BroadcastData(payload, Channel.NetCommands, Global.Lobby.lobbyPeers.ToList());
    }

    public static void NetCommand_StartGame(string scenePathOfMap)
    {
        RPCPacket packet = new();
        packet.type = RPCType.StartGame;
        packet.stringParams = new();
        packet.stringParams.Add(scenePathOfMap);
        byte[] payload = MessagePackSerializer.Serialize(packet);
        Logging.Log($"RPC SENT: Type=StartGame, data={MessagePackSerializer.ConvertToJson(payload)}", "RPCManager");
        Global.network.BroadcastData(payload,Channel.NetCommands,Global.Lobby.lobbyPeers.ToList());
    }

    public static void HandleRPCBytes(byte[] message, ulong sender)
    {
        RPCMessage packet = MessagePackSerializer.Deserialize<RPCMessage>(message);

        ProcessRPC(packet.nodePath,packet.methodName, packet.parameters);
    }

    public static void RPC(Node context, string methodName, List<Object> parameters)
    {

        //bool isAuthority = Global.steamid == context.authority;
        MethodInfo method = context.GetType().GetMethod(methodName);
        if (method == null)
        {
            Logging.Error($"RPC on target type: {context.GetType().ToString()} targets invalid method:{methodName}","RPCManager");
        }
        RPCMethodAttribute attribute = method.GetCustomAttribute<RPCMethodAttribute>();
        if (attribute == null)
        {
            Logging.Error($"RPC on target type: {context.GetType().ToString()} targets method missing RPC annotation!!!:{methodName}", "RPCManager");
        }
        if (attribute.mode == RPCMode.OnlySendToAuth)
        {
            ulong authority = 0;
            if (context is GameObject go)
            {
                authority = go.authority;
            }
            else
            {
                authority = Global.gameState.defaultAuth;
            }
            RPCMessage packet = new();
            packet.nodePath = Global.instance.GetPathTo(context);
            packet.methodName = methodName;
            packet.parameters = parameters;

            Global.network.SendData(MessagePackSerializer.Serialize(packet), Channel.RPC, authority);
        }
        else if(attribute.mode == RPCMode.SendToAllPeers)
        {
            RPCMessage packet = new();
            packet.nodePath = Global.instance.GetPathTo(context);
            packet.methodName = methodName;
            packet.parameters = parameters;

            Global.network.BroadcastData(MessagePackSerializer.Serialize(packet), Channel.RPC, Global.Lobby.AllPeers());
        }
    }

    public static void ProcessRPC(NodePath path, string methodName, List<Object> parameters)
    {

        Node node = Global.instance.GetNode(path);
        if (node == null)
        {
            Logging.Error($"RPC Targeted invalid node: {path}", "RPCManager");
            return;
        }

        Type nodeType = node.GetType();
        MethodInfo method = nodeType.GetMethod(methodName);
        if (method == null)
        {
            Logging.Error($"Node at {path} (reflection type:{nodeType.ToString()} does not have method named: {methodName}", "RPCManager");
        }
        try
        {
            method.Invoke(node, parameters.ToArray());
        }
        catch (Exception e)
        {
            Logging.Error($"Error processing RPC: NodePath: {path} | MethodName: {methodName} | Parameters: {string.Join(",",parameters)} \nMessage: {e.Message}","RPCManager");
            if (e is TargetInvocationException ti)
            {
                Logging.Error($"Inner Exception: {ti.InnerException.Message} \nTrace: {ti.InnerException.StackTrace}", "RPCManager");
            }

        }
    }

}

[MessagePackObject]
public struct RPCMessage
{
    [Key(0)]
    public string nodePath;

    [Key(1)]
    public string methodName;

    [Key(2)]
    public List<Object> parameters;
}