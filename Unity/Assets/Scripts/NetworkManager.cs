using UnityEngine;
using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using Lidgren.Network;

enum PacketTypes
{
    Beat,
    AssignId,
    Ready,
    UpdateName,
	KeepAlive,
    PlayIntro,
    StartGame,
    AddPlayer,
    RemovePlayer,
    PlayerAction,
    PlayerSpecial,
    HurtTarget,
    EnemyHealth,
    SelfHit,
    PlayerHit,
    EnemyPhaseChange,
    EnemyAction,
    EnemyTargetPosition,
    EnemyStartTargeting,
    EnemyEndTargeting,
    Message,
    SettingsChange,
    PlayerCount,
    Disconnect,
    LobbyMessage,
    EnemySync,
	MessageDebug
}

enum GameState
{
    Lobby,
    Intro,
    InGame
}

public class PlayerObject
{
    public int Id;
    public float xVelocity = 0;
    public float yVelocity = 0;
    public GameObject Obj;
    public string Name = "";
    public PlayerController Controller;

    public int dmgnormal = 0;
    public int dmgweakpoint = 0;
    public int hittaken = 0;

    public PlayerObject(int id, GameObject obj, string name)
    {
        Id = id;
        Obj = obj;
        Name = name;
        Controller = Obj.GetComponent<PlayerController>();
		Controller.SetupPlayer(id, id == NetworkManager.Instance.ClientId ? true : false);
    }
}

/*
 * Created by TomoPrime
 * 
 * Added update for Sequence Channels
 * 
 * PacketTypes of:
 * 0 - Message, LobbyMessage, MessageDebug are set to 0
 * 1 - Mostly Server connect types are like: AssignID, AddPlayer, RemovePlayer, StartGame, Disconnect
 * 2 - Player related types are like: PlayerHit, PlayerSpecial, PlayerAction
 * 3 - Enemy types are like: EnemySync, EnemyHealth, EnemyAction
 * 4 - Beat aka heartbeat probably will go away later on replaced by KeepAlive
 * 5 - Mostly Game settings like: SettingsChange, PlayerIntro, Ready, UpdateName
 * 6 - PlayerCount used to keep track of new joiners and leavers
 */

public sealed class NetworkManager : MonoBehaviour {
	
	private static volatile NetworkManager _instance;
	private static object _lock = new object();
	static NetworkManager () {}
    //public static GameObject Container;

    private static NetClient client;
    private NetIncomingMessage inc;

    float roundtriptime = 0f;

    public int ClientId = -1;
    public PlayerObject Player;
    public List<PlayerObject> Players;
    public GameObject Enemy;
    public CrabManager EnemyManager;

    public int difficulty = 1;
    public int healthmod = 1;

    CrabBattleServer.CrabBehavior cb;

    Rect windowrect;
    Rect lobbyrect;
	Rect button;

    GameManager gm;

    string hostIp;
    string username = "";
    string newname = "";

    string curtyping = "";

    List<string> console;
    List<string> lobby;

    bool isConnected = false;
    
    //float lastBeat = 0f;
	float lastSec = 0f;
    internal int numPlayers = 0;

    public float starttime = 0;
    public float endtime = 0;
	
	private NetworkManager() {}
	
	public static NetworkManager Instance
	{
        get
        {
            if (_instance == null)
            {
                lock(_lock)
                {
                    if (_instance == null)
					{
						GameObject Container = new GameObject();
            			Container.name = "NetworkManager";
            			_instance = Container.AddComponent(typeof(NetworkManager)) as NetworkManager;
					}
                }
            }
            return _instance;
        }
		
    }
	 
	void Awake ()
	{
		//DontDestroyOnLoad(this);
		/*
		if (_instance)
		{
			_instance.Shutdown();
		}
		_instance = NetworkManager.Instance;
		*/
	}
	
	void OnDestroy()
    {
		_instance.Shutdown();
    }
	
