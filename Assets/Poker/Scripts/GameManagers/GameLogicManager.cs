using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Winner
{
    public int id;
    public Hand hand;

    public Winner(int id, Hand hand)
    {
        this.id = id;
        this.hand = hand;
    }

    public Winner(Winner winner)
    {
        id = winner.id;
        hand = new Hand(winner.hand);
    }
}

[Serializable]
public class Hand : IComparable
{
    public string name;
    public int index;
    [HideInInspector] public int value;
    [HideInInspector] public List<string> cards5Sorted;

    public Hand(Hand hand)
    {
        name = hand.name;
        index = hand.index;
        value = hand.value;

        cards5Sorted = new List<string>(hand.cards5Sorted);
    }

    public Hand(Hand copy, int value, List<string> cards5Sorted)
    {
        index = copy.index;
        name = copy.name;
        this.value += value;
        this.cards5Sorted = cards5Sorted;
    }

    public int CompareTo(object hand)
    {
        return value.CompareTo(((Hand)hand).value);
    }

    public override bool Equals(object hand)
    {
        return value.Equals(((Hand)hand).value);
    }

    public override int GetHashCode()
    {
        return value;
    }

    public static bool operator >(Hand hand1, Hand hand2) => hand1.CompareTo(hand2) == 1;
    public static bool operator <(Hand hand1, Hand hand2) => hand1.CompareTo(hand2) == -1;
    public static bool operator ==(Hand hand1, Hand hand2) => hand1.Equals(hand2);
    public static bool operator !=(Hand hand1, Hand hand2) => !hand1.Equals(hand2);
}

public class GameLogicManager : MonoBehaviour
{
    [SerializeField] private Hand[] m_Hands;

    private char GetType(string card)
    {
        return card[0];
    }

    private int GetValue(string card)
    {
        switch (card[2])
        {
            case 'J':
                return 11;
            case 'Q':
                return 12;
            case 'K':
                return 13;
            case 'A':
                return 14;
            default:
                break;
        }
        string str = card[2].ToString();
        if(card.Length == 4)
        {
            str += card[3];
        }
        return int.Parse(str);
    }

    public List<Winner> GetWinners(List<PlayerData> players, List<string> table5Cards)
    {
        List<Winner> allPlayers = new List<Winner>(0);
        Debug.Log("players.Count " + players.Count);
        foreach (var player in players)
        {
            if(!player.outOfGame && !player.fold)
            {
                Winner winner = new Winner(player.playerID, GetHand7Cards(player.cards, table5Cards));
                allPlayers.Add(winner);
            }
        }
        Debug.Log("allPlayers.Count " + allPlayers.Count);
        allPlayers.Sort((Winner winner1, Winner winner2) =>
        {
            return winner1.hand.CompareTo(winner2.hand);
        });

        List<Winner> winners = new List<Winner>(0);

        if (allPlayers.Count > 0)
        {
            Winner lastWinner = allPlayers[allPlayers.Count - 1];
            for (int i = allPlayers.Count - 2; i >= 0; i--)
            {
                if (lastWinner.hand == allPlayers[i].hand)
                {
                    winners.Add(allPlayers[i]);
                }
                else
                {
                    break;
                }
            }
            winners.Add(lastWinner);
        }
        return winners;
    }

