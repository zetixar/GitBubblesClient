// Copyright 2016 John fairfield && Mohsen Danesh Pajouh
//version 001

// derived from http://docs.unity3d.com/Manual/UNetClientServer.html
// and http://forum.unity3d.com/threads/master-server-sample-project.331979/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;
using UnityEngine.Networking.Match;
using UnityEngine.EventSystems;


public class netClientMgr : MonoBehaviour {
	#region fields
	public static float nodeSclaeFactor = 1.8f;
	public static int myNodeIndex;
	public static bool stateChoosingServer = true;
	public static bool stateIsConnected = false;
	public static bool initialized = false;
	public static bool spectating = false;
	public static bool generatingLink = false;

	public InputField serverIPInputField;
//	public string serverIP = "192.168.0.2";
	public string serverIP = "52.90.140.113";//"52.91.177.74";// "127.0.0.1"; 52.90.62.24 xlargeServer //Mac: 169.254.191.54
	public InputField playerNickNameInputField;
	private string playerNickName;

	private static string myChatString;

	//add textholder for displaying chats.
	private string playersChats;

	public Button serverIPConnectButton;
	public Button serverIP192ConnectButton;
	public Button serverIPLocalConnectButton;
	public GameObject scrollViewForScaling;
	public Text scrollViewTextDebuging;
	public Text scrollViewDisplayAllChat;
	private List<string> chatHistory = new List<string>();
	private List<string> debugingDisplayHistroy = new List<string>();
	public Button sendChatButton;
	public static InputField myChatInputField;
	public GameObject scrollViewForChat;
	public GameObject chatScrollBar;
	public GameObject debugScrollBar;

	public static Slider speedSlider;
	public Text speedValueText;

	public static Button camLockBtn;
	public static Button blessingModeBtn;
	public static Button pusherLinkBtn;

	public Image minimapImage;
	public Button keyGuide;
	public Image keyGuideImage;

	static NetworkClient myClient;
	static CScommon.GameSizeMsg gameSizeMsg = new CScommon.GameSizeMsg(); //*** I'm not sure whether I need to initilize that or not

	static float camSpeed = 270.0f;
	static Camera mainCamera;

	// define the audio clips
	public AudioClip clipBeepSelectNodeForLink;
	public AudioClip clipTurning;
	public AudioClip clipEatingOthersPV;
	public AudioClip clipGetEatenByOthersPV;
	public AudioClip clipGameSizePV;

	static AudioSource audioSourceBeepSelectNodeForLink;
	static AudioSource audioSourceTurning;
	static AudioSource mainCamAudioSource;
	static AudioClip clipEatingOthers;
	static AudioClip clipGetEatenByOthers;
	static AudioClip clipGameSize;

	public Camera miniCamera;
	#endregion

