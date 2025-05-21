using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class GameStateBaseData
{
    public GameState step;
    public GameState state;
    public int playersMaxBet;
    public int dealerID;
    public int smallBlindID;
    public int bigBlindID;
    public int mainPlayerID;
    public int currentPlayerID;
    public int bet;

    public GameStateBaseData(GameStateData gameStateData)
    {
        step = gameStateData.step;
        state = gameStateData.state;
        playersMaxBet = gameStateData.playersMaxBet;
        dealerID = gameStateData.dealerID;
        smallBlindID = gameStateData.smallBlindID;
        bigBlindID = gameStateData.bigBlindID;
        currentPlayerID = gameStateData.currentPlayerID;
        mainPlayerID = gameStateData.mainPlayerID;
        bet = gameStateData.bet;
    }
}

[Serializable]
public class GameStatePlayersData : GameStateBaseData
{
    public List<PlayerData> players;
    public List<Winner> winners;

    public GameStatePlayersData(GameStateData gameStateData):base(gameStateData)
    {
        players = gameStateData.players;
        winners = gameStateData.winners;
    }
}

[Serializable]
public class GameStateCurrentPlayerData : GameStateBaseData
{
    public PlayerData currentPlayer;

    public GameStateCurrentPlayerData(GameStateData gameStateData) : base(gameStateData)
    {
        currentPlayer = gameStateData.currentPlayer;
    }
}

[Serializable]
public class GameStateTableChipsData : GameStateBaseData
{
    public ChipsColumn[] tableChips;
    public PlayerData currentPlayer;

    public GameStateTableChipsData(GameStateData gameStateData) : base(gameStateData)
    {
        tableChips = gameStateData.tableChips;
        currentPlayer = gameStateData.currentPlayer;
    }
}

[Serializable]
public class CardsData
{
    public List<string> cards;

    public CardsData(List<string> cards)
    {
        this.cards = new List<string>(cards);
    }
}

[Serializable]
public class GameStateTableCardsData : GameStateBaseData
{
    public List<string> tableCards;

    public GameStateTableCardsData(GameStateData gameStateData) : base(gameStateData)
    {
        tableCards = gameStateData.tableCards;
    }
}

[Serializable]
public class GameStatePlayerChooseData : GameStateTableChipsData
{
    public PlayerChooseData playerChooseData;

    public GameStatePlayerChooseData(GameStateData gameStateData) : base(gameStateData)
    {
        playerChooseData = gameStateData.playerChooseData;
    }
}

[Serializable]
public class SavedPlayersCardsData
{
    [Serializable]
    public class CardsData
    {
        public List<string> cards;

        public CardsData(List<string> cards)
        {
            this.cards = cards == null? null : new List<string>(cards);
        }
    }
    public List<CardsData> cardsData;

    public SavedPlayersCardsData(List<List<string>> playersCards)
    {
        cardsData = new List<CardsData>(0);
        foreach (List<string> list in playersCards)
        {
            cardsData.Add(new CardsData(list));
        }
    }

    public List<List<string>> GetSavedPlayersCards()
    {
        List<List<string>> savedPlayersCards = new List<List<string>>();
        foreach (CardsData cardData in cardsData)
        {
            savedPlayersCards.Add(cardData.cards);
        }
        return savedPlayersCards;
    }
}

[Serializable]
public class GameStateData : GameStateBaseData
{
    [SerializeField] private PokerGameConfig m_PokerGameConfig;
    [SerializeField] private List<string> cards;

    public List<string> Cards { get; private set; }
    public List<string> tableCards;
    public ChipsColumn[] chips;
    public ChipsColumn[] tableChips;
    public List<PlayerData> players;
    public PlayerData currentPlayer;
    public List<Winner> winners;
    public PlayerChooseData playerChooseData;
    public ChipsColumn[] winnerPrize;

    private List<List<string>> savedPlayersCards;

    public bool GameIsEnded()
    {
        int inGamePlayersCount = 0;
        foreach (var playerData in players)
        {
            if(!playerData.outOfGame)
            {
                inGamePlayersCount++;
            }
        }

        return inGamePlayersCount <= 1;
    }

