using NetworkMessages;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


public class ChatManager
{

    public delegate void ChatMessageReceived(ChatMessage chatMessage);
    public static event ChatMessageReceived ChatMessageReceivedEvent;

    public static void HandleSteamMessage(SteamNetworkingMessage_t message)
    {
        ChatMessage chatMessage = ChatMessage.Parser.ParseFrom(NetworkUtils.UnwrapSteamMessage(message));
        HandleChatMessage(chatMessage,message.m_identityPeer.GetSteamID64());
    }

    public static void HandleChatMessage(ChatMessage chatMessage,ulong fromUser)
    {
        Logging.Log($"Peer {fromUser} chats: {chatMessage.Message}", "ChatDebug");
        ChatMessageReceivedEvent?.Invoke(chatMessage);
    }

    public static ChatMessage ConstructChatMessage(string message)
    {
        ChatMessage chatMessage = new ChatMessage();
        chatMessage.Message = message;
        return chatMessage;
    }

    public static void SendChatMessageToUser(ChatMessage message, ulong steamID)
    {
        Global.network.SendMessage(message, steamID, NetworkManager.NetworkChannel.Chat);
    }

    public static List<(ulong,EResult)> SendChatMessageToAllUsers(ChatMessage message)
    {
        return Global.network.BroadcastMessage(message, NetworkManager.NetworkChannel.Chat);
    }

    public static void Chat(string message)
    {
        ChatMessage chatMessage = ConstructChatMessage(message);
        foreach( (ulong peer,EResult result) in SendChatMessageToAllUsers(chatMessage))
        {
            Logging.Log($"Chat message sent to: {peer}, result:{result.ToString()}","ChatDebug");   
        }
    }

}