	void Shutdown ()
	{
		if (!gm.isSoloPlay)
        {
            NetOutgoingMessage outmsg = client.CreateMessage();
            outmsg.Write((byte)PacketTypes.Disconnect);
            client.SendMessage(outmsg, NetDeliveryMethod.ReliableOrdered, 1);
			client.Shutdown(username+": Bye All");
			print("Id"+ClientId+" Closing client connection...");
        }
	}
	
	// Use this for initialization
	void Start () 
    {
        Debug.Log("Starting Network Manager");
		button = new Rect(Screen.width - 130, Screen.height -50, 120, 40);
        //Debug.Log(CryptoHelper.GetMd5String("Hello World!"));

        gm = GameManager.GetInstance();
		newname = username = gm.username;
        hostIp = gm.ipAddress;

        console = new List<string>();
        lobby = new List<string>();

        Players = new List<PlayerObject>();
		
        if (!gm.isSoloPlay)
        {
            NetPeerConfiguration config = new NetPeerConfiguration("crab_battle");

            client = new NetClient(config);

            NetOutgoingMessage outmsg = new NetOutgoingMessage();

            client.Start();
            outmsg.Write("A Client");
			client.Connect(hostIp, gm.gamePort, outmsg);

            AddConsoleMessage("Waiting for connection to server...");
        }
        else
        {
            isConnected = true; // In solo mode, we're always connected!
            //username = PlayerPrefs.GetString("Username", "Player");
            //newname = username;
            gm.isReady = true;
            ClientId = 1;
        }

        windowrect = new Rect(Screen.width / 2f - 100f, Screen.height / 2f - 150f, 200f, 310f);
        lobbyrect = new Rect(Screen.width / 2f - 200f, Screen.height - 100f, 400f, 100f);
        
        Enemy = GameObject.Instantiate(Resources.Load("battlecrab"), Vector3.zero, Quaternion.identity) as GameObject;
        Enemy.animation.Play("laying");

        EnemyManager = Enemy.GetComponent<CrabManager>();
		lastSec=Time.time;
	}

    public void OnGUI()
    {
        if (isConnected)
        {
			if (gm.gamephase == 0 || gm.isShowMenu && EnemyManager.isAlive)
			{
            	windowrect.x = Mathf.Clamp(windowrect.x, 0, Screen.width - windowrect.width);
            	windowrect.y = Mathf.Clamp(windowrect.y, 0, Screen.height - windowrect.height);
            	windowrect = GUI.Window(1, windowrect, ConnectionWindow, "Connection");
			}
			
			if (!gm.isSoloPlay)
			{
            	lobbyrect.x = Mathf.Clamp(lobbyrect.x, 0, Screen.width - lobbyrect.width);
            	lobbyrect.y = Mathf.Clamp(lobbyrect.y, 0, Screen.height - lobbyrect.height);
            	lobbyrect = GUI.Window(2, lobbyrect, LobbyWindow, "Lobby");
			}
        }
		if (!gm.isSoloPlay && gm.gamephase == 2 && !gm.isShowMenu)
		{
			if (EnemyManager.isAlive)
			{
				if (GUI.Button(button, "Change Settings"))
				{
					ToggleReady(false);
					gm.isShowMenu = true;
				}
			}
			else gm.isReady = true;
		}
        if (gm.gamephase == 0 || gm.isSoloPlay || gm.gamephase == 3 || gm.isShowMenu)
        {
			if (GUI.Button(button, "Quit Game"))
			{
				if (!gm.isSoloPlay)
				{
				    NetOutgoingMessage outmsg = client.CreateMessage();
				    outmsg.Write((byte)PacketTypes.Disconnect);
				    client.SendMessage(outmsg, NetDeliveryMethod.ReliableOrdered, 1);
					client.Shutdown(username+": Bye All");
					print("Closing client connection...");
				}
            	Application.LoadLevel("opening");
			}
			
            int top = 5;
            foreach (string msg in console)
            {
                GUI.Label(new Rect(10, top, Screen.width, 25), msg);
                top += 17;
            }
        }
    }
    