    public void SavePlayersCards()
    {
        savedPlayersCards = new List<List<string>>();
        foreach (var playerData in players)
        {
            if (playerData.outOfGame)
            {
                savedPlayersCards.Add(null);
            }
            else
            {
                savedPlayersCards.Add(new List<string>(new List<string>(playerData.cards)));
            }
        }
        Debug.Log(JsonUtility.ToJson(new SavedPlayersCardsData(savedPlayersCards)));
    }

    public void SetSavedPlayersCards()
    {
        for (int i = 0; i < players.Count; i++)
        {
            players[i].cards = savedPlayersCards[i];
        }
    }

    public SavedPlayersCardsData GetSavedPlayersCardsData()
    {
        if (savedPlayersCards != null)
        {
            return new SavedPlayersCardsData (savedPlayersCards);
        }
        return null;
    }

    public void SetSavedPlayersCardsData(SavedPlayersCardsData savedPlayersCardsData)
    {
        if (savedPlayersCardsData != null)
        {
            savedPlayersCards = savedPlayersCardsData.GetSavedPlayersCards();
        }
    }

    public int ChackCost => playersMaxBet == 0 ? 10 : (playersMaxBet == 10 ? 20 : playersMaxBet - currentPlayer.bet);
    public int BetCost => playersMaxBet == 0 ? 10 : Mathf.Clamp(ChackCost + 20, 20, currentPlayer.sum);

    public Winner GetWinner(int playerID)
    {
        foreach (var winer in winners)
        {
            if(winer.id == playerID)
            {
                return winer;
            }
        }
        return null;
    }

    public bool AllIsChoosed
    {
        get
        {
            foreach (var player in players)
            {
                if(!player.outOfGame && !player.fold && !player.Choosed)
                {
                    return false;
                }
            }
            return true;
        }
    }

    public int ActivePlayersCount
    {
        get
        {
            int activePlayersCount = 0;
            foreach (var player in players)
            {
                if (!player.fold && !player.outOfGame && player.sum > 0)
                {
                    activePlayersCount++;
                }
            }
            return activePlayersCount;
        }
    }

