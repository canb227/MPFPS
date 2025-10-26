using Godot;
using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Linq;

public static class GameObjectLoader
{


    public static Dictionary<string, (GameObjectType type, string scenePath, Type cls)> GameObjectDictionary = new()
    {
        //Misc
        { "LabelPaper", (GameObjectType.LabelPaper, "res://scenes/GameObjects/props/LabelPaper.tscn", typeof(GOLabelPaper)) },
        { "PaperBox" , (GameObjectType.PaperBox, "res://scenes/GameObjects/props/PaperBox.tscn", typeof(GOComponent)) },
        { "ball", (GameObjectType.Ball, "res://scenes/GameObjects/props/Ball.tscn", typeof(SimpleShape)) },
        {"ghost", (GameObjectType.Ghost, "res://scenes/GameObjects/player/ghost.tscn", typeof(Ghost)) },
        {"basicPlayer" ,(GameObjectType.BasicPlayer,"res://scenes/GameObjects/player/BasicPlayer.tscn",typeof(BasicPlayerCharacter)) },
        {"swarmRobot" ,(GameObjectType.SwarmRobot,"res://scenes/GameObjects/npcs/SwarmRobot.tscn",typeof(SwarmRobot)) },

        //InventoryItems
        {"Hands", (GameObjectType.Hands,"res://scenes/GameObjects/items/equipment/Hands.tscn", typeof(Hands))},
        {"BasicGun", (GameObjectType.BasicGun,"res://scenes/GameObjects/items/equipment/BasicGun.tscn", typeof(BasicGun))},

        //AccessoryItems
        {"PackageRadar", (GameObjectType.PackageRadar,"res://scenes/GameObjects/items/accessory/PackageRadar.tscn", typeof(GOBaseAccessory))},
        {"Flashlight", (GameObjectType.Flashlight,"res://scenes/GameObjects/items/accessory/Flashlight.tscn", typeof(GOBaseAccessory))},
        {"Handcuffs", (GameObjectType.Handcuffs,"res://scenes/GameObjects/items/accessory/Handcuffs.tscn", typeof(GOBaseAccessory))},
        //{"WalkieTalkie", (GameObjectType.WalkieTalkie,"res://scenes/GameObjects/items/accessory/WalkieTalkie.tscn", typeof(GOBaseAccessory))},
        //Components
        {"PowerCell", (GameObjectType.PowerCell,"res://scenes/GameObjects/components/powercell.tscn", typeof(GOComponent))},

        //Package 
        {"Package", (GameObjectType.Package, "res://scenes/GameObjects/props/Package.tscn", typeof(GOPackageBox))},

        //PackageItems
        {"BakingSoda", (GameObjectType.BakingSoda,"res://scenes/GameObjects/props/packageItems/BakingSoda.tscn", typeof(GOPackageItem))},
        {"Book", (GameObjectType.Book,"res://scenes/GameObjects/props/packageItems/Book.tscn", typeof(GOPackageItem))},
        {"CarBattery", (GameObjectType.CarBattery,"res://scenes/GameObjects/props/packageItems/CarBattery.tscn", typeof(GOPackageItem))},
        {"GlassBottle", (GameObjectType.GlassBottle,"res://scenes/GameObjects/props/packageItems/GlassBottle.tscn", typeof(GOPackageItem))},
        {"HandSaw", (GameObjectType.HandSaw,"res://scenes/GameObjects/props/packageItems/HandSaw.tscn", typeof(GOPackageItem))},
        {"JerryCan", (GameObjectType.JerryCan,"res://scenes/GameObjects/props/packageItems/JerryCan.tscn", typeof(GOPackageItem))},
        {"SawBlade", (GameObjectType.SawBlade,"res://scenes/GameObjects/props/packageItems/SawBlade.tscn", typeof(GOPackageItem))},
        {"Tire", (GameObjectType.Tire,"res://scenes/GameObjects/props/packageItems/Tire.tscn", typeof(GOPackageItem))},
    };


    public static List<GameObjectType> GetAllObjectsOfType(Type type)
    {
        return GameObjectDictionary
            .Where(kvp => kvp.Value.cls == type)
            .Select(kvp => kvp.Value.type)
            .ToList();
    }

    public static Type GetTypeOfGameObjectType(GameObjectType gameObjectType)
    {
        return GameObjectDictionary
            .Where(kvp => kvp.Value.type == gameObjectType)
            .Select(kvp => kvp.Value.cls)
            .FirstOrDefault();
    }


    public static List<string> GetAllObjectNamesOfType(Type type)
    {
        return GameObjectDictionary
            .Where(kvp => kvp.Value.cls == type)
            .Select(kvp => kvp.Key)
            .ToList();
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
    //ALL NEW ENUMS MUST GO AT THE BOTTOM OR ALL PACKAGEITEMS KEYS GET SHIFTED IN THEIR SCENE
    ERROR,
    Ball,
    Ghost,
    BasicPlayer,
    GameButton,
    //ALL NEW ENUMS MUST GO AT THE BOTTOM OR ALL PACKAGEITEMS KEYS GET SHIFTED IN THEIR SCENE
    Crusher,
    LabelPaper,
    PaperBox,
    Hands,
    BasicGun,
    //ALL NEW ENUMS MUST GO AT THE BOTTOM OR ALL PACKAGEITEMS KEYS GET SHIFTED IN THEIR SCENE
    Handcuffs,
    PowerCell,
    Package,
    BakingSoda,
    //ALL NEW ENUMS MUST GO AT THE BOTTOM OR ALL PACKAGEITEMS KEYS GET SHIFTED IN THEIR SCENE
    Book,
    CarBattery,
    GlassBottle,
    HandSaw,
    JerryCan,
    SawBlade,
    Tire,
    //ALL NEW ENUMS MUST GO AT THE BOTTOM OR ALL KEYS GET SHIFTED IN THEIR SCENEs
    SwarmRobot,
    Flashlight,
    PackageRadar,
    WalkieTalkie,
}