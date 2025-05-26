using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static ChipsColumn;

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

    [Space(2), Header("Trump Card Prediction")]
    [SerializeField] private TMP_Dropdown m_TrumpSuitDropdown;
    [SerializeField] private InputField m_PredictionInput;
    [SerializeField] private Button m_SubmitPredictionButton;
    [Space(2), Header("Batting $ Prediction")]
    [SerializeField] private TMP_Dropdown m_DollartDropdown;
    [SerializeField] private Button m_SubmitBetButton;


    [Space(10), Header("Player")]
    [SerializeField] private int m_MaxPlayersCount = 4;
    [SerializeField] private Text m_CurrentPlayerText;
    [SerializeField] private Text m_CurrentPlayerText1;
    [SerializeField] private Text m_PredictionStatusText;
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
    public int turnCount = 0;
#if UNITY_WEBGL && !UNITY_EDITOR
    private void Awake()
    {
        m_SaveGameButton.gameObject.SetActive(false);
        m_DeleteGameButton.gameObject.SetActive(false);
    }
#endif

    private IEnumerator Start()
    {
        m_PredictionStatusText.text = "🔮 Prediction Phase Started!";
        m_PredictionStatusText.gameObject.SetActive(true);

        // Enable prediction inputs
        m_TrumpSuitDropdown.interactable = true;
        m_PredictionInput.interactable = true;
        m_SubmitPredictionButton.interactable = true;

        m_GameStateData.SetConfigs();
        m_GameStateData.SaveCards();

        TriggerPlayerChooseButtons(false);
        DisableOtherButtons();

        gamesDictionary = new Dictionary<int, ServerGame>(0);

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
        m_CurrentPlayerText1.text = "Player " + gameStateData.currentPlayerID + " Turn";
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
            PlayerData playerData = new PlayerData(i, false, i.ToString());
            gameStateData.players.Add(playerData);
        }
        gameStateData.currentPlayerID = dealerID + 1 < playersCount ? dealerID + 1 : 0;
        gameStateData.currentPlayer = gameStateData.players[gameStateData.currentPlayerID];

        ServerGame serverGame = new GameObject("Game_" + id).AddComponent<ServerGame>();
        serverGame.transform.SetParent(transform);
        gamesDictionary.Add(id, serverGame);
        serverGame.Setup(id, false, gameStateData);

        gameStateData.mainPlayerID = 1;//For sending to player with id 1
        gameStateData.step = GameState.Start;
        m_ServerMessaging.StartGame(gameStateData.mainPlayerID, serverGame.GameStateAsJSON);

        // asad //serverGame.GiveChipsToPlayers();
        //  serverGame.ResetChipsOnTable();
        //   gameStateData.step =  GameState.GivePlayersChips;
        //  gameStateData.state = GameState.GivePlayersChips;
        //   m_ServerMessaging.GiveChipsToPlayers(gameStateData.mainPlayerID, serverGame.GamePlayersAsJSON);

        m_CurrentPlayerText.text = gameStateData.currentPlayerID + "";
        m_CurrentPlayerText1.text = "Player " + gameStateData.currentPlayerID + " Turn";
        m_ServerMessaging.SetCurrentPlayer(gameStateData.mainPlayerID, serverGame.GameCurrentPlayerAsJSON);
        m_ServerMessaging.OpenTableForPlayersChips(gameStateData.mainPlayerID, serverGame.GameBaseDataAsJSON);

        return serverGame;
    }

    private void SetupGame(ServerGame serverGame)
    {
        GameStateData gameStateData = serverGame.GameStateData;
        int playersCount = gameStateData.players.Count;

        // --- Betting buttons ---
        m_BetButton.onClick.AddListener(() => OnPlayerChoose(serverGame, PlayerChoose.Bet));
        m_CallButton.onClick.AddListener(() => OnPlayerChoose(serverGame, PlayerChoose.Call));
        m_CheckButton.onClick.AddListener(() => OnPlayerChoose(serverGame, PlayerChoose.Check));
        m_FoldButton.onClick.AddListener(() => OnPlayerChoose(serverGame, PlayerChoose.Fold));

        // Initially betting UI disabled, enable only after prediction phase
        ShowBettingUI(false);

        // --- Give Cards button ---
        m_GiveCardsToPlayers.onClick.AddListener(() =>
        {
            m_GiveCardsToPlayers.interactable = false;

            serverGame.GameStateData.step = GameState.GivePlayersCards;
            gameStateData.state = GameState.GivePlayersCards;

            serverGame.GiveCardsToPlayers();
            serverGame.HideOtherPlayersCards(gameStateData.mainPlayerID);
            m_ServerMessaging.GiveCardsToPlayers(gameStateData.mainPlayerID, serverGame.GamePlayersAsJSON);
            StartCoroutine(EnableGiveChipsButton(playersCount * 1.1f));
        });

        // --- Other buttons omitted for brevity (keep as is) ---

        // --- Start Trump & Prediction Phase ---
        StartTrumpAndPredictionPhase(serverGame, gameStateData);

        // In your StartTrumpAndPredictionPhase, after all predictions are done:
        // Add code like this (pseudo-code here, implement inside that method or via event):
        /*
        if (AllPlayersMadePredictions(serverGame))
        {
            // Disable prediction inputs
            m_TrumpSuitDropdown.interactable = false;
            m_PredictionInput.interactable = false;
            m_SubmitPredictionButton.interactable = false;

            // Enable betting UI
            ShowBettingUI(true);
            gameStateData.state = GameState.PlayersBet;

            // Initialize betting tracking here (your own logic)
            ResetBetTracking();
        }
        */

        // Betting phase logic: once all players bet or check, call this method:
        void OnBettingComplete()
        {
            ShowBettingUI(false);
            m_GiveCardsToPlayers.interactable = true;
            gameStateData.state = GameState.GivePlayersCards;
        }

        // You must call OnBettingComplete() once all players have acted during betting phase.

        // --- Continue with other phases as per your existing setup ---
    }

    // Helper to toggle betting UI elements (dropdown or buttons for preset bets)
    private void ShowBettingUI(bool show)
    {
        m_BetButton.gameObject.SetActive(show);
        m_CallButton.gameObject.SetActive(show);
        m_CheckButton.gameObject.SetActive(show);
        m_FoldButton.gameObject.SetActive(show);
        // If you have a dropdown or other UI for preset bets, toggle it here
    }

    // You need to implement:
    // - Player betting tracking to know when all players have bet/check/folded.
    // - Call OnBettingComplete() when betting phase finishes.
    // - Integrate that logic with OnPlayerChoose and serverGame.GameStateData.
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

        ///Count Turn of each Player


        var currentPlayer = serverGame.GameStateData.currentPlayer;

        if (!currentPlayer.fold && !currentPlayer.outOfGame)
        {
            if (serverGame.GameStateData.playerTurnCounts.ContainsKey(currentPlayer.id))
                serverGame.GameStateData.playerTurnCounts[currentPlayer.id]++;
            else
                serverGame.GameStateData.playerTurnCounts[currentPlayer.id] = 1;
        }

        bool allCompleted = serverGame.GameStateData.players
            .Where(p => !p.fold && !p.outOfGame)
            .All(p => serverGame.GameStateData.playerTurnCounts.TryGetValue(p.id, out int count) && count >= 5);

        if (allCompleted)
        {
            Debug.LogError("✅ All active players completed 5 turns!");
        }

        int activePlayersCount = serverGame.GameStateData.players
    .Count(p => !p.fold && !p.outOfGame);

        if (activePlayersCount == 1)
        {
            Debug.LogError("⚠️ Only one player active!");
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
        var gameStateData = serverGame.GameStateData;

        int safety = 0;
        int totalPlayers = gameStateData.players.Count;

        // Loop to find the next player who hasn't predicted yet
        do
        {
            serverGame.NextPlayer();
            safety++;

            if (safety > totalPlayers)
            {
                Debug.LogError("⚠️ SetNextPlayer: Loop safety break — all players may have predicted.");
                EndPredictionPhase(serverGame);
                return;
            }

        } while (gameStateData.PlayerPredictions.ContainsKey(gameStateData.currentPlayerID));

        int currentId = gameStateData.currentPlayerID;

        Debug.Log($"➡️ Switched to player {currentId}");

        UpdateCurrentPlayerUI(currentId);

        // Handle AI or Human
        if (currentId != gameStateData.mainPlayerID)
        {
            EnablePredictionUI(false);
            StartCoroutine(DoAIPredictionsWithDelay(serverGame));
        }
        else
        {
            EnablePredictionUI(true);
        }
    }
    private IEnumerator AutoPlayForAI(ServerGame serverGame)
    {
        yield return new WaitForSeconds(3f); // small delay to simulate thinking time

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
    private void StartTrumpAndPredictionPhase(ServerGame serverGame, GameStateData gameStateData)
    {
        serverGame.GameStateData.state = GameState.TrumpSelection;

        TriggerPlayerChooseButtons(false);
        UpdateCurrentPlayerUI(gameStateData.currentPlayerID);

        m_SubmitPredictionButton.onClick.RemoveAllListeners();
        m_SubmitPredictionButton.onClick.AddListener(() =>
        {
            Suit chosenTrump = (Suit)m_TrumpSuitDropdown.value;
            if (!int.TryParse(m_PredictionInput.text, out int prediction) || prediction < 2 || prediction > 5)
            {
                Debug.LogWarning("Prediction must be between 2 and 5");
                return;
            }

            int currentPlayerId = gameStateData.currentPlayerID;
            serverGame.GameStateData.TrumpSuit = chosenTrump;
            serverGame.GameStateData.PlayerPredictions[currentPlayerId] = prediction;

            Debug.Log($"🧑 Player {currentPlayerId} predicted {prediction}");
            Debug.LogError($"🧑 Player main id {gameStateData.mainPlayerID} predicted {prediction}");

            if (gameStateData.currentPlayerID != 1)
            {
                EnablePredictionUI(false);
                StartCoroutine(ProcessAllAIPredictions(serverGame));
            }
            else
            {
                EnablePredictionUI(true);
            }
        });

        // 🔁 If first turn is AI, begin AI prediction
        if (gameStateData.currentPlayerID != gameStateData.mainPlayerID)
        {
            EnablePredictionUI(false);
            StartCoroutine(DoAIPredictionsWithDelay(serverGame));
        }
        else
        {
            EnablePredictionUI(true);
        }
    }
    private bool AllPlayersMadePredictions(ServerGame serverGame)
    {
        var players = serverGame.GameStateData.players;
        foreach (var player in players)
        {
            int playerId = int.Parse(player.id);
            if (!serverGame.GameStateData.PlayerPredictions.ContainsKey(playerId))
            {
                return false;
            }
        }

        return true;
    }
    private IEnumerator DoAIPredictionsWithDelay(ServerGame serverGame)
    {
        yield return new WaitForSeconds(1f); // Simulate AI thinking

        var gameStateData = serverGame.GameStateData;
        int aiPlayerId = gameStateData.currentPlayerID;

        if (!gameStateData.PlayerPredictions.ContainsKey(aiPlayerId))
        {
            int prediction = UnityEngine.Random.Range(2, 6);
            gameStateData.PlayerPredictions[aiPlayerId] = prediction;
            Debug.Log($"🤖 AI Player {aiPlayerId} predicted {prediction}");
        }

        if (AllPlayersMadePredictions(serverGame))
        {
            EndPredictionPhase(serverGame);
            yield break;
        }

        SetNextPlayer(serverGame); // Advance to next player
    }
    private void EndPredictionPhase(ServerGame serverGame)
    {
        m_PredictionStatusText.text = "✅ Trump Predictions Complete!";
        m_PredictionStatusText.gameObject.SetActive(true);

        m_TrumpSuitDropdown.interactable = false;
        m_PredictionInput.interactable = false;
        m_SubmitPredictionButton.interactable = false;

        ShowBettingUI(true);
        serverGame.GameStateData.state = GameState.PlayersBet;

        Debug.LogError("Trump Predictions are completed ");
        Debug.LogError("------------------------------");

        StartDollarBettingPhase(serverGame);
    }
    private void UpdateCurrentPlayerUI(int currentId)
    {
        m_CurrentPlayerText.text = currentId.ToString();
        m_CurrentPlayerText1.text = "Player " + currentId + " Turn";
    }
    private void EnablePredictionUI(bool enable)
    {
        m_TrumpSuitDropdown.gameObject.SetActive(enable);
        m_PredictionInput.gameObject.SetActive(enable);
        m_SubmitPredictionButton.gameObject.SetActive(enable);
    }

    private IEnumerator ProcessAllAIPredictions(ServerGame serverGame)
    {
        while (serverGame.GameStateData.currentPlayerID != serverGame.GameStateData.mainPlayerID)
        {
            yield return StartCoroutine(DoAIPredictionsWithDelay(serverGame));

            // Wait a moment between predictions
            yield return new WaitForSeconds(0.5f);
        }

        // Now it's main player's turn
        EnablePredictionUI(true);
        UpdateCurrentPlayerUI(serverGame.GameStateData.currentPlayerID);
    }

    private void StartDollarBettingPhase(ServerGame serverGame)
    {
        Debug.Log("Start Dollar Batting...");

        m_PredictionStatusText.text = "Dollar Batting";

        serverGame.GameStateData.state = GameState.PlayersBet;

        TriggerPlayerChooseButtons(false);
        UpdateCurrentPlayerUI(serverGame.GameStateData.currentPlayerID);

        m_SubmitBetButton.onClick.RemoveAllListeners();
        m_SubmitBetButton.onClick.AddListener(() =>
        {
            int currentPlayerId = serverGame.GameStateData.currentPlayerID;
            int betAmount = GetBetAmountFromDropdown();

            if (betAmount <= 0)
            {
                Debug.LogWarning("Invalid bet amount");
                return;
            }

            if (serverGame.GameStateData.PlayerBets == null)
                serverGame.GameStateData.PlayerBets = new Dictionary<int, int>();

            serverGame.GameStateData.PlayerBets[currentPlayerId] = betAmount;
            Debug.Log($"Player {currentPlayerId} bet ${betAmount}");

            if (AllPlayersPlacedBets(serverGame))
            {
                EndDollarBettingPhase(serverGame);
            }
            else
            {
                SetNextPlayer(serverGame);
                EnableDollarBettingUI(IsCurrentPlayerHuman(serverGame));
            }

        });

        Debug.Log("IsCurrentPlayerHuman" + IsCurrentPlayerHuman(serverGame));

        if (IsCurrentPlayerHuman(serverGame))
        {
            EnableDollarBettingUI(true);
        }
        else
        {
            EnableDollarBettingUI(false);
            StartCoroutine(ProcessAIBetsWithDelay(serverGame));
        }
    }
    private int GetBetAmountFromDropdown()
    {
        string selected = m_DollartDropdown.options[m_DollartDropdown.value].text;

        if (selected == "All-In")
        {
            return int.MaxValue; // or a special value representing All-In
        }
        else if (int.TryParse(selected, out int bet))
        {
            return bet;
        }
        return 0;
    }

    private bool AllPlayersPlacedBets(ServerGame serverGame)
    {
        var players = serverGame.GameStateData.players;
        foreach (var player in players)
        {
            int playerId = int.Parse(player.id);
            if (!serverGame.GameStateData.PlayerBets.ContainsKey(playerId))
                return false;
        }
        return true;
    }

    private IEnumerator ProcessAIBetsWithDelay(ServerGame serverGame)
    {
        yield return new WaitForSeconds(1f); // simulate AI thinking

        var gameStateData = serverGame.GameStateData;
        int aiPlayerId = gameStateData.currentPlayerID;

        if (!gameStateData.PlayerBets.ContainsKey(aiPlayerId))
        {
            // Simple AI logic for betting: pick random option or fixed amount
            int[] possibleBets = { 5, 10, 20, 50, 100, int.MaxValue };
            int bet = possibleBets[UnityEngine.Random.Range(0, possibleBets.Length)];
            gameStateData.PlayerBets[aiPlayerId] = bet;
            Debug.Log($"AI Player {aiPlayerId} bet ${bet}");
        }

        if (AllPlayersPlacedBets(serverGame))
        {
            EndDollarBattingPase(serverGame);
            yield break;
        }

        SetNextPlayer(serverGame);
    }
    private void EndDollarBattingPase(ServerGame serverGame)
    {
        Debug.Log("Betting phase complete!");
        ShowBettingUI(false);

        // Continue to next phase (e.g., dealing cards or starting tricks)
        serverGame.GameStateData.state = GameState.NextPhaseAfterBetting;

        // Your next game logic here...
    }
    private void EnableDollarBettingUI(bool enable)
    {
        m_DollartDropdown.gameObject.SetActive(enable);
        m_SubmitBetButton.gameObject.SetActive(enable);
    }

    private bool IsCurrentPlayerHuman(ServerGame serverGame)
    {
        int currentPlayerId = serverGame.GameStateData.currentPlayerID;

        // Player with ID == 1 is human, others are AI
        return currentPlayerId == 1;
    }

    private void EndDollarBettingPhase(ServerGame serverGame)
    {
        Debug.Log("Dollar Betting phase complete!");
        ShowBettingUI(false);

        // Continue to next phase (e.g., dealing cards or starting tricks)
        serverGame.GameStateData.state = GameState.NextPhaseAfterBetting;

        // Your next game logic here...
    }
}
