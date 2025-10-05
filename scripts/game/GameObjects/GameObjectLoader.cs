using Godot;
using System;
using System.Collections.Generic;
using System.Security.AccessControl;

public static class GameObjectLoader
{

    public static Dictionary<string, (GameObjectType type, string scenePath, Type cls)> GameObjectDictionary = new()
    {
        // { "paperlabel", (GameObjectType.LabelPaper, "res://scenes/GameObjects/props/Ball.tscn", typeof()) }
        {"ball", (GameObjectType.Ball, "res://scenes/GameObjects/props/Ball.tscn", typeof(SimpleShape)) },
        {"ghost", (GameObjectType.Ghost, "res://scenes/GameObjects/player/ghost.tscn", typeof(Ghost)) },
        {"tony", (GameObjectType.Tony,"res://scenes/GameObjects/player/tony.tscn", typeof(Tony))},
        {"basicPlayer" ,(GameObjectType.BasicPlayer,"res://scenes/GameObjects/player/BasicPlayer.tscn",typeof(BasicPlayer)) }
    };

    internal static GameObject LoadObjectByType(GameObjectType type)
    {
       
        foreach (var entry in GameObjectDictionary)
        {
            if (entry.Value.type == type)
            {
                GameObject obj = LoadObjectByTypeName(entry.Key);
                obj.type = type;
                return obj;
            }
        }
        return null;
    }
    
    public static T LoadObjectByType<T>(GameObjectType type)
    {
        GameObject obj = LoadObjectByType(type);
        obj.type = type;
        return (T)obj;
    }

    public static GameObject LoadObjectByTypeName(string typeName, out GameObjectType type)
    {
        if (GameObjectDictionary.ContainsKey(typeName))
        {
            type = GameObjectDictionary[typeName].type;
            return LoadObjectByTypeName(typeName);
        }
        else
        {
            Logging.Error($"Cannot load object: No object with TypeName: \"{typeName}\" exists.", "GameObjectLoader");
            type = GameObjectType.ERROR;
            return null;
        }
    }
    public static GameObject LoadObjectByTypeName(string typeName)
    {
        return ResourceLoader.Load<PackedScene>(GameObjectDictionary[typeName].scenePath).Instantiate<GameObject>();

    }

    public static string GetGameObjectTypeName(GameObjectType type)
    {
        foreach (var entry in GameObjectDictionary)
        {
            if (entry.Value.type == type)
            {
                return entry.Key;
            }
        }
        return null;
    }

}

public enum GameObjectType
{
    ERROR,
    Ball,
    Ghost,
    Tony,
    BasicPlayer,
    GameButton,
    Crusher,
    LabelPaper,
}