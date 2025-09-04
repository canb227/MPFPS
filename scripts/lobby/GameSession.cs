using GameMessages;
using Godot;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// GameSession is an abstraction of the actual game we're going to play. Upstream of GameSession is the Lobby, which provides the lobbyPeers and alerts us when our peer list changes. 
/// Provides sessionOptions and the playerData list to downstream consumers, along with a slew of helpful events.
/// </summary>
public class GameSession
{
    /// <summary>
    /// stores the list of players in this session alongside a huge heap of important info about them. 
    /// This list includes any player that has ever been in the session even if they arent currently (unless explicitly forgotten) to support rejoins and game loading.
    /// Sorted so that everyone's list is in the same order (TESTING THIS BIT STILL).
    /// </summary>
    public SortedDictionary<ulong, PlayerData> playerData = new();

    /// <summary>
    /// stores per-session settings like what map to load, difficulty, etc. Really any setting you'd think belongs in the pre-game settings menu the host can use.
    /// These options could change mid-game, but probably shouldn't without much hubub and warnings and such.
    /// </summary>
    public SessionOptions sessionOptions = new();

    /// <summary>
    /// The steamID of the user who is allowed to make changes to the sessionOptions, manage player lists, and other tasks.
    /// By default this is the lobby host.
    /// </summary>
    public ulong sessionAuthority;

    /// <summary>
    /// Fires whenever sessionOptions is changed - either locally or by a remote user
    /// </summary>
    public delegate void GameSessionOptionsChanged();
    public static event GameSessionOptionsChanged GameSessionOptionsChangedEvent;

    /// <summary>
    /// Fires when a new player is added to the session. This is typically at the same time a new peer joins the lobby.
    /// </summary>
    /// <param name="newPlayerSteamID"></param>
    public delegate void GameSessionNewPlayer(ulong newPlayerSteamID);
    public static event GameSessionNewPlayer GameSessionNewPlayerEvent;

    /// <summary>
    /// Fires when a player is removed from the session, for any reason (quit, crash, kick). They remain in the playerData dict. This is typically at the same time a peer leaves the lobby.
    /// </summary>
    /// <param name="removedPlayerSteamID"></param>
    public delegate void GameSessionPlayerRemoved(ulong removedPlayerSteamID);
    public static event GameSessionPlayerRemoved GameSessionPlayerRemovedEvent;

    /// <summary>
    /// Fires when a player reconnects, i.e. a player joins that had already joined before
    /// </summary>
    /// <param name="playerSteamID"></param>
    public delegate void GameSessionPlayerReconnect(ulong playerSteamID);
    public static event GameSessionPlayerReconnect GameSessionPlayerReconnectEvent;

    /// <summary>
    /// Fires when we get option assignments from the host.
    /// </summary>
    public delegate void GameSessionOptionsAssigned();
    public static event GameSessionOptionsAssigned GameSessionOptionsAssignedEvent;


    /// <summary>
    /// Tracks the number of player's we're waiting for to finish loading
    /// </summary>
    private int numberPlayersStillLoading;




    /// <summary>
    /// Creates a new GameSession that listens for lobby events, and pre-populate it with the given list of peers.
    /// It is expected that this list of peers matches the Lobby peers list.
    /// </summary>
    /// <param name="peers"></param>
    public GameSession(List<ulong> peers, ulong hostID)
    {
        if (Global.Lobby.bInLobby == false)
        {
            Logging.Error("GameSession can only be created if you are in a lobby.", "GameSession");
            throw new Exception("GameSession can only be created if you are in a lobby.");
        }
        Lobby.NewLobbyPeerAddedEvent += OnNewLobbyPeerAdded;
        Lobby.LobbyPeerRemovedEvent += OnLobbyPeerRemoved;
        Lobby.LeftLobbyEvent += OnLeftLobby;
        sessionAuthority = hostID;
        Global.world.defaultAuthority = sessionAuthority;
        foreach (ulong peerID in peers)
        {
            AddToSession(peerID);
        }

        Logging.Log($"Started a new GameSession with {playerData.Count} players, and session authority: {sessionAuthority}", "GameSession");

        if (playerData[sessionAuthority].state == PlayerState.INGAME_OK)
        {
            Logging.Log($"The game host is already in-game, are you trying to reconnect?", "GameSession");
            throw new NotImplementedException("Mid game join dun work");
        }
    }

    /////////////////////////////////////// Lobby Event Handlers //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void OnLeftLobby()
    {
        Logging.Warn($"Left lobby, sesssion must end?", "GameSession");
        EndSession();
    }

