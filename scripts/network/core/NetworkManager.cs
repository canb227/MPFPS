using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;



public abstract partial class NetworkManager : Node
{
    //The following events are already available:
    //  ConnectedToServer
    //  ConnectionFailed
    //  PeerConnected 
    //  PeerDisconnected
    //Below are extra ones

    public delegate void HostedServer();
    public static event HostedServer HostedServerEvent;

    public delegate void ClosedServer();
    public static event ClosedServer ClosedServerEvent;

    public delegate void LeftServer();
    public static event LeftServer LeftServerEvent;

    protected void InvokeHostedServerEvent()
    {
        HostedServerEvent?.Invoke();
    }

    protected void InvokeClosedServerEvent()
    {
        ClosedServerEvent?.Invoke();
    }

    protected void InvokeLeftServerEvent()
    {
        LeftServerEvent?.Invoke();
    }

    public abstract void HostServer(ushort port = 2272);
    public abstract void JoinServer(ulong peerID, ushort port = 2272);
    public abstract void LeaveServer();
}

