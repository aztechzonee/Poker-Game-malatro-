using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private ClientMessaging m_ClientMessaging;

    [Space(10), Header("Managers")]
    [SerializeField] private TableManager m_TableManager;
    [SerializeField] private PlayersSeatManager m_PlayersSeatManager;
    [SerializeField] private CardsManager m_CardsManager;
    [SerializeField] private ChipsManager m_ChipsManager;

    [Space(10), Header("Shadow")]
    [SerializeField] private Transform m_ShadowLight;
    [SerializeField] private Transform m_ShadowPlane;

    [Space(10), Header("UI")]
    [SerializeField] private GameUI m_GameUI;

    [SerializeField] private PlayerAIManager m_PlayerAIManager;

    private void Awake()
    {
        m_GameUI.HideLoadingPanel();
        HideUserChoose();

        m_ClientMessaging.OnGameStateData += (GameStateData gameStateData, Action calback) =>
        {
            m_TableManager.Init(gameStateData);
            m_PlayersSeatManager.Init(gameStateData, m_ChipsManager.GetChipsThicknes);
            m_ChipsManager.CreateChipsManagers(gameStateData.chips, m_ShadowLight, m_ShadowPlane);
            m_CardsManager.CreateDeck(m_ShadowLight, m_ShadowPlane);

            m_PlayersSeatManager.MoveDealerToPivot(gameStateData.dealerID);
            m_PlayersSeatManager.MoveSmallBlindToPivot(gameStateData.smallBlindID);
            m_PlayersSeatManager.MoveBigBlindToPivot(gameStateData.bigBlindID);

            SetGameStateData(gameStateData, calback);

            m_ClientMessaging.ReadyToStartGame();
        };

        m_ClientMessaging.OnStartGame += (GameStateData gameStateData, Action calback) =>
        {
            m_TableManager.Init(gameStateData);
            m_PlayersSeatManager.Init(gameStateData, m_ChipsManager.GetChipsThicknes);
            m_ChipsManager.CreateChipsManagers(gameStateData.chips, m_ShadowLight, m_ShadowPlane);
            m_CardsManager.CreateDeck(m_ShadowLight, m_ShadowPlane);
           
            StartCoroutine(m_PlayersSeatManager.MoveDealerToPivot(
            gameStateData.dealerID, m_ChipsManager.GetChipsMoveCurve, m_ChipsManager.GetChipsDeltaYCurve, calback));

            StartCoroutine(m_PlayersSeatManager.MoveSmallBlindToPivot(
            gameStateData.smallBlindID, m_ChipsManager.GetChipsMoveCurve, m_ChipsManager.GetChipsDeltaYCurve, calback));

            StartCoroutine(m_PlayersSeatManager.MoveBigBlindToPivot(
            gameStateData.bigBlindID, m_ChipsManager.GetChipsMoveCurve, m_ChipsManager.GetChipsDeltaYCurve, calback));
        };
        
        m_ClientMessaging.OnGiveChipsToPlayers += (GameStateData gameStateData, Action calback) =>
        {
            StartCoroutine(GiveChipsToPlayers(gameStateData, calback));
        };

        m_ClientMessaging.OnSetCurrentPlayer += SetCurrentPlayer;

        m_ClientMessaging.OnOpenTableForPlayersChips += (GameStateData gameStateData, Action calback) =>
        {
            calback?.Invoke();
        };

        m_ClientMessaging.OnSendUserChoose += (GameStateData gameStateData, Action calback) =>
        {
            PlayerChoose playerChoose = gameStateData.playerChooseData.playerChoose;

            switch (playerChoose)
            {
                case PlayerChoose.Bet:
                case PlayerChoose.Call:
                    StartCoroutine(PutChipsOnTable(gameStateData, calback));
                    break;
                case PlayerChoose.Check:
                    StartCoroutine(OnUserCheck(gameStateData, calback));
                    break;
                case PlayerChoose.Fold:
                    StartCoroutine(OnUserFold(gameStateData, calback));
                    break;
                default:
                    break;
            }
            
        };

        m_ClientMessaging.OnWrongBet += (GameStateData gameStateData, Action calback) =>
        {
            PlayerData currentPlayerData = gameStateData.currentPlayer;

            if (gameStateData.currentPlayerID == gameStateData.mainPlayerID &&
                gameStateData.step < GameState.ShowPlayersCards
                )
            {
                ShowPlayerChoose(currentPlayerData, gameStateData);
                StartCoroutine(m_GameUI.AnimateWrongBet());
            }
            else
            {
                HideUserChoose();
            }

            calback?.Invoke();
        };

        m_ClientMessaging.OnPutCardsOnTable += (GameStateData gameStateData, Action calback) =>
        {
            StartCoroutine(PutCardsOnTable(gameStateData, calback));
        };

        m_ClientMessaging.OnPutCipsFromTableToPlayer += (GameStateData gameStateData, Action calback) =>
        {
            StartCoroutine(GiveChipsToPlayerFromTable(gameStateData, calback));
        };

        m_ClientMessaging.OnShwoCards += (GameStateData gameStateData, Action calback) =>
        {
            StartCoroutine(ShowCards(gameStateData, calback));
        };

        m_ClientMessaging.OnGiveCardsToPlayers += (GameStateData gameStateData, Action calback) =>
        {
            StartCoroutine(GiveCardToPlayers(gameStateData, calback));
        };

        m_ClientMessaging.OnRessetCards += (GameStateData gameStateData, Action calback) =>
        {
            m_GameUI.RessetMaxBetText(gameStateData);
            foreach (var playerData in gameStateData.players)
            {
                if (!playerData.outOfGame)
                {
                    m_PlayersSeatManager.PlayerSeatManagers[playerData.playerID].ShowPlayer();
                }
                m_PlayersSeatManager.ResetAll(playerData.playerID);
            }
            m_CardsManager.RessetCards(gameStateData, m_ShadowLight, m_ShadowPlane);

            StartCoroutine(m_PlayersSeatManager.MoveDealerToPivot(
            gameStateData.dealerID, m_ChipsManager.GetChipsMoveCurve, m_ChipsManager.GetChipsDeltaYCurve, calback));

            StartCoroutine(m_PlayersSeatManager.MoveSmallBlindToPivot(
            gameStateData.smallBlindID, m_ChipsManager.GetChipsMoveCurve, m_ChipsManager.GetChipsDeltaYCurve, calback));

            StartCoroutine(m_PlayersSeatManager.MoveBigBlindToPivot(
            gameStateData.bigBlindID, m_ChipsManager.GetChipsMoveCurve, m_ChipsManager.GetChipsDeltaYCurve, calback));


            calback?.Invoke();
        };

        m_ClientMessaging.OnEndGame += (GameStateData gameStateData, Action calback) =>
        {
            OnEndGame(gameStateData, calback);
        };

        m_GameUI.OnPlayerChoose += OnUserChoose;

        m_PlayersSeatManager.OnTimerEnded += () =>
        {
            if (m_ClientMessaging.GetGameStateData.currentPlayerID == m_ClientMessaging.GetGameStateData.mainPlayerID ||
            m_ClientMessaging.GetGameStateData.currentPlayer.isBot)
            {
                m_PlayerAIManager.Choose(m_ClientMessaging.GetGameStateData, out PlayerChoose playerChoose, out int cost);
                OnUserChoose(playerChoose, cost);
            }
        };
    }

    private void SetGameStateData(GameStateData gameStateData, Action calback)
    {
        GiveChipsToPlayers(gameStateData);
        PutChipsOnTable(gameStateData);
        GiveCardToPlayers(gameStateData);
        SetCurrentPlayer(gameStateData);
        PutCardsOnTable(gameStateData);

        if(gameStateData.state == GameState.ShowPlayersCards ||
            gameStateData.state == GameState.Ended)
        {
            ShowCards(gameStateData);
        }
        if (gameStateData.state == GameState.Ended)
        {
            OnEndGame(gameStateData);
        }

        calback?.Invoke();
    }

    private void HideUserChoose()
    {
        m_GameUI.HideUserChoose();
    }

    private void GiveChipsToPlayers(GameStateData gameStateData)
    {
        foreach (var playerData in gameStateData.players)
        {
            if (!playerData.outOfGame)
            {
                Stack<IChipManager>[] chipsManagers = m_ChipsManager.CreateNewChipsManagers(
                    playerData.chips, m_ShadowLight, m_ShadowPlane);

                m_PlayersSeatManager.GiveChipsToPlayers(playerData.playerID, chipsManagers);
            }
            if(playerData.outOfGame || playerData.fold)
            {
                m_PlayersSeatManager.PlayerSeatManagers[playerData.playerID].HidePlayer();
            }
        }
    }

    private IEnumerator GiveChipsToPlayers(GameStateData gameStateData, Action calback)
    {
        foreach (var playerData in gameStateData.players)
        {
            Stack<IChipManager>[] toPlayerChips = m_ChipsManager.GiveChipsManagers(playerData.chips, m_ShadowLight, m_ShadowPlane);

            yield return StartCoroutine(m_PlayersSeatManager.GiveChipsToPlayers(
                playerData.playerID, m_ChipsManager.GetChipsMoveTime,
                m_ChipsManager.GetChipsMoveCurve, m_ChipsManager.GetChipsDeltaYCurve, toPlayerChips));
        }
        calback.Invoke();
        m_ClientMessaging.ReadyToStartGame();
    }

    private void SetCurrentPlayer(GameStateData gameStateData)
    {
        PlayerData currentPlayerData = gameStateData.currentPlayer;
        if (gameStateData.currentPlayerID == gameStateData.mainPlayerID &&
            gameStateData.step < GameState.ShowPlayersCards)
        {
            ShowPlayerChoose(currentPlayerData, gameStateData);
        }
        else
        {
            HideUserChoose();
        }

        if(gameStateData.step < GameState.ShowPlayersCards)
        {
            int lastPlayerId = gameStateData.currentPlayerID >= 1 ? gameStateData.currentPlayerID - 1 :
                gameStateData.players.Count - 1;

            m_PlayersSeatManager.HideTimer(lastPlayerId);
            m_PlayersSeatManager.ShowTimer(gameStateData.currentPlayerID, gameStateData.currentPlayer.isBot);
        }
    }

    private void SetCurrentPlayer(GameStateData gameStateData, Action calback)
    {
        SetCurrentPlayer(gameStateData);
        calback?.Invoke();
    }

    private void ShowPlayerChoose(PlayerData currentPlayerData, GameStateData gameStateData)
    {
        m_GameUI.SetMaxCost(currentPlayerData.sum);
        m_GameUI.SetCheckAndBetCost(gameStateData.ChackCost, gameStateData.BetCost);
        m_GameUI.ShowPlayerChoose();
    }

    private void PutCardsOnTable(GameStateData gameStateData)
    {
        m_CardsManager.MoveCardsToTable(gameStateData, m_ShadowLight,
            m_ShadowPlane, m_TableManager.CardPivots);
    }

    private IEnumerator PutCardsOnTable(GameStateData gameStateData, Action calback)
    {
        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(m_CardsManager.AnimateMoveCardsToTable(gameStateData, m_ShadowLight,
            m_ShadowPlane, m_TableManager.CardPivots));
        calback?.Invoke();
    }

    private void GiveCardToPlayers(GameStateData gameStateData)
    {
        foreach (var player in gameStateData.players)
        {
            if (!player.outOfGame && player.cards != null && player.cards.Count != 0)
            {
                m_CardsManager.GiveCardsToPlayer(player.playerID, player.playerID == gameStateData.mainPlayerID,
                player.cards, m_PlayersSeatManager.PlayerSeatManagers[player.playerID].CardsStandardPivots,
                m_ShadowLight, m_ShadowPlane);
            }
        }
    }

    private IEnumerator GiveCardToPlayers(GameStateData gameStateData, Action calback)
    {
        foreach (var player in gameStateData.players)
        {
            if (!player.outOfGame)
            {
                yield return StartCoroutine(m_CardsManager.GiveAndAnimateCardsToPlayer(
                    player.playerID, player.playerID == gameStateData.mainPlayerID,
                player.cards, m_PlayersSeatManager.PlayerSeatManagers[player.playerID].CardsStandardPivots,
                m_ShadowLight, m_ShadowPlane));
            }
        }
        calback?.Invoke();
    }

    private void ShowCards(GameStateData gameStateData)
    {
        HideUserChoose();
        foreach (PlayerSeatManager playerSeatManager in m_PlayersSeatManager.PlayerSeatManagers)
        {
            PlayerData playerData = gameStateData.players[playerSeatManager.ID];
            if (!playerData.outOfGame && !playerData.fold)
            {
                if (playerSeatManager.ID != gameStateData.mainPlayerID)
                {
                    m_CardsManager.ShowCards(playerSeatManager.ID, playerSeatManager.CardsShowPivots);
                }
                ShowHand(gameStateData, playerSeatManager);
            }
        }
    }

    private IEnumerator ShowCards(GameStateData gameStateData, Action calback)
    {
        foreach (var playerData in gameStateData.players)
        {
            if (playerData.playerID != gameStateData.mainPlayerID && !playerData.outOfGame && !playerData.fold)
            {
                m_CardsManager.SetFromHiddenCard(playerData.playerID, playerData.cards);
            }
        }

        foreach (IPlayerSeatManager playerSeatManager in m_PlayersSeatManager.PlayerSeatManagers)
        {
            PlayerData playerData = gameStateData.players[playerSeatManager.ID];
            if (!playerData.outOfGame && !playerData.fold)
            {
                if (playerSeatManager.ID != gameStateData.mainPlayerID)
                {
                    StartCoroutine(m_CardsManager.AnimateShowCards(playerSeatManager.ID, playerSeatManager.CardsShowPivots));
                }
                StartCoroutine(AnimateShowHand(gameStateData, playerSeatManager));
            }
        }

        yield return new WaitForSeconds(m_CardsManager.CardsShowTime);

        calback?.Invoke();
    }

    private void ShowHand(GameStateData gameStateData, PlayerSeatManager playerSeatManager)
    {
        Winner winner = gameStateData.GetWinner(playerSeatManager.ID);
        if (winner != null)
        {
            playerSeatManager.ShowHand(winner.hand);

            if (gameStateData.winners.Count == 1)
            {
                showCardsInProcess = true;
                int pivotIndex = 0;
                foreach (var card in winner.hand.cards5Sorted)
                {
                    m_CardsManager.ShowCardOnTable(m_CardsManager.GetCardManager(card), m_TableManager.CardPivots[pivotIndex]);
                    pivotIndex++;
                }

                foreach (var pivot in m_TableManager.CardPivots)
                {
                    m_CardsManager.ResetCardsInPivot(pivot, new Vector3(0.07f, 0.05f, 0.1f));
                }
                showCardsInProcess = false;
            }
        }
    }

    private bool showCardsInProcess = false;

    private IEnumerator AnimateShowHand(GameStateData gameStateData, IPlayerSeatManager playerSeatManager)
    {
        Winner winner = gameStateData.GetWinner(playerSeatManager.ID);
        if (winner != null)
        {
            playerSeatManager.ShowHand(winner.hand);
            while (showCardsInProcess)
            {
                yield return null;
            }

            if (gameStateData.winners.Count == 1)
            {
                showCardsInProcess = true;
                yield return new WaitForSeconds(3f);
                int pivotIndex = 0;
                WaitForSeconds waitForShowHands = new WaitForSeconds(0.5f * m_CardsManager.ShowHandsTime);
                foreach (var card in winner.hand.cards5Sorted)
                {
                    StartCoroutine(m_CardsManager.AnimateAndShowCardOnTable(m_CardsManager.GetCardManager(card),
                        m_TableManager.CardPivots[pivotIndex], new Vector3(0.07f, 0.05f, 0.1f), pivotIndex));
                    yield return waitForShowHands;
                    pivotIndex++;
                }
                showCardsInProcess = false;
            }
        }
    }

    private void OnUserChoose(PlayerChoose choos, int cost)
    {
        m_ClientMessaging.SentChipsCost(choos, cost);
    }

    private IEnumerator OnUserFold(GameStateData gameStateData, Action calback)
    {
        int playerId = gameStateData.currentPlayerID;
        if (playerId != gameStateData.mainPlayerID)
        {
            yield return StartCoroutine(m_PlayersSeatManager.SetChoseTextAndHidePlayer(playerId, PlayerChoose.Fold.ToString()));
        }
        calback?.Invoke();
    }

    private IEnumerator OnUserCheck(GameStateData gameStateData, Action calback)
    {
        int playerId = gameStateData.currentPlayerID;
        if (playerId != gameStateData.mainPlayerID)
        {
            yield return StartCoroutine(m_PlayersSeatManager.SetChoseTextAndWait(playerId, PlayerChoose.Check.ToString()));
        }
        calback?.Invoke();
    }

    private void PutChipsOnTable(GameStateData gameStateData)
    {
        int playerID = gameStateData.currentPlayerID;

        if (gameStateData.playerChooseData != null)
        {
            PlayerChoose userChoose = gameStateData.playerChooseData.playerChoose;
           
            if (playerID != gameStateData.mainPlayerID && !gameStateData.currentPlayer.fold)
            {
                m_PlayersSeatManager.SetChoseText(playerID, userChoose.ToString());
            }
        }

        Stack<IChipManager>[] chipManagers = m_ChipsManager.CreateNewChipsManagers(
            gameStateData.tableChips, m_ShadowLight, m_ShadowPlane);
        for (int index = 0; index < chipManagers.Length; index++)
        {
            Stack<IChipManager> stack = chipManagers[index];

            foreach (var chipManager in stack)
            {
                m_TableManager.PushChip(index, chipManager);
            }
        }

        m_ChipsManager.MoveChipsToTable(chipManagers, m_TableManager.ChipsPivots);

        foreach (var playerData in gameStateData.players)
        {
            if (playerData.bet != 0 && !playerData.fold)
            {
                m_PlayersSeatManager.SetBetText(playerData.playerID, "Sum: " + playerData.sum + ", Bet: " + playerData.bet);
            }
        }

        m_GameUI.SetMaxBetText(gameStateData);
    }
    private IEnumerator PutChipsOnTable(GameStateData gameStateData, Action calback)
    {
        PlayerChoose userChoose = gameStateData.playerChooseData.playerChoose;
        ChipsColumn[] chips = gameStateData.playerChooseData.chips;

       
        int playerID = gameStateData.currentPlayerID;
        yield return StartCoroutine(m_CardsManager.OpenClose(playerID, playerID == gameStateData.mainPlayerID));

        bool hasDifference = gameStateData.playerChooseData.hasDifference;
        if (hasDifference)
        {
            yield return StartCoroutine(m_PlayersSeatManager.GetChips(
                playerID, 1.5f * m_ChipsManager.GetChipsMoveTime, m_ChipsManager.StartChipsPivots,
            m_ChipsManager.GetChipsMoveCurve, m_ChipsManager.GetChipsDeltaYCurve, gameStateData.playerChooseData.toDealerChips,
            (int index, IChipManager chipManager) =>
            {
                m_ChipsManager.PushChip(index, chipManager);
            }));

            Stack<IChipManager>[] toPlayerChips = m_ChipsManager.GiveChipsManagers(
                gameStateData.playerChooseData.toPlayerChips, m_ShadowLight, m_ShadowPlane);

            yield return StartCoroutine(m_PlayersSeatManager.GiveChipsToPlayers(
                playerID, 1.5f * m_ChipsManager.GetChipsMoveTime,
                m_ChipsManager.GetChipsMoveCurve, m_ChipsManager.GetChipsDeltaYCurve, toPlayerChips));
        }

        if (playerID != gameStateData.mainPlayerID)
        {
            StartCoroutine(m_PlayersSeatManager.SetChoseTextAndWait(playerID, userChoose.ToString()));
        }
        
        yield return StartCoroutine(m_PlayersSeatManager.GetChips(
            playerID, m_ChipsManager.GetChipsMoveTime, m_TableManager.ChipsPivots,
        m_ChipsManager.GetChipsMoveCurve, m_ChipsManager.GetChipsDeltaYCurve, chips,
        (int index, IChipManager chipManager) =>
        {
            m_TableManager.PushChip(index, chipManager);
        }));

        PlayerData currentPlayer = gameStateData.currentPlayer;
        if (currentPlayer.bet != 0 && !currentPlayer.fold)
        {
            m_PlayersSeatManager.SetBetText(playerID, "Sum: " + currentPlayer.sum + ", Bet: " + currentPlayer.bet);
        }
        m_GameUI.SetMaxBetText(gameStateData);
        calback?.Invoke();
    }

    IEnumerator GiveChipsToPlayerFromTable(GameStateData gameStateData, Action calback)
    {
        if(gameStateData.currentPlayer.outOfGame || gameStateData.currentPlayer.fold)
        {
            yield break;
        }
        HideUserChoose();

        Stack<IChipManager>[] chipsManagers = m_TableManager.GiveChips(gameStateData.winnerPrize);
        yield return StartCoroutine(m_PlayersSeatManager.GiveChipsToPlayerFromTable(gameStateData.currentPlayerID,
            m_ChipsManager.GetChipsMoveTime,
            m_ChipsManager.GetChipsMoveCurve, m_ChipsManager.GetChipsDeltaYCurve, chipsManagers));
        m_GameUI.SetMaxBetText(gameStateData);

        foreach (var playerData in gameStateData.players)
        {
            m_PlayersSeatManager.HideBetText(playerData.playerID);
        }
        calback?.Invoke();
    }

    public void OnEndGame(GameStateData gameStateData, Action calback)
    {
        OnEndGame(gameStateData);
        calback?.Invoke();
    }

    public void OnEndGame(GameStateData gameStateData)
    {
        foreach (var winnerData in gameStateData.winners)
        {
            m_PlayersSeatManager.SetAsWinner(winnerData.id);
        }
    }
}
