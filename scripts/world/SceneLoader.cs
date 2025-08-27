using System.Collections.Generic;


public class SceneLoader
{
    private Dictionary<string, string> sceneNames = new();
    public SceneLoader()
    {
        InitSceneNames();
    }

    private void InitSceneNames()
    {
        sceneNames.Add("debugplatform", "res://scenes/world/debugPlatform.tscn");


    }

    public bool getScenePathFromName(string name, out string path)
    {
        return sceneNames.TryGetValue(name, out path);
    }


}

