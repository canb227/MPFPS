using Godot;
using System;
using System.Collections.Generic;


public static class MapManager
{
    private static Node3D nodeStaticLevel;
    private static List<Marker3D> PlayerSpawnPoints = new();
    private static ulong staticIDCounter = 1;

    public static Transform3D GetPlayerSpawnTransform()
    {
        return PlayerSpawnPoints[Random.Shared.Next(PlayerSpawnPoints.Count)].GlobalTransform;
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
        nodeStaticLevel = ResourceLoader.Load<PackedScene>(scenePath).Instantiate<Node3D>();
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

            foreach (Marker3D marker in nodeStaticLevel.GetNode("meta/playerSpawns").GetChildren())
            {
                PlayerSpawnPoints.Add(marker);
            }
            Logging.Log($"Loaded {PlayerSpawnPoints.Count} player spawn points.", "GameStateLevel");
        }
        else
        {
            Logging.Warn("Static level meta has no \"playerSpawns\" node! Skipping player spawn init", "GameStateLevel");
        }

    }

}

