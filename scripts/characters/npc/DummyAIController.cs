using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class DummyAIController : AIController
{
    public ulong UID;

    public DummyAIController(ulong UID) 
    {
        this.UID = UID;
        Global.world.controllers.Add(UID,this);
    
    }


    public void ChangeTeam(int teamNumber)
    {
        throw new NotImplementedException();
    }

    public Character GetPossessed()
    {
        throw new NotImplementedException();
    }

    public int GetTeam()
    {
        throw new NotImplementedException();
    }

    public ulong GetUniqueID()
    {
        return UID;
    }

    public bool IsAI()
    {
        return true;
    }

    public bool IsHuman()
    {
        return false;
    }

    public void PerFrame(double delta)
    {
        throw new NotImplementedException();
    }

    public void Possess(Character character)
    {
        throw new NotImplementedException();
    }

    public void Possess(ulong eid)
    {
        throw new NotImplementedException();
    }

    public void PossessLocal(ulong UID)
    {
        throw new NotImplementedException();
    }

    public void Tick(double delta)
    {
        throw new NotImplementedException();
    }
}

