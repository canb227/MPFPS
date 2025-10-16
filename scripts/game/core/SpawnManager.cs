using Godot;
using MessagePack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;



public partial class SpawnManager : Node
{
    
    public static T WeightedRandom<T>(Dictionary<T, float> weights, Random rand)
    {

        float total = 0;
        foreach (var w in weights.Values) total += w;

        float roll = (float)(rand.NextDouble() * total);
        foreach (var kv in weights)
        {
            roll -= kv.Value;
            if (roll <= 0)
                return kv.Key;
        }
        return default;
    }

public static ItemMarker3D PickWeightedMarker(List<ItemMarker3D> markers, Random rand)
{
    float totalWeight = 0;
    foreach (var m in markers)
        totalWeight += m.generalWeight; // assume each marker has a Rule with GeneralWeight

    float roll = (float)(rand.NextDouble() * totalWeight);

    foreach (var m in markers)
    {
        roll -= m.generalWeight;
        if (roll <= 0)
            return m;
    }

    return markers.Last(); // fallback
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
            if(rand.NextSingle() < marker.generalWeight)
            {
                var weights = new Dictionary<string, float>
                {
                    { "Weapon", marker.spawnWeaponWeight },
                    { "Component", marker.spawnComponentsWeight },
                    { "PackageItem", marker.spawnPackageItemWeight }
                };

                string choice = WeightedRandom(weights, rand);
                SpawnItem(choice, marker, roundSpawnPoints);
            }
        }
    }

    private void ForceSpawnItem(GameObjectType type, List<ItemMarker3D> roundSpawnPoints)
    {
        // Pick random markers that allow this type and spawn
        ItemMarker3D choosenMarker = PickWeightedMarker(roundSpawnPoints, new Random());
        SpawnItem(choosenMarker, type);
    }

    private void SpawnItem(string type, ItemMarker3D marker, List<ItemMarker3D> roundSpawnPoints)
    {
        GD.Print($"Spawning {type} at {marker.Name}");
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
        roundSpawnPoints.Remove(marker);
    }

    private void SpawnItem(ItemMarker3D marker, GameObjectType type)
    {
        //select random component and spawn it
        //Global.gameState.Auth_SpawnObject(GameObjectType., );
    }

    private void SpawnRandomWeapon(ItemMarker3D marker)
    {
        //select random weapon and spawn it
        //SpawnItem(marker, type);
    }
    private void SpawnRandomComponent(ItemMarker3D marker)
    {
        //select random component and spawn it
        //SpawnItem(marker, type);
    }

    private void SpawnRandomPackageItem(ItemMarker3D marker)
    {
        //select random component and spawn it
        //SpawnItem(marker, type);
    }


}