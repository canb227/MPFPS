using Godot;
using System;
public class PlayerRoundStats
{
    public int RobotKills = 0;
    public void NewRound()
    {
        RobotKills = 0;
    }
}