    public bool HasComplitedTable
    {
        get
        {
            foreach (var playerData in players)
            {
                if(playerData.chips == null || playerData.chips.Length == 0)
                {
                    return true;
                }
                if (!playerData.outOfGame && !playerData.fold && playerData.sum != 0 && (playerData.bet == 0 || playerData.bet != playersMaxBet))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public GameStateData(GameStateData gameStateData, bool full) : base(gameStateData)
    {
        step = gameStateData.step;
        Cards = new List<string>(gameStateData.cards);
        cards = null;

        chips = ChipsColumn.CopyFrom(gameStateData.chips);

        players = new List<PlayerData>(0);

        if(full)
        {
            dealerID = gameStateData.dealerID;
            smallBlindID = gameStateData.smallBlindID;
            bigBlindID = gameStateData.bigBlindID;

            tableCards = new List<string>(gameStateData.tableCards);
            tableChips = ChipsColumn.CopyFrom(gameStateData.tableChips);
      
            foreach (var player in gameStateData.players)
            {
                players.Add(new PlayerData(player));
            }

            currentPlayer = new PlayerData(gameStateData.currentPlayer);

            if (gameStateData.winners != null)
            {
                winners = new List<Winner>(0);
                foreach (var winner in gameStateData.winners)
                {
                    winners.Add(new Winner(winner));
                }
            }

            playerChooseData = new PlayerChooseData(gameStateData.playerChooseData);

            winnerPrize = new ChipsColumn[gameStateData.winnerPrize.Length];
            for (int i = 0; i < winnerPrize.Length; i++)
            {
                winnerPrize[i] = new ChipsColumn(gameStateData.winnerPrize[i]);
            }
            
        }
    }
   
    public void SetConfigs()
    {
        cards = m_PokerGameConfig.cards;
        chips = m_PokerGameConfig.chips;
    }

    public void SaveCards()
    {
        Cards = new List<string>(cards);
    }

    public void SaveCards(GameStateData gameStateData)
    {
        Cards = new List<string>((cards == null || cards.Count == 0) ? gameStateData.cards : cards);
    }
}

public enum GameState
{
    Start = 0,
    GivePlayersChips,
    PlayersBet,
    GivePlayersCards,
    PutTableCards,
    ShowPlayersCards,
    GiveWinnersPrize,
    Ended
}

[Serializable]
public class PlayerData
{
    public string playerName;
    public int playerID;
    public List<string> cards;
    public ChipsColumn[] chips;
    public int sum;
    public int bet;
    public bool outOfGame;
    public bool fold;
    public int prizeCost;

    public bool isBot;

    public bool Choosed { get; internal set; }

    public PlayerData(string playeName, int playerID, bool isBot)
    {
        this.playerName = playeName;
        this.playerID = playerID;
        this.isBot = isBot;
    }

    public PlayerData(int playerID, bool isBot)
    {
        this.playerName = "Player " + playerID;
        this.playerID = playerID;
        this.isBot = isBot;
    }

    public PlayerData(PlayerData playerData)
    {
        this.playerName = playerData.playerName;
        playerID = playerData.playerID;
        cards = new List<string>(playerData.cards);
        chips = new ChipsColumn[playerData.chips.Length];
        for (int i = 0; i < chips.Length; i++)
        {
            chips[i] = new ChipsColumn(playerData.chips[i]);
        }
        sum = playerData.sum;
        bet = playerData.bet;
        outOfGame = playerData.outOfGame;
        fold = playerData.fold;
        prizeCost = playerData.prizeCost;
        this.isBot = playerData.isBot;
    }
}

[Serializable]
public class PlayerChooseData
{
    public PlayerChoose playerChoose;
    public ChipsColumn[] chips;

    public bool hasDifference;
    public ChipsColumn[] toPlayerChips;
    public ChipsColumn[] toDealerChips;

    public PlayerChooseData(PlayerChooseData playerChooseData)
    {
        playerChoose = playerChooseData.playerChoose;
        chips = ChipsColumn.CopyFrom(playerChooseData.chips);
        hasDifference = playerChooseData.hasDifference;
        toPlayerChips = ChipsColumn.CopyFrom(playerChooseData.toPlayerChips);
        toDealerChips = ChipsColumn.CopyFrom(playerChooseData.toDealerChips);
    }

    public PlayerChooseData(PlayerChoose playerChoose, ChipsColumn[] chips,
        bool hasDifference, ChipsColumn[] toPlayerChips, ChipsColumn[] toDealerChips)
    {
        this.playerChoose = playerChoose;
        this.chips = chips;
        this.hasDifference = hasDifference;
        this.toPlayerChips = toPlayerChips;
        this.toDealerChips = toDealerChips;
    }
}

[Serializable]
public class ChipsColumn
{
    public string name;
    public int cost;
    public int count;

    public static ChipsColumn[] CopyFrom(ChipsColumn[] columns)
    {
        ChipsColumn[] newColumns = new ChipsColumn[columns.Length];
        for (int i = 0; i < newColumns.Length; i++)
        {
            newColumns[i] = new ChipsColumn(columns[i]);
        }
        return newColumns;
    }

    public static ChipsColumn[] CopyFrom(int chipsCount, ChipsColumn[] columns)
    {
        ChipsColumn[] newColumns = new ChipsColumn[columns.Length];
        for (int i = 0; i < newColumns.Length; i++)
        {
            newColumns[i] = new ChipsColumn(chipsCount, columns[i]);
        }
        return newColumns;
    }

    public ChipsColumn(ChipsColumn chipsList)
    {
        name = chipsList.name;
        cost = chipsList.cost;
        count = chipsList.count;
    }

    public ChipsColumn(int chipsCount, ChipsColumn copy)
    {
        name = copy.name;
        cost = copy.cost;
        count = chipsCount;
    }

    public void Clear()
    {
        count = 0;
    }
}
