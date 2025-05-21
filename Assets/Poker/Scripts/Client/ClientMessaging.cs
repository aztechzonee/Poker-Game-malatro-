using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void GameStateDataUpdateHandler(GameStateData gameStateData, Action calback);

public class ClientMessaging : MonoBehaviour
{
    private NetworkManager networkManager;
    private GameStateData gameStateData;
    private Queue<Action<Action>> handlers;
    private Coroutine invokeHandlers;
    private bool isInvoking;

    public event GameStateDataUpdateHandler OnGameStateData;
    public event GameStateDataUpdateHandler OnStartGame;
    public event GameStateDataUpdateHandler OnSetCurrentPlayer;
    public event GameStateDataUpdateHandler OnGiveChipsToPlayers;
    public event GameStateDataUpdateHandler OnOpenTableForPlayersChips;
    public event GameStateDataUpdateHandler OnSendUserChoose;
    public event GameStateDataUpdateHandler OnWrongBet;
    public event GameStateDataUpdateHandler OnPutCipsFromTableToPlayer;
    public event GameStateDataUpdateHandler OnGiveCardsToPlayers;
    public event GameStateDataUpdateHandler OnShwoCards;
    public event GameStateDataUpdateHandler OnPutCardsOnTable;
    public event GameStateDataUpdateHandler OnRessetCards;
    public event GameStateDataUpdateHandler OnEndGame;

    public GameStateData GetGameStateData => gameStateData;

    public void Init(NetworkManager networkManager)
    {
        isInvoking = false;
        handlers = new Queue<Action<Action>>(0);
        this.networkManager = networkManager;
        if(invokeHandlers != null)
        {
            StopCoroutine(invokeHandlers);
        }
        invokeHandlers = StartCoroutine(InvokeHandlers());
    }
   
    private IEnumerator InvokeHandlers()
    {
        while(true)
        {
            if (!isInvoking && handlers.Count != 0)
            {
                isInvoking = true;
                handlers.Dequeue().Invoke(()=>
                {
                    isInvoking = false;
                });
            }
            yield return null;
        }
    }

    #region From Server To Client
    public void SendGameStateData(int mainPlayerID, string gameState)
    {
        OnNewGameState(mainPlayerID, gameState, OnGameStateData);
    }
 
    public void StartGame(int mainPlayerID, string gameState)
    {
        OnNewGameState(mainPlayerID, gameState, OnStartGame);
    }

    public void GiveChipsToPlayers(int mainPlayerID, string gameState)
    {
        OnNewGameState(mainPlayerID, gameState, OnGiveChipsToPlayers);
    }

    public void SetCurrentPlayer(int mainPlayerID, string gameState)
    {
        OnNewGameState(mainPlayerID, gameState, OnSetCurrentPlayer);
    }

    public void OpenTableForPlayersChips(int mainPlayerID, string gameState)
    {
        OnNewGameState(mainPlayerID, gameState, OnOpenTableForPlayersChips);
    }

    public void SendUserChoose(int mainPlayerID, string gameState)
    {
        OnNewGameState(mainPlayerID, gameState, OnSendUserChoose);
    }

    public void SendWrongBet(int mainPlayerID, string gameState)
    {
        OnNewGameState(mainPlayerID, gameState, OnWrongBet);
    }

    public void PutChipsFromTableToPlayer(int mainPlayerID, string gameState)
    {
        OnNewGameState(mainPlayerID, gameState, OnPutCipsFromTableToPlayer);
    }

    public void EndGame(int mainPlayerID, string gameState)
    {
        OnNewGameState(mainPlayerID, gameState, OnEndGame);
    }

    public void ShwoCards(int mainPlayerID, string gameState)
    {
        OnNewGameState(mainPlayerID, gameState, OnShwoCards);
    }

    public void GiveCardsToPlayers(int mainPlayerID, string gameState)
    {
        OnNewGameState(mainPlayerID, gameState, OnGiveCardsToPlayers);
    }

    public void RessetCards(int mainPlayerID, string gameState)
    {
        OnNewGameState(mainPlayerID, gameState, OnRessetCards);
    }

    public void PutCardsOnTable(int mainPlayerID, string gameState)
    {
        OnNewGameState(mainPlayerID, gameState, OnPutCardsOnTable);
    }

    public void DestroyGame()
    {
        OnGameStateData = null;
        OnStartGame = null;
        OnSetCurrentPlayer = null;
        OnGiveChipsToPlayers = null;
        OnOpenTableForPlayersChips = null;
        OnSendUserChoose = null;
        OnWrongBet = null;
        OnPutCipsFromTableToPlayer = null;
        OnGiveCardsToPlayers = null;
        OnShwoCards = null;
        OnPutCardsOnTable = null;
        OnRessetCards = null;
        OnEndGame = null;

        if (invokeHandlers != null)
        {
            StopCoroutine(invokeHandlers);
        }
    }
    #endregion

    #region From Client To Server
    public void ReadyToStartGame()
    {
        networkManager.ReadyToStartGame();
    }

    public void SentChipsCost(PlayerChoose userChoose, int cost)
    {
        networkManager.SentChipsCost(userChoose, cost);
    }
    #endregion

    private void OverwriteFromJSON(int mainPlayerID, string json, out GameStateData newGameStateData)
    {
        Debug.Log(mainPlayerID + " json " + json);
        if (gameStateData == null)
        {
            gameStateData = JsonUtility.FromJson<GameStateData>(json);
            gameStateData.mainPlayerID = mainPlayerID;
        }

        JsonUtility.FromJsonOverwrite(json, gameStateData);
        newGameStateData = new GameStateData(gameStateData, true)
        {
            mainPlayerID = mainPlayerID
        };
    }

    public void OnNewGameState(int mainPlayerID, string gameState, GameStateDataUpdateHandler newEvent)
    {
        OverwriteFromJSON(mainPlayerID, gameState, out GameStateData newGameStateData);

        handlers.Enqueue((Action callback)=>
        {
            newEvent?.Invoke(newGameStateData, callback);
        });
    }
}
