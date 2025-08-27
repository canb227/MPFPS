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



