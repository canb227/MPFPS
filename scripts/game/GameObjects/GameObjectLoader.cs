using Godot;
using System;
using System.Collections.Generic;
using System.Security.AccessControl;

public static class GameObjectLoader
{

    public static Dictionary<string, (GameObjectType type, string scenePath, Type cls)> GameObjectDictionary = new()
    {
        {"ball", (GameObjectType.Ball, "res://scenes/GameObjects/props/Ball.tscn", typeof(SimpleShape)) },
        {"ghost", (GameObjectType.Ghost, "res://scenes/GameObjects/player/ghost.tscn", typeof(Ghost)) },
        {"tony", (GameObjectType.Tony,"res://scenes/GameObjects/player/tony.tscn", typeof(Tony))},
    };

    internal static IGameObject LoadObjectByType(GameObjectType type)
    {
        foreach (var entry in GameObjectDictionary)
        {
            if (entry.Value.type == type)
            {
                IGameObject obj = LoadObjectByTypeName(entry.Key);
                obj.type = type;
                return obj;
            }
        }
        return null;
    }
    
    public static T LoadObjectByType<T>(GameObjectType type)
    {
        IGameObject obj = LoadObjectByType(type);
        obj.type = type;
        return (T)obj;
    }

    public static IGameObject LoadObjectByTypeName(string typeName, out GameObjectType type)
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
    public static IGameObject LoadObjectByTypeName(string typeName)
    {
        return ResourceLoader.Load<PackedScene>(GameObjectDictionary[typeName].scenePath).Instantiate<IGameObject>();
    }
}

public enum GameObjectType
{
    ERROR,
    Ball,
    Ghost,
    Tony,
}