public interface Controller
{
    
    public void Possess(Character character);
    public void Possess(ulong UID);

    public void PossessLocal(ulong UID);

    public Character GetPossessed();

    public int GetTeam();
    public void ChangeTeam(int teamNumber);

    public ulong GetUniqueID();

    public bool IsHuman();
    public bool IsAI();

    public void PerFrame(double delta);

    public void Tick(double delta);

    
}



