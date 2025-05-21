using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.SceneManagement;

public class ServerSimulator : MonoBehaviour
{
    [SerializeField] private GameStateData m_GameStateData;
    [SerializeField] private ServerMessaging m_ServerMessaging;
    [SerializeField] private GameLogicManager m_GameLogicManager;

    private Dictionary<int, ServerGame> gamesDictionary;

    [Space(10), Header("Game"), Header("UI")]
    [SerializeField] private InputField m_PlayersCountField;
    [SerializeField] private Button m_CreateGameButton;
    [SerializeField] private Button m_SaveGameButton;
    [SerializeField] private Button m_DeleteGameButton;
    [Space(10), Header("Player")]
    [SerializeField] private int m_MaxPlayersCount =4;
    [SerializeField] private Text m_CurrentPlayerText;
    [SerializeField] private Text m_CurrentPlayerText1;
    [Space(10), Header("Chips")]
    [SerializeField] private InputField m_PutChipsField;
    [SerializeField] private Button m_BetButton;
    [SerializeField] private Button m_CallButton;
    [SerializeField] private Button m_CheckButton;
    [SerializeField] private Button m_FoldButton;
    [SerializeField] private InputField m_PutChipsFromTableToPlayerIDField;
    [SerializeField] private InputField m_PutChipsFromTableToPlayerField;
    [SerializeField] private Button m_PutChipsFromTableToPlayerButton;
    [Space(10), Header("Cards")]
    [SerializeField] private Button m_GiveCardsToPlayers;
    [SerializeField] private InputField m_PutCardsOnTableField;
    [SerializeField] private Button m_PutCardsOnTableButton;
    [SerializeField] private Button m_ShowCardsButton;
    [SerializeField] private Button m_RessetCardsButton;

#if UNITY_WEBGL && !UNITY_EDITOR
    private void Awake()
    {
        m_SaveGameButton.gameObject.SetActive(false);
        m_DeleteGameButton.gameObject.SetActive(false);
    }
#endif

    private IEnumerator Start()
    {
        m_GameStateData.SetConfigs();
        m_GameStateData.SaveCards();

        TriggerPlayerChooseButtons(false);
        DisableOtherButtons();

        gamesDictionary = new Dictionary<int, ServerGame>(0);

      //asad  m_CreateGameButton.onClick.AddListener(CreateGame);

        m_SaveGameButton.onClick.AddListener(() =>
        {
            SaveGameData(0);
        });

        m_DeleteGameButton.onClick.AddListener(() =>
        {
            m_DeleteGameButton.interactable = false;
            DeleteGame(0);
        });

        yield return SceneManager.LoadSceneAsync(MenuUI.GameScenName, LoadSceneMode.Additive);
        yield return new WaitForEndOfFrame();
        m_ServerMessaging.Init();

        GameStateData gameStateData = GetGameData(0);
        if (gameStateData != null)
        {
            CreateGame(0, gameStateData);
        }
        yield return new WaitForSeconds(1);
        CreateGame();
    }

    private void TriggerPlayerChooseButtons(bool isOn)
    {
        m_BetButton.interactable = isOn;
        m_CallButton.interactable = isOn;
        m_CheckButton.interactable = isOn;
        m_FoldButton.interactable = isOn;
    }

    private void DisableOtherButtons()
    {
        m_GiveCardsToPlayers.interactable = false;
        m_PutCardsOnTableButton.interactable = false;
        m_ShowCardsButton.interactable = false;
        m_PutChipsFromTableToPlayerButton.interactable = false;
        m_RessetCardsButton.interactable = false;
    }

