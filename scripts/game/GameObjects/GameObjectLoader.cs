using Godot;
using System;
using System.Collections.Generic;
using System.Security.AccessControl;

public static class GameObjectLoader
{

    public static Dictionary<string, (GameObjectType type, string scenePath, Type cls)> GameObjectDictionary = new()
    {
        //Misc
        { "LabelPaper", (GameObjectType.LabelPaper, "res://scenes/GameObjects/props/LabelPaper.tscn", typeof(GOLabelPaper)) },
        { "PaperBox" , (GameObjectType.PaperBox, "res://scenes/GameObjects/props/PaperBox.tscn", typeof(GOPaperBox)) },
        { "ball", (GameObjectType.Ball, "res://scenes/GameObjects/props/Ball.tscn", typeof(SimpleShape)) },
        {"ghost", (GameObjectType.Ghost, "res://scenes/GameObjects/player/ghost.tscn", typeof(Ghost)) },
        {"Hands", (GameObjectType.Hands,"res://scenes/GameObjects/items/equipment/Hands.tscn", typeof(Hands))},
        {"basicPlayer" ,(GameObjectType.BasicPlayer,"res://scenes/GameObjects/player/BasicPlayer.tscn",typeof(BasicPlayerCharacter)) },

        //PackageItems
        {"PackageBall", (GameObjectType.PackageBall,"res://scenes/GameObjects/props/packageItems/Ball.tscn", typeof(GOPackageItem))},
        {"PackageBox", (GameObjectType.PackageBox,"res://scenes/GameObjects/props/packageItems/PaperBox.tscn", typeof(GOPackageItem))},
    };

    //get all objects from the GameObjectDictionary of a given type and return a list of their GameObjectType enum
    public static List<GameObjectType> GetAllObjectsOfType(Type type)
    {
        return new List<GameObjectType>();
    }

    //get all objects from the GameObjectDictionary of a given type and return a list of their string keys
    public static List<string> GetAllObjectNamesOfType(Type type)
    {
        return new List<string>();
    }

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
    BasicPlayer,
    GameButton,
    Crusher,
    LabelPaper,
    PaperBox,
    Hands,
    PackageBall,
    PackageBox,


}