    void LobbyWindow(int windowID)
    {
        int top = 15;
        foreach (string msg in lobby)
        {
            GUI.Label(new Rect(10, top, 380, 25), msg);
            top += 13;
        }

        GUI.SetNextControlName("LobbyText");
        curtyping = GUI.TextField(new Rect(10, 75, 380, 20), curtyping);

        if (Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "LobbyText" && curtyping != "")
        {
            SendLobbyText(curtyping);
            curtyping = "";
        }

        GUI.DragWindow();
    }

    void ConnectionWindow(int windowID)
    {
        GUI.Label(new Rect(10, 25, 100, 25), "User Name");
        GUI.SetNextControlName("Namebox");
        newname = GUI.TextField(new Rect(10, 45, 110, 20), newname, 20);
        if (GUI.Button(new Rect(125, 45, 65, 20), "Set"))
            ChangeName(newname);

        GUI.Label(new Rect(10, 70, 90, 25), "Difficulty");
        int newdiff = GUI.SelectionGrid(new Rect(10, 90, 180, 80), difficulty, new string[] { "Easy", "Normal", "Hard", "Lunatic" }, 1);

        GUI.Label(new Rect(10, 175, 90, 25), "Battle Length");
        int newhealth = GUI.SelectionGrid(new Rect(10, 195, 180, 40), healthmod, new string[] { "Short", "Normal", "Long", "Absurd" }, 2);
		
		bool intro = gm.isPlayIntro;
        bool ready = gm.isReady;
		
		if (gm.isShowMenu )//|| Players != null && Players.Count < numPlayers)
		{
			// More like a place holder...
			// The client option to Join is not setup, as the Server will auto start the game for now
			if (Players != null && Players.Count < numPlayers && gm.gamephase !=0)
			{
				gm.gamephase = 2;
				
				if (GUI.Button(new Rect(10, 255, 180, 30), "JOIN"))
				{
					gm.isShowMenu = false;
					//SendStart();
					ToggleReady(true);
				}
				
			}
			else if (GUI.Button(new Rect(10, 255, 180, 30), "RESUME"))
			{
				gm.isShowMenu = false;
				//gm.gamephase = 2;
				ready = true;
				if (newname != gm.username)
					ChangeName(newname);
			}
		}
		else {
			ready = GUI.Toggle(new Rect(20, 240, 80, 20), gm.isReady, "Ready?");
			intro = GUI.Toggle(new Rect(100, 240, 100, 20), gm.isPlayIntro,"Play Intro?");
		
		    if (!ready)
		        GUI.enabled = false;
		
		    if (GUI.Button(new Rect(10, 265, 180, 30), "START"))
				{
					gm.isShowMenu = false;
		        	SendStart();
				}

        	GUI.enabled = true;
		}
		
        if (newdiff != difficulty || newhealth != healthmod)
            ChangeDifficulty(newdiff, newhealth);

        if (ready != gm.isReady)
            ToggleReady(ready);
		
		if (intro != gm.isPlayIntro)
            TogglePlayIntro(intro);

        GUI.DragWindow();

        //If they hit enter while typing their name, send the changes to the server.
        if (Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "Namebox")
            if (newname != gm.username && newname != "")
            {
                ChangeName(newname);
            }
    }

    public void DealEnemyDamage(int damage, bool weakpoint)
    {
        if (EnemyManager.CurrentHealth <= 0)
            return;

        if (!weakpoint)
            Player.dmgnormal += damage;
        else
            Player.dmgweakpoint += damage;

        if (gm.isSoloPlay)
        {
            EnemyManager.CurrentHealth -= damage;
            return;
        }

        NetOutgoingMessage outmsg = new NetOutgoingMessage();
        outmsg.Write((byte)PacketTypes.HurtTarget);
        outmsg.Write((Int16)damage);
        outmsg.Write(weakpoint);
        client.SendMessage(outmsg, NetDeliveryMethod.ReliableUnordered, 3);
    }