    private void DeleteGame(int gameID)
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
        if (gamesDictionary.ContainsKey(gameID))
        {
            gamesDictionary.Remove(gameID);
            File.Delete(GetGameSavePath(gameID));
            File.Delete(GetGameSavedPlayersCardsData(gameID));
            StartCoroutine(ReloadScene());
        }
        #endif
    }

    private IEnumerator ReloadScene()
    {
        yield return SceneManager.UnloadSceneAsync(MenuUI.GameScenName);
        yield return new WaitForEndOfFrame();
        Resources.UnloadUnusedAssets();
        yield return new WaitForEndOfFrame();
        SceneManager.LoadScene("ServerSimulator");
    }

    private void CreateGame()
    {
        TriggerPlayerChooseButtons(true);
        m_CreateGameButton.interactable = false;

        int.TryParse(m_PlayersCountField.text, out int playersCount);
        playersCount = Mathf.Clamp(playersCount, 4, m_MaxPlayersCount);
        m_PlayersCountField.text = playersCount + "";

        m_ServerMessaging.OnUserChoose += OnUserChoose;

        int dealerID = Random.Range(0, playersCount);
        ServerGame serverGame = CreateGame(0, playersCount, dealerID);
        SetupGame(serverGame);




        serverGame.GameStateData.state = GameState.GivePlayersCards; // update state
        serverGame.GiveCardsToPlayers();  // distribute cards
        m_ServerMessaging.GiveCardsToPlayers(serverGame.GameStateData.mainPlayerID, serverGame.GamePlayersAsJSON);

        // Disable card dealing button to avoid multiple dealing
        m_GiveCardsToPlayers.interactable = false;
    }

    private void CreateGame(int id, GameStateData gameStateData)
    {
        gameStateData.currentPlayer = gameStateData.players[gameStateData.currentPlayerID];

        m_ServerMessaging.OnUserChoose += OnUserChoose;

        gameStateData.SaveCards(m_GameStateData);

        ServerGame serverGame = new GameObject("Game_" + id).AddComponent<ServerGame>();
        serverGame.transform.SetParent(transform);
        gamesDictionary.Add(id, serverGame);
        serverGame.Setup(id, true, gameStateData);

        SetupGame(serverGame);

        m_ServerMessaging.SendGameStateData(gameStateData.mainPlayerID, serverGame.GameStateAsJSON);


        m_CreateGameButton.interactable = false;
        TriggerPlayerChooseButtons(gameStateData.step >= GameState.GivePlayersChips &&
                                   gameStateData.step < GameState.ShowPlayersCards);

        m_GiveCardsToPlayers.interactable = gameStateData.step == GameState.PlayersBet;

        m_PutCardsOnTableButton.interactable = gameStateData.step == GameState.GivePlayersCards ||
            (gameStateData.step == GameState.PutTableCards && gameStateData.tableCards?.Count < 5);

        m_ShowCardsButton.interactable = gameStateData.tableCards?.Count == 5 &&
                                               gameStateData.step == GameState.PutTableCards;

        m_PutChipsFromTableToPlayerButton.interactable = gameStateData.step == GameState.ShowPlayersCards;

        m_RessetCardsButton.interactable = gameStateData.step == GameState.GiveWinnersPrize;


        int playersCount = gameStateData.players.Count;
        m_PlayersCountField.text = playersCount + "";

        m_CurrentPlayerText.text = gameStateData.currentPlayerID + "";
        m_CurrentPlayerText1.text = "Player "+gameStateData.currentPlayerID + " Turn";
        m_PutChipsField.text = gameStateData.ChackCost + "";

        if (m_PutCardsOnTableButton.interactable)
        {
            m_PutCardsOnTableField.text = (gameStateData.tableCards == null || gameStateData.tableCards.Count == 0) ? "3" : "1";
        }

        if (gameStateData.state == GameState.ShowPlayersCards)
        {
            if (gameStateData.winners.Count == 1)
            {
                m_PutChipsFromTableToPlayerIDField.text = gameStateData.winners[0].id + "";
            }
            m_PutChipsFromTableToPlayerField.text = gameStateData.bet + "";
        }
    }

    private ServerGame CreateGame(int id, int playersCount, int dealerID)
    {
        if (gamesDictionary.ContainsKey(id))
        {
            return null;
        }
        GameStateData gameStateData = new GameStateData(m_GameStateData, false)
        {
            players = new List<PlayerData>(0),
            dealerID = dealerID,
            smallBlindID = dealerID + 1 < playersCount ? dealerID + 1 : 0,
            bigBlindID = dealerID + 2 < playersCount ? dealerID + 2 : dealerID + 2 - playersCount,
            state = GameState.Start
        };

        for (int i = 0; i < playersCount; i++)
        {
            PlayerData playerData = new PlayerData(i, false);
            gameStateData.players.Add(playerData);
        }
        gameStateData.currentPlayerID = dealerID + 1 < playersCount ? dealerID + 1 : 0;
        gameStateData.currentPlayer = gameStateData.players[gameStateData.currentPlayerID];

        ServerGame serverGame = new GameObject("Game_" + id).AddComponent<ServerGame>();
        serverGame.transform.SetParent(transform);
        gamesDictionary.Add(id, serverGame);
        serverGame.Setup(id, false, gameStateData);

        gameStateData.mainPlayerID = 1;//For sending to player with id 1
        gameStateData.step =  GameState.Start;
        m_ServerMessaging.StartGame(gameStateData.mainPlayerID, serverGame.GameStateAsJSON);

        serverGame.GiveChipsToPlayers();
        serverGame.ResetChipsOnTable();
        gameStateData.step =  GameState.GivePlayersChips;
        gameStateData.state = GameState.GivePlayersChips;
        m_ServerMessaging.GiveChipsToPlayers(gameStateData.mainPlayerID, serverGame.GamePlayersAsJSON);

        m_CurrentPlayerText.text = gameStateData.currentPlayerID + "";
        m_CurrentPlayerText1.text = "Player "+gameStateData.currentPlayerID + " Turn";
        m_ServerMessaging.SetCurrentPlayer(gameStateData.mainPlayerID, serverGame.GameCurrentPlayerAsJSON);
        m_ServerMessaging.OpenTableForPlayersChips(gameStateData.mainPlayerID, serverGame.GameBaseDataAsJSON);

        return serverGame;
    }

    private void SetupGame(ServerGame serverGame)
    {
        GameStateData gameStateData = serverGame.GameStateData;
        int playersCount = gameStateData.players.Count;

        m_BetButton.onClick.AddListener(() =>
        {
            OnPlayerChoose(serverGame, PlayerChoose.Bet);
        });
        m_CallButton.onClick.AddListener(() =>
        {
            OnPlayerChoose(serverGame, PlayerChoose.Call);
        });
        m_CheckButton.onClick.AddListener(() =>
        {
            OnPlayerChoose(serverGame, PlayerChoose.Check);
        });
        m_FoldButton.onClick.AddListener(() =>
        {
            OnPlayerChoose(serverGame, PlayerChoose.Fold);
        });

        m_GiveCardsToPlayers.onClick.AddListener(() =>
        {
            m_GiveCardsToPlayers.interactable = false;

            if (serverGame.GameStateData.step == GameState.PlayersBet)
            {
                serverGame.GameStateData.step = GameState.GivePlayersCards;
            }
            gameStateData.state = GameState.GivePlayersCards;
            serverGame.GiveCardsToPlayers();

            //If PlayerID  is a local player id?
            serverGame.HideOtherPlayersCards(gameStateData.mainPlayerID);

            m_ServerMessaging.GiveCardsToPlayers(gameStateData.mainPlayerID, serverGame.GamePlayersAsJSON);
            StartCoroutine(EnableGiveChipsButton(playersCount * 1.1f));
        });

        m_PutCardsOnTableButton.onClick.AddListener(() =>
        {
            m_PutCardsOnTableButton.interactable = false;
            if (serverGame.GameStateData.step == GameState.GivePlayersCards)
            {
                serverGame.GameStateData.step = GameState.PutTableCards;
            }
            gameStateData.state = GameState.PutTableCards;
            StartCoroutine(EnableGiveChipsButton(playersCount * 1.1f));

            int.TryParse(m_PutCardsOnTableField.text, out int cardsCount);
            cardsCount = Mathf.Clamp(cardsCount, 1, cardsCount);
            serverGame.PutCardsOnTable(cardsCount);
            m_ServerMessaging.PutCardsOnTable(serverGame.GameStateData.mainPlayerID, serverGame.GameTableCardsAsJSON);

            m_PutCardsOnTableField.text = "1";

            if (gameStateData.tableCards.Count >= 5)
            {
                m_ShowCardsButton.interactable = true;
                m_PutCardsOnTableButton.interactable = false;
            }
        });

        m_ShowCardsButton.onClick.AddListener(() =>
        {
            TriggerPlayerChooseButtons(false);
            m_ShowCardsButton.interactable = false;

            serverGame.GameStateData.step = GameState.ShowPlayersCards;
            gameStateData.state = GameState.ShowPlayersCards;
            m_PutChipsFromTableToPlayerButton.interactable = true;

            gameStateData.SetSavedPlayersCards();
            gameStateData.winners = m_GameLogicManager.GetWinners(gameStateData.players, gameStateData.tableCards);
            m_ServerMessaging.ShwoCards(gameStateData.mainPlayerID, serverGame.GamePlayersAsJSON);


            if (gameStateData.winners.Count == 1)
            {
                m_PutChipsFromTableToPlayerIDField.text = gameStateData.winners[0].id + "";
            }

            m_PutChipsFromTableToPlayerField.text = gameStateData.bet + "";
        });

        m_PutChipsFromTableToPlayerButton.onClick.AddListener(() =>
        {
            serverGame.GameStateData.step = GameState.GiveWinnersPrize;
            gameStateData.state = GameState.GiveWinnersPrize;
            int.TryParse(m_PutChipsFromTableToPlayerIDField.text, out int playerID);
            m_PutChipsFromTableToPlayerIDField.text = playerID + "";
            int.TryParse(m_PutChipsFromTableToPlayerField.text, out int chips);
            chips = Mathf.Clamp(chips, 10, chips);
            m_PutChipsFromTableToPlayerField.text = chips + "";

            gameStateData.currentPlayerID = playerID;
            gameStateData.currentPlayer = gameStateData.players[playerID];

            serverGame.PutChipsFromTableToPlayer(serverGame.GameStateData.currentPlayerID, chips);
            serverGame.GameStateData.playersMaxBet = 0;

            m_CurrentPlayerText.text = serverGame.GameStateData.currentPlayerID + "";
            m_CurrentPlayerText1.text = "Player "+serverGame.GameStateData.currentPlayerID + " Turn";
            m_ServerMessaging.PutChipsFromTableToPlayer(serverGame.GameStateData.mainPlayerID, serverGame.GameStateAsJSON);

            m_RessetCardsButton.interactable = true;

            if (serverGame.GameStateData.bet == 0 || serverGame.GameStateData.tableChips == null)
            {
                m_PutChipsFromTableToPlayerButton.interactable = false;
            }
        });

        m_RessetCardsButton.onClick.AddListener(() =>
        {
            TriggerPlayerChooseButtons(true);
            DisableOtherButtons();

            serverGame.GameStateData.step = GameState.GivePlayersChips;

            serverGame.GameStateData.dealerID++;
            if (serverGame.GameStateData.dealerID >= playersCount)
            {
                serverGame.GameStateData.dealerID = 0;
            }
            serverGame.GameStateData.smallBlindID = serverGame.GameStateData.dealerID + 1 < playersCount ?
            serverGame.GameStateData.dealerID + 1 : 0;
            serverGame.GameStateData.bigBlindID = serverGame.GameStateData.dealerID + 2 < playersCount ?
            serverGame.GameStateData.dealerID + 2 : serverGame.GameStateData.dealerID + 2 - playersCount;

            gameStateData.currentPlayerID = gameStateData.dealerID + 1 < playersCount ? gameStateData.dealerID + 1 : 0;
            gameStateData.currentPlayer = gameStateData.players[gameStateData.currentPlayerID];

            gameStateData.state = GameState.GivePlayersChips;

            m_PutChipsField.text = 10 + "";

            m_PutCardsOnTableField.text = "3";
            serverGame.RessetCards(m_GameStateData.Cards);
            m_ServerMessaging.RessetCards(gameStateData.mainPlayerID, serverGame.GameStateAsJSON);

            if (gameStateData.GameIsEnded())
            {
                gameStateData.state = GameState.Ended;
                m_ServerMessaging.EndGame(gameStateData.mainPlayerID, serverGame.GamePlayersAsJSON);
            }
        });
    }

    private void OnPlayerChoose(ServerGame serverGame, PlayerChoose playerChoose)
    {        
        if (serverGame.GameStateData.step == GameState.GivePlayersChips)
        {
            m_GiveCardsToPlayers.interactable = true;
            serverGame.GameStateData.step = GameState.PlayersBet;
        }

        if (!serverGame.GameStateData.currentPlayer.outOfGame &&
            !serverGame.GameStateData.currentPlayer.fold)
        {
            int.TryParse(m_PutChipsField.text, out int cost);
            m_PutChipsField.text = cost + "";

            if (serverGame.GameStateData.state == GameState.GivePlayersChips ||
                serverGame.GameStateData.state == GameState.PlayersBet)
            {
                serverGame.GameStateData.state = GameState.PlayersBet;
                OnUserChooseButton(playerChoose, cost);

                if (!serverGame.GameStateData.HasComplitedTable)
                {
                    StartCoroutine(EnableGiveChipsButton(0.9f));
                }
            }
            else 
            {
                if ((serverGame.GameStateData.tableCards == null || serverGame.GameStateData.tableCards.Count < 5)
                    && serverGame.GameStateData.step < GameState.ShowPlayersCards)
                {
                    m_PutCardsOnTableButton.interactable = true;
                }
                if (serverGame.GameStateData.state == GameState.GivePlayersCards)
                {
                    m_PutCardsOnTableField.text = "3";
                    serverGame.ClearTableCards();
                }
                else
                {
                    m_PutCardsOnTableField.text = "1";
                }
                serverGame.GameStateData.state = GameState.PlayersBet;
                OnUserChooseButton(playerChoose, cost);
                StartCoroutine(EnableGiveChipsButton(1.1f));
            }
        }
        else
        {
            SetNextPlayer(serverGame);
        }
        if (serverGame.GameStateData.currentPlayerID != serverGame.GameStateData.mainPlayerID)
        {
            StartCoroutine(AutoPlayForAI(serverGame));
        }
    }

    private void OnUserChooseButton(PlayerChoose playerChoose, int cost)
    {
        m_ServerMessaging.SentUserChoose(playerChoose, cost);
    }

    private IEnumerator EnableGiveChipsButton(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        m_BetButton.interactable = true;
    }

    private void OnUserChoose(int gameID, PlayerChoose playerChoose, int cost)
    {
        if (gamesDictionary.ContainsKey(gameID))
        {
            ServerGame serverGame = gamesDictionary[gameID];

            bool hasDifference = false;
            ChipsColumn[] toPlayerChips = null;
            ChipsColumn[] toDealerChips = null;

            serverGame.PutChipsFromPlayerOnTable(playerChoose, cost, true,
                out hasDifference, out toPlayerChips, out toDealerChips,
                (bool isOutOfTable, bool wrongBet, ChipsColumn[] chips) =>
            {
                if (!wrongBet)
                {
                    serverGame.GameStateData.playerChooseData = new PlayerChooseData(playerChoose, chips,
                        hasDifference, toPlayerChips, toDealerChips);
                    if (!isOutOfTable)
                    {
                        m_ServerMessaging.SendUserChoose(serverGame.GameStateData.mainPlayerID,
                            serverGame.GamePlayerChooseDataAsJSON);
                    }
                    SetNextPlayer(serverGame);
                }
                else
                {
                    m_ServerMessaging.OnWrongBet(serverGame.GameStateData.mainPlayerID,
                            serverGame.GameBaseDataAsJSON);
                }
            });
        }
    }

    private void SetNextPlayer(ServerGame serverGame)
    {
        serverGame.NextPlayer();
        m_CurrentPlayerText.text = serverGame.GameStateData.currentPlayerID + "";
        m_CurrentPlayerText1.text = "Player "+serverGame.GameStateData.currentPlayerID + " Turn";
        m_PutChipsField.text = serverGame.GameStateData.ChackCost + "";
        m_ServerMessaging.SetCurrentPlayer(serverGame.GameStateData.mainPlayerID, serverGame.GameCurrentPlayerAsJSON);

        // Check if the current player is AI (not main player)
        if (serverGame.GameStateData.currentPlayerID != serverGame.GameStateData.mainPlayerID)
        {
            StartCoroutine(AutoPlayForAI(serverGame));
        }
    }
    private IEnumerator AutoPlayForAI(ServerGame serverGame)
    {
        yield return new WaitForSeconds(1f); // small delay to simulate thinking time

        var currentPlayer = serverGame.GameStateData.currentPlayer;

        if (currentPlayer.outOfGame || currentPlayer.fold)
        {
            // If AI player is out or folded, just move on
            SetNextPlayer(serverGame);
            yield break;
        }

        // Simple AI logic: random choice from available actions
        PlayerChoose aiChoice = PlayerChoose.Check;
        int betAmount = m_PutChipsField.text != "" ? int.Parse(m_PutChipsField.text) : 10;

        // Example: if bet is required, randomly decide between Call or Fold or Bet
        if (serverGame.GameStateData.state == GameState.PlayersBet)
        {
            // Random AI decision (can be improved with your own logic)
            int decision = Random.Range(0, 4);
            switch (decision)
            {
                case 0: aiChoice = PlayerChoose.Check; break;
                case 1: aiChoice = PlayerChoose.Call; break;
                case 2: aiChoice = PlayerChoose.Bet; betAmount = Mathf.Max(10, betAmount); break;
                case 3: aiChoice = PlayerChoose.Fold; break;
            }
        }

        // Call your existing method for AI choice
        OnUserChoose(serverGame.ID, aiChoice, betAmount);
    }
    private string GetGameSavePath(int gameID)
    {
        return Application.dataPath.Replace("Assets", "") + "game_" + gameID + ".text";
    }

    private string GetGameSavedPlayersCardsData(int gameID)
    {
        return Application.dataPath.Replace("Assets", "") + "game_" + gameID + "_CardsData.text";
    }

    private void SaveGameData(int gameID)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (gamesDictionary != null && gamesDictionary.ContainsKey(gameID))
        {
            File.WriteAllText(GetGameSavePath(gameID), gamesDictionary[gameID].GameStateAsJSON);
            File.WriteAllText(GetGameSavedPlayersCardsData(gameID), gamesDictionary[gameID].SavedPlayersCardsDataAsJSON);
        }
#endif
    }

    private GameStateData GetGameData(int gameID)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (File.Exists(GetGameSavePath(gameID)))
        {
            string gameDataAsJSON = File.ReadAllText(GetGameSavePath(gameID));
            GameStateData gameStateData = JsonUtility.FromJson<GameStateData>(gameDataAsJSON);
            if (File.Exists(GetGameSavedPlayersCardsData(gameID)))
            {
                string savedPlayersCardsDataAsJSON = File.ReadAllText(GetGameSavedPlayersCardsData(gameID));
                if (!string.IsNullOrEmpty(savedPlayersCardsDataAsJSON))
                {
                    gameStateData.SetSavedPlayersCardsData(JsonUtility.FromJson<SavedPlayersCardsData>(savedPlayersCardsDataAsJSON));
                }
            }
            return gameStateData;
        }
        else
        {
            return null;
        }
#else
        return null;
#endif
    }
}
