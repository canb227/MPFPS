using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public static class GameObjectLoader
{
    public static Dictionary<string, string> GameObjectSceneMap = new()
    {
        {"NONE",null},
        {"spectator", "res://scenes/characters/character_spectator.tscn"},
        {"ball", "res://scenes/objects/ball.tscn" }

    };


    public static GameObject LoadGameObjectInstance(string type)
    {
        if (GameObjectSceneMap.ContainsKey(type))
        {
            PackedScene pck = ResourceLoader.Load<PackedScene>(GameObjectSceneMap[type]);
            GameObject obj = pck.Instantiate<GameObject>();
            return obj;
        }
        else
        {
            return null;
        }
    }
}


