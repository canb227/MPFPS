using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


public static class MapManager
{
    private static Node3D nodeStaticLevel;
    private static PackedScene cachedLevel;
    private static List<PlayerMarker3D> PlayerSpawnPoints = new();
    private static List<Marker3D> HorderSpawnPoints = new();
    public static List<ItemMarker3D> ItemSpawnPoints = new();
    private static ulong staticIDCounter = 1;

    public static Transform3D GetPlayerSpawnTransform()
    {
        return PlayerSpawnPoints[Random.Shared.Next(PlayerSpawnPoints.Count)].GlobalTransform;
    }

    public static Transform3D GetHordeSpawnTransform()
    {
        return HorderSpawnPoints[Random.Shared.Next(HorderSpawnPoints.Count)].GlobalTransform;
    }
    
    public static void ResetMap()
    {
        nodeStaticLevel.Free();
        nodeStaticLevel = null;
        PlayerSpawnPoints.Clear();
        HorderSpawnPoints.Clear();
        ItemSpawnPoints.Clear();
        staticIDCounter = 1;
        nodeStaticLevel = cachedLevel.Instantiate<Node3D>();
        Global.gameState.AddChild(nodeStaticLevel);
        LoadMapMetas();
        LoadMapGameObjects();
    }

    /// <summary>
    /// Loads a Scene from the file system that holds a static level. Basic processing is done to fetch various nodes we expect to see in the level <see cref="LoadMapMetas"/>
    /// </summary>
    /// <param name="scenePath"></param>
    public static void LoadMap(string scenePath)
    {
        Global.ui.SetLoadingScreenDescription("Loading map...");
        Logging.Log($"Loading static level from scene at path: {scenePath}", "GameStateLevel");
        if (nodeStaticLevel != null)
        {
            nodeStaticLevel.QueueFree();
            nodeStaticLevel = null;
        }
        cachedLevel = ResourceLoader.Load<PackedScene>(scenePath);
        nodeStaticLevel = cachedLevel.Instantiate<Node3D>();
        Global.gameState.AddChild(nodeStaticLevel);
        LoadMapMetas();
        LoadMapGameObjects();
    }

    private static void LoadMapGameObjects()
    {
        Global.ui.SetLoadingScreenDescription("Loading map gameObjects...");
        foreach (Node node in Utils.GetChildrenRecursive(nodeStaticLevel, new()))
        {
            if (node is GameObject obj)
            {
                Global.gameState.Local_RegisterExistingObject(obj, staticIDCounter++, Global.gameState.defaultAuth, obj.type);
            }
        }
    }

    /// <summary>
    /// Parse the loaded static level and try to find some useful stuff that may or may not be there.
    /// </summary>
    public static void LoadMapMetas()
    {
        //TODO: Establish a static level meta contract for expected nodes
        Logging.Log($"Attempting to find meta nodes in static level...", "GameStateLevel");
        Node meta = nodeStaticLevel.GetNode("meta");
        if (meta == null)
        {
            Logging.Warn("Static level has no top-level \"meta\" node! Skipping meta node init", "GameStateLevel");
            return;
        }

        if (meta.GetNode("playerSpawns") != null)
        {
            foreach (PlayerMarker3D marker in nodeStaticLevel.GetNode("meta/playerSpawns").GetChildren())
            {
                PlayerSpawnPoints.Add(marker);
            }
            Logging.Log($"Loaded {PlayerSpawnPoints.Count} player spawn points.", "GameStateLevel");
        }
        else
        {
            Logging.Warn("Static level meta has no \"playerSpawns\" node! Skipping player spawn init", "GameStateLevel");
        }

        if (meta.GetNode("hordeSpawns") != null)
        {

            foreach (Marker3D marker in nodeStaticLevel.GetNode("meta/hordeSpawns").GetChildren())
            {
                HorderSpawnPoints.Add(marker);
            }
            Logging.Log($"Loaded {HorderSpawnPoints.Count} horde spawn points.", "GameStateLevel");
        }
        else
        {
            Logging.Warn("Static level meta has no \"playerSpawns\" node! Skipping player spawn init", "GameStateLevel");
        }

        if (meta.GetNode("itemSpawns") != null)
        {
            Node itemSpawns = meta.GetNode("itemSpawns");
            CollectItemMarkers(itemSpawns, ItemSpawnPoints);
            Logging.Log($"Loaded {ItemSpawnPoints.Count} item spawn points.", "GameStateLevel");
        }
        else
        {
            Logging.Warn("Static level meta has no \"itemSpawns\" node! Skipping item spawn init", "GameStateLevel");
        }

    }

    private static void CollectItemMarkers(Node parent, List<ItemMarker3D> list)
    {

        foreach (Node node in Utils.GetChildrenRecursive(parent, new()))
        {
            if (node is ItemMarker3D marker)
            {
                list.Add(marker);
            }
        }
    }
}

