using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public struct PokerRoomInfo
{
    public string name;
    public byte maxPlayers;
    public int connectedPlayers;
    public bool isOpen;

    public PokerRoomInfo(string name, byte maxPlayers, int connectedPlayers, bool isOpen)
    {
        this.name = name;
        this.maxPlayers = maxPlayers;
        this.connectedPlayers = connectedPlayers;
        this.isOpen = isOpen;
    }
}

public class PhotonUI : MonoBehaviour
{
    [Space(10), Header("Player")]
    [SerializeField] private Transform m_PlayerPanel;
    [SerializeField] private InputField m_PlayerNameText;
    [SerializeField] private Button m_LogInPlayerButton;
    [Space(10), Header("Room")]
    [SerializeField] private Transform m_RoomsPanel;
    [SerializeField] private PhotonRoomItemUI m_PhotonRoomItemUIPrefab;
    [SerializeField] private Transform m_RoomItemsContent;
    [SerializeField] private InputField m_PlayersMaxCount;
    [SerializeField] private Button m_CreateRoomButton;
    [Space(10), Header("Start Game")]
    [SerializeField] private Transform m_StartGamePanel;
    [SerializeField] private Button m_StartGameButton;
    [SerializeField] private Button m_AddBotButton;
    [SerializeField] private Text m_ConnectedPlayersText;
    [Space(10), Header("Wait Game")]
    [SerializeField] private Transform m_WaitGamePanel;
    [SerializeField] private Text m_RoomPlayersText;

    public Action<string> OnLogInPlayerButton;
    public Action<string, int> OnCreateRoomButton;
    public Action<PokerRoomInfo> OnJoinRoomButton;
    public Action OnStartGameButton;
    public Action OnAddBotButton;

    private PhotonRoomItemUI[] roomsUI;

    private void Awake()
    {
        m_PhotonRoomItemUIPrefab.gameObject.SetActive(false);

        m_WaitGamePanel.gameObject.SetActive(false);
        m_RoomsPanel.gameObject.SetActive(false);
        m_StartGamePanel.gameObject.SetActive(false);
        m_PlayerPanel.gameObject.SetActive(true);

        m_LogInPlayerButton.onClick.AddListener(() =>
        {
            m_PlayerPanel.gameObject.SetActive(false);
            if (string.IsNullOrEmpty(m_PlayerNameText.text))
            {
                m_PlayerNameText.text = "Player_1";
            }
            OnLogInPlayerButton?.Invoke(m_PlayerNameText.text);
        });
    }

    public void DisconnectedFromServer()
    {
        m_WaitGamePanel.gameObject.SetActive(false);
        m_RoomsPanel.gameObject.SetActive(false);
        m_StartGamePanel.gameObject.SetActive(false);
        m_PlayerPanel.gameObject.SetActive(true);
    }

    public void ConnectedToServer()
    {
        m_RoomsPanel.gameObject.SetActive(true);
        m_StartGamePanel.gameObject.SetActive(false);

        m_CreateRoomButton.onClick.RemoveAllListeners();
        m_CreateRoomButton.onClick.AddListener(() =>
        {
            int.TryParse(m_PlayersMaxCount.text, out int playersMaxCount);
            playersMaxCount = Mathf.Clamp(playersMaxCount, 3, 7);

            int roomCount = roomsUI == null ? 0 : roomsUI.Length;
            OnCreateRoomButton?.Invoke(m_PlayerNameText.text + "s_Room_" + roomCount, playersMaxCount);
        });
    }

    public void CreatedRoom(int maxPlayers)
    {
        m_RoomsPanel.gameObject.SetActive(false);
        m_StartGamePanel.gameObject.SetActive(true);

        m_StartGameButton.interactable = false;
        m_StartGameButton.onClick.RemoveAllListeners();
        m_StartGameButton.onClick.AddListener(() =>
        {
            OnStartGameButton?.Invoke();
        });

        m_AddBotButton.onClick.RemoveAllListeners();
        m_AddBotButton.onClick.AddListener(() =>
        {
            OnAddBotButton?.Invoke();
        });

        SetConnectedPlayersText(1, maxPlayers);
    }

    public void PlayersCountUpdated(int playersCount, int maxPlayers)
    {
        SetConnectedPlayersText(playersCount, maxPlayers);
        m_AddBotButton.interactable = playersCount < maxPlayers;
    }

    public void JoinedRoom(bool isMasterPlayer, int playersCount, int maxPlayers)
    {
        if(!isMasterPlayer)
        {
            m_RoomsPanel.gameObject.SetActive(false);
            m_WaitGamePanel.gameObject.SetActive(true);
        }
        SetConnectedPlayersText(playersCount, maxPlayers);
    }

    private void SetConnectedPlayersText(int playersCount, int maxPlayers)
    {
        m_ConnectedPlayersText.text = "Connected players: " + playersCount + " from " + maxPlayers;
        m_RoomPlayersText.text = m_ConnectedPlayersText.text;
    }

    public void ActivateStartGameButton()
    {
        m_StartGameButton.interactable = true;
        m_AddBotButton.interactable = false;
    }

    public void RoomListUpdate(PokerRoomInfo[] rooms)
    {
        if (roomsUI != null)
        {
            foreach (var roomUI in roomsUI)
            {
                Destroy(roomUI.gameObject);
            }
        }
        roomsUI = new PhotonRoomItemUI[rooms.Length];
        for (int i = 0; i < rooms.Length; i++)
        {
            roomsUI[i] = Instantiate(m_PhotonRoomItemUIPrefab,
                m_RoomItemsContent.position, m_RoomItemsContent.rotation, m_RoomItemsContent);

            roomsUI[i].gameObject.SetActive(true);
            PokerRoomInfo pokerRoomInfo = rooms[i];
            roomsUI[i].Set(pokerRoomInfo, () =>
            {
                OnJoinRoomButton?.Invoke(pokerRoomInfo);
            });
        }
    }
}
