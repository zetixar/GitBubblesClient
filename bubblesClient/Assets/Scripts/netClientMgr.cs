//version 001

// derived from http://docs.unity3d.com/Manual/UNetClientServer.html
// and http://forum.unity3d.com/threads/master-server-sample-project.331979/

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;
using UnityEngine.Networking.Match;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class netClientMgr : MonoBehaviour {

	public static float nodeSclaeFactor = 1.8f;
	public static int myNodeIndex;
	public static bool stateChoosingServer = true;
	public static bool stateIsConnected = false;
	public static bool initialized = false;
	public static bool choosingNodePhase = false;
	public static bool spectating = false;
	public static bool joiningTheRuningGame = true;
	public static bool gameIsRunning = false;
	public static bool generatingLink = false;
	//	public string serverIP = "192.168.0.2";
	public string serverIP = "52.91.177.74";// "127.0.0.1";
	public InputField playerNickNameInputField;
	public string playerNickName;

	public InputField serverIPInputField;
	public Button serverIPConnectButton;
	public Button serverIP192ConnectButton;
	public Text scrollViewTextDebuging;



	static NetworkClient myClient;
	static CScommon.GamePhaseMsg gamePhaseMsg = new CScommon.GamePhaseMsg(); //*** I'm not sure whether I need to initilize that or not


	static float camSpeed = 270.0f;
	static Camera mainCamera;

	// define the audio clips
	public AudioClip clipBeepSelectNodeForLink;
	public AudioClip clipTurning;

	static AudioSource audioSourceBeepSelectNodeForLink;
	static AudioSource audioSourceTurning;

	public Camera miniCamera;



//	List<MatchDesc> matchList = new List<MatchDesc>();
//	bool matchCreated;
//	NetworkMatch networkMatch;
//
//	MatchInfo matchinfo;

//
//
//	void Awake()
//	{
//		networkMatch = gameObject.AddComponent<NetworkMatch>();
//	}
//	
//	void OnGUI()
//	{
//		// You would normally not join a match you created yourself but this is possible here for demonstration purposes.
//		if(GUILayout.Button("Create Room"))
//		{
//			CreateMatchRequest create = new CreateMatchRequest();
//			create.name = "NewRoom";
//			create.size = 4;
//			create.advertise = true;
//			create.password = "";
//			
//			networkMatch.CreateMatch(create, OnMatchCreate);
//		}
//		
//		if (GUILayout.Button("List rooms"))
//		{
//			networkMatch.ListMatches(0, 20, "", OnMatchList);
//		}
//		
//		if (matchList.Count > 0)
//		{
//			GUILayout.Label("Current rooms");
//		}
//		foreach (var match in matchList)
//		{
//			if (GUILayout.Button(match.name))
//			{
//				networkMatch.JoinMatch(match.networkId, "", OnMatchJoined);
//			}
//		}
//	}
//	
//	public void OnMatchCreate(CreateMatchResponse matchResponse)
//	{
//		if (matchResponse.success)
//		{
//			Debug.Log("Create match succeeded");
//			matchCreated = true;
//			Utility.SetAccessTokenForNetwork(matchResponse.networkId, new NetworkAccessToken(matchResponse.accessTokenString));
//			NetworkServer.Listen(new MatchInfo(matchResponse), 9000);
//		}
//		else
//		{
//			Debug.LogError ("Create match failed");
//		}
//	}
//	
//	public void OnMatchList(ListMatchResponse matchListResponse)
//	{
//		if (matchListResponse.success && matchListResponse.matches != null)
//		{
//			networkMatch.JoinMatch(matchListResponse.matches[0].networkId, "", OnMatchJoined);
//		}
//	}
//	
//	public void OnMatchJoined(JoinMatchResponse matchJoin)
//	{
//		if (matchJoin.success)
//		{
//			Debug.Log("Join match succeeded");
//			if (matchCreated)
//			{
//				Debug.LogWarning("Match already set up, aborting...");
//				return;
//			}
//
//			Utility.SetAccessTokenForNetwork(matchJoin.networkId, new NetworkAccessToken(matchJoin.accessTokenString));
//			myNodeIndex = -1;
//			myClient = new NetworkClient();
//			Debug.Log ("Registering client callbacks");
//			myClient.RegisterHandler(MsgType.Connect, OnConnectedC);
//			//these two never get called because I haven't implemented the server disconnecting a client yet.
//			//They are irrelevant to current functioning.
//			myClient.RegisterHandler(MsgType.Disconnect, OnDisconnectC);
//			//myClient.RegisterHandler(MsgType.Error, OnErrorC);
//			myClient.RegisterHandler (CScommon.gamePhaseMsgType, onGamePhaseMsg);
//			myClient.RegisterHandler(CScommon.initMsgType, onInitMsg);
//			myClient.RegisterHandler(CScommon.nodeIdMsgType, onNodeIDMsg);
//			myClient.RegisterHandler (CScommon.requestNodeIdMsgType, onAssignedMyNodeID);
//			myClient.RegisterHandler (CScommon.initRevisionMsgType, onInitRevisionMsg);
//			myClient.RegisterHandler(CScommon.updateMsgType, onUpdateMsg);
//			myClient.RegisterHandler (CScommon.linksMsgType, onLinksMsg);
//			myClient.RegisterHandler (CScommon.nameNodeIdMsgType, onNameNodeIdMsg);
//			myClient.Connect(new MatchInfo(matchJoin));
////			myClient.Connect(serverIP, CScommon.serverPort);
//			audioSourceBeepSelectNodeForLink.Play ();
//		}
//		else
//		{
//			Debug.LogError("Join match failed");
//		}
//	}
//	
//	public void OnConnected(NetworkMessage msg)
//	{
//		Debug.Log("Connected!");
//	}




















	void Start()
	{
		serverIPInputField.placeholder.GetComponent<Text>().text = serverIP;
		serverIPInputField.text = serverIPInputField.placeholder.GetComponent<Text>().text;

		audioSourceBeepSelectNodeForLink = AddAudio(clipBeepSelectNodeForLink,false,false,0.5f);
		audioSourceTurning = AddAudio(clipTurning,false,false,0.15f);

		mainCamera = Camera.main;
		//setting up the prefabs
		GOspinner.gospinnerStart();
	}

	void Update () 
	{
//#if UNITY_STANDALONE || Unity_WEBPLAYER
		if(stateChoosingServer)
		{
			serverIP = serverIPInputField.text;
			playerNickName = playerNickNameInputField.text;
			return;
		}
//		if (isAtStartup && Input.GetKeyDown(KeyCode.C)) SetupClient();
		if (Input.GetKeyDown (KeyCode.L) && myClient != null && myClient.isConnected) 
		{
			myClient.Disconnect();
			backToChooseServerPhase();
		}
		controllingServerViaClient();
		if (initialized)
		GOspinner.Update ();
//#elif UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE
//#endif
	}
	void controllingServerViaClient()
	{
		if(Input.GetKey(KeyCode.RightShift) && myClient != null && myClient.isConnected)
		{

			CScommon.intMsg gameNum = new CScommon.intMsg();
			gameNum.value = -10;
			if(Input.GetKeyDown(KeyCode.H)) gameNum.value = 21;
			if(Input.GetKeyDown(KeyCode.J)) 
			{
				gameNum.value = 22;
				Debug.Log("J Pressed");
			}
			for(int i = 0; i <10; i++)if (Input.GetKeyDown(""+i))gameNum.value = i;
			if(gameNum.value != -10)
				myClient.Send(CScommon.restartMsgType, gameNum);
		}
	}

	public void backToChooseServerPhase()
	{
		stateChoosingServer = true;
		Application.LoadLevel(Application.loadedLevel);
		serverIPInputField.gameObject.SetActive(true);
		serverIPConnectButton.gameObject.SetActive(true);
		playerNickNameInputField.gameObject.SetActive(true);
		serverIP192ConnectButton.gameObject.SetActive(true);
		initialized = false;
		miniCamera.gameObject.SetActive(false);
		gamePhaseMsg.numNodes = 0;
		gamePhaseMsg.numLinks = 0;
		GOspinner.settingUpTheScene();
		joiningTheRuningGame = true;

	}

	//called by btn in the scene
	public void connectTo19216802Server()
	{
		serverIP = "192.168.0.2";
		audioSourceBeepSelectNodeForLink.Play ();
	}

	//Showing Msg in scrollview in playmode
	public void debugingDesplayinScrollView(string text)
	{
		scrollViewTextDebuging.text += "\n" + text;

	}

	//Audio
	public AudioSource AddAudio (AudioClip clip, bool loop, bool playAwake, float vol) 
	{ 
		AudioSource newAudio = gameObject.AddComponent<AudioSource>();
		newAudio.clip = clip; 
		newAudio.loop = loop;
		newAudio.playOnAwake = playAwake;
		newAudio.volume = vol; 
		return newAudio; 
	}

	//Key Guide
	void OnGUI()
	{
	if (myClient != null && myClient.isConnected) 
		{
			GUI.Label (new Rect (2, 10, 150, 100), "KeyGuid (K)");
			if (Input.GetKey (KeyCode.K))
			GUI.Label (new Rect (2, 25, 320, 600),
				   "\nCAMERA\n"+
				   "WASD: for Moving Camera\n" +
		           "Z: zoom in\n" +
		           "X: zoom out\n" +
				   "Q: turning on/off camera following Player\n" +
		           "F1: focusing on my player\n" +
		           "F7: displayNames off/on\n" +

		           "\nEXTERNAL LINKS\n"+
		           "Left Click: puller external link\n" +
		           "Space+Left Click: pusher external link\n" +
				   "N: no external link\n" +

		           "\nINTERNAL LINKS\n"+
		           "R: pusher internal link\n" +
		           "F: puller internal link\n" +
		           "V: pull/push toggle internal link\n" +
				   "P: auto internal link\n" +

		           "\nTRICYCLE STEERING\n"+
				   "Right Click: steering the tricycle toward\n\n" +

		           "\nINCHWORM\n"+
				   "T: inchworm going forward\n" +
				   "G: inchworm reverse\n" +
				   "B: inchworm forward/reverse toggle\n\n" +

				   "\nMOVESPEED\n"+
				   "Semicolon: speed up\n"+
				   "Quote: speed down\n\n"+

				   "L: disconnect");
		}
	} 

	//Called by btn in the scene and create a client and connect to the server port
	public void SetupClient()
	{	
		myNodeIndex = -1;
		myClient = new NetworkClient();
		Debug.Log ("Registering client callbacks");
		myClient.RegisterHandler(MsgType.Connect, OnConnectedC);
		//these two never get called because I haven't implemented the server disconnecting a client yet.
		//They are irrelevant to current functioning.
		myClient.RegisterHandler(MsgType.Disconnect, OnDisconnectC);
		//myClient.RegisterHandler(MsgType.Error, OnErrorC);

		myClient.RegisterHandler (CScommon.gamePhaseMsgType, onGamePhaseMsg);
		myClient.RegisterHandler(CScommon.initMsgType, onInitMsg);
		myClient.RegisterHandler(CScommon.nodeIdMsgType, onNodeIDMsg);
		myClient.RegisterHandler (CScommon.requestNodeIdMsgType, onAssignedMyNodeID);
		myClient.RegisterHandler (CScommon.initRevisionMsgType, onInitRevisionMsg);
		myClient.RegisterHandler(CScommon.updateMsgType, onUpdateMsg);
		myClient.RegisterHandler (CScommon.linksMsgType, onLinksMsg);
		myClient.RegisterHandler (CScommon.nameNodeIdMsgType, onNameNodeIdMsg);

		myClient.Connect(serverIP, CScommon.serverPort);
		audioSourceBeepSelectNodeForLink.Play ();
	}

	public void OnConnectedC(NetworkMessage netMsg)
	{
		Debug.Log("Connected as client to server");
		Debug.Log ("conn: "+netMsg.conn +"  msgType:" +netMsg.msgType);

		serverIPInputField.gameObject.SetActive(false);
		serverIPConnectButton.gameObject.SetActive(false);
		serverIP192ConnectButton.gameObject.SetActive(false);
		playerNickNameInputField.gameObject.SetActive(false);
		stateChoosingServer = false;
		initialized = false;
		audioSourceTurning.Play();
	}

	public void OnDisconnectC(NetworkMessage info)
	{	
		Debug.Log ("OnDisconnectC: Disconnected from server.");
		backToChooseServerPhase();
	}

	public void onGamePhaseMsg(NetworkMessage netMsg)
	{
		gamePhaseMsg = netMsg.ReadMessage<CScommon.GamePhaseMsg>();
		if (gamePhaseMsg.gamePhase == 2)
		{
			if(joiningTheRuningGame)
//			{
//				CScommon.stringMsg myname = new CScommon.stringMsg();
//				myname.value = playerNickName;
//				myClient.Send (CScommon.initRequestType, myname);
//				return;
//			}
			gameIsRunning = true;
			choosingNodePhase = false;
			if (myNodeIndex == -1)
				spectating = true;
			else
				spectating = false;
		}
		//game is preloading or restarting, I send initRequest
		else if (gamePhaseMsg.gamePhase == 1)
		{	
			Debug.Log ("gamephase1 recieved");

			joiningTheRuningGame = false;
			miniCamera.gameObject.SetActive(true);

			CScommon.stringMsg myname = new CScommon.stringMsg();
			myname.value = playerNickName;
			myClient.Send (CScommon.initRequestType, myname);

			gameIsRunning = false;
			spectating = false;
			if (GOspinner.bubbles != null)
			GOspinner.cleanScene ();
			//Allocating bubbles,links,oomphs etc. & MynodeIndex = -1
			GOspinner.settingUpTheScene();
		}
	}

	public void onInitMsg(NetworkMessage netMsg)
	{
		CScommon.InitMsg initMsgg = netMsg.ReadMessage<CScommon.InitMsg> ();
		GOspinner.prepareForInitialize(initMsgg);
		initialized = true;
		choosingNodePhase = true;
	}

	public void onNodeIDMsg(NetworkMessage netMsg){
		CScommon.intMsg nodeIndexMsg = netMsg.ReadMessage<CScommon.intMsg>();
		myNodeIndex = nodeIndexMsg.value;
		Debug.Log ("   my nodeIndex is " + myNodeIndex);
		debugingDesplayinScrollView ("   my nodeIndex is " + myNodeIndex);
	}

	public void onInitRevisionMsg (NetworkMessage netMsg)
	{
		CScommon.InitRevisionMsg initrevmassege = netMsg.ReadMessage<CScommon.InitRevisionMsg> ();
		GOspinner.initRevisionMsgg (initrevmassege);
	}

	public void onUpdateMsg(NetworkMessage netMsg){
		CScommon.UpdateMsg partOfupdatemsg = netMsg.ReadMessage<CScommon.UpdateMsg> ();
		GOspinner.updatingMasterUpdateMsg(partOfupdatemsg);
	}

	public void onAssignedMyNodeID(NetworkMessage netMsg)
	{
		myNodeIndex = netMsg.ReadMessage<CScommon.intMsg>().value;
		if (myNodeIndex == -1)
		{
			Debug.Log ("You don't have bubble");
		}
		else
			GOspinner.nodePrefabCheck(myNodeIndex,false);
	}


	public void onLinksMsg(NetworkMessage netMsg)
	{
		// three phases for links: 
		//1- generate links list and link gameobjects > done in first call for onLinksMsg() 
		//2- update links list (updating linksMsg) and link gameobjects with their setActive, prefab  > done in onLinksMsg()
		//3- position and rotate and scale(strength) links gameobjects > done in update()
		CScommon.LinksMsg newLinkMsg = netMsg.ReadMessage<CScommon.LinksMsg>();

		//if it is the first linkMsg it will generate link
		if(generatingLink)
		GOspinner.generateLinks(); // I do it once at the begining
		GOspinner.reassignLinksPrefabs(newLinkMsg); // Done every time that I recieve onLinkMsg to fix the prefabs for bones etc. and apply setActive
	}

	public void onNameNodeIdMsg(NetworkMessage netMsg)
	{
		CScommon.NameNodeIdMsg playersNameListMsg = netMsg.ReadMessage<CScommon.NameNodeIdMsg>();
		GOspinner.playerNamesManage(playersNameListMsg);
	}




//GOSPINNER *************************************** GOSPINNER\\ 
	private static class GOspinner {

		public static float linkscalefactor = 20.0f;

		public static Transform pfGoal, pfVegBubble, pfAnimBubble, pfReservdPlayerBubble, pfMyPlayer,
		pfOTeamPlayer, pfSnark, pfOomph, pfExPullLink, pfExPushLink, pfBoneLink, pfPlayerName;

		public static Transform[] bubbles;
		public static Transform[] oomphs; //** for displaying oomph around each bubble
		public static GameObject[] links;

		private static NetworkMessage lastNetMsg;
		private static int netMsgsSinceLastUpdate;

		public static CScommon.UpdateMsg updateMsg;
		public static CScommon.InitMsg initMsg;
		public static CScommon.LinksMsg linkMsg;

		public static Dictionary<int,Transform> playersNameTransforms;
		public static Dictionary<int,string> dicPlayerNames = new Dictionary<int, string>();

		public static void gospinnerStart()
		{
			pfExPullLink = GameObject.Find("pfExPullLink").transform;
			pfExPushLink = GameObject.Find ("pfExPushLink").transform;
			pfBoneLink = GameObject.Find ("pfBoneLink").transform;
			pfVegBubble = GameObject.Find ("pfVegBubble").transform;
			pfAnimBubble = GameObject.Find ("pfAnimBubble").transform;
			pfSnark = GameObject.Find ("pfSnark").transform;
			pfMyPlayer = GameObject.Find ("pfMyPlayer").transform;
			pfOTeamPlayer = GameObject.Find ("pfOTeamPlayer").transform;
			pfOomph = GameObject.Find ("pfOomph").transform;
			pfReservdPlayerBubble = GameObject.Find ("pfReservdPlayerBubble").transform;
			pfGoal = GameObject.Find ("pfGoal").transform;
			pfPlayerName = GameObject.Find("pfPlayerName").transform;
		}

		internal static void prepareForInitialize(CScommon.InitMsg partofinitmsg)
		{
			for (int i = partofinitmsg.start; i < partofinitmsg.start + partofinitmsg.nodeData.Length; i++)
			
			{
				GOspinner.initMsg.nodeData[i] = partofinitmsg.nodeData[i - partofinitmsg.start];
				//false means that it will change bubbles[i] and will not 'add' to the list blindly
				nodePrefabCheck (i, false);
				//**oomp

				oomphs[i] = ((Transform)Instantiate (pfOomph, Vector3.zero, Quaternion.identity));
//				oomphs[i].Rotate(0f,0f, Random.Range (0f,90.0f));
				oomphs [i].tag = "oomphClone";
				oomphs [i].name = "oomph " + i;
			}
		}

		internal static void resetGameStats() {
			mainCamera.orthographicSize = 1600.0f;
			mainCamera.transform.position = new Vector3(0.0f,0.0f,-100.0f);
			cameraZoomOutAtStart = 105;
			gameIsRunning = false;
			generatingLink = true;
			initialized = false;
			cameraFollowMynode = false;
			cameraFollowNodeIndex = 0;
			followingCamera = false;
		}
		
		public static void cleanScene()
		{	if(bubbles.Length > 0){
				for (int i = 0; i < bubbles.Length; i++)
					Destroy(bubbles[i].gameObject);
				for (int i = 0; i < links.Length; i++)
					Destroy(links[i].gameObject);
			}
			GameObject[] playerNames = GameObject.FindGameObjectsWithTag ("PlayerName");
			foreach (GameObject playerName in playerNames)
				Destroy (playerName);
			GameObject[] oomphclones = GameObject.FindGameObjectsWithTag ("oomphClone");
			foreach (GameObject oomphclone in oomphclones)
				Destroy (oomphclone);
			GameObject[] playerclones = GameObject.FindGameObjectsWithTag ("Player");
			foreach (GameObject player in playerclones)
				Destroy (player);
			GameObject[] linkclones = GameObject.FindGameObjectsWithTag ("LinkClone");
			foreach (GameObject linkclone in linkclones)
				Destroy (linkclone);
			GameObject[] goalclones = GameObject.FindGameObjectsWithTag ("GoalClone");
			foreach (GameObject goalclone in goalclones)
				Destroy (goalclone);
		}

		public static void settingUpTheScene()
		{
			GOspinner.bubbles = new Transform[gamePhaseMsg.numNodes];
			GOspinner.oomphs = new Transform[gamePhaseMsg.numNodes];
			GOspinner.links = new GameObject[gamePhaseMsg.numLinks];
			
			GOspinner.initMsg = new CScommon.InitMsg();
			GOspinner.initMsg.nodeData = new CScommon.StaticNodeData[gamePhaseMsg.numNodes];
			
			GOspinner.updateMsg = new CScommon.UpdateMsg();
			GOspinner.updateMsg.nodeData = new CScommon.DynamicNodeData[gamePhaseMsg.numNodes];
			
			GOspinner.linkMsg = new CScommon.LinksMsg();
			GOspinner.linkMsg.links = new CScommon.LinkInfo[gamePhaseMsg.numLinks];
			
			GOspinner.dicPlayerNames = new Dictionary<int, string>();
			GOspinner.playersNameTransforms = new Dictionary<int, Transform>();
			myNodeIndex = -1;
			GOspinner.resetGameStats();

		}


		public static void nodePrefabCheck(int i, bool add)
		{
			CScommon.StaticNodeData nd = initMsg.nodeData[i];
//			string goalTag = "GoalClone";
//			string goalName = "Goal";
			string playerTag = "Player";
			string playerName = "MyPlayer";
			string bblCloneTag = "bblClone";
			string bblCloneName = "bbl";
//			if (i == 0) {
//				managePrefabTagNameBoolAddtrueRepfalse (i, pfGoal, goalTag, goalName, add);
//				return;
//			}
			if (i == myNodeIndex) {
				managePrefabTagNameBoolAddtrueRepfalse (i, pfMyPlayer, playerTag, playerName, add);
				return;
			} else if (CScommon.testBit (nd.dna, CScommon.snarkBit)){	
				managePrefabTagNameBoolAddtrueRepfalse (i, pfSnark, bblCloneTag, bblCloneName, add);
				return;
			}else if (CScommon.testBit (nd.dna, CScommon.playerBit) && !CScommon.testBit (nd.dna, CScommon.playerPlayingBit)) {
				managePrefabTagNameBoolAddtrueRepfalse (i, pfReservdPlayerBubble, bblCloneTag, bblCloneName, add);
				return;
			} else if (CScommon.testBit (nd.dna, CScommon.playerPlayingBit)) {
				managePrefabTagNameBoolAddtrueRepfalse (i, pfOTeamPlayer, bblCloneTag, bblCloneName, add);
				return;
			} else if (CScommon.testBit (nd.dna, CScommon.vegetableBit)) {
				managePrefabTagNameBoolAddtrueRepfalse (i, pfVegBubble, bblCloneTag, bblCloneName, add);
				return;
			} else if (!CScommon.testBit (nd.dna, CScommon.vegetableBit)){	
				managePrefabTagNameBoolAddtrueRepfalse (i, pfAnimBubble, bblCloneTag, bblCloneName, add);
				return;
			}
		}

		private static void managePrefabTagNameBoolAddtrueRepfalse( int i, Transform prefab, string tag, string name, bool addorreplace)
		{
			CScommon.StaticNodeData nd = initMsg.nodeData[i];
			if (addorreplace){
//				bubbles.Add ((Transform)Instantiate (prefab, Vector3.zero, Quaternion.identity));
				bubbles [i] = ((Transform)Instantiate (prefab, Vector3.zero, Quaternion.identity));}

			else
				{bubbles [i] = ((Transform)Instantiate (prefab, Vector3.zero, Quaternion.identity));}
			bubbles[i].localScale = new Vector3(nd.radius*nodeSclaeFactor, nd.radius *nodeSclaeFactor, 0);
			bubbles[i].tag = tag;
			bubbles[i].name = name + i;
		}

		public static void initRevisionMsgg(CScommon.InitRevisionMsg initrevmassege)
		{
			for (int i = 0; i < initrevmassege.nodeInfo.Length; i++) 
			{
				//For distroying previous playerName game object and also cleaning the list of names.
				if(dicPlayerNames.ContainsKey(initrevmassege.nodeInfo[i].nodeIndex)&& 
				   !((CScommon.testBit (initrevmassege.nodeInfo [i].staticNodeData.dna, CScommon.snarkBit))||
					CScommon.testBit (initrevmassege.nodeInfo [i].staticNodeData.dna, CScommon.playerPlayingBit)))
				{
					Destroy(playersNameTransforms[initrevmassege.nodeInfo[i].nodeIndex].gameObject);
					dicPlayerNames.Remove(initrevmassege.nodeInfo[i].nodeIndex);
					playersNameTransforms.Remove(initrevmassege.nodeInfo[i].nodeIndex);
				}

				//updating initmsg with new changes that I get from initrevmessage
				initMsg.nodeData [initrevmassege.nodeInfo [i].nodeIndex] = initrevmassege.nodeInfo [i].staticNodeData;
				//destorying gameobjects that are going to be updated
				Destroy (bubbles[initrevmassege.nodeInfo[i].nodeIndex].gameObject);
				//instantiating new gameobjects according to initRivisionMsg
				int j = initrevmassege.nodeInfo [i].nodeIndex;
				nodePrefabCheck(j, false);
			}
		}

		public static void updatingMasterUpdateMsg (CScommon.UpdateMsg partOfupdatemsg)
		{
			//updating masterupdateMsg with new information
			for (int i = partOfupdatemsg.start, partupdateint = 0  ;
			     i < partOfupdatemsg.start + partOfupdatemsg.nodeData.Length;
			     i++, partupdateint++)
				{
					updateMsg.nodeData[i] = partOfupdatemsg.nodeData[partupdateint];
				}
		}

		public static void generateLinks()
		{
			//linkMsg.links.Length >> I got this data during onGamePhaseMsg()
			for (int i = 0; i < linkMsg.links.Length; i++)
			{ 
				links[i] = (GameObject) Instantiate(pfExPullLink.gameObject,Vector3.zero,Quaternion.identity) as GameObject;
				links[i].name = "Link " + i;
				links[i].tag = "LinkClone";
			}
			generatingLink = false;
		}

		public static void reassignLinksPrefabs(CScommon.LinksMsg linkMsg)
		{
			for (int i = 0; i < linkMsg.links.Length; i++)
			{ 
				CScommon.LinkInfo linkinfo = linkMsg.links[i];
				//updating GOspinner.linkMsg
				GOspinner.linkMsg.links[linkinfo.linkId] = linkinfo;
				
				//updating prefabs in game 
				//(I couldn't do this in generateLinks() because the link information are sent in splited msgs and 
				// I do generateLinks() only once at first msg
				// The second part of the if statement is for not reInstantiating bone links on every onLinkMsg
				if (linkinfo.linkData.linkType == CScommon.LinkType.bone && links[linkinfo.linkId].tag == "LinkClone")
				{
					Destroy(links[linkinfo.linkId]);
					links[linkinfo.linkId] = (GameObject) Instantiate(pfBoneLink.gameObject,Vector3.zero,Quaternion.identity) as GameObject;
					links[linkinfo.linkId].name = "Link " + linkinfo.linkId;
					links[linkinfo.linkId].tag = "LinkCloneBone";
				}

				if(linkinfo.linkData.enabled)
				{
					GOspinner.links[linkinfo.linkId].SetActive(true);
//					GOspinner.links[linkinfo.linkId].GetComponent<SpriteRenderer>().enabled = true;
				}
				else 
				{
					GOspinner.links[linkinfo.linkId].SetActive(false);
//					GOspinner.links[linkinfo.linkId].GetComponent<SpriteRenderer>().enabled = false;
				}
			}
		}

		private static void updateLinksPosRotScale()
		{
			for (int i = 0; i < linkMsg.links.Length; i++) {
			CScommon.LinkInfo linkInfo = linkMsg.links [i];
				links[linkInfo.linkId].transform.position = 
					(bubbles[linkInfo.linkData.sourceId].position +
					 bubbles[linkInfo.linkData.targetId].position)/2;

//public static float rawStrength(float oomph, float maxOomph, long dna, float radiusSquared, float linkLengthSquared)
			links[linkInfo.linkId].transform.localScale = new Vector3(
/*vectore3.x*/		((CScommon.rawStrength
					(updateMsg.nodeData[linkInfo.linkData.sourceId].oomph,
					CScommon.maxOomph(initMsg.nodeData[linkInfo.linkData.sourceId].radius,0L),
					initMsg.nodeData[linkInfo.linkData.sourceId].dna,
					Mathf.Pow (initMsg.nodeData[linkInfo.linkData.sourceId].radius,2.0f),
					distance2(bubbles[linkInfo.linkData.sourceId].position, bubbles[linkInfo.linkData.targetId].position))))
					* linkscalefactor,
/*vectore3.y*/		(bubbles[linkInfo.linkData.sourceId].position - bubbles[linkInfo.linkData.targetId].position).magnitude * 1.2f,
/*vectore3.z*/		0.0f);


			links[linkInfo.linkId].transform.LookAt(bubbles[linkInfo.linkData.targetId].transform);
			links[linkInfo.linkId].transform.Rotate(0.0f,90.0f,90.0f);
			}
		}

		private static float distance2(Vector3 source, Vector3 target){
			return (target.x-source.x)*(target.x-source.x) + (target.y-source.y)*(target.y-source.y);
		}

//tail		private static Vector3 prvsPosition = new Vector3(0,0,0);

		private static void positioning ()
		{
//			CScommon.DynamicNodeData me = updateMsg.nodeData[myNodeIndex];
			for (int i = 0; i < updateMsg.nodeData.Length; i++)
			{
			if (bubbles[i]!= null){
//tail			prvsPosition = bubbles[i].position;
				CScommon.DynamicNodeData nd = updateMsg.nodeData[i];
//**			Vector2 diff = nd.position - me.position;
//				bubbles[i].position =  new Vector3(diff.x, diff.y, 0);
				bubbles[i].position = new Vector3(nd.position.x,nd.position.y,0.0f);

				//**Rotating bubble with tail
//				if (bubbles[i].position != prvsPosition)
//				{
//					Quaternion tailRotation = Quaternion.Euler (new Vector3 (0,0,angleFromTwoPoints(prvsPosition,bubbles[i].position) - 90.0f));
//					bubbles[i].FindChild("Tail").transform.rotation = tailRotation;
//				}

				//** oomph position
//				oomphs[i].position = bubbles[i].position;
				oomphs[i].position = new Vector2 (bubbles[i].position.x, 
					                                  bubbles[i].position.y);// + (initMsg.nodeData[i].radius));// + (Mathf.Sqrt(updateMsg.nodeData[i].oomph) / 2f));
////// I do rotation in prepare for initializion


//** oomph radius and scale
//				float oomphRadius = initMsg.nodeData[i].radius *
//						( 1.0f + ((updateMsg.nodeData[i].oomph / (CScommon.maxOomph (initMsg.nodeData[i].radius,0L))) * 4.0f));				oomphs[i].localScale = new Vector3(oomphRadius *nodeSclaeFactor ,oomphRadius *nodeSclaeFactor ,0.0f);



//				float oomphRadius = initMsg.nodeData[i].radius *
//					(updateMsg.nodeData[i].oomph / (CScommon.maxOomph (initMsg.nodeData[i].radius,0L)));
//					oomphs[i].localScale = new Vector3(oomphRadius *nodeSclaeFactor ,oomphRadius *nodeSclaeFactor ,0.0f);

					float oomphRadius = initMsg.nodeData[i].radius *
										Mathf.Pow(updateMsg.nodeData[i].oomph
						          		/(CScommon.maxOomph (initMsg.nodeData[i].radius,0L)),0.5f);
					oomphs[i].localScale = new Vector3(oomphRadius *nodeSclaeFactor ,oomphRadius * nodeSclaeFactor ,0.0f);

//rectangle oompsh using small square
//				oomphs[i].localScale = new Vector3( Mathf.Sqrt(updateMsg.nodeData[i].oomph) * 2f// * 2f / 16.0f) 
//				                                   ,Mathf.Sqrt(updateMsg.nodeData[i].oomph) / 2f// * 2f / 16.0f))
//				                                   ,0.0f);



// with small square
					//oomphs[i].localScale = new Vector3(Mathf.Sqrt(updateMsg.nodeData[i].oomph)*7.0f * 2.0f ,(Mathf.Sqrt(updateMsg.nodeData[i].oomph)*7.0f) / 2.0f,0.0f);
				
//				if (i == myNodeIndex)
//					{
//						Debug.Log(string.Format("Max oomph: {0} currentoomph: {1} scalesize.y: {2} initMsg.nodeData[i].radius{3} oomphs[i].position{4}", (CScommon.maxOomph (initMsg.nodeData[i].radius,0L)),
//						                        (updateMsg.nodeData[i].oomph), (Mathf.Sqrt(updateMsg.nodeData[i].oomph) / 2f), initMsg.nodeData[i].radius, oomphs[i].position.y -  bubbles[i].position.y ));
//					}

				if(myNodeIndex != -1 && playersNameTransforms.ContainsKey(i))
					{
						if (i == myNodeIndex)
						{
							playersNameTransforms[i].FindChild("playerNameMiniMap").GetComponent<TextMesh>().color =
								Color.white;
							playersNameTransforms[i].FindChild("playerNameMainCam").GetComponent<TextMesh>().color =
								Color.white;
							continue;
						}
						else if (CScommon.testBit (initMsg.nodeData[i].dna, CScommon.playerPlayingBit))
						{
							playersNameTransforms[i].FindChild("playerNameMiniMap").GetComponent<TextMesh>().color =
								Color.grey;
							playersNameTransforms[i].FindChild("playerNameMainCam").GetComponent<TextMesh>().color =
								Color.grey;
							continue;
						}
						else if (updateMsg.nodeData[i].oomph > updateMsg.nodeData[myNodeIndex].oomph)
						{
							playersNameTransforms[i].FindChild("playerNameMiniMap").GetComponent<TextMesh>().color =
								Color.red;
							playersNameTransforms[i].FindChild("playerNameMainCam").GetComponent<TextMesh>().color =
								Color.red;
							continue;
						}
						else 
						{
							playersNameTransforms[i].FindChild("playerNameMiniMap").GetComponent<TextMesh>().color =
								Color.green;
							playersNameTransforms[i].FindChild("playerNameMainCam").GetComponent<TextMesh>().color =
								Color.green;
						}
					}

				//** oomph color
//									Color color = new Color(
//											1.0f,
//											1.0f - (updateMsg.nodeData[i].oomph / (CScommon.maxOomph (initMsg.nodeData[i].radius,0L))),
//											1.0f - (updateMsg.nodeData[i].oomph / (CScommon.maxOomph (initMsg.nodeData[i].radius,0L))),
//											1.0f);
//									oomphs[i].GetComponent<SpriteRenderer>().color = color;
				}
			}
			foreach(int playerID in playersNameTransforms.Keys)
			{
				playersNameTransforms[playerID].position = bubbles [playerID].position;
			}
		}

		private static float angle = 0.0f;
		private static float angleFromTwoPoints (Vector3 b, Vector3 a)
		{
			angle = Mathf.Atan2 (a.y - b.y, a.x - b.x);
			angle = stdAngle(angle);
			return angle;
		}

		public static void playerNamesManage(CScommon.NameNodeIdMsg playersNameListMsg)
		{	
			int playerNodeID = playersNameListMsg.nodeIndex;
			// updating dictionary dicPlayersNames 
			if (dicPlayerNames.ContainsKey(playerNodeID))
				dicPlayerNames.Remove(playerNodeID);
			dicPlayerNames.Add(playerNodeID,playersNameListMsg.name);

			//instantiating or updating playerNameTransform

			foreach(int playerID in playersNameTransforms.Keys)
			{
				if(!dicPlayerNames.ContainsKey(playerID))
				{
					Destroy(playersNameTransforms[playerNodeID].gameObject);
					playersNameTransforms.Remove(playerNodeID);
				}
			}
			if (!playersNameTransforms.ContainsKey(playerNodeID))
			{
				playersNameTransforms.Add
					(playerNodeID,((Transform)Instantiate (pfPlayerName, bubbles[playerNodeID].position, Quaternion.identity)));
			}

			playersNameTransforms[playerNodeID].FindChild("playerNameMainCam").GetComponent<TextMesh>().text =
				dicPlayerNames[playerNodeID];// + " " + updateMsg.nodeData[playerNodeID].oomph.ToString();
			playersNameTransforms[playerNodeID].FindChild("playerNameMiniMap").GetComponent<TextMesh>().text =
				dicPlayerNames[playerNodeID];
//
//			if (playerNodeID == myNodeIndex)
//				playersNameTransforms[playerNodeID].FindChild("playerNameMiniMap").GetComponent<TextMesh>().color =
//					Color.blue;
//			else 
//				playersNameTransforms[playerNodeID].FindChild("playerNameMiniMap").GetComponent<TextMesh>().color =
//					Color.red;


			playersNameTransforms[playerNodeID].name = "playerName" + playerNodeID;
			playersNameTransforms[playerNodeID].tag = "PlayerName";


			//I should check if I need to instantiate the prefab or not. If I don't need that, I just change the colour or
			//the text of existing prefab
			//I also can let players to change their names while they are in choosingNodePhase and it would be up to
			//the server to decide whether they can have a new name or not
//				string minusScoreText = dicPlayerNames[playersNameListMsg.nodeIndex].
//					Substring(dicPlayerNames[playersNameListMsg.nodeIndex].Length - 1);
//				int minusScoreInt = 0; int.TryParse(minusScoreText, out minusScoreInt);
//				Debug.Log (minusScoreInt + minusScoreText);
//				bubbles[playersNameListMsg.nodeIndex].FindChild("playerNameMesh").GetComponent<TextMesh>().color =
//					Color.blue;
//				if ( minusScoreInt > 2)
//				{
//					bubbles[playersNameListMsg.nodeIndex].FindChild("playerNameMesh").GetComponent<TextMesh>().color =
//						Color.blue;
//				}
			}

		private static int displayNames = 0; // 0 display all, 1 display only on minimap, 2 display only on main scene, 3 don't displaythem
		private static void changeHowToDisPlayPlayersName()
		{
			foreach(int playerID in playersNameTransforms.Keys)
			{
				switch (displayNames)
				{
				case 0:{
					playersNameTransforms[playerID].gameObject.SetActive(true);
					playersNameTransforms[playerID].FindChild("playerNameMainCam").gameObject.SetActive(true);
					playersNameTransforms[playerID].FindChild("playerNameMiniMap").gameObject.SetActive(true);
					break;}
				case 1:{
					playersNameTransforms[playerID].gameObject.SetActive(true);
					playersNameTransforms[playerID].FindChild("playerNameMainCam").gameObject.SetActive(false);
					playersNameTransforms[playerID].FindChild("playerNameMiniMap").gameObject.SetActive(true);
					break;}
				case 2:{
					playersNameTransforms[playerID].gameObject.SetActive(true);
					playersNameTransforms[playerID].FindChild("playerNameMainCam").gameObject.SetActive(true);
					playersNameTransforms[playerID].FindChild("playerNameMiniMap").gameObject.SetActive(false);
					break;}
				case 3:{
					playersNameTransforms[playerID].gameObject.SetActive(false);
					break;}
				}
			}
		}





		static bool followingCamera = false;
		static int cameraFollowNodeIndex = 0;
		public static int cameraZoomOutAtStart;

		public static void Update()
		{	
			if (cameraZoomOutAtStart > 0)
			{
				mainCamera.orthographicSize -= 13.0f;
				cameraZoomOutAtStart --;
				return;
			}
			if (choosingNodePhase && Input.GetMouseButtonDown (0))ChoosingMyNode ();
			cameraMover ();
			positioning ();
			updateLinksPosRotScale();

			if (Input.GetKeyDown(KeyCode.Semicolon))
			{
				MusSpeedController(20);
			}
			if (Input.GetKeyDown(KeyCode.Quote))
			{
				MusSpeedController(-20);
			}
			//Requesting to make push internal link for my inchworm
			if (Input.GetKeyDown(KeyCode.R))
			{
				InversInchwomrsLink(1);
			}
			//Requesting to make pull internal link for my inchworm
			if (Input.GetKeyDown(KeyCode.F))
			{
				InversInchwomrsLink(2);
			}
			if (Input.GetKeyDown(KeyCode.P))
			{
				InversInchwomrsLink(0);
			}
			if (Input.GetKeyDown(KeyCode.V))
			{
				InversInchwomrsLink(3);
			}

			//forward
			if (Input.GetKeyDown(KeyCode.T))
			{
				inchwormForwardBackWard(0);
			}
			//backward
			if (Input.GetKeyDown(KeyCode.G))
			{
				inchwormForwardBackWard(1);
			}
			if (Input.GetKeyDown(KeyCode.B))
			{
				inchwormForwardBackWard(2);
			}

			if (Input.GetKeyDown(KeyCode.F7))
			{
				displayNames++;
				if (displayNames > 3) displayNames = 0;
				changeHowToDisPlayPlayersName();

			}
			if (!gameIsRunning || myNodeIndex < 0) return;
			requestLinktoTarget ();


			if (Input.GetKeyDown(KeyCode.U))
			{
				CScommon.intMsg myDesiredRotationto= new CScommon.intMsg();
				myDesiredRotationto.value = 1;
				myClient.Send (CScommon.turnMsgType, myDesiredRotationto);
				Debug.Log ("Turn to Left");
				
			}
			if (Input.GetKeyDown(KeyCode.I))
			{
				CScommon.intMsg myDesiredRotationto= new CScommon.intMsg();
				myDesiredRotationto.value = -1;
				myClient.Send (CScommon.turnMsgType, myDesiredRotationto);
				Debug.Log ("Turn to Right");
			}
		

			if(Input.GetMouseButtonDown (1))
			{
				requestToRotateMe();
			}
		}

		static int myInternalMusSpeed = 80;
		//static int myExternalMusSpeed = 80;

		static void MusSpeedController(int increaseOrDecreaseSpeed)
		{
			CScommon.intMsg myDesiredSpeed= new CScommon.intMsg();

			if (0 < myInternalMusSpeed && myInternalMusSpeed < 300)	myInternalMusSpeed += increaseOrDecreaseSpeed;
			else myInternalMusSpeed = 80;

			myDesiredSpeed.value = myInternalMusSpeed;
			myClient.Send (CScommon.speedMsgType, myDesiredSpeed);
			Debug.Log ("My Internal Mus Speed: " + myInternalMusSpeed);
		}
	
		static bool cameraFollowMynode = false;
		static float mainCamMoveSpeed = 3.0f;
//** need to clamp the camera so it cannot go over up/down/right/left.
		static void cameraMover()
		{
			if (Input.GetAxis("Horizontal") != 0.0f)
				{
				mainCamera.transform.Translate(new Vector3((Input.GetAxis("Horizontal"))*camSpeed * Time.deltaTime,0,0));
				}
			if (Input.GetAxis("Vertical") != 0.0f)
				{
				mainCamera.transform.Translate(new Vector3(0,(Input.GetAxis("Vertical"))*camSpeed * Time.deltaTime,0));
				}

			if (cameraFollowMynode && bubbles[myNodeIndex].gameObject != null)
			{	Vector3 playerDefualtCamPos = new Vector3 
					(bubbles[myNodeIndex].transform.position.x,
					 bubbles[myNodeIndex].transform.position.y,
					 -100);
				mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position,playerDefualtCamPos,Time.deltaTime * mainCamMoveSpeed);
			}
			if (Input.GetKeyDown (KeyCode.Q) && myNodeIndex != -1)
			{
				cameraFollowMynode = !cameraFollowMynode;
			}
			if (Input.GetKey (KeyCode.Z))
				zoomIn (4.0f);
			else if (Input.GetKey(KeyCode.X))
				 zoomOut (4.0f);

			if (Input.GetAxis("Mouse ScrollWheel") > 0)
			{
				zoomIn (7f);
			}
			if (Input.GetAxis("Mouse ScrollWheel") < 0)
			{
				zoomOut (8f);
			}

//			 to let Spectator follow one node
			if (gameIsRunning && spectating)
			{
				if(Input.anyKey && !Input.GetMouseButton(0) && !Input.GetKey(KeyCode.Z) && !Input.GetKey(KeyCode.X))
					followingCamera = false;

				if (Input.GetMouseButtonDown (0))
				{
					cameraFollowNodeIndex = closestBubbleIndexNumber();
					followingCamera = true;
					Debug.Log ("Camera follows node#: " + cameraFollowNodeIndex);
				}
				if (followingCamera && bubbles[cameraFollowNodeIndex] != null)
				{
					Vector3 nodeFollowedPos = new Vector3 
						(bubbles[cameraFollowNodeIndex].transform.position.x,
						 bubbles[cameraFollowNodeIndex].transform.position.y,
						 -100);
					mainCamera.transform.position = nodeFollowedPos;
				}
//				mainCamera.transform.position = bubbles[cameraFollowNodeIndex].position;
			}
			if (Input.GetKeyDown(KeyCode.F1)&& myNodeIndex != -1 && bubbles[myNodeIndex].gameObject != null)
			{
				mainCamera.orthographicSize = 150.0f;
				Vector3 playerDefualtCamPos = new Vector3 
					(bubbles[myNodeIndex].transform.position.x,
					 bubbles[myNodeIndex].transform.position.y,
					 -100);
				mainCamera.transform.position = playerDefualtCamPos;
			}
//			mainCamera.transform.position.x = Mathf.Clamp(mainCamera.transform.position.x, -200.0f,200.0f)
		}

		static void zoomIn(float camorthsizeminus) {
			if (mainCamera.orthographicSize > 14.0f)
			{
				mainCamera.orthographicSize -= camorthsizeminus;
			}
		}
		
		static void zoomOut(float camorthsizeplus) {
			if (mainCamera.orthographicSize < 700.0f)
			{
				mainCamera.orthographicSize += camorthsizeplus;
			}
		}

//
//		public void resetCam()
//		{
//			StartCoroutine(LerpToPosition(camPanDuration, farLeft.position, true));    
//		}
//		
//		IEnumerator LerpToPosition(float lerpSpeed, Vector3 newPosition, bool useRelativeSpeed = false)
//		{    
//			if (useRelativeSpeed)
//			{
//				float totalDistance = farRight.position.x - farLeft.position.x;
//				float diff = transform.position.x - farLeft.position.x;
//				float multiplier = diff / totalDistance;
//				lerpSpeed *= multiplier;
//			}
//			
//			float t = 0.0f;
//			Vector3 startingPos = transform.position;
//			while (t < 1.0f)
//			{
//				t += Time.deltaTime * (Time.timeScale / lerpSpeed);
//				
//				transform.position = Vector3.Lerp(startingPos, newPosition, t);
//				yield return 0;
//			}    
//		}






		static void ChoosingMyNode()
		{
			CScommon.intMsg myDesiredNodeIndexNumber= new CScommon.intMsg();
			if (Input.GetKey (KeyCode.LeftControl))
				myDesiredNodeIndexNumber.value = -1;
			else 
				myDesiredNodeIndexNumber.value = GOspinner.closestBubbleIndexNumber ();
			myClient.Send (CScommon.requestNodeIdMsgType, myDesiredNodeIndexNumber);
			
		}
		//for requesting to have a specefic type of a link from 'me' to the node that is closest node 
		//to the position that I have clicked on.
		internal static void requestLinktoTarget()
		{
			CScommon.TargetNodeMsg nim = new CScommon.TargetNodeMsg ();
			if (Input.GetKeyDown(KeyCode.N))
			{
				nim.nodeIndex = myNodeIndex;
				myClient.Send (CScommon.targetNodeType, nim);
				return;
			}
//#if UNITY_EDITOR || UNITY_STANDALONE || Unity_WEBPLAYER
			if (Input.GetMouseButtonDown (0))// && myClient != null && myClient.isConnected && gameIsRunning) 
			{	//On serverside if I send my own nodeId as the target I'll have no external link
				nim.nodeIndex = GOspinner.closestBubbleIndexNumber ();
				nim.linkType = CScommon.LinkType.puller;
				if (Input.GetKey(KeyCode.Space))
					nim.linkType = CScommon.LinkType.pusher;
				myClient.Send (CScommon.targetNodeType, nim);
				audioSourceBeepSelectNodeForLink.Play ();
			}

//#elif UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE

//			if (Input.touchCount > 0 && myClient != null && myClient.isConnected && gameIsRunning) 
//			{
//				Touch myTouch = Input.touches[0];
//
//			}
//#endif
		}

		//static void requestToRotateMe(vectore3 rightClickMousePosition)
		internal static void requestToRotateMe()
		{
  
			Vector3 mousePosInWorldCordV3 = mouseWorldPostion();

			CScommon.intMsg myDesiredRotationto= new CScommon.intMsg();

			Vector3 midPointBetweenMyTails = (bubbles[myNodeIndex+1].position + bubbles[myNodeIndex+2].position)/2;

//			Vector3 midPointBetweenMyTailss = new Vector3 (midPointBetweenMyTails.x,midPointBetweenMyTails.y,0.0f);
			float angleBetween2Points  = stdAngle(angleFromTwoPoints(mousePosInWorldCordV3,midPointBetweenMyTails)- 
				angleFromTwoPoints(bubbles[myNodeIndex].position,midPointBetweenMyTails));
			Debug.Log("anglebetween2points" + angleBetween2Points);
			if(angleBetween2Points < 0){
				myDesiredRotationto.value = 1;
			}
			else if (angleBetween2Points == 0)
			{
				myDesiredRotationto.value = 0;
			}
			else 
			{
				myDesiredRotationto.value = -1;
			}
			audioSourceTurning.Play ();
			myClient.Send (CScommon.turnMsgType, myDesiredRotationto);
		}

		public static float stdAngle(float angl)
		{	while (angl < -Mathf.PI) angl += 2*Mathf.PI;
			while (angl >  Mathf.PI) angl -= 2*Mathf.PI;
			return angl;
		}

		public static int closestBubbleIndexNumber()
		{
//
//			void singleMindedNearest(int sourceId, Vector3 aScreenPosition, CScommon.LinkType linkType){
//				Ray ray = Camera.main.ScreenPointToRay (aScreenPosition);    
//				Vector3 point = ray.origin + (ray.direction * (-Camera.main.transform.position.z)); 
//				//		Vector3 point = aScreenPosition + new Vector3(Camera.main.transform.position.x,Camera.main.transform.position.y, 0);
//				int targetId = Bub.closestNodeId(point.x, point.y);

			Vector3 point = mouseWorldPostion();

			Vector2 vec = new Vector2 (point.x, point.y);	
			int closestI = -1;
			float leastDistance = 3000000000.0f; 
			for (int i = 0; i < bubbles.Length; i++) {
				
				Vector2 v2 = new Vector2 ( bubbles [i].position.x,bubbles [i].position.y) ;
				
				float distance = (vec - v2).SqrMagnitude();
				if (distance < leastDistance) {
					leastDistance = distance;
					closestI = i;
				}
				
			}
			return closestI;
		}

		public static Vector3 mouseWorldPostion()
		{
			float distancee = -mainCamera.transform.position.z;
			Ray ray = mainCamera.ScreenPointToRay (Input.mousePosition);    
			return ray.origin + (ray.direction * distancee);
		}

		static void InversInchwomrsLink(int push1Pull2Auto0Toggle3)

		{
			CScommon.intMsg myDesiredInternalLink = new CScommon.intMsg();
			myDesiredInternalLink.value = push1Pull2Auto0Toggle3;
			myClient.Send (CScommon.push1Pull2MsgType, myDesiredInternalLink);
		}

		static void inchwormForwardBackWard(int forward0Backward1Toggle2)
			
		{
			CScommon.intMsg myDesiredForwardOrBackward = new CScommon.intMsg();
			myDesiredForwardOrBackward.value = forward0Backward1Toggle2;
			myClient.Send (CScommon.forward0Reverse1Type, myDesiredForwardOrBackward);
		}


		//statistics
		private static int totMsgsLost;
		private static int numUpdates;
		
		private static void keepStats(int msgsSinceLastUpdate){
			
			totMsgsLost += msgsSinceLastUpdate-1;
			numUpdates += 1;
			
			if (numUpdates == 50){
//				Debug.Log ("avg msgs lost: "+totMsgsLost/(float)numUpdates);
				totMsgsLost = numUpdates = 0;
			}
		}
	}
}