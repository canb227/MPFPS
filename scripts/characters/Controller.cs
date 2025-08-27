using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public interface Controller
{

    public void Possess(Character character);
    public void Possess(ulong eid);

    public Character GetPossessed();

    public int GetTeam();
    public void ChangeTeam(int teamNumber);

    public bool IsHuman();
    public bool IsAI();
}