    public void SendPlayerSpecial(int SpecialType)
    {
        if (gm.isSoloPlay)
            return;

        NetOutgoingMessage outmsg = new NetOutgoingMessage();
        outmsg.Write((byte)PacketTypes.PlayerSpecial);
        outmsg.Write((Int16)SpecialType);
        client.SendMessage(outmsg, NetDeliveryMethod.ReliableUnordered, 2);
    }

    public void SendPlayerUpdate(int id, float xvel, float yvel, bool firing)
    {
        if (gm.isSoloPlay)
            return;

        PlayerObject player = Players.Find(p => p.Id == id);
		if (player == null) return;
        
		NetOutgoingMessage outmsg = new NetOutgoingMessage();
        outmsg.Write((byte)PacketTypes.PlayerAction);
        outmsg.Write(player.Obj.transform.position.x);
        outmsg.Write(player.Obj.transform.position.z);
        outmsg.Write(xvel);
        outmsg.Write(yvel);
        outmsg.Write(firing);
        client.SendMessage(outmsg, NetDeliveryMethod.ReliableOrdered, 2);
    }
		
	public void SendKeepAlive() 
	{
		if (gm.isSoloPlay) return;
		NetOutgoingMessage outmsg = new NetOutgoingMessage();
		outmsg.Write((byte)PacketTypes.KeepAlive);
		client.SendMessage(outmsg, NetDeliveryMethod.ReliableUnordered, 4);
	}
	
    public void SendLobbyText(string msg)
    {
		if (gm.isSoloPlay) return;
        NetOutgoingMessage outmsg = new NetOutgoingMessage();
        outmsg.Write((byte)PacketTypes.LobbyMessage);
        outmsg.Write(msg);
        client.SendMessage(outmsg, NetDeliveryMethod.ReliableOrdered, 0);
    }

    public void AddLobbyMessage(string message)
    {
        lobby.Add(message);
        if (lobby.Count > 4)
            lobby.Remove(lobby[0]);
    }

    public void AddConsoleMessage(string message)
    {
        console.Add(message);
        if (console.Count > 5)
            console.Remove(console[0]);
    }

    public void ShowScores()
    {
        GameObject scores = new GameObject("ScoresManager");
        scores.AddComponent<Scores>();
    }

    void StartSoloGame()
    {
        Vector3 position = new Vector3(0f, 15f, -400); // -500

        username = newname; //You only need to hit Set when playing multiplayer.

        PlayerPrefs.SetString("Username", username);

        PlayerPrefs.Save();

        GameObject p = GameObject.Instantiate(Resources.Load("player"), position, Quaternion.identity) as GameObject;

        Players.Add(new PlayerObject(1, p, username));
		Player = Players[0];
		
		numPlayers = 1;
        
		cb = new CrabBattleServer.CrabBehavior();

        Debug.Log(username);
		gm.gamephase = 1; //Start game.
    }

    public void SendStart()
    {
        if (gm.isSoloPlay)
        {
            StartSoloGame();
            return;
        }
		
		if (newname != gm.username)
			ChangeName(newname);
		
        NetOutgoingMessage outmsg = new NetOutgoingMessage();
        outmsg.Write((byte)PacketTypes.StartGame);
        client.SendMessage(outmsg, NetDeliveryMethod.ReliableOrdered, 1);
    }

    public void TakeHit()
    {
        Player.hittaken += 1;

        if (gm.isSoloPlay)
            return;
		
        NetOutgoingMessage outmsg = new NetOutgoingMessage();
        outmsg.Write((byte)PacketTypes.PlayerHit);
        client.SendMessage(outmsg, NetDeliveryMethod.ReliableUnordered, 2);
    }

    public void ToggleReady(bool ready)
    {
		// no point in changing ready flag as there is only one player
        if (gm.isSoloPlay)
            return;

		if (gm.isReady != ready)
		{
	        NetOutgoingMessage outmsg = new NetOutgoingMessage();
	        outmsg.Write((byte)PacketTypes.Ready);
	        outmsg.Write(ready);
	        client.SendMessage(outmsg, NetDeliveryMethod.ReliableOrdered, 5);
		}
		
		gm.isReady = ready;
    }
	
