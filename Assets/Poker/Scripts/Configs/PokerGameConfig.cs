using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PokerConfig", menuName = "Poker/Config", order = 1)]
public class PokerGameConfig : ScriptableObject
{
    public int PlayerChooseTime;

    public List<string> cards;
    public ChipsColumn[] chips;

}
