using System;
using UnityEngine;
using UnityEngine.UI;

public class PhotonRoomItemUI : MonoBehaviour
{
    [SerializeField] private Text m_RoomNameText;
    [SerializeField] private Text m_PlayersMaxCount;
    [SerializeField] private Button m_CreateRoomButton;

    public void Set(PokerRoomInfo roomInfo, Action OnClick)
    {
        m_RoomNameText.text = roomInfo.name;
        m_PlayersMaxCount.text = roomInfo.connectedPlayers + "/" + roomInfo.maxPlayers;
        m_CreateRoomButton.interactable = roomInfo.isOpen;
        if(roomInfo.isOpen)
        {
            m_CreateRoomButton.onClick.RemoveAllListeners();
            m_CreateRoomButton.onClick.AddListener(() =>
            {
                OnClick?.Invoke();
            });
        }
    }
}