	public void TogglePlayIntro(bool intro)
    {
        gm.isPlayIntro = intro;
		
		if (gm.isSoloPlay)
            return;

        NetOutgoingMessage outmsg = new NetOutgoingMessage();
        outmsg.Write((byte)PacketTypes.PlayIntro);
        outmsg.Write(gm.isPlayIntro);
        client.SendMessage(outmsg, NetDeliveryMethod.ReliableOrdered, 5);
    }

    public void ChangeDifficulty(int newdiff, int newhealth)
    {
        if (gm.isSoloPlay)
        {
            difficulty = newdiff;
            healthmod = newhealth;
            return;
        }

        difficulty = newdiff;
        healthmod = newhealth;

        Debug.Log("Setting difficulty to " + difficulty + " and health to " + healthmod);

        NetOutgoingMessage outmsg = new NetOutgoingMessage();
        outmsg.Write((byte)PacketTypes.SettingsChange);
        outmsg.Write((Int16)difficulty);
        outmsg.Write((Int16)healthmod);
        client.SendMessage(outmsg, NetDeliveryMethod.ReliableOrdered, 5);
    }

    public void ChangeName(string name)
    {
        if (gm.isSoloPlay)
            return;

        NetOutgoingMessage outmsg = new NetOutgoingMessage();
        outmsg.Write((byte)PacketTypes.UpdateName);
        outmsg.Write(name);
        client.SendMessage(outmsg, NetDeliveryMethod.ReliableOrdered, 5);
        AddConsoleMessage("Name change request sent to the server.");
		gm.username = name;
    }
	