    public void OnLobbyPeerRemoved(ulong removedPlayerSteamID)
    {
        Logging.Warn($"Peer left lobby, flagging them as removed.", "GameSession");
        RemovePlayer(removedPlayerSteamID);
    }

    public void OnNewLobbyPeerAdded(ulong newPlayerSteamID)
    {
        if (playerData.ContainsKey(newPlayerSteamID))
        {
            Logging.Warn($"A previously encountered player has rejoined the lobby: {newPlayerSteamID}, attempting to handle re-adding them...", "GameSession");
            AttemptSessionResumption(newPlayerSteamID);
        }
        else
        {
            Logging.Log($"A new player has been added to the lobby: {newPlayerSteamID}, adding them to the session.", "GameSession");
            AddToSession(newPlayerSteamID);
        }
    }

    /////////////////////////////////////// Player Management Functions ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Flags a player as removed, but keep their info around in case we need it for session resumption or other stuff
    /// </summary>
    /// <param name="playerSteamID"></param>
    public void RemovePlayer(ulong playerSteamID)
    {
        if (playerData.ContainsKey(playerSteamID))
        {
            Logging.Log($"Removing player {playerSteamID} from the current GameSession (but keeping their data).", "GameSession");
            playerData[playerSteamID].removed = true;
            GameSessionPlayerRemovedEvent?.Invoke(playerSteamID);
        }
        else
        {
            Logging.Error($"Player {playerSteamID} cannot be removed from session, it is not in the session.", "GameSession");
        }
    }

    /// <summary>
    /// Fully deletes a player, deleting all of our stored info about them. If this player rejoins, they will be treated as a brand new player.
    /// </summary>
    /// <param name="playerSteamID"></param>
    public void ForgetPlayer(ulong playerSteamID)
    {
        Logging.Log($"Forgetting player {playerSteamID}.", "GameSession");
        if (playerData.ContainsKey(playerSteamID))
        {
            playerData.Remove(playerSteamID);
        }
    }

    /// <summary>
    /// Ends and closes the session, forgetting all data about all players and reseting all session options.
    /// </summary>
    public void EndSession()
    {
        playerData = new();
        sessionOptions = new();
        sessionAuthority = new();
        Global.GameSession = null;

        Lobby.NewLobbyPeerAddedEvent -= OnNewLobbyPeerAdded;
        Lobby.LobbyPeerRemovedEvent -= OnLobbyPeerRemoved;
        Lobby.LeftLobbyEvent -= OnLeftLobby;

        //TODO: Do we need to do any other session cleanup here? Saving player progression, other stuff?
    }

    /// <summary>
    /// Attempt to restablish a correct session state with a player that has joined before, left, and is now rejoining.
    /// </summary>
    /// <param name="newPlayerSteamID"></param>
    public void AttemptSessionResumption(ulong newPlayerSteamID)
    {
        //TODO: Session resumption logic
        Logging.Error($"Session Resumption is not implemented yet. Just don't get disconnected lul.", "GameSession");
        //GameSessionPlayerReconnectEvent?.Invoke(newPlayerSteamID);
        return;
    }

    /// <summary>
    /// Add a brand new player with the given steamID to our gamesession, create a new PlayerData object for them, and push relevant events. This user must be in our Lobby.
    /// </summary>
    /// <param name="newPlayerSteamID"></param>
    public void AddToSession(ulong newPlayerSteamID)
    {
        Logging.Log($"Adding player {newPlayerSteamID} to gameSession.", "GameSession");
        if (!Global.Lobby.lobbyPeers.Contains(newPlayerSteamID))
        {
            Logging.Error($"This player is not in our lobby and cannot be added to the session. Something has gone very wrong.", "GameSession");
            return;
        }

        if (playerData.ContainsKey(newPlayerSteamID))
        {
            Logging.Warn($"This player is already in our session. Something has gone wrong.", "GameSession");
        }

        PlayerData pd = new();
        pd.steamID = newPlayerSteamID;
        pd.options = new PlayerOptions();
        pd.state = PlayerState.PREGAME_WAITINGFORINFO;
        pd.playerController = new(newPlayerSteamID);
        Global.world.AddChild(pd.playerController);

        if (!NetworkUtils.IsMe(newPlayerSteamID))
        {
            //Ask the new player for their information so we can cache it.
            Logging.Log($"Player isnt me, requesting data.", "GameSession");
            SendSessionMessage([0], SessionMessageType.REQUEST_PROGRESSION, newPlayerSteamID);
            SendSessionMessage([0], SessionMessageType.REQUEST_CONFIG, newPlayerSteamID);

            //TODO: Investigate potential race conditions involving PlayerOptions.
            //The new joiner asks the hosts for default PlayerOptions values, but we ask the new joiner for them.
            //If this request gets to them before the host's response (if our ping/connection is signifigantly faster to the peer than the peer's connection to the host), things might explode.
            SendSessionMessage([0], SessionMessageType.REQUEST_PLAYEROPTIONS, newPlayerSteamID);
        }
        else
        {
            Logging.Log($"Player is me, loading local data", "GameSession");
            pd.progression = Global.Config.loadedPlayerProgression;
            pd.config = Global.Config.loadedPlayerConfig;
            if (Global.steamid == sessionAuthority)
            {
                pd.options = GenerateOptions(Global.steamid);
                pd.state = PlayerState.PREGAME_OK;
            }
            else
            {
                //TODO: Investigate potential race conditions involving PlayerOptions.
                //We ask the host for our PlayerOptions, but might get a request for the results before we get them from the host under some network conditions.
                SendSessionMessage([0], SessionMessageType.REQUEST_ASSIGNPLAYEROPTIONS, sessionAuthority);
            }
        }
        playerData[newPlayerSteamID] = pd;
        GameSessionNewPlayerEvent?.Invoke(newPlayerSteamID);
    }