	void Start()
	{
		mainCamera = Camera.main;
		serverIPInputField.placeholder.GetComponent<Text>().text = serverIP;
		serverIPInputField.text = serverIPInputField.placeholder.GetComponent<Text>().text;
		myChatInputField = GameObject.Find("ChatInput").GetComponent<InputField>();
		speedSlider = GameObject.Find("SpeedSlider").GetComponent<Slider>();
		camLockBtn = GameObject.Find("camLockBtn").GetComponent<Button>();
		blessingModeBtn = GameObject.Find("blessingModeBtn").GetComponent<Button>();
		pusherLinkBtn = GameObject.Find("pusherLinkBtn").GetComponent<Button>();

		myChatInputField.gameObject.SetActive(false);
		speedSlider.gameObject.SetActive(false);
		camLockBtn.gameObject.SetActive(false);
		blessingModeBtn.gameObject.SetActive(false);
		pusherLinkBtn.gameObject.SetActive(false);

		audioSourceBeepSelectNodeForLink = AddAudio(clipBeepSelectNodeForLink,false,false,0.5f);
		audioSourceTurning = AddAudio(clipTurning,false,false,0.15f);
		mainCamAudioSource = mainCamera.GetComponent<AudioSource>();
		clipEatingOthers = clipEatingOthersPV;//AddAudio(clipEatingOthersPV,false,false,0.5f);
		clipGetEatenByOthers = clipGetEatenByOthersPV;
		clipGameSize = clipGameSizePV;

		speedValueText.text = speedSlider.value.ToString();
		minimapImage.gameObject.SetActive(false);
		keyGuide.gameObject.SetActive(false);
		keyGuideImage.gameObject.SetActive(false);
		keyGuideImageDisplaybool = false;

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
		if (myChatInputField.text != string.Empty)
			myChatString = myChatInputField.text;
//		if (isAtStartup && Input.GetKeyDown(KeyCode.Tab) SetupClient();
		if (!myChatInputField.isFocused && Input.GetKeyDown (KeyCode.F12) && myClient != null && myClient.isConnected) 
		{
			myClient.Disconnect();
			backToChooseServerPhase();
		}
		controllingServerViaClient();
		if(myChatInputField.isFocused)
		{
			if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown ("enter") || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown ("return")) 
				sendMyChat();
		}
		if (initialized)
		{
			if(!myChatInputField.isFocused && Input.GetKeyDown(KeyCode.Tab))
				displayChatWindows(!displayingChatwindowsstatus);
			GOspinner.Update ();
		}
//#elif UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE
//#endif
	}

	// value 0 is the exception, it pauses/unpauses current game without restarting it.
	// value -1 relaunches the current game, without changing current scale values.
	// Values 1-9 select a particular predefined game setup to be launched with its default scale values.
	// Other values scale and restart the current game:
	// 21/22 scale down/up the average size (radius) of nodes, 
	// 31/32 scale down/up the ratio between the size of hunter organisms and the size of other organisms,
	// 41/42 scale down/up photoYield, the rate energy trickles into everyone's tanks, i.e. the 'starved' speed of everything
	// 51/52 scale down/up baseMetabolicRate, the base rate at which muscles consume energy, i.e. the 'fed' speed of everything
	// 61/62 scale down/up the worldRadius (which scales up/down the relative lengths of links in the world, i.e. the size of organisms )
	// 71/72 move down/up by 1/10 between 0 and 1 the fraction of their maxOomph fed to veg nodes before launch
	// 81/82 move down/up by 1/10 between 0 and 1 the fraction of their maxOomph fed to nonveg nodes before launch

	public static bool eligibleToControlServer = false;
	void controllingServerViaClient()
	{
		if (Input.GetKeyDown(KeyCode.H) && !myChatInputField.isFocused)
		{
			CScommon.intMsg gameNum = new CScommon.intMsg();
			gameNum.value = 0;
			myClient.Send(CScommon.restartMsgType, gameNum);
		}

		if (Input.GetKey(KeyCode.RightShift)&&Input.GetKeyDown(KeyCode.Slash)&& !myChatInputField.isFocused)
		{
			eligibleToControlServer = !eligibleToControlServer;
			scrollViewForScaling.SetActive(eligibleToControlServer);
			debugScrollBar.GetComponent<Scrollbar>().value = 1;
		}

		if(eligibleToControlServer && myClient != null && myClient.isConnected)
		{
			CScommon.intMsg gameNum = new CScommon.intMsg();
			gameNum.value = -10;
			if(!myChatInputField.isFocused)
			{
				if(Input.GetKeyDown(KeyCode.BackQuote)) gameNum.value = -1;
				for(int i = 0; i <10; i++) if (Input.GetKeyDown(""+i)) gameNum.value = i;
				if(Input.GetKeyDown(KeyCode.Minus)) gameNum.value = 21;
				if(Input.GetKeyDown(KeyCode.Minus)&&(Input.GetKey(KeyCode.RightShift)||Input.GetKey(KeyCode.LeftShift))) gameNum.value = 22;
				if(Input.GetKeyDown(KeyCode.Equals)) gameNum.value = 31;
				if(Input.GetKeyDown(KeyCode.Equals)&&(Input.GetKey(KeyCode.RightShift)||Input.GetKey(KeyCode.LeftShift))) gameNum.value = 32;
				if(Input.GetKeyDown(KeyCode.LeftBracket)) gameNum.value = 41;
				if(Input.GetKeyDown(KeyCode.LeftBracket)&&(Input.GetKey(KeyCode.RightShift)||Input.GetKey(KeyCode.LeftShift))) gameNum.value = 42;
				if(Input.GetKeyDown(KeyCode.RightBracket)) gameNum.value = 51;
				if(Input.GetKeyDown(KeyCode.RightBracket)&&(Input.GetKey(KeyCode.RightShift)||Input.GetKey(KeyCode.LeftShift))) gameNum.value = 52;
				if(Input.GetKeyDown(KeyCode.Backslash)) gameNum.value = 61;
				if(Input.GetKeyDown(KeyCode.Backslash)&&(Input.GetKey(KeyCode.RightShift)||Input.GetKey(KeyCode.LeftShift))) gameNum.value = 62;
				if(Input.GetKeyDown(KeyCode.Semicolon)) gameNum.value = 71;
				if(Input.GetKeyDown(KeyCode.Semicolon)&&(Input.GetKey(KeyCode.RightShift)||Input.GetKey(KeyCode.LeftShift))) gameNum.value = 72;
				if(Input.GetKeyDown(KeyCode.Quote)) gameNum.value = 81;
				if(Input.GetKeyDown(KeyCode.Quote)&&(Input.GetKey(KeyCode.RightShift)||Input.GetKey(KeyCode.LeftShift))) gameNum.value = 82;
			}
			if(gameNum.value != -10)
			{
				myClient.Send(CScommon.restartMsgType, gameNum);
				Debug.Log("gamenum.value: " +gameNum.value);
			}
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
		serverIPLocalConnectButton.gameObject.SetActive(true);
		initialized = false;
		miniCamera.gameObject.SetActive(false);
		gameSizeMsg.numNodes = 0;
		gameSizeMsg.numLinks = 0;
		if (GOspinner.bubbles != null)
			GOspinner.cleanScene ();
		GOspinner.settingUpTheScene();
		speedSlider.gameObject.SetActive(false);
		camLockBtn.gameObject.SetActive(false);
		blessingModeBtn.gameObject.SetActive(false);
		pusherLinkBtn.gameObject.SetActive(false);

		minimapImage.gameObject.SetActive(false);
		keyGuide.gameObject.SetActive(false);
		keyGuideImage.gameObject.SetActive(false);
	}

	//called by btn in the scene
	public void connectTo19216802Server()
	{
		serverIP = "169.254.191.54"; // "192.168.0.2"
		SetupClient();
		audioSourceBeepSelectNodeForLink.Play ();
	}
	public void connectToLocalHost()
	{
		serverIP = "127.0.0.1";
		SetupClient();
		audioSourceBeepSelectNodeForLink.Play ();
	}

	//Showing Msg in scrollview in playmode
	public void debugingDesplayinScrollView(string text)
	{
		debugingDisplayHistroy.Add(text);
		scrollViewTextDebuging.text = "";// if I don't do this what will happen?
		for (int i = debugingDisplayHistroy.Count; i > 0; i--)
			scrollViewTextDebuging.text += debugingDisplayHistroy[i-1] + "\n";
//		scrollViewTextDebuging.text += "\n" + text;
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

	#region Key Guide
	void OnGUI()
	{
	if (myClient != null && myClient.isConnected) 
		{
//			GUI.Label (new Rect (2, 25, 200, 100), "KeyGuide(F1)");

			if (Input.GetKey (KeyCode.F1)&&!myChatInputField.isFocused)
			GUI.Label (new Rect (2, 40, 320, 600),

				   "\nH: start the game\n"+
		           "Ctrl + LeftClick: selecting my node\n" +
		           "Ctrl + Shift + LeftClick: canceling node selection\n" +

		           "Tab: turn off/on chat windows\n" +

				   "\nCAMERA\n"+
				   "WASD: for Moving Camera\n" +
		           "Q: zoom in\n" +
		           "E: zoom out\n" +
				   "C: turning on/off camera following Player\n" +
		           "F2: focusing on my player\n" +
		           "F7: displayNames off/on\n" +

		           "\nEXTERNAL LINKS\n"+
		           "Left Click: left hand external link\n" +
				   "Right Click: right hand external link\n"+
		           "Space + Either Clicks: pusher external link\n" +
				   "N: no external link\n" +

					//draging. droping. 
					//zooming /right link/ pushpull link/ camera following me or not/ spectating?/ blessing/ moving camera/ changing my node
					//speed
					//left handed vs right hand
					//displaying the status of 
					//two finger click is right click

					//AA push/right ha
				



		           "\nTRICYCLE STEERING\n"+
				   "Z: steering the tricycle to left\n" +
		           "X: steering the tricycle to right\n" +

		           "\nMOVESPEED\n"+
		           "Mouse ScrollWheel: speed up/down\n"+
				   "B: inchworm forward/reverse toggle\n" +

					"\nShift + LeftClick: blessing that node \n(give half of my current oomph to that node)\n" +

		           "\nF12: disconnect");

			if (eligibleToControlServer && (Input.GetKey (KeyCode.RightControl) || Input.GetKey (KeyCode.LeftControl))&&!myChatInputField.isFocused)
				GUI.Label (new Rect (2, 25, 500, 600),
		           "Minus:\n"+ 
		           "scale down/up the average size (radius) of nodes\n\n" +

		           "Equals:\n"+
		           "scale down/up the ratio between the size of hunter organisms and the size of other organisms\n\n" +

		           "LeftBracket:\n"+
		           "scale down/up photoYield, the rate energy trickles into everyone's tanks,\n"+
		           "i.e. the 'starved' speed of everything\n\n" +

		           "RightBracket:\n"+ 
					"scale down/up baseMetabolicRate, the base rate at which muscles consume energy,\n" +
					"i.e. the 'fed' speed of everything\n\n" +

		           "Backslash:\n"+
		           "scale down/up the worldRadius (which scales up/down the relative lengths of links in the world,\n" +
		           "i.e. the size of organisms\n\n"+

		           "BackQuote:\n"+
		           "relaunches the current game, without changing current scale values\n\n"+

		           "Semicolon:\n"+
		           "move down/up by 1/10 between 0 and 1 the fraction of their maxOomph fed to veg nodes before launch\n\n"+

		           "Quote:\n"+
		           "move down/up by 1/10 between 0 and 1 the fraction of their maxOomph fed to nonveg nodes before launch\n\n"+
 		           "");
		}
	} 
	#endregion

	#region SetupClient
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

		myClient.RegisterHandler (CScommon.gameSizeMsgType, onGameSizeMsg );
		myClient.RegisterHandler(CScommon.initMsgType, onInitMsg);
		myClient.RegisterHandler(CScommon.nodeIdMsgType, onNodeIDMsg);
		myClient.RegisterHandler (CScommon.initRevisionMsgType, onInitRevisionMsg);
		myClient.RegisterHandler(CScommon.updateMsgType, onUpdateMsg);
		myClient.RegisterHandler (CScommon.linksMsgType, onLinksMsg);
		myClient.RegisterHandler (CScommon.nodeNamesMsgType, onNodeNamesMsg);
		myClient.RegisterHandler (CScommon.broadCastMsgType, onBroadCastMsg);
		myClient.RegisterHandler (CScommon.scaleMsgType, onScaleMsg);
		myClient.RegisterHandler (CScommon.scoreMsgType, onScoreMsg);

		myClient.Connect(serverIP, CScommon.serverPort);
		audioSourceBeepSelectNodeForLink.Play ();
	}
	#endregion

	#region Message Handlers
	public void OnConnectedC(NetworkMessage netMsg)
	{
		Debug.Log("Connected as client to server");
		Debug.Log ("conn: "+netMsg.conn +"  msgType:" +netMsg.msgType);

		serverIPInputField.gameObject.SetActive(false);
		serverIPConnectButton.gameObject.SetActive(false);
		serverIP192ConnectButton.gameObject.SetActive(false);
		serverIPLocalConnectButton.gameObject.SetActive(false);
		playerNickNameInputField.gameObject.SetActive(false);
		stateChoosingServer = false;
		initialized = false;
		eligibleToControlServer = false;
		audioSourceTurning.Play();
		debugingDisplayHistroy = new List<string>();
		chatHistory = new List<string>();
//		displayChatWindows(true);
		minimapImage.gameObject.SetActive(true);
		keyGuide.gameObject.SetActive(true);
		keyGuideImage.gameObject.SetActive(false);
		keyGuideImageDisplaybool = false;
		spectating = true;
	}

	public void OnDisconnectC(NetworkMessage info)
	{	
		Debug.Log ("OnDisconnectC: Disconnected from server.");
		backToChooseServerPhase();
	}


	public void onGameSizeMsg(NetworkMessage netMsg)
	{
		gameSizeMsg = netMsg.ReadMessage<CScommon.GameSizeMsg>();
		CScommon.stringMsg myname = new CScommon.stringMsg();
		myname.value = playerNickName;
		myClient.Send (CScommon.initRequestType, myname);
		GOspinner.cleanScene ();
		GOspinner.settingUpTheScene();
		miniCamera.gameObject.SetActive(true);

		mainCamAudioSource.clip = clipGameSize;
		mainCamAudioSource.Play();

		Debug.Log(gameSizeMsg.worldRadius);
	}

	public void onInitMsg(NetworkMessage netMsg)
	{
		CScommon.InitMsg initMsgg = netMsg.ReadMessage<CScommon.InitMsg> ();
		GOspinner.prepareForInitialize(initMsgg);
		initialized = true;
	}

	public void onNodeIDMsg(NetworkMessage netMsg){
		CScommon.intMsg nodeIndexMsg = netMsg.ReadMessage<CScommon.intMsg>();
		myNodeIndex = nodeIndexMsg.value;
		speedSlider.gameObject.SetActive(true);
		camLockBtn.gameObject.SetActive(true);
		blessingModeBtn.gameObject.SetActive(true);
		pusherLinkBtn.gameObject.SetActive(true);

		speedSlider.value = GOspinner.myInternalMusSpeed;
		spectating = false;
		GOspinner.cameraFollowMynode = true;
		if(myNodeIndex == -1)
		{
			speedSlider.gameObject.SetActive(false);
			camLockBtn.gameObject.SetActive(false);
			blessingModeBtn.gameObject.SetActive(false);
			pusherLinkBtn.gameObject.SetActive(false);

			GOspinner.cameraFollowMynode = false;
			spectating = true;
		}
		GOspinner.nodeIdMsgg();
		Debug.Log ("my nodeIndex is " + myNodeIndex);


//		debugingDesplayinScrollView ("   my nodeIndex is " + myNodeIndex);
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

	public void onLinksMsg(NetworkMessage netMsg)
	{
		// three phases for links: 
		//1- generate links list and link gameobjects > done in first call for onLinksMsg() 
		//2- update links list (updating linksMsg) and link gameobjects with their setActive, prefab  > done in onLinksMsg()
		//3- position and rotate and scale(strength) links gameobjects > done in update()
		CScommon.LinksMsg newLinkMsg = netMsg.ReadMessage<CScommon.LinksMsg>();

		//if it is the first linkMsg it will generate link
		if(generatingLink && GOspinner.links[0] == null)
		GOspinner.generateLinks(); // I do it once at the begining
		GOspinner.reassignLinksPrefabs(newLinkMsg); // Done every time that I recieve onLinkMsg to fix the prefabs for bones etc. and apply setActive
	}


	public void onNodeNamesMsg(NetworkMessage netMsg)
	{
		CScommon.NodeNamesMsg playersNameListMsg = netMsg.ReadMessage<CScommon.NodeNamesMsg>();
		GOspinner.playerNamesManage(playersNameListMsg);
	}

	public void onBroadCastMsg(NetworkMessage netMsg)
	{
		CScommon.stringMsg recievedNewChat = netMsg.ReadMessage<CScommon.stringMsg>();
		chatHistory.Add(recievedNewChat.value);
		Debug.Log (chatHistory.Count + "chathistorycount");
		scrollViewDisplayAllChat.text = "";// if I don't do this what will happen?
		for (int i = chatHistory.Count; i > 0; i--)
			scrollViewDisplayAllChat.text += chatHistory[i-1] + "\n";
		displayChatWindows(true);
	}

	public void onScaleMsg(NetworkMessage netMsg)
	{
		string scaleString = netMsg.ReadMessage<CScommon.stringMsg>().value;
		debugingDesplayinScrollView(scaleString);
	}


	public void onScoreMsg(NetworkMessage netMsg)
	{
		CScommon.ScoreMsg scoreMsg = netMsg.ReadMessage<CScommon.ScoreMsg>();
		GOspinner.scoreManager(scoreMsg);
	}

	#endregion

	#region Scene Functions
	public void sendMyChat()
	{	
		if(myChatString == string.Empty) return;
		CScommon.stringMsg myChatStringMsg = new CScommon.stringMsg();
		myChatStringMsg.value = myChatString;
		myClient.Send (CScommon.broadCastMsgType, myChatStringMsg);
		Debug.Log ("my chat sent");
		myChatInputField.text = string.Empty;
		myChatString = string.Empty;
	}

	private static bool displayingChatwindowsstatus = false;
	private void displayChatWindows(bool show)
	{
		sendChatButton.gameObject.SetActive(show);
		myChatInputField.gameObject.SetActive(show);
		scrollViewForChat.gameObject.SetActive(show);
		displayingChatwindowsstatus = show;
		chatScrollBar.GetComponent<Scrollbar>().value = 1;
	}

	//called directly from scene in unity
	public void MusSpeedControllerr(int myDesiredSpeed)
	{
		GOspinner.MusSpeedController(myDesiredSpeed);
		speedValueText.text = speedSlider.value.ToString();	
	}

	bool keyGuideImageDisplaybool = false;
	public void displayKeyGuideImage()
	{
		keyGuideImageDisplaybool = !keyGuideImageDisplaybool;
		keyGuideImage.gameObject.SetActive(keyGuideImageDisplaybool);
	}
	#endregion


//GOSPINNER *************************************** GOSPINNER\\ 
	public static class GOspinner {

		#region Fields
		public static float linkscalefactor = 20.0f;

		public static Transform pfOurGoal, pfOTeamGoal, pfNonEaterBubble, pfEaterBubble, pfReservdPlayerBubble, pfMyPlayer, pfMyTeamPlayer,
		pfOTeamPlayer, pfSnark, pfOomph, pfExPullLink, pfExPushLink, pfBoneLink, pfPlayerName;

		public static Transform[] bubbles;
		public static Transform[] oomphs; //** for displaying oomph around each bubble
		public static GameObject[] links;

		private static NetworkMessage lastNetMsg;
		private static int netMsgsSinceLastUpdate;

		public static CScommon.UpdateMsg updateMsg;
		public static CScommon.InitMsg initMsg;
		public static CScommon.LinksMsg linkMsg;
		public static Dictionary<int,CScommon.ScoreStruct> scoreMsgGOspinner; //int = nodeID
		public static Dictionary<int,System.Diagnostics.Stopwatch> stopwatches = new Dictionary<int,System.Diagnostics.Stopwatch>();
		public static Dictionary<int,Transform> playersNameTransforms;
		public static Dictionary<int,string> dicPlayerNamesIntString = new Dictionary<int, string>();
		#endregion

		#region setting up the scene
		public static void gospinnerStart()
		{
			pfExPullLink = GameObject.Find("pfExPullLink").transform;
			pfExPushLink = GameObject.Find ("pfExPushLink").transform;
			pfBoneLink = GameObject.Find ("pfBoneLink").transform;
			pfNonEaterBubble = GameObject.Find ("pfNonEaterBubble").transform;
			pfEaterBubble = GameObject.Find ("pfEaterBubble").transform;
			pfSnark = GameObject.Find ("pfSnark").transform;
			pfMyPlayer = GameObject.Find ("pfMyPlayer").transform;
			pfMyTeamPlayer = GameObject.Find ("pfMyTeamPlayer").transform;
			pfOTeamPlayer = GameObject.Find ("pfOTeamPlayer").transform;
			pfOomph = GameObject.Find ("pfOomph").transform;
			pfReservdPlayerBubble = GameObject.Find ("pfReservdPlayerBubble").transform;
			pfOurGoal = GameObject.Find ("pfOurGoal").transform;
			pfOTeamGoal = GameObject.Find ("pfOTeamGoal").transform;
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



		public static void cleanScene()
		{	
			if(GOspinner.bubbles != null && bubbles.Length > 0)
			{
				for (int i = 0; i < bubbles.Length; i++)
					if(bubbles[i] != null)
					Destroy(bubbles[i].gameObject);
				for (int i = 0; i < links.Length; i++)
					if(links[i] != null)
					Destroy(links[i].gameObject);
			}
			GameObject[] bubblesClone = GameObject.FindGameObjectsWithTag ("bblClone");
			foreach (GameObject bubbleClone in bubblesClone)
				Destroy (bubbleClone);
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
			GOspinner.bubbles = new Transform[gameSizeMsg.numNodes];
			GOspinner.oomphs = new Transform[gameSizeMsg.numNodes];
			GOspinner.links = new GameObject[gameSizeMsg.numLinks];


			GOspinner.initMsg = new CScommon.InitMsg();
			GOspinner.initMsg.nodeData = new CScommon.StaticNodeData[gameSizeMsg.numNodes];
			
			GOspinner.updateMsg = new CScommon.UpdateMsg();
			GOspinner.updateMsg.nodeData = new CScommon.DynamicNodeData[gameSizeMsg.numNodes];
			
			GOspinner.linkMsg = new CScommon.LinksMsg();
			GOspinner.linkMsg.links = new CScommon.LinkInfo[gameSizeMsg.numLinks];
			
			GOspinner.dicPlayerNamesIntString = new Dictionary<int, string>();
			GOspinner.playersNameTransforms = new Dictionary<int, Transform>();
			GOspinner.scoreMsgGOspinner = new Dictionary<int, CScommon.ScoreStruct>();
			GOspinner.stopwatches = new Dictionary<int,System.Diagnostics.Stopwatch>();

			GOspinner.resetGameStats();

		}
		
		internal static void resetGameStats() {
			myNodeIndex = -1;
			mainCamera.orthographicSize = 250.0f;
			mainCamera.transform.position = new Vector3(0.0f,0.0f,-100.0f);
			cameraZoomOutAtStart = 80;
			generatingLink = true;
			initialized = false;
			cameraFollowMynode = false;
			cameraFollowNodeIndex = 0;
			followingCamera = false;
			displayNames = 2;
			changeHowToDisPlayPlayersName();
		}
		#endregion

		#region prefab manager
		public static void nodePrefabCheck(int i, bool add)
		{
			CScommon.StaticNodeData nd = initMsg.nodeData[i];
			string goalTag = "GoalClone";
			string goalName = "Goal";
			string playerTag = "Player";
			string playerName = "MyPlayer";
			string bblCloneTag = "bblClone";
			string bblCloneName = "bbl";
//			if (i == 0) {
//				managePrefabTagNameBoolAddtrueRepfalse (i, pfOurGoal, goalTag, goalName, add);
//				return;
//			}
			if (i == myNodeIndex) {
				managePrefabTagNameBoolAddtrueRepfalse (i, pfMyPlayer, playerTag, playerName, add);
				return;
			} else if (CScommon.testBit (nd.dna, CScommon.snarkBit)){	
				managePrefabTagNameBoolAddtrueRepfalse (i, pfSnark, bblCloneTag, bblCloneName, add);
				return;
			} 


			else if (CScommon.testBit (nd.dna, CScommon.goalBit)) 
			{
				if (teamNumCheck(nd.dna) == 1)
				{
					managePrefabTagNameBoolAddtrueRepfalse (i, pfOurGoal, goalTag, goalName, add);
					return;
				}
				else
					managePrefabTagNameBoolAddtrueRepfalse (i, pfOTeamGoal, bblCloneTag, bblCloneName, add);
				return;
			} 


			else if (teamNumCheck(nd.dna) == 1)
			{
				Debug.Log("teamnum" + teamNumCheck(nd.dna));// + " "+ teamNumCheck(initMsg.nodeData[myNodeIndex].dna));
				managePrefabTagNameBoolAddtrueRepfalse (i, pfMyTeamPlayer, bblCloneTag, bblCloneName, add);
				return;
			}
			else if(teamNumCheck(nd.dna) > 1)
			{
				managePrefabTagNameBoolAddtrueRepfalse (i, pfOTeamPlayer, bblCloneTag, bblCloneName, add);
				return;
			}

//
//			else if (CScommon.testBit (nd.dna, CScommon.playerPlayingBit)) 
//			{
////				if (myNodeIndex >= 0 && (teamNumCheck(nd.dna) == teamNumCheck(initMsg.nodeData[myNodeIndex].dna)))
//				if (teamNumCheck(nd.dna) == 1)
//				{
//					Debug.Log("teamnum" + teamNumCheck(nd.dna));// + " "+ teamNumCheck(initMsg.nodeData[myNodeIndex].dna));
//					managePrefabTagNameBoolAddtrueRepfalse (i, pfMyTeamPlayer, bblCloneTag, bblCloneName, add);
//					return;
//				}
////				else if (teamNumCheck(nd.dna) == 4)
////				{
////					
////				}
//				Debug.Log("teamnum" + teamNumCheck(nd.dna));// + " "+ teamNumCheck(initMsg.nodeData[myNodeIndex].dna) );
//				managePrefabTagNameBoolAddtrueRepfalse (i, pfOTeamPlayer, bblCloneTag, bblCloneName, add);
//				return;
//			}
			else if (CScommon.testBit (nd.dna, CScommon.playerBit)) { // && !CScommon.testBit (nd.dna, CScommon.playerPlayingBit)) {
				managePrefabTagNameBoolAddtrueRepfalse (i, pfReservdPlayerBubble, bblCloneTag, bblCloneName, add);
				return;

			}

//			else if (CScommon.testBit (nd.dna, CScommon.goalBit)) 
//			{
//				if (myNodeIndex >= 0 && (teamNumCheck(nd.dna) == teamNumCheck(initMsg.nodeData[myNodeIndex].dna)))
//				{
//					managePrefabTagNameBoolAddtrueRepfalse (i, pfOurGoal, goalTag, goalName, add);
//					return;
//				}
//				else
//					managePrefabTagNameBoolAddtrueRepfalse (i, pfOTeamGoal, bblCloneTag, bblCloneName, add);
//				return;
//			} 
			else if (CScommon.testBit (nd.dna, CScommon.eaterBit)) {
				managePrefabTagNameBoolAddtrueRepfalse (i, pfEaterBubble, bblCloneTag, bblCloneName, add);
				return;

			} else if (!CScommon.testBit (nd.dna, CScommon.eaterBit)){	
				managePrefabTagNameBoolAddtrueRepfalse (i, pfNonEaterBubble, bblCloneTag, bblCloneName, add);
				return;
			}
		}
		private static long teamNumCheck(long dna)
		{
			return CScommon.dnaNumber(dna, CScommon.leftTeamBit,CScommon.rightTeamBit);
		}

		private static void managePrefabTagNameBoolAddtrueRepfalse( int i, Transform prefab, string tag, string name, bool addorreplace)
		{
			CScommon.StaticNodeData nd = initMsg.nodeData[i];
			if (addorreplace){
//				bubbles.Add ((Transform)Instantiate (prefab, Vector3.zero, Quaternion.identity));
				bubbles [i] = ((Transform)Instantiate (prefab, Vector3.zero, Quaternion.identity));}

			else
				{bubbles [i] = ((Transform)Instantiate (prefab, Vector3.zero, Quaternion.identity));}
			bubbles[i].localScale = new Vector3(nd.radius*nodeSclaeFactor, nd.radius *nodeSclaeFactor, nd.radius*nodeSclaeFactor);
			bubbles[i].tag = tag;
			bubbles[i].name = name + i;
		}

		#endregion
		public static void nodeIdMsgg()
		{
//			if(dicPlayerNamesIntString.ContainsKey(myNodeIndex))
//			{
//				Destroy(playersNameTransforms[myNodeIndex].gameObject);
//				dicPlayerNamesIntString.Remove(myNodeIndex);
//				playersNameTransforms.Remove(myNodeIndex);
//			}
			//updating initmsg with new changes that I get from initrevmessage
			if (bubbles[myNodeIndex].gameObject != null)
			//destorying gameobjects that are going to be updated
			Destroy (bubbles[myNodeIndex].gameObject);
			//instantiating new gameobjects according to initRivisionMsg
			nodePrefabCheck(myNodeIndex, false);
		}


		#region initRev & updateMsg manager
		public static void initRevisionMsgg(CScommon.InitRevisionMsg initrevmassege)
		{
			for (int i = 0; i < initrevmassege.nodeInfo.Length; i++) 
			{
				int j = initrevmassege.nodeInfo [i].nodeIndex;
				//For destroying previous playerName game object and also cleaning the list of names.
				if(dicPlayerNamesIntString.ContainsKey(j)&& 
				   !CScommon.testBit (initrevmassege.nodeInfo [i].staticNodeData.dna, CScommon.playerPlayingBit))
				{
					Destroy(playersNameTransforms[j].gameObject);
					dicPlayerNamesIntString.Remove(j);
					playersNameTransforms.Remove(j);
				}
				//updating initmsg with new changes that I get from initrevmessage
				initMsg.nodeData [j] = initrevmassege.nodeInfo [i].staticNodeData;
				//destorying gameobjects that are going to be updated
				Destroy (bubbles[j].gameObject);
				//instantiating new gameobjects according to initRivisionMsg
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

		#endregion

		#region LINKS
		public static void generateLinks()
		{
			//linkMsg.links.Length >> I got this data during gameSizeMsg()
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
					Destroy(links[linkinfo.linkId].gameObject);
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
					 bubbles[linkInfo.linkData.targetId].position)/2.0f;

//public static float rawStrength(float oomph, float maxOomph, long dna, float radiusSquared, float linkLengthSquared)
//			links[linkInfo.linkId].transform.localScale = new Vector3(
///*vectore3.x*/		((CScommon.rawStrength
//					(updateMsg.nodeData[linkInfo.linkData.sourceId].oomph,
//					CScommon.maxOomph(initMsg.nodeData[linkInfo.linkData.sourceId].radius,0L),
//					initMsg.nodeData[linkInfo.linkData.sourceId].dna,
//					Mathf.Pow (initMsg.nodeData[linkInfo.linkData.sourceId].radius,2.0f),
//					distance2(bubbles[linkInfo.linkData.sourceId].position, bubbles[linkInfo.linkData.targetId].position))))
//					* linkscalefactor,
///*vectore3.y*/		(bubbles[linkInfo.linkData.sourceId].position - bubbles[linkInfo.linkData.targetId].position).magnitude * 1.2f,
///*vectore3.z*/		0.0f);
			links[linkInfo.linkId].transform.localScale = new Vector3(
					initMsg.nodeData[linkInfo.linkData.sourceId].radius,
/*vectore3.y*/		(bubbles[linkInfo.linkData.sourceId].position - bubbles[linkInfo.linkData.targetId].position).magnitude * 0.17f,// * 1.2f,
//					(bubbles[linkInfo.linkData.sourceId].position - bubbles[linkInfo.linkData.targetId].position).magnitude,
/*vectore3.z*/		0.0f);


			links[linkInfo.linkId].transform.LookAt(bubbles[linkInfo.linkData.targetId].transform);
			links[linkInfo.linkId].transform.Rotate(0.0f,90.0f,90.0f);
			}
		}

		private static float distance2(Vector3 source, Vector3 target){
			return (target.x-source.x)*(target.x-source.x) + (target.y-source.y)*(target.y-source.y);
		}

		#endregion

		#region positioning
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
//						else if (CScommon.testBit (initMsg.nodeData[i].dna, CScommon.playerPlayingBit))
//						{
//							playersNameTransforms[i].FindChild("playerNameMiniMap").GetComponent<TextMesh>().color =
//								Color.grey;
//							playersNameTransforms[i].FindChild("playerNameMainCam").GetComponent<TextMesh>().color =
//								Color.grey;
//							continue;
//						}
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
				playersNameTransforms[playerID].position = new Vector2(bubbles [playerID].position.x,bubbles [playerID].position.y + initMsg.nodeData[playerID].radius* 1.20f) ;
			}
		}

		private static float angle = 0.0f;
		private static float angleFromTwoPoints (Vector3 b, Vector3 a)
		{
			angle = Mathf.Atan2 (a.y - b.y, a.x - b.x);
			angle = stdAngle(angle);
			return angle;
		}

		#endregion

		#region displayNames
		internal static void playerNamesManage (CScommon.NodeNamesMsg partofnames)
		{
			for (int i = 0; i < partofnames.arry.Length; i++)
			{
				int nodeId = partofnames.arry[i].nodeId;

				if(GOspinner.dicPlayerNamesIntString.ContainsKey(nodeId))
					GOspinner.dicPlayerNamesIntString.Remove(nodeId);
				if(GOspinner.playersNameTransforms.ContainsKey(nodeId))
				{
					Destroy(playersNameTransforms[nodeId].gameObject);
					playersNameTransforms.Remove(nodeId);
				}
				if(partofnames.arry[i].name == string.Empty)continue;

				GOspinner.dicPlayerNamesIntString.Add(nodeId,partofnames.arry[i].name);
				playersNameTransforms.Add
				(nodeId,((Transform)Instantiate (pfPlayerName, bubbles[nodeId].position, Quaternion.identity)));
				playersNameTransforms[nodeId].FindChild("playerNameMainCam").GetComponent<TextMesh>().text =
					GOspinner.dicPlayerNamesIntString[nodeId];
				playersNameTransforms[nodeId].FindChild("playerNameMiniMap").GetComponent<TextMesh>().text =
					GOspinner.dicPlayerNamesIntString[nodeId];
				playersNameTransforms[nodeId].name = "playerName" + nodeId +" " + GOspinner.dicPlayerNamesIntString[nodeId];
				playersNameTransforms[nodeId].tag = "PlayerName";
			}
			changeHowToDisPlayPlayersName();
		}

		public static void scoreManager(CScommon.ScoreMsg scoreMsg)
		{

			for(int i=0; i < scoreMsg.arry.Length; i++)
			{
				int nodeId = scoreMsg.arry[i].nodeId;

				if(scoreMsgGOspinner.ContainsKey(nodeId)) 
				{
					scoreMsgGOspinner.Remove(nodeId); 
				}
				if(nodeId == myNodeIndex)
				{
					if(scoreMsg.arry[i].neither0Winner1Loser2 == 1)
					{
						bubbles[nodeId].GetComponent<AudioSource>().clip = clipEatingOthers;
						bubbles[nodeId].GetComponent<AudioSource>().Play();
					}
					if(scoreMsg.arry[i].neither0Winner1Loser2 == 2)
					{
						bubbles[nodeId].GetComponent<AudioSource>().clip = clipGetEatenByOthers;
						bubbles[nodeId].GetComponent<AudioSource>().Play();
					}
				}

				scoreMsgGOspinner.Add(nodeId,scoreMsg.arry[i]);// I don't need to delete any score from this dictionary

				if(!stopwatches.ContainsKey(nodeId))
				{
					System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
					stopwatches.Add(nodeId,stopwatch);
				}
				stopwatches[nodeId].Reset();
				stopwatches[nodeId].Start ();
				//				if(!scoreMsg.zeroAteOne)continue;
				//because even if some body go out of the game I set that node to have no name and if some body else take the same node
				//I'll get scoreMsg with zero for this node and I'll replace previous score with new one which is zero.
				displayNameChanger(nodeId);
			}
		}

		public static void displayNamesManager()
		{
			foreach (int i in scoreMsgGOspinner.Keys)
			{
				displayNameChanger(i);
			}
		}
		public static void displayNameChanger(int nodeId)
		{
			playersNameTransforms[nodeId].FindChild("playerNameMainCam").GetComponent<TextMesh>().text =
				" " + GOspinner.dicPlayerNamesIntString[nodeId] +
				"\n +" + scoreMsgGOspinner[nodeId].plus.ToString()+"    -" + scoreMsgGOspinner[nodeId].minus.ToString();
			//					+ " P" + currentPerformance(nodeId).ToString();
			playersNameTransforms[nodeId].FindChild("playerNameMiniMap").GetComponent<TextMesh>().text =
				" " + GOspinner.dicPlayerNamesIntString[nodeId]+": +" + scoreMsgGOspinner[nodeId].plus.ToString()+"    -" + scoreMsgGOspinner[nodeId].minus.ToString();
			//					+ " P" + currentPerformance(nodeId).ToString();
		}

		public static float currentPerformance(int nodeId)
		{
			long delta = stopwatches[nodeId].ElapsedMilliseconds; //the amount of time, in milliseconds, since you last received a scoreMsg for this player
			return Mathf.Round(scoreMsgGOspinner[nodeId].performance * Mathf.Pow(2,-delta/CScommon.performanceHalfLifeMilliseconds)* 100f) / 100f;
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

		#endregion

		#region Gospinner.Update

		static bool followingCamera = false;
		static int cameraZoomOutAtStart = 80;
		static int cameraFollowNodeIndex = 0;
		static int steeringCounter = 5;
		static int displayNameCounter = 10;

		public static void Update()
		{	
//			if (cameraZoomOutAtStart > 0)
//			{
//				mainCamera.orthographicSize -= 13.0f;
//				cameraZoomOutAtStart --;
//				return;
//			}
			//Ctrl + Click = choose my node, Ctrl + Shift + Click = dismount and be spectator
			if ((Input.GetKey (KeyCode.LeftControl)|| Input.GetKey (KeyCode.LeftControl)) && Input.GetMouseButtonUp (0) && Input.touchCount < 2
				&& !EventSystem.current.IsPointerOverGameObject())
//			    &&!myChatInputField.isFocused)
				ChoosingMyNode ();
			cameraMover ();
			positioning ();
			updateLinksPosRotScale();

			if(displayNameCounter == 0)
			{
				displayNamesManager();
				displayNameCounter = 10;
			}
			else displayNameCounter--;

			if(!myChatInputField.isFocused)
			{
				if (Input.GetKeyDown(KeyCode.F7))
				{
					displayNames++;
					if (displayNames > 3) displayNames = 0;
					changeHowToDisPlayPlayersName();
				}
	// all the things bellow this line are for player and all the things above it are for both player and spectator
				if (myNodeIndex < 0) return;

				//reversing my internal mus speed
				if (Input.GetKeyDown(KeyCode.B))
				{
					MusSpeedController(-2 * myInternalMusSpeed);
				}

				//turn to left
				if (Input.GetKeyDown(KeyCode.U))
				{
					CScommon.intMsg myDesiredRotationto= new CScommon.intMsg();
					myDesiredRotationto.value = 1;
					myClient.Send (CScommon.turnMsgType, myDesiredRotationto);
					Debug.Log ("Turn to Left");
				}
				//turn to right
				if (Input.GetKeyDown(KeyCode.I))
				{
					CScommon.intMsg myDesiredRotationto= new CScommon.intMsg();
					myDesiredRotationto.value = -1;
					myClient.Send (CScommon.turnMsgType, myDesiredRotationto);
					Debug.Log ("Turn to Right");
				}

				//rotate to left
				if (Input.GetKey(KeyCode.Z))
				{
					if (Input.GetKeyDown(KeyCode.Z))steeringCounter = 1;
					if(steeringCounter == 0)requestToRotateMe(-1);
					steeringCounter--;
					if(steeringCounter < 0)steeringCounter = 5;
					Debug.Log(steeringCounter);
				}
				//rotate to right
				if (Input.GetKey(KeyCode.X))
				{
					if (Input.GetKeyDown(KeyCode.X))steeringCounter = 1;
					if(steeringCounter == 0)requestToRotateMe(1);
					steeringCounter--;
					if(steeringCounter < 0)steeringCounter = 5;
				}
			}
			if (Input.GetAxis("Mouse ScrollWheel") > 0)
			{
				MusSpeedController(10);
			}
			if (Input.GetAxis("Mouse ScrollWheel") < 0)
			{
				MusSpeedController(-10);
			}
			requestLinktoTarget ();
			requestToBlessThatNode();
		}

		#endregion

		#region MusSpeedController

		public static int myInternalMusSpeed = 80;
		//static int myExternalMusSpeed = 80;
		public static void MusSpeedController(int increaseOrDecreaseSpeed)
		{
			CScommon.intMsg myDesiredSpeed= new CScommon.intMsg();

			myInternalMusSpeed = (int)speedSlider.value;//+= increaseOrDecreaseSpeed;
			if(increaseOrDecreaseSpeed != 0)
			{
				myInternalMusSpeed += increaseOrDecreaseSpeed;
			}
			else
			{
				myInternalMusSpeed = (int)speedSlider.value;//+= increaseOrDecreaseSpeed;
			}
			if (-300 > myInternalMusSpeed)	myInternalMusSpeed = -300;
			if (myInternalMusSpeed > 300) myInternalMusSpeed = 300;

			myDesiredSpeed.value = myInternalMusSpeed;
			myClient.Send (CScommon.speedMsgType, myDesiredSpeed);
			speedSlider.value = myInternalMusSpeed;
			Debug.Log ("My Internal Mus Speed: " + myInternalMusSpeed);
		}
	

		#endregion

		#region cameraMover

		public static bool cameraFollowMynode = false;
		static float mainCamMoveSpeed = 3.0f;
//** need to clamp the camera so it cannot go over up/down/right/left.
		static void cameraMover()
		{
//#if UNITY_STANDALONE || Unity_WEBPLAYER

			if (Input.GetAxis("Horizontal") != 0.0f &&!myChatInputField.isFocused)
			{
				mainCamera.transform.Translate(new Vector3((Input.GetAxis("Horizontal"))*camSpeed * Time.deltaTime,0,0));
			}
			if (Input.GetAxis("Vertical") != 0.0f&&!myChatInputField.isFocused)
			{
				mainCamera.transform.Translate(new Vector3(0,(Input.GetAxis("Vertical"))*camSpeed * Time.deltaTime,0));
			}
			if (cameraFollowMynode && myNodeIndex >= 0 && bubbles[myNodeIndex].gameObject != null)
			{	Vector3 playerDefualtCamPos = new Vector3 
					(bubbles[myNodeIndex].transform.position.x,
					 bubbles[myNodeIndex].transform.position.y,
					 -100);
				mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position,playerDefualtCamPos,Time.deltaTime * mainCamMoveSpeed);
			}
			if (Input.GetKeyDown (KeyCode.C) && myNodeIndex != -1&&!myChatInputField.isFocused)
			{
				cameraFollowMynode = !cameraFollowMynode;
				camLockBtn.image.color = cameraFollowMynode ? Color.green : Color.grey;
			}

			if (Input.GetKey (KeyCode.Q)&&!myChatInputField.isFocused)
				zoomIn (2f);
			else if (Input.GetKey(KeyCode.E)&&!myChatInputField.isFocused)
				 zoomOut (2f);


//#elif UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE  ||  UNITY_EDITOR
			if (Input.touchCount == 1 && !EventSystem.current.IsPointerOverGameObject())
			{
				cameraFollowMynode = false;
				Touch touchZero = Input.GetTouch(0);
				if(touchZero.phase == TouchPhase.Moved)
				{
					Vector2 prvsPos = touchZero.position - touchZero.deltaPosition;
					mainCamera.transform.Translate(touchZero.deltaPosition * -1f);//touchZero.deltaPosition * -0.02f);
					Debug.Log("touchZero.deltaPosition: " + touchZero.deltaPosition +"touchZero.position: "+ touchZero.position + "prvsPos: " + prvsPos );
				}
			}

			if (Input.touchCount == 2) 
			{
				cameraFollowMynode = false;
				Vector2 cameraViewSize = new Vector2 (mainCamera.pixelWidth, mainCamera.pixelHeight);
				Touch touchZero = Input.GetTouch(0);
				Touch touchOne = Input.GetTouch(1);

				// Find the position in the previous frame of each touch.
				Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
				Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

				// Find the magnitude of the vector (the distance) between the touches in each frame.
				float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
				float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

				// Find the difference in the distances between each frame.
				float deltaMagnitudeDiff = (prevTouchDeltaMag - touchDeltaMag) / 6.0f;
				Debug.Log("pinch for mobile zooming" + deltaMagnitudeDiff);

				mainCamera.transform.position += mainCamera.transform.TransformDirection((touchZeroPrevPos + touchOnePrevPos - cameraViewSize) * mainCamera.orthographicSize / cameraViewSize.y);
				if (deltaMagnitudeDiff > 0.0f)
				{
					zoomOut(deltaMagnitudeDiff);
				}
				if (deltaMagnitudeDiff < 0.0f)
				{
					zoomIn(deltaMagnitudeDiff * -1);
				}
				mainCamera.transform.position -= mainCamera.transform.TransformDirection((touchZero.position + touchOne.position - cameraViewSize) * mainCamera.orthographicSize / cameraViewSize.y);
			}
//#endif

//			 to let Spectator follow one node
			if (spectating)
			{
				if(Input.anyKey && !Input.GetMouseButton(0) && !Input.GetKey(KeyCode.Q) && !Input.GetKey(KeyCode.E)&&!myChatInputField.isFocused)
					followingCamera = false;

				if (Input.GetMouseButtonUp (0) && Input.touchCount < 2 && !myChatInputField.isFocused)
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
			if (Input.GetKeyDown(KeyCode.F2)&& myNodeIndex != -1 && bubbles[myNodeIndex].gameObject != null&&!myChatInputField.isFocused)
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

		#endregion

		#region zoomIn, zoomOut
		static void zoomIn(float camorthsizeminus) {
			if (mainCamera.orthographicSize > 14.0f)
			{
				if(mainCamera.orthographicSize < 100)
				{
					mainCamera.orthographicSize -= (camorthsizeminus / 2.0f);
				}
				else
					mainCamera.orthographicSize -= camorthsizeminus;
				if (mainCamera.orthographicSize < 14.0f) mainCamera.orthographicSize = 14.0f;
			}
		}
		
		static void zoomOut(float camorthsizeplus) {
			if (mainCamera.orthographicSize < 1100.0f)
			{	
				if(mainCamera.orthographicSize < 100)
				{
					mainCamera.orthographicSize += (camorthsizeplus / 2.0f);
				}
			else
				mainCamera.orthographicSize += camorthsizeplus;
				if (mainCamera.orthographicSize > 1100.0f) mainCamera.orthographicSize = 1100.0f;
			}
		}

		static void zoomInMobile(float camorthsizplus)
		{
			cameraFollowMynode = false;

		}
		static void zoomOutMobile(float camorthsizplus)
		{

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
		#endregion

		#region choosingMyNode
		static void ChoosingMyNode()
		{
			CScommon.intMsg myDesiredNodeIndexNumber= new CScommon.intMsg();
			if ((Input.GetKey (KeyCode.LeftShift)|| Input.GetKey(KeyCode.RightShift))&&!myChatInputField.isFocused)
				myDesiredNodeIndexNumber.value = -1;
			else 
				myDesiredNodeIndexNumber.value = GOspinner.closestBubbleIndexNumber ();
			myClient.Send (CScommon.requestNodeIdMsgType, myDesiredNodeIndexNumber);
			Debug.Log("my desired node " + myDesiredNodeIndexNumber.value);
			
		}
		#endregion

		#region requestLinktoTarget

		public static bool pushLinkMode = false;
		//for requesting to have a specefic type of a link from 'me' to the node that is closest node 
		//to the position that I have clicked on.
		internal static void requestLinktoTarget()
		{
			CScommon.TargetNodeMsg nim = new CScommon.TargetNodeMsg ();

			//We should destory all the links by pressing N When I send hand 100
			if (Input.GetKeyDown(KeyCode.N)&&!myChatInputField.isFocused)
			{
				nim.nodeIndex = myNodeIndex;
				nim.hand = 0;
				myClient.Send (CScommon.targetNodeType, nim);
				nim.nodeIndex = myNodeIndex;
				nim.hand = 1;
				myClient.Send (CScommon.targetNodeType, nim);
				return;
			}
//#if UNITY_EDITOR || UNITY_STANDALONE || Unity_WEBPLAYER
			if ((Input.GetMouseButtonUp (0) || Input.GetMouseButtonUp (1)) && Input.touchCount < 2
			    && !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				&& (!EventSystem.current.IsPointerOverGameObject() || pushLinkMode))

//			if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved)
//			{
//				return;
//			}

//			    && !myChatInputField.isFocused)

				// && myClient != null && myClient.isConnected && gameIsRunning) 
			{	//On serverside if I send my own nodeId as the target I'll have no external link

				Debug.Log("inputcount = " + Input.touchCount);
				nim.nodeIndex = GOspinner.closestBubbleIndexNumber ();
				nim.linkType = CScommon.LinkType.puller;
				nim.hand = 0;
				if (Input.GetMouseButtonDown (1))nim.hand = 1;
				if ((Input.GetKey(KeyCode.Space) || pushLinkMode) &&!myChatInputField.isFocused)
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

		#endregion

		#region requestToBlessThatNode

		public static bool blessingMode = false;

		internal static void requestToBlessThatNode()
		{
			if (Input.GetMouseButtonUp (0) && Input.touchCount < 2
				&& ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
					&& !myChatInputField.isFocused) ||
				(Input.touchCount == 2 && blessingMode))     // && myClient != null && myClient.isConnected && gameIsRunning) 
			{
				CScommon.intMsg nim = new CScommon.intMsg ();
				nim.value = GOspinner.closestBubbleIndexNumber ();
				myClient.Send (CScommon.blessMsgType, nim);
				Debug.Log("bless" +nim.value);

			//	audioSourceBeepSelectNodeForLink.Play ();
			}
		}
		#endregion

		#region requestToRotateMe
		internal static void requestToRotateMe(int rotateToLminus1Rplus1)
		{
//			Vector3 mousePosInWorldCordV3 = mouseWorldPostion();

			CScommon.intMsg myDesiredRotationto= new CScommon.intMsg();
			myDesiredRotationto.value = rotateToLminus1Rplus1;

			//			Vector3 midPointBetweenMyTails = (bubbles[myNodeIndex+1].position + bubbles[myNodeIndex+2].position)/2;
//
////			Vector3 midPointBetweenMyTailss = new Vector3 (midPointBetweenMyTails.x,midPointBetweenMyTails.y,0.0f);
//			float angleBetween2Points  = stdAngle(angleFromTwoPoints(mousePosInWorldCordV3,midPointBetweenMyTails)- 
//				angleFromTwoPoints(bubbles[myNodeIndex].position,midPointBetweenMyTails));
//			Debug.Log("anglebetween2points" + angleBetween2Points);
//			if(angleBetween2Points < 0){
//				myDesiredRotationto.value = 1;
//			}
//			else if (angleBetween2Points == 0)
//			{
//				myDesiredRotationto.value = 0;
//			}
//			else 
//			{
//				myDesiredRotationto.value = -1;
//			}
			audioSourceTurning.Play ();
			myClient.Send (CScommon.turnMsgType, myDesiredRotationto);
		}
		#endregion

		#region handyfunctions

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

//		static void InversInchwomrsLink(int push1Pull2Auto0Toggle3)
//
//		{
//			CScommon.intMsg myDesiredInternalLink = new CScommon.intMsg();
//			myDesiredInternalLink.value = push1Pull2Auto0Toggle3;
//			myClient.Send (CScommon.push1Pull2MsgType, myDesiredInternalLink);
//		}
//
//		static void inchwormForwardBackWard(int forward0Backward1Toggle2)
//			
//		{
//			CScommon.intMsg myDesiredForwardOrBackward = new CScommon.intMsg();
//			myDesiredForwardOrBackward.value = forward0Backward1Toggle2;
//			myClient.Send (CScommon.forward0Reverse1Type, myDesiredForwardOrBackward);
//		}


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
		#endregion
	}
}

