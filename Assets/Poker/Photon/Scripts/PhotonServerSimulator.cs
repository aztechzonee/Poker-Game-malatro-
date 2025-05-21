#if PHOTON_UNITY_NETWORKING
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonServerSimulator : MonoBehaviour
{
    [SerializeField] private GameStateData m_GameStateData;
    [SerializeField] private ServerMessaging m_ServerMessaging;
    [SerializeField] private GameLogicManager m_GameLogicManager;

    private PhotonView netView;
    private GameStateData gameStateData;
    private ServerGame serverGame;
    private int readyPlayersCount;

    private int PlayersCount => PhotonNetwork.CurrentRoom.PlayerCount + (playerBots != null ? playerBots.Count : 0);

    private List<PlayerBot> playerBots;

    public void Init(PhotonView netView)
    {
        readyPlayersCount = 0;
        m_GameStateData.SetConfigs();
        m_GameStateData.SaveCards();
        this.netView = netView;
        m_ServerMessaging.Init();
    }

    public void StartGameMaster(List<PlayerBot> playerBots)
    {
        Debug.Log("StartGameMaster " + PhotonNetwork.LocalPlayer.ActorNumber);

        this.playerBots = playerBots;

        netView.RPC("InitBots", RpcTarget.Others);

        foreach (var bot in playerBots)
        {
            netView.RPC("AddBot", RpcTarget.Others, bot.NickName, bot.ActorNumber);
        }

        int dealerID = Random.Range(0, PlayersCount);
        netView.RPC("CreateGame", RpcTarget.All, PlayersCount, dealerID);
    }

    [PunRPC]
    public void InitBots()
    {
        playerBots = new List<PlayerBot>();
    }

    [PunRPC]
    public void AddBot(string name, int number)
    {
        playerBots.Add(new PlayerBot(name, number));
    }

    [PunRPC]
    public void CreateGame(int playersCount, int dealerID)
    {
        m_ServerMessaging.OnUserChoose += OnUserChoose;
        m_ServerMessaging.OnReadyToStartGame += ()=>
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                netView.RPC("OnPlayerReadyToStartGame", PhotonNetwork.MasterClient, PhotonNetwork.LocalPlayer);
            }

            foreach (var item in playerBots)
            {
                OnPlayerReadyToStartGame(null);
            }
        };

        gameStateData = new GameStateData(m_GameStateData, false)
        {
            players = new List<PlayerData>(0),
            dealerID = dealerID,
            smallBlindID = dealerID + 1 < playersCount ? dealerID + 1 : 0,
            bigBlindID = dealerID + 2 < playersCount ? dealerID + 2 : dealerID + 2 - playersCount,
            state = GameState.Start
        };

        foreach (var item in PhotonNetwork.CurrentRoom.Players)
        {
            Player player = item.Value;
            PlayerData playerData = new PlayerData(player.NickName, player.ActorNumber - 1, false);
            gameStateData.players.Add(playerData);
        }

        foreach (var item in playerBots)
        {
            PlayerBot player = item;
            PlayerData playerData = new PlayerData(player.NickName, player.ActorNumber - 1, true);
            gameStateData.players.Add(playerData);
        }

        gameStateData.players.Sort(delegate (PlayerData player1, PlayerData player2)
        {
            return player1.playerID.CompareTo(player2.playerID);
        });

        foreach (var item in gameStateData.players)
        {
            Debug.Log("Player with index " + item.playerID + " is bot? " + item.isBot + " name  " + item.playerName);
        }

        gameStateData.currentPlayerID = gameStateData.smallBlindID;
        gameStateData.currentPlayer = gameStateData.players[gameStateData.currentPlayerID];

        serverGame = new GameObject("Game").AddComponent<ServerGame>();
        serverGame.transform.SetParent(transform);
        serverGame.Setup(0, false, gameStateData);
        gameStateData.step = GameState.Start;

        gameStateData.mainPlayerID = PhotonNetwork.LocalPlayer.ActorNumber - 1;

        m_ServerMessaging.StartGame(gameStateData.mainPlayerID, serverGame.GameStateAsJSON);
     
        serverGame.GiveChipsToPlayers();
        serverGame.ResetChipsOnTable();
        gameStateData.step = GameState.GivePlayersChips;
        gameStateData.state = GameState.GivePlayersChips;

        m_ServerMessaging.GiveChipsToPlayers(gameStateData.mainPlayerID, serverGame.GamePlayersAsJSON);
        m_ServerMessaging.SetCurrentPlayer(gameStateData.mainPlayerID, serverGame.GameCurrentPlayerAsJSON);
        m_ServerMessaging.OpenTableForPlayersChips(gameStateData.mainPlayerID, serverGame.GameBaseDataAsJSON);

        SentPlayerChoose((int)PlayerChoose.Bet, 10, false);
        SentPlayerChoose((int)PlayerChoose.Bet, 20, false);
    }

    [PunRPC]
    public void OnPlayerReadyToStartGame(Player player)
    {
        readyPlayersCount++;
        Debug.Log("OnPlayerReadyToStartGame " + readyPlayersCount);
        if (readyPlayersCount + 1 >= PlayersCount)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                GiveCardsToPlayers();
                SavedPlayersCardsData savedPlayersCardsData = gameStateData.GetSavedPlayersCardsData();

                netView.RPC("GiveCardsToPlayers", RpcTarget.Others, JsonUtility.ToJson(savedPlayersCardsData));
            }
        }
    }

    [PunRPC]
    public void SentPlayerChoose(int playerChoose, int cost, bool setChoose)
    {
        if (gameStateData.step == GameState.GivePlayersChips)
        {
            gameStateData.step = GameState.PlayersBet;
        }

        serverGame.GameStateData.state = GameState.PlayersBet;

        bool hasDifference = false;
        ChipsColumn[] toPlayerChips = null;
        ChipsColumn[] toDealerChips = null;

        serverGame.PutChipsFromPlayerOnTable((PlayerChoose)playerChoose, cost, setChoose,
             out hasDifference, out toPlayerChips, out toDealerChips,
            (bool isOutOfTable, bool wrongBet, ChipsColumn[] chips) =>
        {
            if (!wrongBet)
            {
                serverGame.GameStateData.playerChooseData = new PlayerChooseData((PlayerChoose)playerChoose, chips,
                hasDifference, toPlayerChips, toDealerChips);

                if (!isOutOfTable)
                {
                    m_ServerMessaging.SendUserChoose(gameStateData.mainPlayerID,
                        serverGame.GamePlayerChooseDataAsJSON);
                }
                bool sendNextAction = gameStateData.HasComplitedTable && gameStateData.AllIsChoosed;
                if (!sendNextAction)
                {
                    SetNextPlayer(serverGame);
                }
            }
            else
            {
                m_ServerMessaging.OnWrongBet(serverGame.GameStateData.mainPlayerID,
                        serverGame.GameBaseDataAsJSON);
            }
        });
    }

    [PunRPC]
    public void GiveCardsToPlayers(string cardsData)
    {
        SavedPlayersCardsData savedPlayersCardsData = JsonUtility.FromJson<SavedPlayersCardsData>(cardsData);
        if (serverGame.GameStateData.step == GameState.PlayersBet)
        {
            serverGame.GameStateData.step = GameState.GivePlayersCards;
        }
        gameStateData.state = GameState.GivePlayersCards;

        serverGame.GiveCardsToPlayers(savedPlayersCardsData);

        //If PlayerID  is a local player id?
        serverGame.HideOtherPlayersCards(gameStateData.mainPlayerID);

        m_ServerMessaging.GiveCardsToPlayers(gameStateData.mainPlayerID, serverGame.GamePlayersAsJSON);
    }

    [PunRPC]
    public void SetCardsOnTable(string tableCardsData)
    {
        gameStateData.currentPlayerID = gameStateData.smallBlindID;
        gameStateData.currentPlayer = gameStateData.players[gameStateData.currentPlayerID];
        m_ServerMessaging.SetCurrentPlayer(gameStateData.mainPlayerID, serverGame.GameCurrentPlayerAsJSON);

        if (serverGame.GameStateData.step == GameState.GivePlayersCards)
        {
            serverGame.GameStateData.step = GameState.PutTableCards;
        }
        gameStateData.state = GameState.PutTableCards;

        CardsData cardsData = JsonUtility.FromJson<CardsData>(tableCardsData);

        serverGame.SetCardsOnTable(cardsData.cards);
        m_ServerMessaging.PutCardsOnTable(serverGame.GameStateData.mainPlayerID, serverGame.GameTableCardsAsJSON);
    }

    [PunRPC]
    public void ShwoCards()
    {
        serverGame.GameStateData.step = GameState.ShowPlayersCards;
        gameStateData.state = GameState.ShowPlayersCards;
        gameStateData.SetSavedPlayersCards();
        gameStateData.winners = m_GameLogicManager.GetWinners(gameStateData.players, gameStateData.tableCards);
        m_ServerMessaging.ShwoCards(gameStateData.mainPlayerID, serverGame.GamePlayersAsJSON);
        StartCoroutine(GiveWinnersChips());
    }

    private IEnumerator GiveWinnersChips()
    {
        yield return new WaitForSeconds(7f);

        serverGame.GameStateData.step = GameState.GiveWinnersPrize;
        gameStateData.state = GameState.GiveWinnersPrize;

        int tableChipsSum = 0;
        List<PlayerData> otherPlayers = new List<PlayerData>(gameStateData.players);

        List<Winner> winners = new List<Winner>(gameStateData.winners);

        do
        {
            foreach (var item in gameStateData.winners)
            {
                otherPlayers.Remove(gameStateData.players[item.id]);
            }

            if (gameStateData.winners.Count == 1)
            {
                Winner winner = gameStateData.winners[0];
                gameStateData.currentPlayerID = winner.id;
                gameStateData.currentPlayer = gameStateData.players[winner.id];

                int prizeCost = gameStateData.currentPlayer.prizeCost;
                gameStateData.currentPlayer.prizeCost = 0;
                DecreaseOtherPlayersPrize(prizeCost);

                serverGame.PutChipsFromTableToPlayer(serverGame.GameStateData.currentPlayerID, prizeCost);
                m_ServerMessaging.PutChipsFromTableToPlayer(serverGame.GameStateData.mainPlayerID, serverGame.GameStateAsJSON);
                gameStateData.currentPlayer.prizeCost = 0;
            }
            else
            {
                foreach (Winner winner in gameStateData.winners)
                {
                    gameStateData.currentPlayerID = winner.id;
                    gameStateData.currentPlayer = gameStateData.players[winner.id];

                    int prizeCost = gameStateData.currentPlayer.prizeCost / gameStateData.players.Count;
                    gameStateData.currentPlayer.prizeCost = 0;
                    DecreaseOtherPlayersPrize(prizeCost);

                    serverGame.PutChipsFromTableToPlayer(serverGame.GameStateData.currentPlayerID, prizeCost);
                    m_ServerMessaging.PutChipsFromTableToPlayer(serverGame.GameStateData.mainPlayerID, serverGame.GameStateAsJSON);
                    gameStateData.currentPlayer.prizeCost = 0;
                }
            }

            tableChipsSum = serverGame.GetSum(gameStateData.tableChips);
            if(tableChipsSum > 0 && otherPlayers.Count > 0)
            {
                gameStateData.winners = m_GameLogicManager.GetWinners(otherPlayers, gameStateData.tableCards);
            }
        }
        while (tableChipsSum > 0 && otherPlayers.Count > 0 && gameStateData.winners.Count > 0);

        void DecreaseOtherPlayersPrize(int cost)
        {
            foreach (var otherPlayer in otherPlayers)
            {
                otherPlayer.prizeCost -= cost;
                if (otherPlayer.prizeCost < 0)
                {
                    otherPlayer.prizeCost = 0;
                }
            }
        }
        
        if (tableChipsSum > 0)
        {
            gameStateData.currentPlayerID = gameStateData.dealerID;
            gameStateData.currentPlayer = gameStateData.players[gameStateData.currentPlayerID];

            serverGame.PutChipsFromTableToPlayer(serverGame.GameStateData.currentPlayerID, tableChipsSum);
            m_ServerMessaging.PutChipsFromTableToPlayer(serverGame.GameStateData.mainPlayerID, serverGame.GameStateAsJSON);
        }

        gameStateData.winners = winners;

        serverGame.GameStateData.playersMaxBet = 0;

        serverGame.GameStateData.step = GameState.GivePlayersChips;

        serverGame.GameStateData.dealerID++;
        int playersCount = gameStateData.players.Count;

        if (serverGame.GameStateData.dealerID >= playersCount)
        {
            serverGame.GameStateData.dealerID = 0;
        }
        serverGame.GameStateData.smallBlindID = serverGame.GameStateData.dealerID + 1 < playersCount ?
        serverGame.GameStateData.dealerID + 1 : 0;
        serverGame.GameStateData.bigBlindID = serverGame.GameStateData.dealerID + 2 < playersCount ?
        serverGame.GameStateData.dealerID + 2 : serverGame.GameStateData.dealerID + 2 - playersCount;

        gameStateData.currentPlayerID = gameStateData.smallBlindID;
        gameStateData.currentPlayer = gameStateData.players[gameStateData.currentPlayerID];

        gameStateData.state = GameState.GivePlayersChips;

        yield return new WaitForSeconds(3f);

        serverGame.RessetCards(m_GameStateData.Cards);
        m_ServerMessaging.RessetCards(gameStateData.mainPlayerID, serverGame.GameStateAsJSON);

        if (!gameStateData.GameIsEnded())
        {
            yield return new WaitForSeconds(1f);
            m_ServerMessaging.SetCurrentPlayer(gameStateData.mainPlayerID, serverGame.GameCurrentPlayerAsJSON);

            SentPlayerChoose((int)PlayerChoose.Bet, 10, false);
            SentPlayerChoose((int)PlayerChoose.Bet, 20, false);

            if (PhotonNetwork.IsMasterClient)
            {
                GiveCardsToPlayers();
                SavedPlayersCardsData savedPlayersCardsData = gameStateData.GetSavedPlayersCardsData();

                netView.RPC("GiveCardsToPlayers", RpcTarget.Others, JsonUtility.ToJson(savedPlayersCardsData));
            }
        }
        else
        {
            yield return new WaitForSeconds(2f);
            gameStateData.state = GameState.Ended;
            m_ServerMessaging.EndGame(gameStateData.mainPlayerID, serverGame.GamePlayersAsJSON);
        }
    }

    private void OnUserChoose(int gameID, PlayerChoose playerChoose, int cost)
    {   
        SentPlayerChoose((int)playerChoose, cost, true);
        netView.RPC("SentPlayerChoose", RpcTarget.Others, (int)playerChoose, cost, true);

        bool sendNextAction = gameStateData.HasComplitedTable && gameStateData.AllIsChoosed;

        if (sendNextAction)
        {
            SendNextAction(gameStateData.step);
        }
    }

    private void SetNextPlayer(ServerGame serverGame)
    {
        serverGame.NextPlayer();
        m_ServerMessaging.SetCurrentPlayer(gameStateData.mainPlayerID, serverGame.GameCurrentPlayerAsJSON);
    }

    private void GiveCardsToPlayers()
    {
        if (serverGame.GameStateData.step == GameState.PlayersBet)
        {
            serverGame.GameStateData.step = GameState.GivePlayersCards;
        }
        gameStateData.state = GameState.GivePlayersCards;

        serverGame.GiveCardsToPlayers();

        //If PlayerID  is a local player id?
        serverGame.HideOtherPlayersCards(gameStateData.mainPlayerID);

        m_ServerMessaging.GiveCardsToPlayers(gameStateData.mainPlayerID, serverGame.GamePlayersAsJSON);
    }

    private void PutCardsOnTable(int cardsCount)
    {
        gameStateData.currentPlayerID = gameStateData.smallBlindID;
        gameStateData.currentPlayer = gameStateData.players[gameStateData.currentPlayerID];
        m_ServerMessaging.SetCurrentPlayer(gameStateData.mainPlayerID, serverGame.GameCurrentPlayerAsJSON);

        if (serverGame.GameStateData.step == GameState.GivePlayersCards)
        {
            serverGame.GameStateData.step = GameState.PutTableCards;
        }
        gameStateData.state = GameState.PutTableCards;

        serverGame.PutCardsOnTable(cardsCount);
        m_ServerMessaging.PutCardsOnTable(serverGame.GameStateData.mainPlayerID, serverGame.GameTableCardsAsJSON);
    }

    private void SendNextAction(GameState step)
    {
        if (gameStateData.ActivePlayersCount <= 1)
        {
            if (gameStateData.tableCards == null)
            {
                gameStateData.tableCards = new List<string>(0);
            }
            if (gameStateData.tableCards.Count < 5)
            {
                PutCardsOnTable(5 - gameStateData.tableCards.Count);
                netView.RPC("SetCardsOnTable", RpcTarget.Others, JsonUtility.ToJson(new CardsData(gameStateData.tableCards)));
            }
            netView.RPC("ShwoCards", RpcTarget.All);
            return;
        }
       
        switch (step)
        {
            case GameState.GivePlayersCards:
                PutCardsOnTable(3);
                netView.RPC("SetCardsOnTable", RpcTarget.Others, JsonUtility.ToJson(new CardsData(gameStateData.tableCards)));
                break;
            case GameState.PutTableCards:
                if (gameStateData.tableCards.Count < 5)
                {
                    PutCardsOnTable(1);
                    netView.RPC("SetCardsOnTable", RpcTarget.Others, JsonUtility.ToJson(new CardsData(gameStateData.tableCards)));
                }
                else
                {
                    netView.RPC("ShwoCards", RpcTarget.All);
                }
                break;
            default:
                break;
        }
    }
}
#endif