    /////////////////////////////////////// Network Functions ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Packs the data byte array into the correct format for a Session Message, which takes a one byte type flag and sets it as the first byte of the message payload.
    /// Then sends the message to the designated user.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="type"></param>
    /// <param name="toSteamID"></param>
    /// <returns></returns>
    public EResult SendSessionMessage(byte[] data, SessionMessageType type, ulong toSteamID)
    {
        Logging.Log($"Sending Session Message with type {type.ToString()} to {toSteamID} | payload length:{data.Length}", "GameSessionWire");
        byte[] newData = new byte[data.Length + 1];
        newData[0] = (byte)type;
        data.CopyTo(newData, 1);
        SteamNetworkingIdentity identity = new SteamNetworkingIdentity();
        identity.SetSteamID64(toSteamID);
        return Global.network.SendData(newData, NetType.SESSION_BYTES, identity);
    }

    /// <summary>
    /// Helper function that sends a Session message to all peers.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public List<EResult> BroadcastSessionMessage(byte[] data, SessionMessageType type)
    {
        Logging.Log($"Broadcasting Session Message with type {type.ToString()} to all {Global.Lobby.lobbyPeers.Count} players in our lobby...", "GameSessionWire");
        List<EResult> retval = new List<EResult>();
        foreach (ulong steamID in Global.Lobby.lobbyPeers)
        {
            retval.Add(SendSessionMessage(data, type, steamID));
        }
        return retval;
    }

