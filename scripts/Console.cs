using Godot;
using Limbo.Console.Sharp;
using NetworkMessages;
using Steamworks;

/// <summary>
/// Using (MIT licensed) LimboConsole (https://github.com/limbonaut/limbo_console) to provide a simple console interface. 
/// LimboConsole config file at addons/limbo_console
/// May replace this with custom version at some point
/// </summary>
public partial class Console : Node
{
    public override void _Ready()
    {

        LimboConsole.SetEvalBaseInstance(this);

        //Register functions as commands 
        LimboConsole.RegisterCommand(new Callable(this, MethodName.TestConsole1), "TestConsole1", "Runs a simple function to test the console.");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.TestConsole2), "TestConsole2", "Runs a test with optional (string) arg");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.TestConsole3), "TestConsole3", "Runs a test with one mandatory (int) args");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.TestConsole4), "TestConsole4", "Runs a test with two mandatory (string) args");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.status), "status", "Prints the current game status.");

    }
    public void status()
    {
        LimboConsole.Info("Game Status");
        LimboConsole.Info($"  Game Version: {Global.VERSION}");
        LimboConsole.Info($"  Connected to Steam: {Global.bIsSteamConnected}");
        LimboConsole.Info($"  Status of connection to Steam Relay Network: {Global.network.GetSteamNetworkStatus().ToString()}");
        LimboConsole.Info($"  SteamID: {Global.steamid}");


    }



    public void TestConsole1()
    {
        LimboConsole.Info("Info is a standard line.");
        LimboConsole.PrintBoxed("boxed is fun");
        LimboConsole.PrintLine("This prints to Godot output too.", true);
        LimboConsole.PrintLine("This is the same as Info.", false);
        LimboConsole.Error("This is Error.");
        LimboConsole.Warn("This is Warn");
    }

    public void TestConsole2(string optionalArgument)
    {
        LimboConsole.Info("Singular nullable arguments are considered optional.");
        LimboConsole.Info($"Optional Argument: {optionalArgument}");
    }


    public void TestConsole3(int mandatoryArgument)
    {
        LimboConsole.Info("non-nullable (int) parameters are considered mandatory");
        LimboConsole.Info($"Mandatory Argument: {mandatoryArgument}");
    }

    public void TestConsole4(string optionalArgument1, string optionalArgument2)
    {
        LimboConsole.Info("multiple of any argument makes them all required");
        LimboConsole.Info($"Optional Argument: {optionalArgument1} and {optionalArgument2}");

    }

}
