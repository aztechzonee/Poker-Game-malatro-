using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayersSeatManager : MonoBehaviour
{
    [SerializeField] private Transform m_Dealer;
    [SerializeField] private Transform m_SmallBlind;
    [SerializeField] private Transform m_BigBlind;
    [SerializeField] private float m_DealerMoveTime;
    [SerializeField] private Transform m_PlayersContent;
    [SerializeField] private Transform[] m_Pivots;
    [SerializeField] IPlayerSeatManager playerSeatManagerPrefab;
    [SerializeField] private float m_ChuseHideTime;
    [SerializeField] private PokerGameConfig m_PokerGameConfig;

    public List<IPlayerSeatManager> PlayerSeatManagers { get; private set; }

    public event Action OnTimerEnded;

    private float chipsThicknes;

    private Coroutine updatePlayerTimer;

    private void Awake()
    {
        foreach (var pivot in m_Pivots)
        {
            for (int i = 0; i < pivot.childCount; i++)
            {
                Destroy(pivot.GetChild(i).gameObject);
            }
        }
    }

    public void Init(GameStateData gameStateData, float chipsThicknes)
    {
        this.chipsThicknes = chipsThicknes;

        PlayerSeatManagers = new List<IPlayerSeatManager>(0);

        int difference = m_Pivots.Length - gameStateData.players.Count;
        int pivotIndex = 0;
        List<int> indexes = new List<int>(0);

        for (int i = 0; i < gameStateData.players.Count && i < m_Pivots.Length; i++)
        {
            indexes.Add(pivotIndex);
            pivotIndex++;

            if (difference > 0)
            {
                pivotIndex++;
                difference--;
            }
        }

        for (int i = 0; i < gameStateData.players.Count && i < m_Pivots.Length; i++)
        {
            IPlayerSeatManager playerSeatManager = Instantiate(playerSeatManagerPrefab, m_PlayersContent);
            playerSeatManager.name = i == gameStateData.mainPlayerID ? "MainPlayerSeat" : "PlayerSeat_" + i;
            pivotIndex = i >= gameStateData.mainPlayerID ? i - gameStateData.mainPlayerID :
                gameStateData.players.Count + i - gameStateData.mainPlayerID;
            pivotIndex = indexes[pivotIndex];

            playerSeatManager.Init(i == gameStateData.mainPlayerID, gameStateData.chips.Length, i,
                m_Pivots[pivotIndex].position, m_Pivots[pivotIndex].rotation,
                Camera.main.transform.position);

            playerSeatManager.SetName(gameStateData.players[i].playerName);
            PlayerSeatManagers.Add(playerSeatManager);
        }

        foreach (var pivot in m_Pivots)
        {
            Destroy(pivot.gameObject);
        }
    }

    public void MoveDealerToPivot(int dealerID)
    {
        PlayerSeatManagers[dealerID].MoveDealerToPivot(m_Dealer);
    }

    public void MoveSmallBlindToPivot(int smallBlindID)
    {
        PlayerSeatManagers[smallBlindID].MoveDealerToPivot(m_SmallBlind);
    }

    public void MoveBigBlindToPivot(int bigBlindID)
    {
        PlayerSeatManagers[bigBlindID].MoveDealerToPivot(m_BigBlind);
    }

    public IEnumerator MoveDealerToPivot(int dealerID,
        AnimationCurve chipsMoveCurve, AnimationCurve chipsDeltaYCurve, Action calback)
    {
        yield return StartCoroutine(PlayerSeatManagers[dealerID].MoveDealerToPivot(
            m_DealerMoveTime, m_Dealer, chipsMoveCurve, chipsDeltaYCurve));
        calback?.Invoke();
    }

    public IEnumerator MoveSmallBlindToPivot(int smallBlindID,
       AnimationCurve chipsMoveCurve, AnimationCurve chipsDeltaYCurve, Action calback)
    {
        yield return StartCoroutine(PlayerSeatManagers[smallBlindID].MoveDealerToPivot(
            m_DealerMoveTime, m_SmallBlind, chipsMoveCurve, chipsDeltaYCurve));
        calback?.Invoke();
    }

    public IEnumerator MoveBigBlindToPivot(int bigBlindID,
       AnimationCurve chipsMoveCurve, AnimationCurve chipsDeltaYCurve, Action calback)
    {
        yield return StartCoroutine(PlayerSeatManagers[bigBlindID].MoveDealerToPivot(
            m_DealerMoveTime, m_BigBlind, chipsMoveCurve, chipsDeltaYCurve));
        calback?.Invoke();
    }

    public void GiveChipsToPlayers(int playerID, Stack<IChipManager>[] chipsManagers)
    {
        PlayerSeatManagers[playerID].GiveChipsToPlayer(chipsThicknes, chipsManagers);
    }

    public IEnumerator GiveChipsToPlayers(int playerID, float moveTime, AnimationCurve chipsMoveCurve,
        AnimationCurve chipsDeltaYCurve, Stack<IChipManager>[] chipsManagers)
    {
        yield return StartCoroutine(PlayerSeatManagers[playerID].GiveChipsToPlayer(chipsThicknes, moveTime,
            chipsMoveCurve, chipsDeltaYCurve, chipsManagers));
    }

    public IEnumerator GetChips(int playerID, float moveTime, Transform[] chipsTablePivots,
        AnimationCurve chipsMoveCurve, AnimationCurve chipsDeltaYCurve, ChipsColumn[] chips, Action<int, IChipManager> onGetChip)
    {
        yield return StartCoroutine(PlayerSeatManagers[playerID].GetChips(chipsThicknes, moveTime, chipsTablePivots,
            chipsMoveCurve, chipsDeltaYCurve, chips, onGetChip));
    }

    public IEnumerator GiveChipsToPlayerFromTable(int playerIndex, float moveTime,
        AnimationCurve chipsMoveCurve, AnimationCurve chipsDeltaYCurve, Stack<IChipManager>[] chipsManagers)
    {
        yield return StartCoroutine(PlayerSeatManagers[playerIndex].GiveChipsFromTable(chipsThicknes, moveTime, chipsMoveCurve, chipsDeltaYCurve, chipsManagers));
    }

    public void HideBetText(int playerID)
    {
        PlayerSeatManagers[playerID].HideBetText();
    }

    public void SetName(int playerID, string nickName)
    {
        PlayerSeatManagers[playerID].SetName(nickName);
    }

    public void SetBetText(int playerID, string text)
    {
        PlayerSeatManagers[playerID].SetBetText(text);
    }

    public IEnumerator SetChoseTextAndHidePlayer(int playerID, string text)
    {
        yield return StartCoroutine(PlayerSeatManagers[playerID].SetChoseTextAndWait(text, m_ChuseHideTime));
        PlayerSeatManagers[playerID].HidePlayer();
    }

    public void SetChoseText(int playerID, string text)
    {
        PlayerSeatManagers[playerID].SetChoseText(text);
    }

    public IEnumerator SetChoseTextAndWait(int playerID, string text)
    {
        yield return StartCoroutine(PlayerSeatManagers[playerID].SetChoseTextAndWait(text, m_ChuseHideTime));
    }

    public void SetAsWinner(int playerID)
    {
        PlayerSeatManagers[playerID].SetAsWinner();
    }

    public void ResetAll(int playerID)
    {
        PlayerSeatManagers[playerID].ResetAll();
    }

    public void ShowTimer(int playerID, bool isBot)
    {
        HideTimer(playerID);
        PlayerSeatManagers[playerID].ShowTimer();

        int waitTime = !isBot ? m_PokerGameConfig.PlayerChooseTime : UnityEngine.Random.Range(1, m_PokerGameConfig.PlayerChooseTime / 2);
        updatePlayerTimer = StartCoroutine(StartUpdatePlayerTimer(playerID, waitTime, m_PokerGameConfig.PlayerChooseTime));
    }

    public void HideTimer(int playerID)
    {
        if (updatePlayerTimer != null)
        {
            StopCoroutine(updatePlayerTimer);
        }
        PlayerSeatManagers[playerID].HideTimer();
    }

    private IEnumerator StartUpdatePlayerTimer(int playerID, int waitTime, int chooseTime)
    {
        int time = waitTime;
        int wTime = chooseTime;
        WaitForSeconds waitForSeconds = new WaitForSeconds(1f);

        while(time >= 0)
        {
            PlayerSeatManagers[playerID].OnTimer(wTime);
            time --;
            wTime--;
            yield return waitForSeconds;
        }
       
        PlayerSeatManagers[playerID].OnTimer(wTime);
        PlayerSeatManagers[playerID].HideTimer();
        OnTimerEnded?.Invoke();
    }
}
