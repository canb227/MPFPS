using Godot;
using MessagePack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;



public partial class ItemSpawnManager : Node
{


    public static ItemMarker3D PickWeightedMarker(List<ItemMarker3D> markers, GameObjectType type, Random rand)
{
    Type objectClass = GameObjectLoader.GetTypeOfGameObjectType(type);

    List<ItemMarker3D> validMarkers = new List<ItemMarker3D>();
    foreach (var marker in markers)
    {
        if (objectClass == typeof(GOPackageItem))
        {
            if (!marker.canSpawnPackageItems)
                continue;
        }
        else if (objectClass == typeof(BasicGun))
        {
            if (!marker.canSpawnWeapons)
                continue;
        }
        else if (objectClass == typeof(GOComponent))
        {
            if (!marker.canSpawnComponents)
                continue;
        }
    
        else if (objectClass == typeof(GOBaseAccessory))
        {
            if (!marker.canSpawnAccessories)
                continue;
        }

        validMarkers.Add(marker);
    }

    if (validMarkers.Count == 0)
        return null;

    int totalWeight = 0;
    foreach (var marker in validMarkers)
        totalWeight += marker.generalWeight;

    int roll = rand.Next(0, totalWeight);
    int cumulative = 0;

    foreach (var marker in validMarkers)
    {
        cumulative += marker.generalWeight;
        if (roll < cumulative)
            return marker;
    }

    return validMarkers[validMarkers.Count - 1];
}



    public void GenerateItems(Dictionary<GameObjectType, int> minimumItemTypeCount)
    {
        List<ItemMarker3D> roundSpawnPoints = MapManager.ItemSpawnPoints.ToList();
        //force spawn minimum packages
        foreach (var item in minimumItemTypeCount)
        {
            for (int i = 0; i < item.Value; i++)
            {
                ForceSpawnItem(item.Key, roundSpawnPoints);
            }
        }

        Random rand = new();
        //fill markers using random weighting
        foreach (var marker in roundSpawnPoints)
        {
            if(rand.NextSingle() < marker.spawnChance)
            {
                List<string> spawnOptions = new();
                if (marker.canSpawnWeapons)
                {
                    spawnOptions.Add("Weapon");
                }
                if (marker.canSpawnComponents)
                {
                    spawnOptions.Add("Component");
                }
                if (marker.canSpawnPackageItems)
                {
                    spawnOptions.Add("PackageItem");
                }
                if (marker.canSpawnAccessories)
                {
                    spawnOptions.Add("Accessories");
                }

                if (spawnOptions.Count > 0)
                {
                    SpawnRandomItem(spawnOptions[rand.Next(spawnOptions.Count)], marker, roundSpawnPoints);
                }
                else
                {
                    Logging.Warn("Spawn Marker with no valid spawn options!", "ItemSpawnManager");
                }
            }
        }
    }

    private void ForceSpawnItem(GameObjectType type, List<ItemMarker3D> roundSpawnPoints)
    {
        // Pick random markers that allow this type and spawn
        ItemMarker3D choosenMarker = PickWeightedMarker(roundSpawnPoints, type, new Random());
        SpawnItem(choosenMarker, type);
        roundSpawnPoints.Remove(choosenMarker);
    }

    private void SpawnRandomItem(string type, ItemMarker3D marker, List<ItemMarker3D> roundSpawnPoints)
    {
        GD.Print($"Trying to spawn {type} at {marker.Name}");
        if (type == "Weapon")
        {
            SpawnRandomWeapon(marker);
        }
        else if (type == "Component")
        {
            SpawnRandomComponent(marker);
        }
        else if (type == "PackageItem")
        {
            SpawnRandomPackageItem(marker);
        }
        else if (type == "Accessories")
        {
            SpawnRandomAccessory(marker);
        }
    }

    private void SpawnItem(ItemMarker3D marker, GameObjectType type)
    {
        //spawn the provided object type
        GameObjectConstructorData data = new(type);
        data.spawnTransform = marker.Transform;
        Global.gameState.Auth_SpawnObject(type, data);
        GD.Print($"Spawned {type} at {marker.Name}");
    }

    private void SpawnRandomWeapon(ItemMarker3D marker)
    {
        //select random weapon and spawn it
        var weaponList = GameObjectLoader.GetAllObjectsOfType(typeof(BasicGun));
        if(weaponList.Count <= 0)
        {
            Logging.Error("Weapon List is Empty and Tried to Spawn a Weapon", "ItemSpawnManager");
            return;
        }
        Random rand = new();
        SpawnItem(marker, weaponList[rand.Next(weaponList.Count)]);
    }
    private void SpawnRandomComponent(ItemMarker3D marker)
    {
        //select random component and spawn it
        var componentList = GameObjectLoader.GetAllObjectsOfType(typeof(GOComponent));
        if(componentList.Count <= 0)
        {
            Logging.Error("Component List is Empty and Tried to Spawn a Component", "ItemSpawnManager");
            return;
        }
        Random rand = new();
        SpawnItem(marker, componentList[rand.Next(componentList.Count)]);
    }

    private void SpawnRandomPackageItem(ItemMarker3D marker)
    {
        //select random packageitem and spawn it
        var packageItemList = GameObjectLoader.GetAllObjectsOfType(typeof(GOPackageItem));
        if (packageItemList.Count <= 0)
        {
            Logging.Error("Package Item List is Empty and Tried to Spawn a Package Item", "ItemSpawnManager");
            return;
        }
        Random rand = new();
        SpawnItem(marker, packageItemList[rand.Next(packageItemList.Count)]);
    }
    
    private void SpawnRandomAccessory(ItemMarker3D marker)
    {
        //select random packageitem and spawn it
        var accessoryItemList = GameObjectLoader.GetAllObjectsOfType(typeof(GOBaseAccessory));
        if(accessoryItemList.Count <= 0)
        {
            Logging.Error("Package Item List is Empty and Tried to Spawn a Package Item", "ItemSpawnManager");
            return;
        }
        Random rand = new();
        SpawnItem(marker, accessoryItemList[rand.Next(accessoryItemList.Count)]);
    }


}