    /// <summary>
    /// Core handler for incoming SESSION typed network messages. Works just like the Lobby one, strips off the first byte to use as a routing flag
    /// </summary>
    /// <param name="payload"></param>
    /// <param name="fromSteamID"></param>
    /// <exception cref="ArgumentException"></exception>
    public void HandleSessionMessageBytes(byte[] payload, ulong fromSteamID)
    {
        Logging.Log($"Session Message from {fromSteamID} has type {((SessionMessageType)payload[0]).ToString()}", "GameSessionWire");
        SessionMessageType type = (SessionMessageType)payload[0];
        byte[] data = payload.Skip(1).ToArray();
        switch (type)
        {

            //Request Message Handlers
            case SessionMessageType.REQUEST_CONFIG:
                byte[] config_data = NetworkUtils.StructToBytes(playerData[Global.steamid].config);
                SendSessionMessage(config_data, SessionMessageType.RESPONSE_CONFIG, fromSteamID);
                break;
            case SessionMessageType.REQUEST_PROGRESSION:
                byte[] prg_data = NetworkUtils.StructToBytes(playerData[Global.steamid].progression);
                SendSessionMessage(prg_data, SessionMessageType.RESPONSE_PROGRESSION, fromSteamID);
                break;
            case SessionMessageType.REQUEST_PLAYEROPTIONS:
                byte[] popt_data = NetworkUtils.StructToBytes(playerData[Global.steamid].options);
                SendSessionMessage(popt_data, SessionMessageType.RESPONSE_PLAYEROPTIONS, fromSteamID);
                break;
            case SessionMessageType.REQUEST_PLAYERSTATE:
                byte[] state_data = new byte[1] { (byte)(playerData[Global.steamid].state) };
                SendSessionMessage(state_data, SessionMessageType.RESPONSE_PLAYERSTATE, fromSteamID);
                break;
            case SessionMessageType.REQUEST_SESSIONOPTIONS:
                byte[] sopt_data = NetworkUtils.StructToBytes(sessionOptions);
                SendSessionMessage(sopt_data, SessionMessageType.RESPONSE_SESSIONOPTIONS, fromSteamID);
                break;
            case SessionMessageType.REQUEST_ASSIGNPLAYEROPTIONS:
                if (Global.steamid == sessionAuthority)
                {
                    byte[] apopt_data = NetworkUtils.StructToBytes(GenerateOptions(fromSteamID));
                    SendSessionMessage(apopt_data, SessionMessageType.RESPONSE_ASSIGNPLAYEROPTIONS, fromSteamID);
                    break;
                }
                else
                {
                    Logging.Warn($"Someone just asked me to assign them options but im not the host, wtf?", "GameSession");
                    break;
                }

            //Response Message Handlers
            case SessionMessageType.RESPONSE_CONFIG:
                PlayerConfig cfg = NetworkUtils.BytesToStruct<PlayerConfig>(data);
                playerData[fromSteamID].config = cfg;
                break;
            case SessionMessageType.RESPONSE_PROGRESSION:
                PlayerProgression prg = NetworkUtils.BytesToStruct<PlayerProgression>(data);
                playerData[fromSteamID].progression = prg;
                break;
            case SessionMessageType.RESPONSE_PLAYEROPTIONS:
                PlayerOptions popt = NetworkUtils.BytesToStruct<PlayerOptions>(data);
                playerData[fromSteamID].options = popt;
                playerData[fromSteamID].state = PlayerState.PREGAME_OK;
                break;
            case SessionMessageType.RESPONSE_PLAYERSTATE:
                PlayerState state = (PlayerState)data[0];
                Logging.Log($"Player {fromSteamID} just sent us their new state: {state}", "GameSession");
                playerData[fromSteamID].state = state;
                HandlePlayerState(state, fromSteamID);
                break;
            case SessionMessageType.RESPONSE_SESSIONOPTIONS:
                SessionOptions sopt = NetworkUtils.BytesToStruct<SessionOptions>(data);
                sessionOptions = sopt;
                GameSessionOptionsChangedEvent?.Invoke();
                break;
            case SessionMessageType.RESPONSE_ASSIGNPLAYEROPTIONS:
                PlayerOptions apopt = NetworkUtils.BytesToStruct<PlayerOptions>(data);
                playerData[Global.steamid].options = apopt;
                playerData[Global.steamid].state = PlayerState.PREGAME_OK;
                break;

            //Command Message Handlers
            case SessionMessageType.COMMAND_STARTGAME:
                if (fromSteamID == sessionAuthority)
                {
                    numberPlayersStillLoading = playerData.Count;
                    playerData[Global.steamid].state = PlayerState.PREGAME_LOADING;
                    Logging.Log($"Host has just commanded us to start the game, starting loading and informing peers", "GameSession");
                    byte[] new_state_data = new byte[1];
                    new_state_data[0] = (byte)(playerData[Global.steamid].state);
                    BroadcastSessionMessage(new_state_data, SessionMessageType.RESPONSE_PLAYERSTATE);

                    StartLoadingGame();

                    playerData[Global.steamid].state = PlayerState.PREGAME_DONELOADING;
                    Logging.Log($"We're done loading! Informing peers and waiting for others to finish.", "GameSession");
                    byte[] new2_state_data = new byte[1];
                    new2_state_data[0] = (byte)(playerData[Global.steamid].state);
                    BroadcastSessionMessage(new2_state_data, SessionMessageType.RESPONSE_PLAYERSTATE);


                }
                else
                {
                    Logging.Warn($"Non-host just commanded us to start. wtf?", "GameSession");
                }
                break;

            default:
                throw new ArgumentException($"Malformed Session Message | First Byte: {((int)payload[0])} Cast As SessionMessageType:{((SessionMessageType)payload[0]).ToString()}");
        }
    }

    private void StartLoadingGame()
    {
        Global.ui.StartLoadingScreen();
        Global.ui.SetLoadingScreenDescription("Loading game world...");
        Global.world.LoadWorld();
        Global.ui.StopLoadingScreen();
        Global.ui.PregameWaitingForPlayers();
    }

