using UnityEngine;

public enum PlayerChoose
{
    Non = 0,
    Fold,
    Check,
    Call,
    Bet
}

public delegate void PutCipsFromPlayerOnTableHandler(int gameID, int playerIndex, int cost);

public class NetworkManager : MonoBehaviour
{
    [SerializeField] private ServerMessaging m_ServerMessaging;
    private ClientMessaging clientMessaging;

    public void Init(ClientMessaging clientMessaging)
    {
        this.clientMessaging = clientMessaging;
        clientMessaging.Init(this);
    }

    #region From Server to Client

    public void SendGameStateData(int mainPlayerID, string gameState)
    {
        clientMessaging.SendGameStateData(mainPlayerID, gameState);
    }
    
    public void StartGame(int mainPlayerID, string gameState)
    {
        clientMessaging.StartGame(mainPlayerID, gameState);
    }

    public void SetCurrentPlayer(int mainPlayerID, string gameState)
    {
        clientMessaging.SetCurrentPlayer(mainPlayerID, gameState);
    }    

    public void GiveChipsToPlayers(int mainPlayerID, string gameState)
    {
        clientMessaging.GiveChipsToPlayers(mainPlayerID, gameState);
    }

    public void OpenTableForPlayersChips(int mainPlayerID, string gameState)
    {
        clientMessaging.OpenTableForPlayersChips(mainPlayerID, gameState);
    }

    public void SendUserChoose(int mainPlayerID, string gameState)
    {
        clientMessaging.SendUserChoose(mainPlayerID, gameState);
    }

    public void OnWrongBet(int mainPlayerID, string gameState)
    {
        clientMessaging.SendWrongBet(mainPlayerID, gameState);
    }

    public void PutChipsFromTableToPlayer(int mainPlayerID, string gameState)
    {
        clientMessaging.PutChipsFromTableToPlayer(mainPlayerID, gameState);
    }

    public void ShwoCards(int mainPlayerID, string gameState)
    {
        clientMessaging.ShwoCards(mainPlayerID, gameState);
    }

    public void GiveCardsToPlayers(int mainPlayerID, string gameState)
    {
        clientMessaging.GiveCardsToPlayers(mainPlayerID, gameState);
    }

    public void PutCardsOnTable(int mainPlayerID, string gameState)
    {
        clientMessaging.PutCardsOnTable(mainPlayerID, gameState);
    }

    public void RessetCards(int mainPlayerID, string gameState)
    {
        clientMessaging.RessetCards(mainPlayerID, gameState);
    }

    public void EndGame(int mainPlayerID, string gameState)
    {
        clientMessaging.EndGame(mainPlayerID, gameState);
    }

    public void DestroyGame()
    {
        clientMessaging.DestroyGame();
    }
    #endregion

    #region From Client To Server
    public void ReadyToStartGame()
    {
        m_ServerMessaging.ReadyToStartGame();
    }

    public void SentChipsCost(PlayerChoose userChoose, int cost)
    {
        m_ServerMessaging.SentUserChoose(userChoose, cost);
    }
    #endregion
}
