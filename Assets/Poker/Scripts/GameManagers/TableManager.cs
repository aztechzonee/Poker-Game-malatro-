using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TableManager : MonoBehaviour
{
    [SerializeField] private GameObject[] m_ObjectsForHide;
    [SerializeField] private Transform[] m_ChipsPivots;
    [SerializeField] private Transform[] m_CardPivots;

    private Stack<IChipManager>[] chipsManagers;

    public Transform[] ChipsPivots => m_ChipsPivots;
    public Transform[] CardPivots => m_CardPivots;

    private void Awake()
    {
        foreach (var item in m_CardPivots)
        {
            Destroy(item.GetChild(0).gameObject);
        }
        foreach (var chipsPivots in m_ChipsPivots)
        {
            Destroy(chipsPivots.GetChild(0).gameObject);
        }
        TriggerObjects(false);
    }

    public void Init(GameStateData gameStateData)
    {
        chipsManagers = new Stack<IChipManager>[gameStateData.chips.Length];
        for (int i = 0; i < chipsManagers.Length; i++)
        {
            chipsManagers[i] = new Stack<IChipManager>(0);
        }
        TriggerObjects(true);
    }

    public void PushChip(int index, IChipManager chipManager)
    {
        chipsManagers[index].Push(chipManager);
    }

    public Stack<IChipManager>[] GiveChips(ChipsColumn[] chips)
    {
        Stack<IChipManager>[] getChipsManagers = new Stack<IChipManager>[chips.Length];
        for (int i = 0; i < getChipsManagers.Length; i++)
        {
            getChipsManagers[i] = new Stack<IChipManager>(0);
        }
        for (int i = 0; i < chips.Length; i++)
        {
            Stack<IChipManager> chipsStack = new Stack<IChipManager>(0);
            int chipsCount = chips[i].count;
            for (int j = 0; j < chipsCount; j++)
            {
                if (chipsManagers[i].Count != 0)
                {
                    chipsStack.Push(chipsManagers[i].Pop());
                }
            }
            getChipsManagers[i] = chipsStack;
        }
        return getChipsManagers;
    }

    private void TriggerObjects(bool isOn)
    {
        foreach (var item in m_ObjectsForHide)
        {
            item.SetActive(isOn);
        }
    }
}