using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public partial class GameState : Node
{

    public World world;

    public override void _Ready()
    {
        world = new();
        world.Name = "World";
        AddChild(world);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void StartGame(string map)
    {
        Global.ui.StartLoadingScreen();
        if (Multiplayer.IsServer())
        {
            world.ChangeLevel(map);
            world.Rpc(World.MethodName.SpawnPlayer, 1);
            foreach (int id in Multiplayer.GetPeers())
            {
                world.Rpc(World.MethodName.SpawnPlayer, id);
            }
        }
        Global.ui.StopLoadingScreen();
    }

}

