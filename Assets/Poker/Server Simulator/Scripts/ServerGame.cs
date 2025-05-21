using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerGame : MonoBehaviour
{
    [SerializeField] private GameStateData m_GameStateData;

    public int ID { get; private set; }

    public string GameStateAsJSON => JsonUtility.ToJson(m_GameStateData);
    public string GameBaseDataAsJSON => JsonUtility.ToJson(new GameStateBaseData(m_GameStateData));
    public string GamePlayersAsJSON => JsonUtility.ToJson(new GameStatePlayersData(m_GameStateData));
    public string GameCurrentPlayerAsJSON => JsonUtility.ToJson(new GameStateCurrentPlayerData(m_GameStateData));
    public string GameTableChipsAsJSON => JsonUtility.ToJson(new GameStateTableChipsData(m_GameStateData));
    public string GameTableCardsAsJSON => JsonUtility.ToJson(new GameStateTableCardsData(m_GameStateData));
    public string GamePlayerChooseDataAsJSON => JsonUtility.ToJson(new GameStatePlayerChooseData(m_GameStateData));

    public string SavedPlayersCardsDataAsJSON => JsonUtility.ToJson(m_GameStateData.GetSavedPlayersCardsData());

    public GameStateData GameStateData => m_GameStateData;

    private ChipsColumn[] defaultChips;

    public void Setup(int id, bool fromSavedData, GameStateData gameStateData)
    {
        ID = id;
        int playersCount = gameStateData.players.Count;
        if (!fromSavedData)
        {
            foreach (var chip in gameStateData.chips)
            {
                chip.count *= playersCount + 1;
            }
        }
        defaultChips = ChipsColumn.CopyFrom(gameStateData.chips);
        m_GameStateData = gameStateData;
    }

    public void NextPlayer()
    {
        int chackCount = 0;
        do
        {
            chackCount++;
            if (m_GameStateData.currentPlayerID + 1 < m_GameStateData.players.Count)
            {
                m_GameStateData.currentPlayerID++;
            }
            else
            {
                m_GameStateData.currentPlayerID = 0;
            }
            m_GameStateData.currentPlayer = m_GameStateData.players[m_GameStateData.currentPlayerID];

        }
        while (chackCount < m_GameStateData.players.Count && (m_GameStateData.currentPlayer.outOfGame ||
        m_GameStateData.currentPlayer.fold || m_GameStateData.currentPlayer.sum == 0));
    }

    public void GiveChipsToPlayers()
    {
        int playersCount = m_GameStateData.players.Count;

        int[] addedChips = new int[m_GameStateData.chips.Length];

        foreach (var player in m_GameStateData.players)
        {
            if (!player.outOfGame)
            {
                player.chips = new ChipsColumn[m_GameStateData.chips.Length];
                for (int i = 0; i < m_GameStateData.chips.Length; i++)
                {
                    ChipsColumn chipsColumn = m_GameStateData.chips[i];
                    int chipsCount = chipsColumn.count / (playersCount + 1);
                    player.chips[i] = new ChipsColumn(chipsCount, chipsColumn);
                    addedChips[i] += chipsCount;
                }

                player.sum = GetSum(player.chips);
            }
            else
            {
                playersCount--;
            }
        }

        for (int i = 0; i < addedChips.Length; i++)
        {
            m_GameStateData.chips[i].count -= addedChips[i];
        }
    }

    public int GetSum(ChipsColumn[] chips)
    {
        if (chips == null)
        {
            return 0;
        }
        int sum = 0;
        foreach (var chipList in chips)
        {
            sum += chipList.cost * chipList.count;
        }
        return sum;
    }

    

    public void ResetChipsOnTable()
    {
        m_GameStateData.tableChips = null;
        foreach (var player in m_GameStateData.players)
        {
            player.bet = 0;
        }
    }

    private int GetRealCost(int sum, ChipsColumn[] playerChips, out ChipsColumn[] chips)
    {
        chips = new ChipsColumn[m_GameStateData.chips.Length];

        int[] chipsCounts = new int[chips.Length];
        int subCostPerIndex = sum;
        int realCost = 0;

        for (int i = playerChips.Length - 1; i >= 0; i--)
        {
            ChipsColumn chipsListsByIndex = playerChips[i];
            if (chipsListsByIndex.count == 0)
            {
                continue;
            }
            int chipsCount = Mathf.Clamp(subCostPerIndex / chipsListsByIndex.cost, 0, chipsListsByIndex.count);
            int deltaCost = chipsCount * chipsListsByIndex.cost;
            subCostPerIndex -= deltaCost;
            realCost += deltaCost;
            chipsCounts[i] = chipsCount;
        }
        for (int i = 0; i < chipsCounts.Length; i++)
        {
            chips[i] = new ChipsColumn(chipsCounts[i], playerChips[i]);
        }

        return realCost;
    }

    public void PutChipsFromPlayerOnTable(PlayerChoose userChoose, int cost, bool setChoose,
        out bool hasDifference, out ChipsColumn[] toPlayerChips, out ChipsColumn[] toDealerChips,
        Action<bool, bool, ChipsColumn[]> handler)
    {
        PlayerData playerData = m_GameStateData.players[m_GameStateData.currentPlayerID];

        hasDifference = false;
        toPlayerChips = null;
        toDealerChips = null;

        if (setChoose)
        {
            playerData.Choosed = true;
        }

        if (playerData.outOfGame || playerData.fold)
        {
            playerData.prizeCost = 0;
            handler?.Invoke(true, false, null);
            return;
        }

        if (userChoose == PlayerChoose.Check)
        {
            handler?.Invoke(false, false, null);
            return;
        }

        if (userChoose == PlayerChoose.Fold)
        {
            playerData.fold = true;
            playerData.prizeCost = 0;
            handler?.Invoke(false, false, null);
            return;
        }

        if (cost + playerData.bet < m_GameStateData.playersMaxBet &&
            playerData.sum + playerData.bet >= m_GameStateData.playersMaxBet)
        {
            handler?.Invoke(false, true, null);
            return;
        }

        int realCost = GetRealCost(cost, playerData.chips, out ChipsColumn[] chips);

        if (m_GameStateData.tableChips == null || m_GameStateData.tableChips.Length == 0)
        {
            m_GameStateData.tableChips = new ChipsColumn[m_GameStateData.chips.Length];
            for (int i = 0; i < m_GameStateData.tableChips.Length; i++)
            {
                m_GameStateData.tableChips[i] = new ChipsColumn(0, m_GameStateData.chips[i]);
            }
        }

        for (int i = 0; i < chips.Length; i++)
        {
            playerData.chips[i].count -= chips[i].count;
            m_GameStateData.tableChips[i].count += chips[i].count;
        }

        if (realCost < cost && playerData.sum > cost)
        {
            hasDifference = true;
            int difference = cost - realCost;
            
            realCost = defaultChips[0].cost * (cost / defaultChips[0].cost);
            GetRealCost(difference, defaultChips, out ChipsColumn[] needChips);
            toDealerChips = ChipsColumn.CopyFrom(0, chips);

            for (int i = 0; i < playerData.chips.Length; i++)
            {
                if(playerData.chips[i].count > 0)
                {
                    toDealerChips[i].count = 1;
                    break;
                }
            }
            Debug.Log("difference " + difference);
            difference = GetSum(toDealerChips) - GetSum(needChips);
            GetRealCost(difference, defaultChips, out ChipsColumn[] defChips);

            toPlayerChips = new ChipsColumn[chips.Length];

            for (int i = 0; i < chips.Length; i++)
            {
                toPlayerChips[i] = new ChipsColumn(needChips[i].count + defChips[i].count, chips[i]);
            }

            for (int i = 0; i < chips.Length; i++)
            {
                playerData.chips[i].count -= toDealerChips[i].count;
                playerData.chips[i].count += toPlayerChips[i].count;

                m_GameStateData.chips[i].count += toDealerChips[i].count;
                m_GameStateData.chips[i].count -= toPlayerChips[i].count;
                if(m_GameStateData.chips[i].count < 0)
                {
                    m_GameStateData.chips[i].count = 0;
                }

                chips[i].count += needChips[i].count;
                playerData.chips[i].count -= needChips[i].count;
                m_GameStateData.tableChips[i].count += needChips[i].count;
            }
        }

        playerData.bet += realCost;
        playerData.sum = GetSum(playerData.chips);
        m_GameStateData.bet += realCost;

        if (m_GameStateData.playersMaxBet < playerData.bet)
        {
            m_GameStateData.playersMaxBet = playerData.bet;
        }

        foreach (var player in GameStateData.players)
        {
            player.prizeCost = 0;
            if (!player.outOfGame && !player.fold && player.bet != 0)
            {
                foreach (var item in GameStateData.players)
                {
                    if (!item.outOfGame && item.bet != 0)
                    {
                        player.prizeCost += Mathf.Clamp(item.bet, 0, player.bet);
                    }
                }
            }
        }

        handler?.Invoke(false, false, chips);
    }


    public void PutChipsFromTableToPlayer(int playerID, int cost)
    {
        ChipsColumn[] chips = new ChipsColumn[m_GameStateData.chips.Length];

        int[] chipsCounts = new int[chips.Length];
        int subCostPerIndex = cost;
        int realCost = 0;

        for (int i = m_GameStateData.tableChips.Length - 1; i >= 0; i--)
        {
            ChipsColumn chipsListsByIndex = m_GameStateData.tableChips[i];
            if (chipsListsByIndex.count == 0)
            {
                continue;
            }
            int chipsCount = Mathf.Clamp(subCostPerIndex / chipsListsByIndex.cost, 0, chipsListsByIndex.count);
            int deltaCost = chipsCount * chipsListsByIndex.cost;
            subCostPerIndex -= deltaCost;
            realCost += deltaCost;
            chipsCounts[i] = chipsCount;
        }

        for (int i = 0; i < chipsCounts.Length; i++)
        {
            chips[i] = new ChipsColumn(chipsCounts[i], m_GameStateData.tableChips[i]);
        }

        int chipsIndex = 0;
        PlayerData playerData = m_GameStateData.players[playerID];
        foreach (var chipColumn in chips)
        {
            playerData.chips[chipsIndex].count += chipColumn.count;
            m_GameStateData.tableChips[chipsIndex].count -= chipColumn.count;
            chipsIndex++;
        }

        playerData.sum = GetSum(playerData.chips);

        m_GameStateData.bet -= realCost;

        m_GameStateData.winnerPrize = chips;
    }

    public void GiveCardsToPlayers()
    {
        foreach (var player in m_GameStateData.players)
        {
            if (!player.outOfGame)
            {
                player.cards = new List<string>(0);
                for (int i = 0; i < 2; i++)
                {
                    int cardIndex = UnityEngine.Random.Range(0, m_GameStateData.Cards.Count);
                    string cardName = m_GameStateData.Cards[cardIndex];
                    m_GameStateData.Cards.RemoveAt(cardIndex);
                    player.cards.Add(cardName);
                }
            }
            else
            {
                player.cards = null;
            }
        }

        m_GameStateData.SavePlayersCards();
    }

    public void GiveCardsToPlayers(SavedPlayersCardsData savedPlayersCardsData)
    {
        int playerIndex = 0;
        foreach (var player in m_GameStateData.players)
        {
            if (!player.outOfGame)
            {
                player.cards = savedPlayersCardsData.cardsData[playerIndex].cards;
                foreach (var cardName in player.cards)
                {
                    if (m_GameStateData.Cards.Contains(cardName))
                    {
                        m_GameStateData.Cards.Remove(cardName);
                    }
                }
            }
            else
            {
                player.cards = null;
            }
            playerIndex++;
        }

        m_GameStateData.SavePlayersCards();
    }
    /// <summary>
    /// Hide other players card from local player
    /// </summary>
    /// <param name="playerID"></param>
    public void HideOtherPlayersCards(int playerID)
    {
        foreach (var player in m_GameStateData.players)
        {
            if (player.playerID != playerID)
            {
                if (player.outOfGame)
                {
                    player.cards = null;
                }
                else
                {
                    player.cards = new List<string>(0);
                    for (int i = 0; i < 2; i++)
                    {
                        player.cards.Add("Hidden_" + player.playerID + "_" + i);
                    }
                }
            }
        }
    }

    public void ClearTableCards()
    {
        m_GameStateData.tableCards = null;
    }

    public void PutCardsOnTable(int count)
    {
        if (m_GameStateData.tableCards == null)
        {
            m_GameStateData.tableCards = new List<string>(0);
        }
        if (m_GameStateData.tableCards.Count < 5)
        {
            for (int i = 0; i < count; i++)
            {
                if (m_GameStateData.tableCards.Count < 5)
                {
                    int cardIndex = UnityEngine.Random.Range(0, m_GameStateData.Cards.Count);
                    string cardName = m_GameStateData.Cards[cardIndex];
                    m_GameStateData.Cards.RemoveAt(cardIndex);
                    m_GameStateData.tableCards.Add(cardName);
                }
            }
        }
        foreach (var playerData in m_GameStateData.players)
        {
            playerData.Choosed = false;
        }
    }

    public void SetCardsOnTable(List<string> tableCards)
    {
        m_GameStateData.tableCards = new List<string>(0);

        foreach (string cardName in tableCards)
        {
            if(m_GameStateData.Cards.Contains(cardName))
            {
                m_GameStateData.Cards.Remove(cardName);
            }
            m_GameStateData.tableCards.Add(cardName);
        }
        foreach (var playerData in m_GameStateData.players)
        {
            playerData.Choosed = false;
        }
    }

    public void RessetCards(List<string> cards)
    {
        m_GameStateData.Cards.Clear();
        foreach (var card in cards)
        {
            m_GameStateData.Cards.Add(card);
        }
        m_GameStateData.tableCards.Clear();
        foreach (var playerData in m_GameStateData.players)
        {
            playerData.bet = 0;
            playerData.prizeCost = 0;
            playerData.Choosed = false;
            playerData.sum = GetSum(playerData.chips);
            playerData.cards?.Clear();

            playerData.outOfGame = playerData.sum == 0;
            if (playerData.outOfGame)
            {
                playerData.chips = null;
            }

            if (!playerData.outOfGame)
            {
                playerData.fold = false;
            }
        }

        m_GameStateData.bet = 0;
        m_GameStateData.playersMaxBet = 0;


        Debug.Log("End " + m_GameStateData.Cards.Count);
    }
}
