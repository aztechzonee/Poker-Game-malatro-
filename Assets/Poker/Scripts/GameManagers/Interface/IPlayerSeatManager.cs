using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class IPlayerSeatManager : MonoBehaviour
{
    public int ID { get; protected set; }


    protected Stack<IChipManager>[] chipsManagers;

    public abstract Transform[] CardsStandardPivots { get; }
    public abstract Transform[] CardsShowPivots { get; }

    public abstract void Init(bool isMainPlayer, int chipsLength, int id, Vector3 position, Quaternion rotation, Vector3 cameraPosition);

    public abstract void ShowHand(Hand hand);

    public abstract void MoveDealerToPivot(Transform dealer);

    public abstract IEnumerator MoveDealerToPivot(float moveTime, Transform dealer, AnimationCurve chipsMoveCurve, AnimationCurve chipsDeltaYCurve);

    public abstract void HidePlayer();

    public abstract void ShowPlayer();

    public abstract void GiveChipsToPlayer(float chipsThicknes, Stack<IChipManager>[] chipsManagers);

    public abstract IEnumerator GiveChipsToPlayer(float chipsThicknes, float moveTime,
        AnimationCurve chipsMoveCurve, AnimationCurve chipsDeltaYCurve, Stack<IChipManager>[] chipsManagers);

    public abstract IEnumerator GetChips(float chipsThicknes, float moveTime, Transform[] chipsPivots,
       AnimationCurve chipsMoveCurve, AnimationCurve chipsDeltaYCurve, ChipsColumn[] chips, Action<int, IChipManager> onGetChip);

    public abstract IEnumerator GiveChipsFromTable(float chipsThicknes, float moveTime,
        AnimationCurve chipsMoveCurve, AnimationCurve chipsDeltaYCurve, Stack<IChipManager>[] chipsManagers);

    public abstract void SetName(string nickName);

    public abstract void HideBetText();

    public abstract void SetBetText(string text);

    public abstract void SetChoseText(string text);

    public abstract IEnumerator SetChoseTextAndWait(string text, float hideTime);

    public abstract void ResetAll();

    public abstract void SetAsWinner();

    public abstract void ShowTimer();

    public abstract void HideTimer();

    public abstract void OnTimer(int timer);
}
