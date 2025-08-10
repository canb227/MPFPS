using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class EntityLoader
{

    private Dictionary<string, string> entityNames = new();

    public EntityLoader() 
    {
        InitEntityNames();
    }

    private void InitEntityNames()
    {
        entityNames.Add("player", "res://scenes/characters/player.tscn");



    }

    public bool getEntityPathFromName(string name, out string path)
    {
        return entityNames.TryGetValue(name, out path);
    }
}

