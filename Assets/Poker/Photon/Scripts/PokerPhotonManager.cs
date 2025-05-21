#if PHOTON_UNITY_NETWORKING
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerBot
{
    public string NickName;
    public int ActorNumber;

    public PlayerBot(string name, int number)
    {
        NickName = name;
        ActorNumber = number;
    }
}

public class PokerPhotonManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private PhotonUI m_PhotonUI;
    [SerializeField] private BackUI m_BackUI;
    [SerializeField] private PhotonServerSimulator m_PhotonServerSimulator;

    private string playerName;
    private PhotonView netView;
    private bool isGameLoaded;
    private int readyPlayersCount;

    private List<PlayerBot> playerBots;

    private int lastPlayerNumber;

    private void Awake()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }

        m_PhotonUI.OnLogInPlayerButton += OnLogInPlayerButton;
        m_PhotonUI.OnCreateRoomButton += OnCreateRoomButton;
        m_PhotonUI.OnJoinRoomButton += OnJoinRoomButton;
        m_PhotonUI.OnStartGameButton += OnStartGameButton;

        m_PhotonUI.OnAddBotButton += OnAddBotButton;

        netView = gameObject.AddComponent<PhotonView>();
        netView.ViewID = 1;
        netView.Synchronization = ViewSynchronization.Off;
        netView.ObservedComponents = new List<Component> { this, m_PhotonServerSimulator };
    }

    private int PlayersCount => PhotonNetwork.CurrentRoom.PlayerCount + (playerBots != null ? playerBots.Count : 0);

    [PunRPC]
    public void LoadGameScene()
    {
        isGameLoaded = true;

        m_PhotonUI.gameObject.SetActive(false);
        m_BackUI.gameObject.SetActive(false);

        StartCoroutine(WaitingForSceneLoading());        
    }

    [PunRPC]
    public void OnPlayerReady(Player player)
    {
        readyPlayersCount++;
        if(readyPlayersCount + 1 >= PlayersCount)
        {
            m_PhotonServerSimulator.StartGameMaster(playerBots);
        }
    }    

    private IEnumerator WaitingForSceneLoading()
    {
        yield return SceneManager.LoadSceneAsync(MenuUI.GameScenName, LoadSceneMode.Additive);
        yield return new WaitForEndOfFrame();

        m_PhotonServerSimulator.Init(netView);

        if(!PhotonNetwork.IsMasterClient)
        {
            netView.RPC("OnPlayerReady", PhotonNetwork.MasterClient, PhotonNetwork.LocalPlayer);
        }
        else
        {
            for (int i = 0; i < playerBots.Count; i++)
            {
                OnPlayerReady(null);
            }
        }
    }

    public override void OnLeftLobby()
    {
        Debug.Log("OnLeftLobby");
        m_PhotonUI.gameObject.SetActive(true);
        m_BackUI.gameObject.SetActive(true);
        m_PhotonUI.DisconnectedFromServer();

        if (isGameLoaded)
        {
            isGameLoaded = false;
            SceneManager.UnloadSceneAsync(MenuUI.GameScenName);
        }
    }

    private void OnDestroy()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
    }

    private void OnStartGameButton()
    {
        Debug.Log("OnStartGameButton " + PlayersCount);
        if(PlayersCount >= PhotonNetwork.CurrentRoom.MaxPlayers)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            isGameLoaded = true;
            netView.RPC("LoadGameScene", RpcTarget.All);
        }
        else
        {
            Debug.LogWarning("Players count less than 3");
        }
    }

    private void OnLogInPlayerButton(string playerName)
    {
        this.playerName = playerName;
        PhotonNetwork.ConnectUsingSettings();
    }

    private void OnCreateRoomButton(string roomName, int maxPlayers)
    {
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = (byte)maxPlayers
        };
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    private void OnJoinRoomButton(PokerRoomInfo pokerRoomInfo)
    {
        PhotonNetwork.JoinRoom(pokerRoomInfo.name);
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.LocalPlayer.NickName = playerName;
        
        PhotonNetwork.JoinLobby();
        Debug.Log("OnConnectedToMaster");
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("OnJoinedLobby");
        playerBots = new List<PlayerBot>();
        
        m_PhotonUI.ConnectedToServer();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("OnPlayerEnteredRoom " + newPlayer.NickName);
        lastPlayerNumber = newPlayer.ActorNumber;
        if (PlayersCount == PhotonNetwork.CurrentRoom.MaxPlayers)
        {
            m_PhotonUI.ActivateStartGameButton();
            PhotonNetwork.CurrentRoom.IsOpen = false;
        }
        m_PhotonUI.PlayersCountUpdated(PlayersCount, PhotonNetwork.CurrentRoom.MaxPlayers);
    }

    private void OnAddBotButton()
    {
        lastPlayerNumber++;
        playerBots.Add(new PlayerBot("PlayerBot" + lastPlayerNumber, lastPlayerNumber));

        if (PlayersCount == PhotonNetwork.CurrentRoom.MaxPlayers)
        {
            m_PhotonUI.ActivateStartGameButton();
            PhotonNetwork.CurrentRoom.IsOpen = false;
        }
        m_PhotonUI.PlayersCountUpdated(PlayersCount, PhotonNetwork.CurrentRoom.MaxPlayers);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("OnPlayerLeftRoom " + otherPlayer.NickName);
        if (PlayersCount < PhotonNetwork.CurrentRoom.MaxPlayers)
        {
            if (!isGameLoaded)
            {
                PhotonNetwork.CurrentRoom.IsOpen = true;
            }
        }
        m_PhotonUI.PlayersCountUpdated(PlayersCount, PhotonNetwork.CurrentRoom.MaxPlayers);
    }

    
    public override void OnCreatedRoom()
    {
        readyPlayersCount = 0;
        Debug.Log("OnCreatedRoom");
        m_PhotonUI.CreatedRoom(PhotonNetwork.CurrentRoom.MaxPlayers);
    }

    public override void OnJoinedRoom()
    {
        if(PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            lastPlayerNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            Debug.Log("OnJoinedRoom " + lastPlayerNumber);
        }
        m_PhotonUI.JoinedRoom(PhotonNetwork.LocalPlayer.IsMasterClient,
            PlayersCount, PhotonNetwork.CurrentRoom.MaxPlayers);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log("OnRoomListUpdate " + roomList.Count);
        PokerRoomInfo[] rooms = new PokerRoomInfo[roomList.Count];
        for (int i = 0; i < rooms.Length; i++)
        {
            RoomInfo roomInfo = roomList[i];
            rooms[i] = new PokerRoomInfo(roomInfo.Name, roomInfo.MaxPlayers, roomInfo.PlayerCount, roomInfo.IsOpen);
        }

        m_PhotonUI.RoomListUpdate(rooms);
    }
}
#endif