    /// <summary>
    /// The host uses this function to pick PlayerOptions for a newly joined player. The idea is that some PlayerOptions are best assigned by the host.
    /// Think assigning everyone a unique color, or assigning teams, or other stuff.
    /// </summary>
    /// <param name="fromSteamID"></param>
    /// <returns></returns>
    private PlayerOptions GenerateOptions(ulong fromSteamID)
    {
        return new();
    }

    /// <summary>
    /// When we get a PlayerState message in <see cref="HandleSessionMessageBytes(byte[], ulong)"/> it can trigger other behavior. Split that handling off into a seperate function just for organizational purposes.
    /// </summary>
    /// <param name="state"></param>
    /// <param name="fromSteamID"></param>
    /// <exception cref="ArgumentException"></exception>
    private void HandlePlayerState(PlayerState state, ulong fromSteamID)
    {
        switch (state)
        {
            case PlayerState.PREGAME_WAITINGFORINFO:
                break;
            case PlayerState.PREGAME_OK:
                break;
            case PlayerState.PREGAME_LOADING:
                break;
            case PlayerState.PREGAME_DONELOADING:
                numberPlayersStillLoading--;
                Logging.Log($"Player {fromSteamID} is done loading. That leaves {numberPlayersStillLoading} players still left to finish.", "GameSession");
                if (numberPlayersStillLoading == 0)
                {
                    Logging.Log("All players are done loading. Starting game and informing peers I'm starting game.", "GameSession");
                    Global.world.InGameStart();
                    Global.ui.InGameStart();
                    playerData[Global.steamid].state = PlayerState.INGAME_OK;
                    BroadcastSessionMessage([(byte)(playerData[Global.steamid].state)], SessionMessageType.RESPONSE_PLAYERSTATE);
                }
                break;
            case PlayerState.INGAME_OK:
                break;
            case PlayerState.INGAME_LOADING:
                break;
            case PlayerState.INGAME_DONELOADING:
                break;
            default:
                throw new ArgumentException($"Unrecognized state enum value in Session PlayerState Message | State enum value is:{state.ToString()}");
        }
    }
}

/// <summary>
/// Class to hold all the data we store about each player. It's a class instead of a struct to remind me that I shouldn't be shoving this entire across the wire.
/// </summary>
public class PlayerData
{
    public ulong steamID; //steamID
    public PlayerProgression progression; //meta progression
    public PlayerConfig config; //player config settings
    public PlayerOptions options; //gamesession specific per-player data
    public PlayerState state; //status of this player
    public PlayerController playerController; //hook into local world state
    public bool removed = false; // if true, this player is not in the gamesession at the moment
}

/// <summary>
/// Simple enum to decribe player's state.
/// </summary>
public enum PlayerState : byte
{
    NONE = 0,

    PREGAME_WAITINGFORINFO = 1,
    PREGAME_OK = 2,
    PREGAME_LOADING = 3,
    PREGAME_DONELOADING = 4,

    INGAME_OK = 11,
    INGAME_LOADING = 12,
    INGAME_DONELOADING = 13,
}

/// <summary>
/// Struct that holds all possible per-player options for the session. This can be anything from color choices, team number, maybe class or loadout selections, anything!
/// (Some conditions apply, fields must be basic type or struct of basic types.)
/// </summary>
public struct PlayerOptions
{

    Color chosenColor { get; set; }

    public PlayerOptions()
    {
        chosenColor = Colors.Blue;
    }
}

/// <summary>
/// Struct that holds all possible per-session options. Normally only changable by the session authority.
/// Think difficulty settings, map selection, game mode settings, that stuff.
/// </summary>
public struct SessionOptions
{
    public int DEBUG_DIRECTLOADMAPINDEX;
    public bool DEBUG_DIRECTLOADMAP;
}

/// <summary>
/// one byte enum value to store session message type information
/// </summary>
public enum SessionMessageType
{
    ERROR = 0,

    REQUEST_CONFIG = 1,
    REQUEST_PROGRESSION = 2,
    REQUEST_PLAYEROPTIONS = 3,
    REQUEST_PLAYERSTATE = 4,
    REQUEST_SESSIONOPTIONS = 5,
    REQUEST_ASSIGNPLAYEROPTIONS = 6,

    RESPONSE_CONFIG = 11,
    RESPONSE_PROGRESSION = 12,
    RESPONSE_PLAYEROPTIONS = 13,
    RESPONSE_PLAYERSTATE = 14,
    RESPONSE_SESSIONOPTIONS = 15,
    RESPONSE_ASSIGNPLAYEROPTIONS = 16,

    COMMAND_STARTGAME = 21,
}