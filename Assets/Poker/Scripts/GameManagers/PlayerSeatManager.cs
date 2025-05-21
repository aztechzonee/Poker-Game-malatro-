using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSeatManager : IPlayerSeatManager
{
    [Space(10), Header("Player")]
    [SerializeField] private int m_ID;
    [SerializeField] private TextMesh m_NameText;
    [SerializeField] private Transform m_NamePivot;

    [Space(10), Header("Cards")]
    [SerializeField] private Transform[] m_CardsStandardPivots;
    [SerializeField] private Transform[] m_CardsShowPivots;
    [SerializeField] private Transform m_CardsShowPivot;

    [Space(10), Header("Info Text")]
    [SerializeField] private TextMesh m_BetText;
    [SerializeField] private TextMesh m_HandText;
    [SerializeField] private TextMesh m_ChooseText;

    [Space(10), Header("Info Background")]
    [SerializeField] private Transform m_HandShowPivot;
    [SerializeField] private Transform m_BetPivot;
    [SerializeField] private Transform m_ChoosePivot;
    [SerializeField] private Transform m_WinnerPivot;
    [SerializeField] private Transform m_InfoPivot;
    [SerializeField] private Transform m_MainPlayerInfoPivot;

    [Space(10), Header("Other")]
    [SerializeField] private Transform m_DealerPivot;
    [SerializeField] private Transform[] m_ChipsPivots;

    [Space(10), Header("Timer")]
    [SerializeField] private Transform m_TimerPivot;
    [SerializeField] private TextMesh m_TimerText;

    public override Transform[] CardsStandardPivots => m_CardsStandardPivots;
    public override Transform[] CardsShowPivots => m_CardsShowPivots;    

    private void Awake()
    {
        m_NamePivot.gameObject.SetActive(false);
        m_BetPivot.gameObject.SetActive(false);
        m_HandShowPivot.gameObject.SetActive(false);
        m_ChoosePivot.gameObject.SetActive(false);
        m_WinnerPivot.gameObject.SetActive(false);

        foreach (var chipPivot in m_ChipsPivots)
        {
            Destroy(chipPivot.GetChild(0).gameObject);
        }
        foreach (var cardStandardPivot in m_CardsStandardPivots)
        {
            Destroy(cardStandardPivot.GetChild(0).gameObject);
        }
        foreach (var cardsShowPivot in m_CardsShowPivots)
        {
            Destroy(cardsShowPivot.GetChild(0).gameObject);
        }
        for (int i = 0; i < m_MainPlayerInfoPivot.childCount; i++)
        {
            Destroy(m_MainPlayerInfoPivot.GetChild(i).gameObject);
        }

        HideTimer();
    }

    public override void Init(bool isMainPlayer, int chipsLength, int id, Vector3 position, Quaternion rotation, Vector3 cameraPosition)
    {
        ID = id;
        m_ID = id;//Just for Show in inspector
        transform.position = position;
        transform.rotation = rotation;
        m_CardsShowPivot.LookAt(cameraPosition, Vector3.up);
        m_InfoPivot.LookAt(cameraPosition, Vector3.up);

        m_TimerPivot.LookAt(cameraPosition, Vector3.up);

        chipsManagers = new Stack<IChipManager>[chipsLength];
        for (int i = 0; i < chipsLength; i++)
        {
            chipsManagers[i] = new Stack<IChipManager>(0);
        }

        if (isMainPlayer)
        {
            m_InfoPivot.position = m_MainPlayerInfoPivot.position;
        }
        m_InfoPivot.localScale = (Mathf.Sqrt(Vector3.Distance(m_InfoPivot.position, cameraPosition)) / 1.61f) * Vector3.one;

        HideTimer();
    }

    public override void ShowHand(Hand hand)
    {
        m_HandText.text = hand.name;
        m_HandShowPivot.gameObject.SetActive(true);
    }

    public override void MoveDealerToPivot(Transform dealer)
    {
        dealer.position = m_DealerPivot.position;
    }

    public override IEnumerator MoveDealerToPivot(float moveTime, Transform dealer, AnimationCurve chipsMoveCurve, AnimationCurve chipsDeltaYCurve)
    {
        float time = 0f;
        WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();
        Vector3 startPosition = dealer.position;
        while (time < moveTime)
        {
            float t = time / moveTime;
            Vector3 deltaY = new Vector3(0f, chipsDeltaYCurve.Evaluate(t), 0f);
            dealer.position = deltaY + Vector3.Lerp(startPosition, m_DealerPivot.position, chipsMoveCurve.Evaluate(t));
            time += Time.deltaTime;
            yield return waitForEndOfFrame;
        }
        dealer.position = m_DealerPivot.position;
    }

    public override void HidePlayer()
    {
        gameObject.SetActive(false);
    }

    public override void ShowPlayer()
    {
        gameObject.SetActive(true);
    }

    private void MoveChips(bool add, float chipsThicknes, Transform[] chipsPivots,
        Stack<IChipManager>[] chipsManagers, Action<int> OnMoveChip)
    {
        for (int i = 0; i < chipsPivots.Length; i++)
        {
            foreach (var chipManager in chipsManagers[i])
            {
                chipManager.transform.SetParent(chipsPivots[i]);
                if (add)
                {
                    this.chipsManagers[i].Push(chipManager);
                }
                Vector3 randomDisplacement = new Vector3(UnityEngine.Random.Range(
                    0f, 2f * chipsThicknes), 0f, UnityEngine.Random.Range(0f, 2f * chipsThicknes));
                chipManager.MoveToPivot(randomDisplacement +
                    chipsPivots[i].position + (float)chipsPivots[i].childCount * chipsThicknes * Vector3.up);
                OnMoveChip?.Invoke(i);
            }
        }
    }

    private IEnumerator MoveChips(bool add, float chipsThicknes, float moveTime,
        Transform[] chipsPivots, AnimationCurve chipsMoveCurve, AnimationCurve chipsDeltaYCurve,
        Stack<IChipManager>[] chipsManagers, Action<int> OnMoveChip)
    {
        for (int i = 0; i < chipsPivots.Length; i++)
        {
            foreach (var chipManager in chipsManagers[i])
            {
                chipManager.transform.SetParent(chipsPivots[i]);
                if (add)
                {
                    this.chipsManagers[i].Push(chipManager);
                }
                Vector3 randomDisplacement = new Vector3(UnityEngine.Random.Range(
                    0f, 2f * chipsThicknes), 0f, UnityEngine.Random.Range(0f, 2f * chipsThicknes));
                StartCoroutine(chipManager.MoveToPivot(moveTime, randomDisplacement +
                    chipsPivots[i].position + (float)chipsPivots[i].childCount * chipsThicknes * Vector3.up,
                    chipsMoveCurve, chipsDeltaYCurve));
                OnMoveChip?.Invoke(i);
            }
        }
        yield return new WaitForSeconds(moveTime);
    }

    public override void GiveChipsToPlayer(float chipsThicknes, Stack<IChipManager>[] chipsManagers)
    {
        MoveChips(true, chipsThicknes, m_ChipsPivots, chipsManagers, (int chipIndex) =>
        {
            
        });
    }

    public override IEnumerator GiveChipsToPlayer(float chipsThicknes, float moveTime,
        AnimationCurve chipsMoveCurve, AnimationCurve chipsDeltaYCurve, Stack<IChipManager>[] chipsManagers)
    {
        yield return StartCoroutine(MoveChips(true, chipsThicknes, moveTime, m_ChipsPivots, chipsMoveCurve, chipsDeltaYCurve, chipsManagers, (int chipIndex) =>
        {
            
        }));
    }

    public override IEnumerator GetChips(float chipsThicknes, float moveTime, Transform[] chipsPivots,
        AnimationCurve chipsMoveCurve, AnimationCurve chipsDeltaYCurve, ChipsColumn[] chips, Action<int, IChipManager> onGetChip)
    {
        Stack<IChipManager>[] chipsManagers = PutChips(chips, onGetChip);
        yield return StartCoroutine(MoveChips(false, chipsThicknes, moveTime, chipsPivots, chipsMoveCurve, chipsDeltaYCurve, chipsManagers, (int chipIndex) =>
        {
            
        }));
    }

    public override IEnumerator GiveChipsFromTable(float chipsThicknes, float moveTime,
        AnimationCurve chipsMoveCurve, AnimationCurve chipsDeltaYCurve, Stack<IChipManager>[] chipsManagers)
    {
        yield return StartCoroutine(MoveChips(true, chipsThicknes, moveTime, m_ChipsPivots, chipsMoveCurve, chipsDeltaYCurve, chipsManagers, (int chipIndex) =>
        {
            
        }));
    }

    public override void SetName(string nickName)
    {
        m_NamePivot.gameObject.SetActive(true);
        m_NameText.text = nickName;
    }

    public override void HideBetText()
    {
        m_BetPivot.gameObject.SetActive(false);
    }

    public override void SetBetText(string text)
    {
        m_BetText.text = text;
        m_BetPivot.gameObject.SetActive(true);
    }

    public override void SetChoseText(string text)
    {
        m_ChooseText.text = text;
        m_ChoosePivot.gameObject.SetActive(true);
        m_ChoosePivot.gameObject.SetActive(false);
    }

    public override IEnumerator SetChoseTextAndWait(string text, float hideTime)
    {
        m_ChooseText.text = text;
        m_ChoosePivot.gameObject.SetActive(true);
        yield return new WaitForSeconds(hideTime);
        m_ChoosePivot.gameObject.SetActive(false);
    }

    public override void ResetAll()
    {
        m_BetPivot.gameObject.SetActive(false);
        m_HandShowPivot.gameObject.SetActive(false);
        m_ChoosePivot.gameObject.SetActive(false);
        m_WinnerPivot.gameObject.SetActive(false);
    }

    public override void SetAsWinner()
    {
        ResetAll();
        m_WinnerPivot.gameObject.SetActive(true);
    }

    private Stack<IChipManager>[] PutChips(ChipsColumn[] chips, Action<int, IChipManager> onGetChip)
    {
        Stack<IChipManager>[] getChipsManagers = new Stack<IChipManager>[chips.Length];
        for (int i = 0; i < getChipsManagers.Length; i++)
        {
            getChipsManagers[i] = new Stack<IChipManager>(0);
        }
        for (int i = 0; i < chips.Length; i++)
        {
            if (chipsManagers[i].Count != 0)
            {
                Stack<IChipManager> chipsStack = new Stack<IChipManager>(0);
                int chipsCount = chips[i].count;
                for (int j = 0; j < chipsCount; j++)
                {
                    IChipManager chipManager = chipsManagers[i].Pop();
                    chipsStack.Push(chipManager);
                    onGetChip.Invoke(i, chipManager);
                }

                getChipsManagers[i] = chipsStack;
            }
        }
        return getChipsManagers;
    }

    public override void HideTimer()
    {
        m_TimerPivot.gameObject.SetActive(false);
    }

    public override void ShowTimer()
    {
        m_TimerPivot.gameObject.SetActive(true);
    }

    public override void OnTimer(int timer)
    {
        m_TimerText.text = timer.ToString();
    }
}