	// Update is called once per frame
	void Update () 
    {
        if (gm.isSoloPlay)
        {
            if(gm.gamephase == 2)
                cb.GoGoBattleCrab(Time.time, Time.deltaTime);
            return; //Don't do any of the net management stuff if we're solo.
        }

        if (client.Status == NetPeerStatus.Running && (inc = client.ReadMessage()) != null)
        {
            switch (inc.MessageType)
            {
                case NetIncomingMessageType.Data:
                    {
                        switch (inc.ReadByte())
                        {
							case (byte)PacketTypes.StartGame:
                                {
									// Server sent go ahead to auto start
									ToggleReady(true);
									gm.gamephase = 1;
									if (newname != gm.username)
										ChangeName(newname);
									print("Received ok to start game intro");	
								}
								break;
							case (byte)PacketTypes.PlayIntro:
                                {
									gm.isPlayIntro = inc.ReadBoolean();
                                    AddConsoleMessage("isPlayIntro "+gm.isPlayIntro);
									Debug.Log("isPlayIntro "+gm.isPlayIntro);
                                }
                                break;
                            case (byte)PacketTypes.Message:
                                {
                                    AddConsoleMessage(inc.ReadString());
                                }
                                break;
							case (byte)PacketTypes.MessageDebug:
                                {
                                    print(inc.ReadString());
                                }
                                break;
                            case (byte)PacketTypes.LobbyMessage:
                                {
                                    AddLobbyMessage(inc.ReadString());
                                }
                                break;

                            case (byte)PacketTypes.PlayerCount:
                                {
                                    numPlayers = inc.ReadInt16();
                                    AddConsoleMessage("The number of connected users has changed to " + numPlayers + ".");
                                }
                                break;
                            case (byte)PacketTypes.SettingsChange:
                                {
                                    difficulty = inc.ReadInt16();
                                    healthmod = inc.ReadInt16();
                                    Debug.Log("Difficulty: "+difficulty + " Battle Length: " + healthmod);
                                    AddConsoleMessage("Game difficulty: "+difficulty + " Battle Length: " + healthmod);
									EnemyManager.CalculateHealth();
                                }
                                break;
                            case (byte)PacketTypes.AssignId:
                                {
                                    ClientId = inc.ReadInt32();

                                    isConnected = true;
                                    AddConsoleMessage("Server assigned you an id of " + ClientId + ".");
									ChangeName(newname);
                                }
                                break;
                            case (byte)PacketTypes.AddPlayer:
                                {
                                    int playerid = inc.ReadInt16();
                                    float x = inc.ReadFloat();
                                    float y = inc.ReadFloat();
                                    string name = inc.ReadString();
                                    Vector3 position = new Vector3(x, 15, y);
                                    Debug.Log("Adding player " +name+" ("+playerid+") to scene.");
					
									if (Players.Exists(play => play.Id == playerid)) return;
                                    
									GameObject p;
                                    if (playerid == ClientId)
                                    {
                                        //Debug.Log("YOU");
                                        p = GameObject.Instantiate(Resources.Load("player"), position, Quaternion.identity) as GameObject;
                                        Player = new PlayerObject(playerid, p, name);
										Players.Add(Player);
										
										Enemy.animation.Play("laying");
										lastSec=Time.time;
                                    }
                                    else {
										//Debug.Log("THEM");
                                        p = GameObject.Instantiate(Resources.Load("ally"), position, Quaternion.identity) as GameObject;
										Players.Add(new PlayerObject(playerid, p, name));
										}  
                                }
                                break;
							case (byte)PacketTypes.RemovePlayer:
                                {
									int playerid = inc.ReadInt16();
									//if (gm.gamephase == 0) break;
                                   
									PlayerObject player = Players.Find(p => p.Id == playerid);
									if (player == null) break;
									Debug.Log("Player " + player.Name+" ("+playerid+") had disconnected");
									PlayerController pc = player.Obj.GetComponent<PlayerController>();
									bool status = Players.Remove(player);
									Debug.Log("Removing player " + playerid +" "+status);
									Destroy(pc.playername);
									Destroy(player.Obj);
                                }
                                break;
							case (byte)PacketTypes.UpdateName:
                                {
                                    //A Player changed their name. 
                                    string namechange = inc.ReadString();
									int playerid = inc.ReadInt16();
                                    
                                    //SendLobbyMessage("Server", player.Name + " (Id"+player.Id+") changed their name to '" + newname + "'.");
									PlayerObject player = Players.Find(p => p.Id == playerid);
									if (player == null) break;
									Debug.Log(player.Name + " (Id"+playerid+") changed their name to '" + namechange + "'.");
									player.Name = namechange;
									var pn = player.Controller.playername;
									if (pn == null) break;
									pn.guiText.text = player.Name;
                                }
                                break;
                            case (byte)PacketTypes.Beat:
                                {
                                    NetOutgoingMessage outmsg = new NetOutgoingMessage();
                                    outmsg.Write((byte)PacketTypes.Beat);
                                    outmsg.Write(inc.ReadInt16());
                                    if (Player != null)
                                    {
                                        outmsg.Write(Player.Obj.transform.position.x);
                                        outmsg.Write(Player.Obj.transform.position.z);
                                    }
                                    else
                                    {
                                        outmsg.Write(0f);
                                        outmsg.Write(0f);
                                    }
                                    outmsg.Write((float)Enemy.transform.position.x);
                                    outmsg.Write((float)Enemy.transform.position.z);
                                    client.SendMessage(outmsg, NetDeliveryMethod.ReliableOrdered, 4);
                                    roundtriptime = inc.ReadFloat();
                                }
                                break;
                            case (byte)PacketTypes.EnemySync:
                                {
                                    inc.ReadInt16();
                                    EnemyManager.CrabMoveSync(inc.ReadFloat(), inc.ReadFloat(), inc.ReadFloat(), inc.ReadFloat(), inc.ReadBoolean(), inc.ReadFloat() + roundtriptime);
                                }
                                break;
                            case (byte)PacketTypes.PlayerSpecial:
                                {
                                    int playerid = inc.ReadInt16();

                                    PlayerObject player = Players.Find(p => p.Id == playerid);
                                    if (player == null)
                                        break;

                                    int specialType = inc.ReadInt16();

                                    Debug.Log("Got special action request for player " + playerid + " ("+player.Name+")");

                                    if(playerid != ClientId)
                                        player.Controller.UseSpecial(specialType);
                                }
                                break;
                            case (byte)PacketTypes.PlayerAction:
                                {
                                    int playerid = inc.ReadInt16();

                                    PlayerObject player = Players.Find(p => p.Id == playerid);
                                    if (player == null)
                                        break;

                                    float x = inc.ReadFloat();
                                    float y = inc.ReadFloat();

                                    player.xVelocity = inc.ReadFloat();
                                    player.yVelocity = inc.ReadFloat();

                                    bool isShooting = inc.ReadBoolean();

                                    float triptime = inc.ReadFloat() + roundtriptime;
                                    
                                    //Only push updates for remote players...
                                    if(playerid != ClientId)
                                        player.Controller.PushUpdate(x, y, player.xVelocity, player.yVelocity, isShooting, triptime);

                                }
                                break;
                            case (byte)PacketTypes.EnemyAction:
                                {
                                    int actionId = inc.ReadInt16();
                                    float speed = inc.ReadFloat();
                                    int seed = inc.ReadInt16();

                                    EnemyManager.CrabCommand(actionId, speed, seed);
                                }
                                break;
                            case (byte)PacketTypes.EnemyHealth:
                                {
                                    EnemyManager.CurrentHealth = inc.ReadInt16();
                                }
                                break;
                            case (byte)PacketTypes.PlayerHit:
                                {
                                    int playerid = inc.ReadInt16();

                                    if (Player==null||Player.Id == playerid)
                                        break;

                                    PlayerObject player = Players.Find(p => p.Id == playerid);
									if (player!=null)
                                    	GameObject.Instantiate(Resources.Load("Detonator-Insanity"), player.Obj.transform.position, Quaternion.identity);
                                }
                                break;
                            default:
                                inc.Reset();
                                Debug.Log("Unhandled Packet Type Recieved.  Packettype is " + Enum.GetName(typeof(PacketTypes), (int)inc.ReadByte()) + ".");
                                break;
                        }
                    }
                    break;
                default:

                    break;
            }
        }
		
		if (Time.time > lastSec+1) 
		{
			lastSec=Time.time;
			SendKeepAlive();
			
			if (client.ConnectionStatus == NetConnectionStatus.Disconnected)
			{
				AddLobbyMessage("Lost connection to server. Attempting to reconnect...");
				NetOutgoingMessage outmsg = new NetOutgoingMessage();
				outmsg.Write("A Client");
				
				IPAddress addr = (Dns.GetHostEntry(hostIp)).AddressList[0];
				IPEndPoint ip = new IPEndPoint(addr,gm.gamePort);
				NetConnection nc = client.GetConnection(ip);
				
				if (nc !=null)
				{
					isConnected = false;
                	gm.isReady = false;
					client.AcceptConnection(nc);
					AddConsoleMessage("Lost connection to server. Attempting to reconnect...");
					print("KeepAlive: Reconnecting to server...");
				}
				else 
				{
					client.Connect(hostIp, gm.gamePort, outmsg);
					if (gm.gamephase == (int) GameState.InGame)
					{
						gm.isShowMenu = false;
						gm.gamephase = 0;
						ToggleReady(false);
						Application.LoadLevel("mainscene");
					}
					else 
					{
						ToggleReady(false);
						AddConsoleMessage("Attempting to connect to the server...");
					}
				}
			}
		}
		/*
        if ((lastBeat + 10 < Time.time) || (lastBeat + 4 < Time.time && !isConnected))
        {
            if (isConnected)
            {
                AddConsoleMessage("Lost connection to server.  Attempting to reconnect...");
                isConnected = false;
                gm.isReady = false;
            }
            else
			{
                AddConsoleMessage("Attempting to connect to the server...");

		        NetOutgoingMessage outmsg = new NetOutgoingMessage();
		        outmsg.Write("A Client");
			
				print("Update: Reconnecting to server...");
            	client.Connect(hostIp, gm.gamePort, outmsg);
			}

            lastBeat = Time.time;
        }
        */	
	}

    public void OnApplicationQuit()
    {
		Shutdown();
    }
}
