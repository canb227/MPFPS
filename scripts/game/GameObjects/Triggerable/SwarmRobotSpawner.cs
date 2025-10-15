using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class SwarmRobotSpawner : GOBaseStaticTriggerable
{
    public override void ActivateTriggerEffects(string triggerName, ulong byID)
    {
        GameObjectConstructorData data = new(GameObjectType.SwarmRobot);
        data.paramList.Add(Global.instance.GetPathTo(Global.gameState.GameObjects[byID] as Node3D).ToString());
        data.paramList.Add(SwarmRobotState.SIMPLECHASE);
        Vector3 spawnPosition = GlobalPosition;
        spawnPosition.Y += 5;
        data.spawnTransform = Transform3D.Identity;
        data.spawnTransform.Origin = spawnPosition;
        Global.gameState.Auth_SpawnObject(GameObjectType.SwarmRobot, data);
    }
}

