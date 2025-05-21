using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public delegate void PutCostFromPlayerOnTableHandler(int gameID, PlayerChoose playerChoose, int cost);

public class ServerMessaging : MonoBehaviour
{
    [SerializeField] private NetworkManager m_NetworkManager;

    public event PutCostFromPlayerOnTableHandler OnUserChoose;
    public event Action OnReadyToStartGame;

    public void Init()
    {
        m_NetworkManager.Init(FindObjectOfType<ClientMessaging>());
    }

    #region From Server to Client

    /// <summary>
    /// Send when player was reconnected
    /// </summary>
    /// <param name="serverGame"></param>
    public void SendGameStateData(int mainPlayerID, string gameState)
    {
        m_NetworkManager.SendGameStateData(mainPlayerID, gameState);
    }

    public void StartGame(int mainPlayerID, string gameState)
    {
        m_NetworkManager.StartGame(mainPlayerID, gameState);
    }

    public void GiveChipsToPlayers(int mainPlayerID, string gameState)
    {
        m_NetworkManager.GiveChipsToPlayers(mainPlayerID, gameState);
    }

    public void SetCurrentPlayer(int mainPlayerID, string gameState)
    {
        m_NetworkManager.SetCurrentPlayer(mainPlayerID, gameState);
    }
    //Todo
    public void OpenTableForPlayersChips(int mainPlayerID, string gameState)
    {
        m_NetworkManager.OpenTableForPlayersChips(mainPlayerID, gameState);
    }

    public void SendUserChoose(int mainPlayerID, string gameState)
    {
        m_NetworkManager.SendUserChoose(mainPlayerID, gameState);
    }

    public void OnWrongBet(int mainPlayerID, string gameState)
    {
        m_NetworkManager.OnWrongBet(mainPlayerID, gameState);
    }

    public void PutChipsFromTableToPlayer(int mainPlayerID, string gameState)
    {
        m_NetworkManager.PutChipsFromTableToPlayer(mainPlayerID, gameState);
    }

    public void ShwoCards(int mainPlayerID, string gameState)
    {
        m_NetworkManager.ShwoCards(mainPlayerID, gameState);
    }

    public void EndGame(int mainPlayerID, string gameState)
    {
        m_NetworkManager.EndGame(mainPlayerID, gameState);
    }

    public void GiveCardsToPlayers(int mainPlayerID, string gameState)
    {
        m_NetworkManager.GiveCardsToPlayers(mainPlayerID, gameState);
    }

    public void PutCardsOnTable(int mainPlayerID, string gameState)
    {
        m_NetworkManager.PutCardsOnTable(mainPlayerID, gameState);
    }

    public void RessetCards(int mainPlayerID, string gameState)
    {
        m_NetworkManager.RessetCards(mainPlayerID, gameState);
    }

    public void DestroyGame(string gameState)
    {
        m_NetworkManager.DestroyGame();
    }

    #endregion

    #region From Client To Server

    public void ReadyToStartGame()
    {
        OnReadyToStartGame?.Invoke();
    }

    public void SentUserChoose(PlayerChoose playerChoose, int cost)
    {
        OnUserChoose?.Invoke(0, playerChoose, cost);
    }
    #endregion
}