    public Hand GetHand7Cards(List<string> player2Cards, List<string> table5Cards)
    {
        foreach (var item in player2Cards)
        {
            Debug.Log(item);
        }
        if(player2Cards.Count != 2 && table5Cards.Count != 5)
        {
            return null;
        }
        List<Hand> hands = new List<Hand>(0);
        List<string> currentTableCards = new List<string>(player2Cards);

        for (int i = 0; i < table5Cards.Count; i++)
        {
            for (int j = i + 1; j < table5Cards.Count; j++)
            {
                for (int index = 0; index < table5Cards.Count; index++)
                {
                    if (index != i && index != j)
                    {
                        currentTableCards.Add(table5Cards[index]);
                    }
                }

                hands.Add(GetHand5Cards(currentTableCards));
                currentTableCards.RemoveRange(player2Cards.Count, table5Cards.Count - player2Cards.Count);
            }
        }

        for (int i = 0; i < player2Cards.Count; i++)
        {
            currentTableCards.Clear();
            currentTableCards.Add(player2Cards[i]);

            for (int j = 0; j < table5Cards.Count; j++)
            {
                for (int index = 0; index < table5Cards.Count; index++)
                {
                    if (index != j)
                    {
                        currentTableCards.Add(table5Cards[index]);
                    }
                }

                hands.Add(GetHand5Cards(currentTableCards));
                currentTableCards.RemoveRange(1, table5Cards.Count - 1);
            }
        }

        hands.Add(GetHand5Cards(table5Cards));

        hands.Sort();
        //Debug.Log("hands.Count " + hands.Count);
        //foreach (var hand in hands)
        //{
        //    Debug.Log(hand.name + "  " + hand.value);
        //}
        return hands[hands.Count - 1];
    }
    private Hand GetHand5Cards(List<string> cards)
    {
        if(cards.Count != 5)
        {
            return null;
        }
        List<string> cardsSorted = new List<string>(cards);
        cardsSorted.Sort((string item1, string item2) =>
        {
            return GetValue(item1).CompareTo(GetValue(item2));
        });

        string card1 = cardsSorted[0];
        string card2 = cardsSorted[1];
        string card3 = cardsSorted[2];
        string card4 = cardsSorted[3];
        string card5 = cardsSorted[4];

        int value1 = GetValue(card1);
        int value2 = GetValue(card2);
        int value3 = GetValue(card3);
        int value4 = GetValue(card4);
        int value5 = GetValue(card5);

        int value = value1 + value2 + value3 + value4 + value5;

        bool straight = value1 + 1 == value2 && value2 + 1 == value3 && value3 + 1 == value4 && value4 + 1 == value5;

        bool flush = GetType(card1) == GetType(card2) && GetType(card1) == GetType(card3) && GetType(card1) == GetType(card4) && GetType(card1) == GetType(card5);

        if (straight && flush && value5 == 14)
        {
            value += 100000;
            return new Hand(m_Hands[0], value, cardsSorted);//Royal flush
        }
        else if(straight && flush)
        {
            value += 80000 + value5;
            return new Hand(m_Hands[1], value, cardsSorted);//Straight flush
        }
        else if( (value1 == value2 && value1 == value3 && value1 == value4) ||
                 (value2 == value3 && value2 == value4 && value2 == value5))
        {
            if(value1 == value2 && value1 == value3 && value1 == value4)
            {
                value += 100 * value1;
            }
            else
            {
                value += 100 * value2;
            }
            value += 70000 + value5;
            return new Hand(m_Hands[2], value, cardsSorted);//Four of a kind
        }
        else if ((value1 == value2 && value1 == value3 && value4 == value5) ||
                 (value1 == value2 && value3 == value4 && value3 == value5))
        {
            if(value1 == value2 && value1 == value3 && value4 == value5)
            {
                value += 200 * value1 + 100 * value4;
            }
            else
            {
                value += 200 * value3 + 100 * value1;
            }
            value += 60000 + value5;
            return new Hand(m_Hands[3], value, cardsSorted);//Full house
        }
        else if(flush)
        {
            value += 50000 + value5;
            return new Hand(m_Hands[4], value, cardsSorted);//Flush
        }
        else if(straight)
        {
            value += 40000 + value5;
            return new Hand(m_Hands[5], value, cardsSorted);//Straight
        }
        else if ((value1 == value2 && value1 == value3) ||
                 (value2 == value3 && value2 == value4) ||
                 (value3 == value4 && value3 == value5))
        {
            if(value1 == value2 && value1 == value3)
            {
                value += 100 * value1;
            }
            else if(value2 == value3 && value2 == value4)
            {
                value += 100 * value2;
            }
            else
            {
                value += 100 * value3;
            }
            value += 30000 + value5;
            return new Hand(m_Hands[6], value, cardsSorted);//Three of a kind
        }
        else if ((value1 == value2 && value3 == value4) ||
                 (value1 == value2 && value4 == value5) ||
                 (value2 == value3 && value4 == value5))
        {
            if(value1 == value2 && value3 == value4)
            {
                value += 100 * Mathf.Max(value1, value3);
            }
            else if (value1 == value2 && value4 == value5)
            {
                value += 100 * Mathf.Max(value1, value4);
            }
            else
            {
                value += 100 * Mathf.Max(value2, value4);
            }
            value += 20000 + value5;
            return new Hand(m_Hands[7], value, cardsSorted);//Two pair
        }
        else if (value1 == value2 ||
                 value2 == value3 ||
                 value3 == value4 ||
                 value4 == value5)
        {
            if(value1 == value2)
            {
                value += 100 * value1;
            }
            else if (value2 == value3)
            {
                value += 100 * value2;
            }
            else if (value3 == value4)
            {
                value += 100 * value3;
            }
            else
            {
                value += 100 * value4;
            }
            value += 10000 + value5;
            return new Hand(m_Hands[8], value, cardsSorted);//Pair
        }
        else
        {
            value += value5;
            return new Hand(m_Hands[9], value, cardsSorted);//High Card
        }
    }
}
